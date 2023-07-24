using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenTap;
using ProjectCommons.DataTypes;

namespace packageSniffer.RemoteSniffer {
    [Display("Remote Sniffer")]
    public class RemoteSnifferInstrument : Instrument, ICaptureNetPacketsInstrument {
        #region Settings

        [Display("IP Addr", "IP Addr of remote Sniffing Device to connect via SSH", Group: "General")]
        public string IpAddr { get; set; }

        [Display("Remote Capture Folder", Group: "General")]
        public string RemoteFolderPath { get; set; } = "~/capture";

        internal string RemoteFileName { get; set; } = "trace.pcap";

        [Display("Capture Username", Group: "General", Order: 1)]
        public string CaptureUsername { get; set; }

        [Display("Config User", Group: "General", Order: 1)]
        public string ConfigUsername { get; set; }

        [Display("Capture user Password", Group: "General", Order: 2)]
        [Browsable(true)]
        //[XmlIgnore]
        public string CaptureUsrPassword { get; set; }

        [Display("Config File Path", Group: "Config", Order: 1)]
        [FilePath]
        public string ConfigFile { get; set; } = "../../config/RemoteSnifferConfig.json";

        #endregion

        private Task<string> _captureTask;
        private CancellationTokenSource _captureTaskCancel;

        private RemoteSnifferLogic.CmdAsyncState _activeAsyncCapture;

        [Display("Load Settings", "Load Settings from Settings File", Group: "Config", Order: 2)]
        [Browsable(true)]
        public void LoadSettings() {
            if (!File.Exists(ConfigFile.Trim())) {
                Log.Warning("No such config file. Check Path..");
                return;
            }

            CaptureDeviceSettings settings = CaptureDeviceSettings.LoadFromFile(ConfigFile);
            IpAddr = settings.IPAddress;
            RemoteFolderPath = settings.RemoteFilePath;
            CaptureUsrPassword = settings.CaptureUsrPassword;
            CaptureUsername = settings.CaptureUsername;
            ConfigUsername = settings.configUsr;
        }

        [Display("Store Settings", "Store Settings to Settings File", Group: "Config", Order: 3)]
        [Browsable(true)]
        public void SaveCurrentSettings() {
            CaptureDeviceSettings settings = new CaptureDeviceSettings() {
                CaptureUsername = this.CaptureUsername, CaptureUsrPassword = CaptureUsrPassword, IPAddress = IpAddr,
                RemoteFilePath = RemoteFolderPath,
                configUsr = ConfigUsername
            };
            settings.StoreSettingsInJsonFile(ConfigFile);
            Log.Info($"Wrote Settings to file: {Path.GetFullPath(ConfigFile)}");
        }

        [Browsable(true)]
        public void PrintTcpDumpPiDs() {
            if (this.IsConnected) {
                var pid = GetTcPdumpPiDs();
                Log.Warning($"PID output: [{pid}]");
            } else {
                Log.Warning("Device not Ready");
            }
        }

        public string GetTcPdumpPiDs() {
            return ExecCmd("pgrep tcpdump", TapThread.Current.AbortToken, (t) => Log.Info("PS output: " + t));
        }

        public bool TcpdumpActive() {
            return IsConnected && GetTcPdumpPiDs().Trim().Length > 0;
        }

        public RemoteSnifferInstrument() {
            Name = "Remote Sniffer";
            LoadSettings();
        }

        public void CopyFileToRemote(string localFile, string remoteFile) {
            RemoteSnifferLogic.CopyFileToRemote(localFile, remoteFile, IpAddr, CaptureUsername, CaptureUsrPassword);
        }

        public bool IsReady() {
            return RemoteSnifferLogic.IsReachable(IpAddr);
        }

        public string ExecCmd(string cmd, CancellationToken cancellationToken, Action<string> logAction = null, string userName = null) {
            return RemoteSnifferLogic.RunSSHcmd(this, cmd, cancellationToken, logAction, userName);
        }

