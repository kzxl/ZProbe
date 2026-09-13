using System.Windows;
using ZeroProbe.UI.Core;
namespace ZeroProbe.UI.Modules.SSLChecker
{
    public class SSLTool : ITool
    {
        private SSLViewModel? _vm;
        public string Name => "SSL Checker";
        public string Icon => "🔐";
        public string Description => "SSL/TLS certificate inspection";
        public string Group => "Web";
        public int Order => 52;
        public ToolViewModelBase ViewModel => _vm ??= new SSLViewModel();
        public FrameworkElement CreateView() { var v = new SSLPanel(); v.DataContext = ViewModel; return v; }
    }
}
