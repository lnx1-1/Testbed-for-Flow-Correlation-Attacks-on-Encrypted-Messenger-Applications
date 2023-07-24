using System.IO;
using OpenTap;
using packageSniffer.RemoteSniffer;
using ProjectCommons;

namespace packageSniffer.Teststeps {
    [Display("Setup Remote Gateway and Sniffing Device")]
    public class setupRemoteSniffer : TestStep {
        #region Settings

        [Display("Setup Script File")]
        [FilePath]
        public string scriptPath { get; set; }

        public RemoteSnifferInstrument _RemoteSniffer { get; set; }

        #endregion

        public setupRemoteSniffer() {
            scriptPath = Path.Combine(Utilitys.GetRootDirectory(), "Scripts", "setupRaspiSniffer.bash");
        }

        public override void PrePlanRun() {
            base.PrePlanRun();
        }

        public override void Run() {
            string scriptName = "setupScript.bash";
            _RemoteSniffer.CopyFileToRemote(scriptPath, scriptName);
            _RemoteSniffer.ExecCmd("dos2unix " + scriptName, TapThread.Current.AbortToken, userName: _RemoteSniffer.ConfigUsername);
            _RemoteSniffer.ExecCmd("chmod +x " + scriptName, TapThread.Current.AbortToken, s => Log.Debug(s), userName: _RemoteSniffer.ConfigUsername);
            _RemoteSniffer.ExecCmd($"sudo ./{scriptName} {_RemoteSniffer.CaptureUsrPassword} -N 1>&2", TapThread.Current.AbortToken, s => Log.Info(s), userName: _RemoteSniffer.ConfigUsername);
            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun() {
            // ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
            base.PostPlanRun();
        }
    }
}