using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Nextended.UI.Input;

namespace Nextended.UI.WPF.Controls
{
    public class KeyBindChangedEventArgs : EventArgs
    {
        public KeyBindChangedEventArgs(StoredInputBinding? oldValue, StoredInputBinding? newValue, bool isUserChange = false)
        {
            OldValue = oldValue;
            NewValue = newValue;
            IsUserChange = isUserChange;
        }

        public StoredInputBinding? OldValue { get; }
        public StoredInputBinding? NewValue { get; }

        /// <summary>True when the user recorded, deleted or re-timed the binding (not a programmatic set).</summary>
        public bool IsUserChange { get; }
    }

    /// <summary>Raised before a user edit (record or delete) is applied; set <see cref="CancelEventArgs.Cancel"/> to reject it.</summary>
    public class KeyBindChangingEventArgs : CancelEventArgs
    {
        public KeyBindChangingEventArgs(StoredInputBinding? oldValue, StoredInputBinding newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public StoredInputBinding? OldValue { get; }
        public StoredInputBinding NewValue { get; }
    }

    /// <summary>
    ///     A single key-bind editor: shows the current <see cref="KeyBind"/> (keyboard, mouse or
    ///     gamepad — singles and chords like Ctrl+X or LT+A), records a new one on click
    ///     (commit-on-release via the <see cref="BindingManager"/>), supports a per-binding
    ///     min-hold-time and context-menu delete. Persistence is the consumer's job — listen to
    ///     <see cref="KeyBindChanged"/> (and <see cref="KeyBindChanging"/> to veto user edits).
    ///     Texts and colors are dependency properties so apps can localize and theme it.
    /// </summary>
    public partial class KeyBindChanger : INotifyPropertyChanged, IDisposable
    {
        private readonly string _autoId = $"keybind_{Guid.NewGuid():N}";
        private bool _subscribed;
        private bool _userChange;
        private string? _registeredId;

        public event EventHandler<KeyBindChangingEventArgs>? KeyBindChanging;
        public event EventHandler<KeyBindChangedEventArgs>? KeyBindChanged;
        public event EventHandler? KeyDeleted;
        /// <summary>Raised when the registered binding is pressed globally (outside record mode).</summary>
        public event EventHandler? BindingPressed;

        public KeyBindChanger()
        {
            InitializeComponent();
            // The context menu lives outside the element tree, so its header can't bind to this control.
            DeleteMenuItem.Header = DeleteBindingText;
        }

        #region Dependency properties

        public static readonly DependencyProperty KeyBindProperty =
            DependencyProperty.Register(nameof(KeyBind), typeof(StoredInputBinding), typeof(KeyBindChanger),
                new PropertyMetadata(null, HandleKeyBindChanged));

        public StoredInputBinding? KeyBind
        {
            get => (StoredInputBinding?)GetValue(KeyBindProperty);
            set => SetValue(KeyBindProperty, value);
        }

        public static readonly DependencyProperty BindingManagerProperty =
            DependencyProperty.Register(nameof(BindingManager), typeof(InputBindingManager), typeof(KeyBindChanger),
                new PropertyMetadata(null));

        public InputBindingManager? BindingManager
        {
            get => (InputBindingManager?)GetValue(BindingManagerProperty);
            set => SetValue(BindingManagerProperty, value);
        }

        public static readonly DependencyProperty KeyConfigNameProperty =
            DependencyProperty.Register(nameof(KeyConfigName), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata(null, (d, _) => ((KeyBindChanger)d).RefreshRegistration()));

