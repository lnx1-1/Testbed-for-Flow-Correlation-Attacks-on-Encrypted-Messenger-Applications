using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using TelegramAPIPlugin.TdlibSetup;

namespace TelegramAPIPlugin.Utils {
    public class AuthSettingsFIleHandler {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static  List<AuthSetting> LoadAuthSettingsFromJson(string filePath) {
            using FileStream fileStream = File.OpenRead(filePath);
            List<AuthSetting> packetList =  JsonSerializer.Deserialize<AuthListWrapper>(fileStream).auths;
            log.Debug($"Loaded {packetList.Count} Auth profiles from config File");
            return packetList;
        }

        public static void WriteAuthSettingsToJsonFile(List<AuthSetting> authSettings, string filePath) {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            string jsonFile = JsonSerializer.Serialize(new AuthListWrapper() { auths = authSettings }, options);
            File.WriteAllText(filePath, jsonFile);
            var fullPath = Path.GetFullPath(filePath);
            log.Debug($"Wrote AuthSettingsFile to: {fullPath}");
        }

        internal class AuthListWrapper {
            public List<AuthSetting> auths { get; set; }
        }
    }
}