        private RemoteSnifferLogic.CmdAsyncState ExecCmdAsync(string cmdString, CancellationToken currentAbortToken, Action<string> logAction, string userName = null) {
            return RemoteSnifferLogic.RunSshCmdBackground(this, cmdString, currentAbortToken, logAction, userName);
        }

        public string StoreCapture(string localFilePath) {
            var testPath = @"/home/captureUsr/capture/trace.pcap";
            var setupPath = RemoteFolderPath + "/" + RemoteFileName;
            return RemoteSnifferLogic.CopyFileToLocal(testPath, localFilePath, IpAddr,
                CaptureUsername,
                CaptureUsrPassword);
        }


        public void Cleanup() {
            var result = ExecCmd($"rm {RemoteFolderPath}/*", TapThread.Current.AbortToken);
            if (result.EndsWith("No such file or directory")) {
                Log.Debug("nothing to clean");
            } else if (result.Length == 0) {
                Log.Debug("Cleaned remote folder");
            } else {
                Log.Warning($"Something went during remote Cleanup: [{result}]");
            }
        }

        public void StartCapture(string ifaceName, string captureFilter, Action<string> logAction = null) {
            string remoteFilePath = RemoteFolderPath + "/" + RemoteFileName;
            string cmdString = $"tcpdump -i {ifaceName} -w {remoteFilePath} {captureFilter}"; //! hatte kein Sudo hier vorher

            _activeAsyncCapture = ExecCmdAsync(cmdString, TapThread.Current.AbortToken, logAction);
            Log.Debug("Started Capture Task.. Waiting 1s");

            TapThread.Sleep(1000);
            if (!_activeAsyncCapture.isActive()) {
                var debugOutput = _activeAsyncCapture.getOutput();
                if (debugOutput.Contains("SIOCGIFHWADDR")) {
                    Log.Error($"Probably Wrong Capture Interface Specified: [{ifaceName}]");
                } else {
                    Log.Error($"Something went wrong during Capture Start.. \n{debugOutput}");
                }
            } else {
                Log.Debug("Async capture is Running Sucessfull");
            }
        }


        /// <summary>
        /// Starts the Capture ands waits 1s to check if something went wrong during startup or if everything is Running fine
        /// </summary>
        /// <param name="ifaceName"></param>
        /// <param name="captureFilter"></param>
        /// <param name="logAction"></param>
        public void StartCaptureOld(string ifaceName, string captureFilter, Action<string> logAction = null) {
            string remoteFilePath = RemoteFolderPath + "/" + RemoteFileName;
            string cmdString = $"tcpdump -i {ifaceName} -w {remoteFilePath} {captureFilter}"; //! hatte kein Sudo hier vorher

            _captureTaskCancel = new CancellationTokenSource();

            _captureTask = Task.Run(() => ExecCmd(cmdString, TapThread.Current.AbortToken, logAction), _captureTaskCancel.Token);


            Log.Debug("Started Capture Task.. Waiting 1s");
            TapThread.Sleep(1000);
            if (_captureTask.IsCompleted || _captureTask.IsCanceled || _captureTask.IsFaulted) {
                var debugOutput = _captureTask.GetAwaiter().GetResult();
                if (debugOutput.Contains("SIOCGIFHWADDR")) {
                    Log.Error($"Probably Wrong Capture Interface Specified: [{ifaceName}]");
                } else {
                    Log.Error($"Something went wrong during Capture Start.. \n{debugOutput}");
                }
            } else {
                Log.Debug("Task started Successfull");
            }
        }

