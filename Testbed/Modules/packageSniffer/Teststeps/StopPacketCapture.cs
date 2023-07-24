using OpenTap;
using ProjectCommons.DataTypes;

namespace packageSniffer.Teststeps {
    [Display("Stop PacketCapture", Group:"Packet Capture")]
    public class StopPacketCapture : TestStep {
       private TraceSource log = OpenTap.Log.CreateSource("PacketCapture");

        #region Settings
      
        [Display("Capture Instrument")] public ICaptureNetPacketsInstrument _RemoteSniffer { get; set; }

        #endregion

        public StopPacketCapture() {
        }

        public override void PrePlanRun() {
            base.PrePlanRun();
        }

        public override void Run() {
            Log.Info("Stopping Packet Capture");
            var result = _RemoteSniffer.StopRunningCapture();

            if(result == 0){
                Log.Warning("No Packets Captured");
                UpgradeVerdict(Verdict.Inconclusive);
            } else if (result > 0) {
                Log.Info($"Captured {result} packets");
                UpgradeVerdict(Verdict.Pass);
            } else {
                Log.Error($"Something went wrong while Capturing. Result: [{result}");
                UpgradeVerdict(Verdict.Error);
            }
            
            
            RunChildSteps(); //If step has child steps.
        }

        public override void PostPlanRun() {
            // ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
            base.PostPlanRun();
        }
    }
}