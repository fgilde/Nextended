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
        public KeyBindChangedEventArgs(StoredInputBinding? oldValue, StoredInputBinding? newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public StoredInputBinding? OldValue { get; }
        public StoredInputBinding? NewValue { get; }
    }

    /// <summary>
    ///     A single key-bind editor: shows the current <see cref="KeyBind"/> (keyboard, mouse or
    ///     gamepad — singles and chords like Ctrl+X or LT+A), records a new one on click
    ///     (commit-on-release via the <see cref="BindingManager"/>), supports a per-binding
    ///     min-hold-time and context-menu delete. Persistence is the consumer's job — listen to
    ///     <see cref="KeyBindChanged"/>.
    /// </summary>
    public partial class KeyBindChanger : INotifyPropertyChanged, IDisposable
    {
        private readonly string _autoId = $"keybind_{Guid.NewGuid():N}";
        private bool _subscribed;

        public event EventHandler<KeyBindChangedEventArgs>? KeyBindChanged;
        public event EventHandler? KeyDeleted;
        /// <summary>Raised when the registered binding is pressed globally (outside record mode).</summary>
        public event EventHandler? BindingPressed;

        public KeyBindChanger()
        {
            InitializeComponent();
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
                new PropertyMetadata(null));

        /// <summary>Registration id used with the <see cref="BindingManager"/>. Auto-generated when unset.</summary>
        public string? KeyConfigName
        {
            get => (string?)GetValue(KeyConfigNameProperty);
            set => SetValue(KeyConfigNameProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata(string.Empty, (d, e) => ((KeyBindChanger)d).TitleLabel.Content = e.NewValue));

        /// <summary>Optional title shown left of the bind button.</summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty InvalidTextProperty =
            DependencyProperty.Register(nameof(InvalidText), typeof(string), typeof(KeyBindChanger),
                new PropertyMetadata("None"));

        /// <summary>Shown on the bind button while no binding is set.</summary>
        public string InvalidText
        {
            get => (string)GetValue(InvalidTextProperty);
            set => SetValue(InvalidTextProperty, value);
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

        private string EffectiveId => !string.IsNullOrEmpty(KeyConfigName) ? KeyConfigName! : _autoId;

        private static void HandleKeyBindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (KeyBindChanger)d;
            var newValue = (StoredInputBinding?)e.NewValue;
            var oldValue = (StoredInputBinding?)e.OldValue;
            if (newValue != null && oldValue is { IsValid: true, MinTime: > 0 } && newValue.MinTime <= 0)
                newValue.SetMinTime(oldValue.MinTime);

            self.SetContent(newValue);
            self.SyncRegistration(newValue);
            self.KeyBindChanged?.Invoke(self, new KeyBindChangedEventArgs(oldValue, newValue));
            self.OnPropertyChanged(nameof(ShowTimeEdit));
            self.OnPropertyChanged(nameof(HasTimeValue));
        }

        private void SyncRegistration(StoredInputBinding? binding)
        {
            if (BindingManager == null) return;
            if (binding is { IsValid: true })
                BindingManager.RegisterBinding(EffectiveId, binding);
            else
                BindingManager.RemoveBinding(EffectiveId);
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
            if (!HasKeySet || key != EffectiveId || InUpdateMode) return;
            if (KeyBind?.Device == InputDeviceType.Mouse && (IsMouseOver || BindContextMenu.IsOpen))
                return;
            BindingPressed?.Invoke(this, EventArgs.Empty);
        }

        private void SetContent(StoredInputBinding? keybind)
        {
            HasKeySet = keybind is { IsValid: true };
            SetDeviceIcon();
            if (!HasKeySet)
            {
                KeyLabel.Content = InvalidText;
                ToolTip = "Click to record a binding";
                return;
            }

            if (keybind!.IsCombo)
            {
                // Chord: pick a device glyph from the components (gamepad > mouse > keyboard) and
                // join the per-component names. Never feed the synthetic joined Key to
                // KeyDisplayName.For (it parses a single key and would fail on "Ctrl+X").
                SetDeviceIcon(GetDeviceGlyph(keybind));
                var comboName = string.Join(" + ", keybind.Components!.ConvertAll(c => KeyDisplayName.For(c.Key)));
                ToolTip = comboName;
                KeyLabel.Content = comboName;
                return;
            }

            SetDeviceIcon(GetDeviceGlyph(keybind));
            var keyName = KeyDisplayName.For(keybind.Key);
            ToolTip = $"{keybind.DeviceName}: {keyName}";
            KeyLabel.Content = keyName;
        }

        // Segoe Fluent Icons glyphs: gamepad, mouse, keyboard, record-dot.
        private const string GamepadGlyph = "\uE7FC";
        private const string MouseGlyph = "\uF8AF";
        private const string KeyboardGlyph = "\uE765";
        private const string RecordGlyph = "\uEA3B";

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
            ToolTip = "Press the key(s) for the new binding";
            BindingManager.StartListeningForBinding(EffectiveId);

            Action<string, StoredInputBinding>? bindingSetHandler = null;
            bindingSetHandler = (bindingId, key) =>
            {
                if (bindingId != EffectiveId) return;
                BindingManager.OnBindingSet -= bindingSetHandler;
                Dispatcher.Invoke(() => KeyBind = key);
                Task.Delay(700).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    InUpdateMode = false;
                    OnPropertyChanged(nameof(ShowTimeEdit));
                    MainGrid.ContextMenu = BindContextMenu;
                }));
            };

            BindingManager.OnBindingSet += bindingSetHandler;
        }

        private void DeleteBinding_Click(object sender, RoutedEventArgs e)
        {
            BindingManager?.RemoveBinding(EffectiveId);
            KeyBind = StoredInputBinding.Empty;
            KeyDeleted?.Invoke(this, EventArgs.Empty);
        }

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
            KeyBindChanged?.Invoke(this, new KeyBindChangedEventArgs(bind, bind));
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
