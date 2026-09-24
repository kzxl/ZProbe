using System.Collections.ObjectModel;
using ZProbe.UI.Core;
using ZProbe.UI.Modules.ApiClient;
using ZProbe.UI.Modules.DnsLookup;
using ZProbe.UI.Modules.GeoIP;
using ZProbe.UI.Modules.HttpHeaders;
using ZProbe.UI.Modules.IPScan;
using ZProbe.UI.Modules.LoadTest;
using ZProbe.UI.Modules.Ping;
using ZProbe.UI.Modules.PortScan;
using ZProbe.UI.Modules.SSLChecker;
using ZProbe.UI.Modules.Traceroute;
using ZProbe.UI.Modules.WebSocket;
using ZProbe.UI.Modules.Whois;

namespace ZProbe.UI.ViewModels
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
