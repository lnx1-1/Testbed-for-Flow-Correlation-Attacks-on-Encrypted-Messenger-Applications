using Event_Based_Impl.Utility_tools;
using IMSniff_Testbed_OpenTAP;
using OpenTap;

namespace AlgorithmAdapter {
    public class AlgorithmSettings : ComponentSettings<GeneralSettings>{
        [Display("Interpacket Delay Threshold", "The Interpacket Delay Threshold -> Seite 10 unten im Paper erklärt")]
        public double T_e {
            get => Config.T_e;
            set => Config.T_e = value;
        }
    }
}