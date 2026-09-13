using System.Windows;

namespace ZeroProbe.UI.Modules.ApiClient
{
    public partial class CurlImportDialog : Window
    {
        public string CurlCommand { get; private set; } = string.Empty;

        public CurlImportDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var clip = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(clip) && clip.TrimStart().StartsWith("curl", System.StringComparison.OrdinalIgnoreCase))
                {
                    CurlTextBox.Text = clip.Trim();
                    CurlTextBox.SelectAll();
                }
            }
            catch { }
            CurlTextBox.Focus();
        }

        private void PasteClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    CurlTextBox.Text = text.Trim();
                }
            }
            catch { }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            CurlCommand = CurlTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(CurlCommand))
            {
                MessageBox.Show("Vui lòng nhập hoặc dán câu lệnh cURL!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
