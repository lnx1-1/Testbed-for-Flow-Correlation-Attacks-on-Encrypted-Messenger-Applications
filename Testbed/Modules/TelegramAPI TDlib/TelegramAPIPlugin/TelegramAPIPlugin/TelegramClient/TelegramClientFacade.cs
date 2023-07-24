using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using ProjectCommons;
using ProjectCommons.DataTypes;
using ProjectCommons.IMAppMessageLogger;
using TdLib;

namespace TelegramAPIPlugin.TelegramClient {
    public class TelegramClientFacade : I_IMApp {
        private ClientAbstractionLayer _CAL;
        private MessageTraceLogger MsgLog { get; }
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private const int ChannelFetchLimit = 500;

        public TelegramClientFacade(ClientAbstractionLayer CAL, MessageTraceLogger msgLog) {
            _CAL = CAL;
            MsgLog = msgLog;
        }

        public MessageTraceLogger GetMsgLogger() {
            return MsgLog;
        }

        ~TelegramClientFacade() {
            Log.Debug("Facade Destructor called. Not closing it");
            // _CAL.CloseSession().GetAwaiter().GetResult();
        }

        public void CloseSession() {
            _CAL.CloseSession().GetAwaiter().GetResult();
        }

        public async void JoinChatByName(string name) {
            var chatID = await _CAL.LookupChatIDAsLong(name);
            await _CAL.JoinChat(chatID);
        }

        public bool JoinChat(string chatID) {
            if (long.TryParse(chatID, out long idAsLong)) {
                return _CAL.JoinChat(idAsLong).GetAwaiter().GetResult();
            } else {
                Log.Error("couldn't join chat.. chatID not parseable as long: " + chatID);
                return false;
            }
        }


        public bool LeaveChat(string chatID) {
            if (long.TryParse(chatID, out long longID)) {
                return _CAL.LeaveChat(longID).GetAwaiter().GetResult();
            } else {
                return false;
            }
        }

        public string LookupChatId(string chatName) {
            var id = _CAL.LookupChatIDAsLong(chatName).GetAwaiter().GetResult();
            return id == 0 ? "" : id.ToString();
        }

        public bool IsChatJoined(string chatID) {
            if (long.TryParse(chatID, out long longChatID)) {
                return _CAL.IsChatJoined(longChatID).GetAwaiter().GetResult();
            } else {
                Log.Error($"Error.. wrong ChatID. Not Parseable.. [{chatID}]");
                return false;
            }
        }

        public void SendMessage(string chatID, IMessageContent msg) {
            if (long.TryParse(chatID, out long longChatID)) {
                if (msg is MessageContentText textMsg) {
                    _CAL.SendTextMessage(longChatID, textMsg.Text).GetAwaiter().GetResult();
                    Log.Info($"|{_CAL.ClientName}| Sending Text msg");
                } else if (msg is MessageContentPhoto photoMsg) {
                    if (!File.Exists(photoMsg.Path)) {
                        Log.Warn($"Couldnt send photo. File is not present [{photoMsg.Path}]");
                        return;
                    }

                    _CAL.SendPhotoMessage(longChatID, photoMsg.Path).GetAwaiter().GetResult();
                    Log.Info($"|{_CAL.ClientName}| Sending Photo msg");
                } else if (msg is MessageContentAudio audiomsg) {
                    if (!File.Exists(audiomsg.Path)) {
                        Log.Warn($"Couldnt send audio. File is not present [{audiomsg.Path}]");
                        return;
                    }

                    _CAL.SendAudioMessage(longChatID, audiomsg.Path).GetAwaiter().GetResult();
                    Log.Info($"|{_CAL.ClientName}| Sending Audio msg");
                } else if (msg is MessageContentVideo video) {
                    if (!File.Exists(video.Path)) {
                        Log.Warn($"Couldnt send video. File is not present [{video.Path}]");
                        return;
                    }

                    _CAL.SendVideoMessage(longChatID, video.Path).GetAwaiter().GetResult();
                    Log.Info($"|{_CAL.ClientName}| Sending Video msg");
                } else if (msg is MessageContentVoiceNote voiceNote) {
                    if (!File.Exists(voiceNote.Path)) {
                        Log.Warn($"Couldnt send voiceNote. File is not present [{voiceNote.Path}]");
                        return;
                    }

                    _CAL.SendVideoMessage(longChatID, voiceNote.Path).GetAwaiter().GetResult();
                    Log.Info($"|{_CAL.ClientName}| Sending voice msg");
                } else {
                    Log.Error("Not Implemented Yet.. Would Throw an Exception");
                    throw new NotImplementedException("Only Text Messages are support");
                }
            } else {
                Log.Error($"Error.. wrong ChatID. Not Parseable.. [{chatID}]");
            }
        }

        public List<string> GetAllChats() {
            return _CAL.GetChannelIDs(ChannelFetchLimit).GetAwaiter().GetResult()
                .Select((l => l.ToString()))
                .ToList();
        }

        public async Task<string> GetUserInfoString() {
            StringBuilder builder = new StringBuilder();
            var usr = await _CAL.GetMyself();
            builder.Append("------------------------------------------------\n");
            builder.Append("	User INFO of current Logged in User\n");
            builder.Append($"		Name: [{usr.Usernames.ActiveUsernames.First()}]\n");
            builder.Append($"		PhoneNum: [{usr.PhoneNumber}]\n");
            builder.Append("------------------------------------------------\n");
            return builder.ToString();
        }

        public async Task PrintUserInfo() {
            Log.Info(await GetUserInfoString());
        }
    }
}