        /// <summary>Registration id used with the <see cref="BindingManager"/>. Auto-generated when unset.</summary>
        public string? KeyConfigName
        {
            get => (string?)GetValue(KeyConfigNameProperty);
            set => SetValue(KeyConfigNameProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata(string.Empty, (d, e) =>
                {
                    var title = ((KeyBindChanger)d).TitleLabel;
                    title.Content = e.NewValue;
                    title.Visibility = string.IsNullOrEmpty((string?)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
                }));

        /// <summary>Optional title shown left of the bind button (collapsed while empty).</summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty InvalidTextProperty =
            DependencyProperty.Register(nameof(InvalidText), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("None", (d, _) => ((KeyBindChanger)d).SetContent(((KeyBindChanger)d).KeyBind)));

        /// <summary>Shown on the bind button while no binding is set.</summary>
        public string InvalidText
        {
            get => (string)GetValue(InvalidTextProperty);
            set => SetValue(InvalidTextProperty, value);
        }

        public static readonly DependencyProperty EmptyToolTipProperty =
            DependencyProperty.Register(nameof(EmptyToolTip), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("Click to record a binding", (d, _) => ((KeyBindChanger)d).SetContent(((KeyBindChanger)d).KeyBind)));

        /// <summary>Tooltip while no binding is set.</summary>
        public string EmptyToolTip
        {
            get => (string)GetValue(EmptyToolTipProperty);
            set => SetValue(EmptyToolTipProperty, value);
        }

        public static readonly DependencyProperty BoundToolTipFormatProperty =
            DependencyProperty.Register(nameof(BoundToolTipFormat), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("{0}: {1}", (d, _) => ((KeyBindChanger)d).SetContent(((KeyBindChanger)d).KeyBind)));

        /// <summary>Tooltip while a binding is set; {0} = device name (see <see cref="DeviceNameProvider"/>), {1} = key name.</summary>
        public string BoundToolTipFormat
        {
            get => (string)GetValue(BoundToolTipFormatProperty);
            set => SetValue(BoundToolTipFormatProperty, value);
        }

        public static readonly DependencyProperty RecordingToolTipProperty =
            DependencyProperty.Register(nameof(RecordingToolTip), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("Press the key(s) for the new binding"));

        /// <summary>Tooltip while a new binding is being recorded.</summary>
        public string RecordingToolTip
        {
            get => (string)GetValue(RecordingToolTipProperty);
            set => SetValue(RecordingToolTipProperty, value);
        }

        public static readonly DependencyProperty DeleteBindingTextProperty =
            DependencyProperty.Register(nameof(DeleteBindingText), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("Delete binding", (d, e) => ((KeyBindChanger)d).DeleteMenuItem.Header = e.NewValue));

        /// <summary>Header of the context-menu entry that deletes the binding.</summary>
        public string DeleteBindingText
        {
            get => (string)GetValue(DeleteBindingTextProperty);
            set => SetValue(DeleteBindingTextProperty, value);
        }

        public static readonly DependencyProperty MinTimeTextProperty =
            DependencyProperty.Register(nameof(MinTimeText), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("Min hold time (seconds)"));

        /// <summary>Title of the min-hold-time popup and tooltip of its clock icon.</summary>
        public string MinTimeText
        {
            get => (string)GetValue(MinTimeTextProperty);
            set => SetValue(MinTimeTextProperty, value);
        }

        public static readonly DependencyProperty ButtonBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonBackground), typeof(Brush), typeof(KeyBindChanger),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x22, 0x80, 0x80, 0x80))));

        public Brush ButtonBackground
        {
            get => (Brush)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }

        public static readonly DependencyProperty ButtonBorderBrushProperty =
            DependencyProperty.Register(nameof(ButtonBorderBrush), typeof(Brush), typeof(KeyBindChanger),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x55, 0x80, 0x80, 0x80))));

        public Brush ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }

        public static readonly DependencyProperty PopupBackgroundProperty =
            DependencyProperty.Register(nameof(PopupBackground), typeof(Brush), typeof(KeyBindChanger),
                new PropertyMetadata(SystemColors.ControlBrush));

        /// <summary>Background of the min-hold-time popup; its text uses <see cref="Control.Foreground"/>.</summary>
        public Brush PopupBackground
        {
            get => (Brush)GetValue(PopupBackgroundProperty);
            set => SetValue(PopupBackgroundProperty, value);
        }

        public static readonly DependencyProperty CanRemoveBindingProperty =
            DependencyProperty.Register(nameof(CanRemoveBinding), typeof(bool), typeof(KeyBindChanger),
                new PropertyMetadata(true));

        public bool CanRemoveBinding
        {
            get => (bool)GetValue(CanRemoveBindingProperty);
            set => SetValue(CanRemoveBindingProperty, value);
        }

        public static readonly DependencyProperty CanEditMinTimeProperty =
            DependencyProperty.Register(nameof(CanEditMinTime), typeof(bool), typeof(KeyBindChanger),
                new PropertyMetadata(true, (d, _) => ((KeyBindChanger)d).OnPropertyChanged(nameof(ShowTimeEdit))));

        public bool CanEditMinTime
        {
            get => (bool)GetValue(CanEditMinTimeProperty);
            set => SetValue(CanEditMinTimeProperty, value);
        }

        public static readonly DependencyProperty WithBorderProperty =
            DependencyProperty.Register(nameof(WithBorder), typeof(bool), typeof(KeyBindChanger),
                new PropertyMetadata(true, WithBorderChanged));

        public bool WithBorder
        {
            get => (bool)GetValue(WithBorderProperty);
            set => SetValue(WithBorderProperty, value);
        }

        private static void WithBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var kc = (KeyBindChanger)d;
            kc.MainBorder.BorderThickness = (bool)e.NewValue ? new Thickness(1, 0, 1, 0) : new Thickness(0);
            kc.Background = new SolidColorBrush(Colors.Transparent);
        }

        #endregion

        public bool ShowTimeEdit => KeyBind is { IsValid: true } && CanEditMinTime;
        public bool HasTimeValue => KeyBind is { IsValid: true, MinTime: > 0 };
        public bool HasKeySet { get; private set; }
        public bool InUpdateMode { get; private set; }

        /// <summary>The id this control registers its binding under: <see cref="KeyConfigName"/>, or a generated one.</summary>
        public string BindingId => !string.IsNullOrEmpty(KeyConfigName) ? KeyConfigName! : _autoId;

        private static void HandleKeyBindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (KeyBindChanger)d;
            var newValue = (StoredInputBinding?)e.NewValue;
            var oldValue = (StoredInputBinding?)e.OldValue;
            if (newValue != null && oldValue is { IsValid: true, MinTime: > 0 } && newValue.MinTime <= 0)
                newValue.SetMinTime(oldValue.MinTime);

            self.SetContent(newValue);
            self.SyncRegistration(newValue);
            self.KeyBindChanged?.Invoke(self, new KeyBindChangedEventArgs(oldValue, newValue, self._userChange));
            self.OnPropertyChanged(nameof(ShowTimeEdit));
            self.OnPropertyChanged(nameof(HasTimeValue));
        }

        private void RefreshRegistration() => SyncRegistration(KeyBind);

        private void SyncRegistration(StoredInputBinding? binding)
        {
            if (BindingManager == null) return;
            if (_registeredId != null && _registeredId != BindingId)
                BindingManager.RemoveBinding(_registeredId);
            _registeredId = BindingId;
            if (binding is { IsValid: true })
                BindingManager.RegisterBinding(_registeredId, binding);
            else
                BindingManager.RemoveBinding(_registeredId);
        }

        private void KeyBindChanger_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (BindingManager == null || _subscribed) return;
            _subscribed = true;
            SyncRegistration(KeyBind);
            BindingManager.OnBindingPressed += OnGlobalKeyHandler;
            SetContent(KeyBind);
        }

        private void KeyBindChanger_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe so removed items don't leak; Loaded re-subscribes on reuse.
            Dispose();
        }

        private void OnGlobalKeyHandler(string key)
        {
            if (!HasKeySet || key != BindingId || InUpdateMode) return;
            if (KeyBind?.Device == InputDeviceType.Mouse && (IsMouseOver || BindContextMenu.IsOpen))
                return;
            BindingPressed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Device name shown in the tooltip; defaults to <see cref="StoredInputBinding.DeviceName"/>
        ///     ("Keyboard", "Mouse", "Gamepad", "Combo"). Set it to localize.
        /// </summary>
        public Func<StoredInputBinding, string>? DeviceNameProvider { get; set; }

        private void SetContent(StoredInputBinding? keybind)
        {
            HasKeySet = keybind is { IsValid: true };
            SetDeviceIcon();
            if (!HasKeySet)
            {
                KeyLabel.Content = InvalidText;
                ToolTip = EmptyToolTip;
                return;
            }

            // Combos join the per-component names; never feed the synthetic joined Key to
            // KeyDisplayName.For (it parses a single key and would fail on "Ctrl+X").
            var keyName = keybind!.IsCombo
                ? string.Join(" + ", keybind.Components!.ConvertAll(c => KeyDisplayName.For(c.Key)))
                : KeyDisplayName.For(keybind.Key);
            SetDeviceIcon(GetDeviceGlyph(keybind));
            ToolTip = string.Format(BoundToolTipFormat, DeviceNameProvider?.Invoke(keybind) ?? keybind.DeviceName, keyName);
            KeyLabel.Content = keyName;
        }

        // Segoe Fluent Icons glyphs: gamepad, mouse, keyboard, record-dot.
        private const string GamepadGlyph = "";
        private const string MouseGlyph = "";
        private const string KeyboardGlyph = "";
        private const string RecordGlyph = "";

        private static string GetDeviceGlyph(StoredInputBinding keybind)
        {
            if (keybind.IsCombo)
            {
                if (keybind.Components!.Exists(c => c.Device == InputDeviceType.Gamepad)) return GamepadGlyph;
                if (keybind.Components!.Exists(c => c.Device == InputDeviceType.Mouse)) return MouseGlyph;
                return KeyboardGlyph;
            }
            return keybind.Device switch
            {
                InputDeviceType.Gamepad => GamepadGlyph,
                InputDeviceType.Mouse => MouseGlyph,
                _ => KeyboardGlyph,
            };
        }

        private void SetDeviceIcon(string icon = "", Brush? fg = null)
        {
            DeviceIcon.Text = icon;
            DeviceIcon.FontSize = icon == MouseGlyph ? 14 : 18;
            if (fg != null)
            {
                DeviceIcon.Foreground = fg;
                DeviceIcon.Opacity = 1;
            }
            else
            {
                DeviceIcon.ClearValue(TextBlock.ForegroundProperty);
                DeviceIcon.Opacity = 0.7;
            }
            DeviceIcon.Visibility = string.IsNullOrEmpty(icon) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BindButton_OnClick(object sender, MouseButtonEventArgs e)
        {
            RecordNewBinding();
        }

        /// <summary>Records the next input(s) from the <see cref="BindingManager"/> as the new binding.</summary>
        public void RecordNewBinding()
        {
            if (InUpdateMode || BindingManager == null)
                return;
            InUpdateMode = true;
            BindContextMenu.IsOpen = false;
            OnPropertyChanged(nameof(ShowTimeEdit));
            MainGrid.ContextMenu = null;
            SetDeviceIcon(RecordGlyph, Brushes.Red);
            KeyLabel.Content = "...";
            ToolTip = RecordingToolTip;
            BindingManager.StartListeningForBinding(BindingId);

            Action<string, StoredInputBinding>? bindingSetHandler = null;
            bindingSetHandler = (bindingId, key) =>
            {
                if (bindingId != BindingId) return;
                BindingManager.OnBindingSet -= bindingSetHandler;
                var applied = Dispatcher.Invoke(() =>
                {
                    if (!ApplyUserChange(key))
                    {
                        // Rejected: drop the manager's copy of the recorded binding again.
                        SyncRegistration(KeyBind);
                        SetContent(KeyBind);
                        return false;
                    }
                    return true;
                });
                // Keep record mode briefly after a commit so the releasing key doesn't fire the new binding.
                Task.Delay(applied ? 700 : 10).ContinueWith(_ => Dispatcher.BeginInvoke(() =>
                {
                    InUpdateMode = false;
                    OnPropertyChanged(nameof(ShowTimeEdit));
                    MainGrid.ContextMenu = BindContextMenu;
                }));
            };

            BindingManager.OnBindingSet += bindingSetHandler;
        }

        /// <summary>Clears the binding as if the user chose "delete" (subject to <see cref="KeyBindChanging"/>).</summary>
        public void DeleteBinding()
        {
            if (ApplyUserChange(StoredInputBinding.Empty))
                KeyDeleted?.Invoke(this, EventArgs.Empty);
        }

        private bool ApplyUserChange(StoredInputBinding newValue)
        {
            var args = new KeyBindChangingEventArgs(KeyBind, newValue);
            KeyBindChanging?.Invoke(this, args);
            if (args.Cancel)
                return false;

            _userChange = true;
            try { KeyBind = newValue; }
            finally { _userChange = false; }
            return true;
        }

        private void DeleteBinding_Click(object sender, RoutedEventArgs e) => DeleteBinding();

        private void ContextMenu_OnOpened(object sender, MouseButtonEventArgs e)
        {
            if (!CanRemoveBinding || InUpdateMode || KeyBind is not { IsValid: true })
            {
                e.Handled = true;
                BindContextMenu.IsOpen = false;
            }
        }

        private void ConfigureMinTimeLabel_OnClick(object sender, MouseButtonEventArgs e)
        {
            MinTimeSlider.Value = KeyBind?.MinTime ?? 0;
            TimeSliderPopup.IsOpen = true;
        }

        private void MinTimeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MinTimeValueText.Text = e.NewValue.ToString("0.0");
            if (KeyBind is not { IsValid: true } bind || Math.Abs(bind.MinTime - e.NewValue) < 0.001)
                return;
            bind.MinTime = e.NewValue;
            SyncRegistration(bind);
            KeyBindChanged?.Invoke(this, new KeyBindChangedEventArgs(bind, bind, isUserChange: true));
            OnPropertyChanged(nameof(HasTimeValue));
        }

        public void Dispose()
        {
            if (BindingManager != null && _subscribed)
            {
                BindingManager.OnBindingPressed -= OnGlobalKeyHandler;
                _subscribed = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
