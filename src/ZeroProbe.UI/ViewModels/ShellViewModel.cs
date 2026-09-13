using System.Collections.ObjectModel;
using ZeroProbe.UI.Core;
using ZeroProbe.UI.Modules.ApiClient;
using ZeroProbe.UI.Modules.DnsLookup;
using ZeroProbe.UI.Modules.GeoIP;
using ZeroProbe.UI.Modules.HttpHeaders;
using ZeroProbe.UI.Modules.IPScan;
using ZeroProbe.UI.Modules.LoadTest;
using ZeroProbe.UI.Modules.Ping;
using ZeroProbe.UI.Modules.PortScan;
using ZeroProbe.UI.Modules.SSLChecker;
using ZeroProbe.UI.Modules.Traceroute;
using ZeroProbe.UI.Modules.WebSocket;
using ZeroProbe.UI.Modules.Whois;

namespace ZeroProbe.UI.ViewModels
{
    /// <summary>
    /// Shell ViewModel — orchestrate tools từ ToolRegistry.
    /// </summary>
    public class ShellViewModel : ViewModelBase
    {
        private ITool? _selectedTool;

        public ShellViewModel()
        {
            // ── Web ─────────────────────────────────
            ToolRegistry.Register(new ApiClientTool());
            ToolRegistry.Register(new LoadTestTool());
            ToolRegistry.Register(new HttpHeadersTool());
            ToolRegistry.Register(new SSLTool());
            ToolRegistry.Register(new WebSocketTool());

            // ── Network ─────────────────────────────
            ToolRegistry.Register(new PingTool());
            ToolRegistry.Register(new TracerouteTool());

            // ── Discovery ───────────────────────────
            ToolRegistry.Register(new DnsTool());
            ToolRegistry.Register(new PortScanTool());
            ToolRegistry.Register(new IPScanTool());
            ToolRegistry.Register(new GeoIPTool());
            ToolRegistry.Register(new WhoisTool());

            Tools = new ObservableCollection<ITool>(ToolRegistry.Tools);

            if (Tools.Count > 0)
                SelectedTool = Tools[0];
        }

        public ObservableCollection<ITool> Tools { get; }

        public ITool? SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }

        public string StatusText => SelectedTool?.ViewModel.StatusText ?? "Ready";
    }
}
