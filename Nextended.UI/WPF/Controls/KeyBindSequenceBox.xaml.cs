using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Nextended.UI.Input;

namespace Nextended.UI.WPF.Controls
{
    /// <summary>
    ///     Editor for an ORDERED sequence of key binds (each slot may be a single input or a chord):
    ///     a wrap-list of <see cref="KeyBindChanger"/>s plus a trailing "+" placeholder to append,
    ///     and an optional "record sequence" mode that captures presses in order (stamping the gap
    ///     to the previous press into each binding's MinTime). Use with
    ///     <see cref="InputSequenceMatcher"/> to react to the sequence at runtime.
    /// </summary>
    public partial class KeyBindSequenceBox : INotifyPropertyChanged
    {
        public event EventHandler<IReadOnlyList<StoredInputBinding>>? Changed;

        public KeyBindSequenceBox()
        {
            InitializeComponent();
            if (Keys == null)
                Keys = new ObservableCollection<StoredInputBinding>();
            Normalize();
        }

        #region Dependency properties

        public static readonly DependencyProperty KeysProperty =
            DependencyProperty.Register(nameof(Keys), typeof(ObservableCollection<StoredInputBinding>), typeof(KeyBindSequenceBox),
                new PropertyMetadata(null, (d, _) => ((KeyBindSequenceBox)d).Normalize()));

        public ObservableCollection<StoredInputBinding> Keys
        {
            get => (ObservableCollection<StoredInputBinding>)GetValue(KeysProperty);
            set => SetValue(KeysProperty, value);
        }

        public static readonly DependencyProperty BindingManagerProperty =
            DependencyProperty.Register(nameof(BindingManager), typeof(InputBindingManager), typeof(KeyBindSequenceBox),
                new PropertyMetadata(null));

        public InputBindingManager? BindingManager
        {
            get => (InputBindingManager?)GetValue(BindingManagerProperty);
            set => SetValue(BindingManagerProperty, value);
        }

        public static readonly DependencyProperty AllowDuplicatesProperty =
            DependencyProperty.Register(nameof(AllowDuplicates), typeof(bool), typeof(KeyBindSequenceBox),
                new PropertyMetadata(true));

        /// <summary>Sequences repeat steps by nature (X, X), so duplicates default to allowed.</summary>
        public bool AllowDuplicates
        {
            get => (bool)GetValue(AllowDuplicatesProperty);
            set => SetValue(AllowDuplicatesProperty, value);
        }

