using OpenTap;
using ProjectCommons.DataTypes;

namespace packageSniffer.Teststeps {
    [Display("Start PacketCapture", Group:"Packet Capture")]
    public class StartPacketCapture : TestStep {
        private const string TelegramServerFilter =
            "net 91.105.192.0/23 or net 91.108.4.0/22 or net 91.108.8.0/22 or net 91.108.12.0/22 or net 91.108.16.0/22 or net 91.108.20.0/22 or net 91.108.56.0/22 or net 149.154.160.0/20 or net 185.76.151.0/24";


        private TraceSource log = OpenTap.Log.CreateSource("PacketCapture");

        #region Settings

        [Display("Capture Interface",
            "Capture Interface of Remote device e.g. br0, eth0, wlan0.\nbr0 would be both interfaces",
            Group: "Capture Settings")]
        public string _iface { get; set; } = "br0";

        [Display("Capture Filter",
            Description:
            "Specify a Capture Filter for TCPDump. For Syntax see here: https://www.tcpdump.org/manpages/pcap-filter.7.html",
            Group: "Capture Settings")]
        public string _CaptureFilter { get; set; } = TelegramServerFilter;


        [Display("Capture Instrument")] public ICaptureNetPacketsInstrument _RemoteSniffer { get; set; }

        #endregion

        public StartPacketCapture() {
        }

        public override void PrePlanRun() {
            base.PrePlanRun();
        }

        public override void Run() {
            Log.Info("Starting Packet Capture");
            _RemoteSniffer.StartCapture(_iface,_CaptureFilter,(t) => log.Info(t));

            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun() {
            // ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
            base.PostPlanRun();
        }
    }
}