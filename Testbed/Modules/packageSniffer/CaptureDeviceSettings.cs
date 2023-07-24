using System.IO;
using System.Text.Json;

namespace packageSniffer {
    // [Display("CaptureDevice Settings")]
    // [SettingsGroup("Infrastructure", Profile: true)]
    public class CaptureDeviceSettings {
        // : ComponentSettings<CaptureDeviceSettings>
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        public string IPAddress { get; set; }
        public string CaptureUsername { get; set; }
        public string CaptureUsrPassword { get; set; }
        public string RemoteFilePath { get; set; }
        
        public string configUsr { get; set; }

        public bool IsValid() {
            return IPAddress != null && CaptureUsername != null && CaptureUsrPassword != null;
        }
        
        public static CaptureDeviceSettings LoadFromFile(string path) {
            FileStream fileStream = File.OpenRead(path);
            CaptureDeviceSettings settings = JsonSerializer.Deserialize<CaptureDeviceSettings>(fileStream);
            Log.Debug($"Loaded Loaded Config from config File");
            fileStream.Close();
            return settings;
        }

        public void StoreSettingsInJsonFile(string path) {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            string jsonFile = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, jsonFile);
            var fullPath = Path.GetFullPath(path);
            Log.Debug($"Wrote AuthSettingsFile to: {fullPath}");
        }


        public CaptureDeviceSettings() {
            LoadDefault();
        }

        public void LoadDefault() {
            IPAddress = "192.168.2.92";
            CaptureUsrPassword = "lnAdmin";
            CaptureUsername = "lnx";
            RemoteFilePath = "captures/capture";
            configUsr = "captureUsr";
        }
    }
}