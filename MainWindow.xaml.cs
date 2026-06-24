using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace POE_Part3
{
    // ── Bool → Visibility converter ───────────────────────────────────────────
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ── MainWindow ────────────────────────────────────────────────────────────
    public partial class MainWindow : Window
    {
        private readonly ChatViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ChatViewModel();
            DataContext = _vm;

            // Auto-scroll when messages are added
            _vm.Messages.CollectionChanged += (_, _) =>
            {
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => ChatScrollViewer.ScrollToBottom()));
            };

            // Focus the input box on load
            Loaded += (_, _) => InputTextBox.Focus();
        }

        // ── Send on Enter key ─────────────────────────────────────────────────
        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyboardDevice.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                await _vm.SendMessageAsync();
                InputTextBox.Focus();
            }
        }

        // ── Send button ───────────────────────────────────────────────────────
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await _vm.SendMessageAsync();
            InputTextBox.Focus();
        }

        // ── Quick topic chips ─────────────────────────────────────────────────
        private async void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                string label = btn.Content?.ToString() ?? "";
                string keyword = label.Contains(' ')
                    ? label.Substring(label.IndexOf(' ') + 1).Trim()
                    : label;

                _vm.InputText = keyword;
                await _vm.SendMessageAsync();
                InputTextBox.Focus();
            }
        }

        // ── Borderless window drag ────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // ── Window controls ───────────────────────────────────────────────────
        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}