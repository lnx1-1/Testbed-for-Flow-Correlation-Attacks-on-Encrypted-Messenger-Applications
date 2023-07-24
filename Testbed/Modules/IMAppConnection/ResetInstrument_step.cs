using OpenTap;
using ProjectCommons.DataTypes;

namespace IMAppConnection {
    public class ResetInstrument_step: TestStep {
        
        [Display("IMApp Instrument", "The IMApp to use", Group: "Instrument setup", Collapsed: true)]
        public IOpenTapIMApp IMApp { get; set; }
        
        private const int waitTimeout = 10;
        
        public override void Run() {
            if (IMApp.IsConnected) {
                IMApp.Close();
            }

            var currTimeout = waitTimeout;
            Log.Info("Waiting for instrument Close");
            do {
                TapThread.Sleep(500);
                if (currTimeout <= 0) {
                    UpgradeVerdict(Verdict.Error);
                    return;
                }
                currTimeout--;
            } while (IMApp.IsConnected);
            
            Log.Info("Instrument closed!");
            TapThread.Sleep(500);
            Log.Info("Trying to openInstrument");
            IMApp.Open();
            
            do {
                TapThread.Sleep(500);
                if (currTimeout <= 0) {
                    UpgradeVerdict(Verdict.Error);
                    return;
                }
                currTimeout--;
            } while (!IMApp.IsConnected);
            Log.Info("Instrument openend!!");
            UpgradeVerdict(Verdict.Pass);
        }
    }
}