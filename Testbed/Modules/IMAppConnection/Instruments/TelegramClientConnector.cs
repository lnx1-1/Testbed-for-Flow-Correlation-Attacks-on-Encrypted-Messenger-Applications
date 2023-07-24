using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OpenTap;
using ProjectCommons;
using ProjectCommons.DataTypes;
using ProjectCommons.IMAppMessageLogger;
using TelegramAPIPlugin;
using TelegramAPIPlugin.TdlibSetup;
using TelegramAPIPlugin.TelegramClient;
using TelegramAPIPlugin.Utils;

namespace IMAppConnection.Instruments {
    [Display("TelegramClient Connector")]
    public class TelegramClientConnector : Instrument, ILogIMMessages, IOpenTapIMApp {
        private readonly TraceSource _Log = OpenTap.Log.CreateSource(nameof(TelegramClientConnector));

        private I_IMApp imApp = null;
        private MessageTraceLogger _logIMMessagesImplementation = null;

        #region Settings

        // [XmlIgnore]
        private List<AuthSetting> _AvailAuthSettingsList { get; set; }

        [Display("Loaded Profiles", "Loaded Account profile to use with this connector", Group: "Config File", Collapsed: true)]

        // [XmlIgnore]
        [Browsable(true)]
        public List<AuthSetting> AvailAbleAuthProfiles {
            get { return _AvailAuthSettingsList; }
            set {
                _AvailAuthSettingsList = value;
                OnPropertyChanged(nameof(AvailAbleAuthProfiles));
            }
        }

        [Display("Select Profile")]
        [AvailableValues(nameof(AvailAbleAuthProfiles))]
        [Browsable(true)]
        // [XmlIgnore]
        public AuthSetting SelectedProfile { get; set; }

        [Display("Auth Config Path", Group: "Config File")]
        [FilePath]
        public string authConfigPath { get; set; } = Config.configPath;

        #endregion

        [Browsable(true)]
        [Display("Update Name")]
        public void updateName() {
            Name = "TelegramApp: " + SelectedProfile.Name;
        }


        [Display("ReloadConfig", Group: "Config File", Collapsed: true)]
        [Browsable(true)]
        public void LoadSettings() {
            if (!File.Exists(authConfigPath)) {
                Log.Warning($"Couldn't find ConfigFile: {authConfigPath}");
                return;
            }

            _AvailAuthSettingsList = AuthSettingsFIleHandler.LoadAuthSettingsFromJson(authConfigPath);
            if (_AvailAuthSettingsList.Count > 0) {
                SelectedProfile = _AvailAuthSettingsList[0];
            }
        }

        public TelegramClientConnector() {
            //Set default values for properties / settings.
            Name = "TelegramClientConnector";
            LoadSettings();
        }

        public override void Open() {
            base.Open();
            if (SelectedProfile == null) {
                Log.Error("Connection profile not Set.. Please select Connection Profile in Instrument config");
                IsConnected = false;
                return;
            }

            //Open the connection to the instrument here
            imApp = TelegramClientFactory.GetInstance(SelectedProfile).GetAwaiter().GetResult();
            _logIMMessagesImplementation = imApp.GetMsgLogger();
            _Log.Info("Opened Telegram instance with ID");
            _Log.Info(imApp.GetUserInfoString().GetAwaiter().GetResult());
            var t = new OpenTap.ConsoleTraceListener(true, false, true);


            //if (!IdnString.Contains("Instrument ID"))
            //{
            //    Log.Error("This instrument driver does not support the connected instrument.");
            //    throw new ArgumentException("Wrong instrument type.");
            // }
        }

        public override void Close() {
            _Log.Info("Trying to Close Telegram Session");
            imApp.CloseSession();
            base.Close();
        }

        public void StartLogging() {
            _logIMMessagesImplementation.StartLogging();
        }

        public void StopLogging() {
            _logIMMessagesImplementation.StopLogging();
        }

        public List<MsgEvent> getMsgLog() {
            return _logIMMessagesImplementation.GetMsgLog();
        }

        public void ClearLog() {
            _logIMMessagesImplementation.ClearLog();
        }

        public List<string> GetLogAsCsv() {
            return _logIMMessagesImplementation.GetLogAsCsv();
        }

        public double getStartTime() {
            return _logIMMessagesImplementation.StartTimeStamp();
        }

        public bool JoinChat(string chatID) {
            return imApp.JoinChat(chatID);
        }

        public bool LeaveChat(string chatID) {
            return imApp.LeaveChat(chatID);
        }

        public string LookupChatId(string chatName) {
            return imApp.LookupChatId(chatName);
        }

        public bool IsChatJoined(string chatID) {
            return imApp.IsChatJoined(chatID);
        }

        public void SendMessage(string chatID, IMessageContent msg) {
            imApp.SendMessage(chatID, msg);
        }

        public List<string> GetAllChats() {
            return imApp.GetAllChats();
        }

        public MessageTraceLogger GetMsgLogger() {
            return imApp.GetMsgLogger();
        }

        public Task<string> GetUserInfoString() {
            return imApp.GetUserInfoString();
        }

        public void CloseSession() {
            imApp.CloseSession();
        }
    }
}