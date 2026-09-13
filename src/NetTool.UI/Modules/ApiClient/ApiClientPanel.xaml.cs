using System.Windows;
using System.Windows.Controls;
using NetTool.UI.Modules.ApiClient.Models;

namespace NetTool.UI.Modules.ApiClient
{
    public partial class ApiClientPanel : UserControl
    {
        public ApiClientPanel()
        {
            InitializeComponent();
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item)
            {
                var text = item.Content?.ToString();
                if (!string.IsNullOrEmpty(text) && !text.Contains("..."))
                {
                    if (DataContext is ApiClientViewModel vm)
                    {
                        vm.LoadPreset(text);
                    }
                }
            }
        }

        private void AddJsonHeader_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ApiClientViewModel vm)
            {
                // Check if already exists
                var existing = vm.Headers.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Value = "application/json";
                    existing.IsEnabled = true;
                }
                else
                {
                    vm.Headers.Add(new HttpHeaderItem("Content-Type", "application/json", true));
                }
            }
        }

        private void AddAcceptHeader_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ApiClientViewModel vm)
            {
                var existing = vm.Headers.FirstOrDefault(h => h.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Value = "application/json";
                    existing.IsEnabled = true;
                }
                else
                {
                    vm.Headers.Add(new HttpHeaderItem("Accept", "application/json", true));
                }
            }
        }
    }
}