        public int StopRunningCapture() {
            if (_activeAsyncCapture == null) {
                Log.Error("No Capture Task Started..");
                return -1;
            }

            if (!_activeAsyncCapture.isActive()) {
                Log.Warning("Capture Task isnt Active anymore!");
                Log.Warning(_activeAsyncCapture.getOutput());
                return -1;
            }

            Log.Debug("Stopping Active Capture");
            var killResult = ExecCmd("killall -s SIGINT tcpdump", TapThread.Current.AbortToken, str => Log.Debug(str));
            if (killResult.Contains("no process found")) {
                Log.Warning($"no tcp dump was running in background that could be stopped: {killResult}");
            } else if (killResult.Trim().Length != 0) {
                Log.Warning($"Got output while we tried to stop tcpdump");
                Log.Warning($"KillResult: [{killResult}]");
            }

            while (_activeAsyncCapture.isActive()) {
                Log.Debug($"Capture Proc still alive");
                try {
                    TapThread.ThrowIfAborted();
                } catch (OperationCanceledException) {
                    break;
                }

                TapThread.Sleep(1000);
            }


            var debugOutput = _activeAsyncCapture.getOutput();
            _activeAsyncCapture = null;
            Log.Debug($"Task Finished. Evaluating output: DebugOutput: {debugOutput}");
            return GetCapturedPacketsCount(debugOutput);
        }

        public int StopRunningCaptureOld() {
            if (_captureTask == null) {
                Log.Error("No Capture Task Started..");
                return -1;
            }

            var killResult = ExecCmd("killall -s SIGINT tcpdump", TapThread.Current.AbortToken, str => Log.Debug(str));
            if (killResult.Contains("no process found")) {
                Log.Warning($"no tcp dump was running in background that could be stopped: {killResult}");
            } else if (killResult.Trim().Length != 0) {
                Log.Warning($"Got output while we tried to stop tcpdump");
                Log.Warning($"KillResult: [{killResult}]");
            }

            Log.Debug("Waiting for TCPDump Task to Finish");


            while (_captureTask != null && _captureTask.Status == TaskStatus.Running) {
                Log.Debug($"Current Task State: {_captureTask.Status}");
                try {
                    TapThread.ThrowIfAborted();
                } catch (OperationCanceledException) {
                    break;
                }

                TapThread.Sleep(1000);
            }


            if (_captureTask != null && _captureTask.IsCompleted) {
                var debugOutput = _captureTask.GetAwaiter().GetResult();
                _captureTask = null;
                Log.Debug($"Task Finished. Evaluating output: DebugOutput: {debugOutput}");
                return GetCapturedPacketsCount(debugOutput);
            } else {
                Log.Error("Something was aborted or went wrong");
                return -1;
            }
        }


        public int RunPacketCapture(int duration, string ifaceName, string captureFilter,
            Action<string> logAction = null) {
            string remoteFilePath = RemoteFolderPath + "/" + RemoteFileName;
            string captureCommand = $"timeout {duration}s tcpdump -i {ifaceName} -w " + remoteFilePath;
            var cmdString = captureCommand + " " + captureFilter;


            var debugOutput = ExecCmd(cmdString, TapThread.Current.AbortToken, logAction);

            if (debugOutput.Contains("SIOCGIFHWADDR")) {
                Log.Error($"Probably Wrong Capture Interface Specified: [{ifaceName}]");
                return -1;
            }

            return GetCapturedPacketsCount(debugOutput);
        }


        private int GetCapturedPacketsCount(string resultString) {
            string pattern = @"tcpdump: listening on.*(\d+) packets captured";
            RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;

            var match = Regex.Match(resultString, pattern, options);
            if (match.Success && match.Groups.Count > 1) {
                if (match.Groups[1].Value.Equals("0")) {
                    return 0;
                } else {
                    return int.Parse(match.Groups[1].Value);
                }
            }

            return -1;
        }

        public override void Open() {
            base.Open();
            if (RemoteSnifferLogic.IsReachable(IpAddr)) {
                IsConnected = true;
            } else {
                Log.Error(
                    "Remote Device Isn't reachable. (Answering our ICMP ping). Please check IP and Device Status");
                IsConnected = false;
            }

            //if (!IdnString.Contains("Instrument ID"))
            //{
            //    Log.Error("This instrument driver does not support the connected instrument.");
            //    throw new ArgumentException("Wrong instrument type.");
            // }
        }


        public override void Close() {
            if (_captureTask != null) {
                _captureTaskCancel.Cancel();
                // StopRunningCapture();
            }

            base.Close();
        }
    }
}