        public static readonly DependencyProperty CanRecordSequenceProperty =
            DependencyProperty.Register(nameof(CanRecordSequence), typeof(bool), typeof(KeyBindSequenceBox),
                new PropertyMetadata(true, (d, e) => ((KeyBindSequenceBox)d).RecordButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public bool CanRecordSequence
        {
            get => (bool)GetValue(CanRecordSequenceProperty);
            set => SetValue(CanRecordSequenceProperty, value);
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(KeyBindSequenceBox),
                new PropertyMetadata(string.Empty, (d, _) => ((KeyBindSequenceBox)d).OnPropertyChanged(nameof(HasError))));

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public static readonly DependencyProperty RecordButtonTextProperty =
            DependencyProperty.Register(nameof(RecordButtonText), typeof(string), typeof(KeyBindSequenceBox),
                new PropertyMetadata("Record sequence", (d, e) => ((KeyBindSequenceBox)d).UpdateRecordButton()));

        public string RecordButtonText
        {
            get => (string)GetValue(RecordButtonTextProperty);
            set => SetValue(RecordButtonTextProperty, value);
        }

        public static readonly DependencyProperty StopRecordButtonTextProperty =
            DependencyProperty.Register(nameof(StopRecordButtonText), typeof(string), typeof(KeyBindSequenceBox),
                new PropertyMetadata("Stop recording", (d, e) => ((KeyBindSequenceBox)d).UpdateRecordButton()));

        public string StopRecordButtonText
        {
            get => (string)GetValue(StopRecordButtonTextProperty);
            set => SetValue(StopRecordButtonTextProperty, value);
        }

        #endregion

        /// <summary>The current sequence WITHOUT the trailing placeholder slot.</summary>
        public IReadOnlyList<StoredInputBinding> ValidKeys
            => Keys?.Where(k => k is { IsValid: true }).ToList() ?? (IReadOnlyList<StoredInputBinding>)Array.Empty<StoredInputBinding>();

        private bool _internalChange;

        /// <summary>Drop invalid entries, keep exactly one trailing "+" placeholder, raise Changed.</summary>
        private void Normalize()
        {
            if (_internalChange || Keys == null) return;
            _internalChange = true;
            try
            {
                var keys = Keys;
                foreach (var invalid in keys.Where(k => k is not { IsValid: true }).ToList())
                    keys.Remove(invalid);
                keys.Add(new StoredInputBinding());
                Changed?.Invoke(this, ValidKeys);
            }
            finally
            {
                _internalChange = false;
            }
        }

        private void Child_OnKeyBindChanged(object? sender, KeyBindChangedEventArgs e)
        {
            if (_internalChange || sender is not KeyBindChanger changer) return;
            var newBinding = e.NewValue;
            if (newBinding is not { IsValid: true }) return;

            ErrorMessage = string.Empty;
            if (!AllowDuplicates && Keys.Any(b => b?.Equals(newBinding) == true))
            {
                // Revert the changer to its previous value.
                _internalChange = true;
                changer.KeyBind = e.OldValue ?? StoredInputBinding.Empty;
                _internalChange = false;
                ErrorMessage = $"'{newBinding.Key}' is already in the sequence";
                return;
            }

            if (changer.Tag is StoredInputBinding { IsValid: true } existing && Keys.Contains(existing))
                Keys[Keys.IndexOf(existing)] = newBinding;
            else
                Keys.Insert(Math.Max(0, Keys.Count - 1), newBinding); // before the trailing placeholder

            Normalize();
        }

        private void Remove_Key_Click(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is StoredInputBinding binding)
            {
                Keys.Remove(binding);
                Normalize();
            }
        }

        #region Sequence recording

        private bool _recording;
        private readonly List<StoredInputBinding> _pressed = new();
        private Stopwatch? _timeStopwatch;

        private void RecordButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (BindingManager == null) return;
            _recording = !_recording;
            UpdateRecordButton();
            SetRecordSequence(_recording);
        }

        private void UpdateRecordButton()
        {
            RecordButton.Content = _recording ? StopRecordButtonText : RecordButtonText;
            if (_recording)
                RecordButton.Foreground = Brushes.Red;
            else
                RecordButton.ClearValue(ForegroundProperty);
        }

        private void SetRecordSequence(bool record)
        {
            if (BindingManager == null) return;
            if (record)
            {
                BindingManager.EnsureHookEvents();
                BindingManager.OnKeyPressed += BindingManager_OnKeyPressed;
                BindingManager.OnKeyReleased += BindingManager_OnKeyReleased;
            }
            else
            {
                _timeStopwatch?.Stop();
                _timeStopwatch = null;
                _pressed.Clear();
                BindingManager.OnKeyPressed -= BindingManager_OnKeyPressed;
                BindingManager.OnKeyReleased -= BindingManager_OnKeyReleased;
            }
        }

        private void BindingManager_OnKeyPressed(StoredInputBinding obj)
        {
            // Ignore the click on the record button itself.
            if (RecordButton.IsMouseOver && obj.Device == InputDeviceType.Mouse)
                return;
            if (!_pressed.Contains(obj))
            {
                // Stamp the gap since the previous press — timing metadata for consumers.
                obj.MinTime = _timeStopwatch?.Elapsed.TotalSeconds ?? 0;
                _timeStopwatch?.Stop();
                _timeStopwatch = Stopwatch.StartNew();
                _pressed.Add(obj);
            }
        }

        private void BindingManager_OnKeyReleased(StoredInputBinding obj)
        {
            var idx = _pressed.FindIndex(p => p.Equals(obj));
            if (idx < 0) return;
            var key = _pressed[idx];
            _pressed.RemoveAt(idx);
            Dispatcher.Invoke(() =>
            {
                Keys.Insert(Math.Max(0, Keys.Count - 1), key);
                Normalize();
            });
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
