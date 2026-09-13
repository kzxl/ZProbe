using System.Windows;
using ZeroProbe.UI.Core;

namespace ZeroProbe.UI.Modules.ApiClient
{
    public class ApiClientTool : ITool
    {
        private ApiClientViewModel? _viewModel;

        public string Name => "API Client";
        public string Icon => "⚡";
        public string Description => "Interactive REST API tester with headers, params, body & auth";
        public string Group => "Web";
        public int Order => 25;

        public ToolViewModelBase ViewModel => _viewModel ??= new ApiClientViewModel();

        public FrameworkElement CreateView()
        {
            var view = new ApiClientPanel();
            view.DataContext = ViewModel;
            return view;
        }
    }
}
