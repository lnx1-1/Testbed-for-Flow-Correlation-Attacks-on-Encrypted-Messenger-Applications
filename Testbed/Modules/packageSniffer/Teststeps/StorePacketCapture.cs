using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using OpenTap;
using ProjectCommons;
using ProjectCommons.DataTypes;

namespace packageSniffer.Teststeps {
    [Display("Store PacketCapture", Group:"Packet Capture")]
    public class StorePacketCapture : TestStep {
        #region Settings

        [Display("ResultFolderPath", Group: "Output", Order: 3)]
        [DirectoryPath]
        [Output]
        public string _captureFolderPath { get; set; } = "results";

        [Display("Overwrite Existing local File")]
        public bool _overwriteExistingFile { get; set; } = false;

        [Display("File Name")] public string _captureFileName { get; set; } = "capture-.pcap";
        public ICaptureNetPacketsInstrument _captureDevice { get; set; }

        [Display("Resulting Output File", Group: "Output")]
        [FilePath]
        [Output]
        public string _outFilePath { get;private set; } = "";

        #endregion

        // private static string _captureFileName = "currentCapture";
        // private static string _captureFileEnding = ".pcap";
        // private static string _captureFileFolder;

        public StorePacketCapture() {
        }

        [Browsable(true)]
        [Display("Open Folder", Group: "Output", Order: 3)]
        public void OpenCaptureFolder() {
            string directoryFilePath = (Path.GetFullPath(_captureFolderPath));
            Process.Start(new ProcessStartInfo(directoryFilePath)
                { UseShellExecute = true }); //opens a new Explorer Folder (should work Crossplattform)
        }


        public override void PrePlanRun() {
            var resultingFilePath = Path.Combine(_captureFolderPath, _captureFileName);
            if (_overwriteExistingFile && File.Exists(resultingFilePath)) {
                File.Delete(resultingFilePath);
                Log.Info("RemovedOldFile");
            }

            base.PrePlanRun();
        }

        public override void Run() {
            if (!_captureDevice.IsConnected) {
                Log.Error("Device Isn't reachable. Please check Device Status");
                return;
            }

            var resultPath = "";
            if (Directory.Exists(_captureFolderPath)) {
                resultPath = Path.Combine(_captureFolderPath, _captureFileName);
                while (File.Exists(resultPath)) {
                    Log.Debug($"File {resultPath} exists.");
                    int num = Utilitys.GetFileNumber(resultPath);
                    var fileNameWithoutNumber = _captureFileName.Replace(num + "", "");
                    resultPath = Utilitys.FormatFilePath(num + 1, _captureFolderPath,
                        Path.GetFileNameWithoutExtension(fileNameWithoutNumber),
                        Path.GetExtension(_captureFileName));
                }
            }
            else {
                Log.Warning("Not a Existing folder");
            }

            Log.Debug($"using fileName: {resultPath}");
            var result = _captureDevice.StoreCapture(resultPath);
            _outFilePath = result;
            _captureFolderPath = Utilitys.GetDirectoryPath(result);
            Log.Info("Capture File Succesfully saved to: " + _captureFolderPath);
            // if (!Directory.Exists(_captureFolderPath) || !Directory.EnumerateFiles(_captureFolderPath).Any()) {
            // 	Log.Error("Download path doesnt exist or no files were downloaded");
            // 	return;
            // }

            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun() {
            base.PostPlanRun();
        }
    }
}