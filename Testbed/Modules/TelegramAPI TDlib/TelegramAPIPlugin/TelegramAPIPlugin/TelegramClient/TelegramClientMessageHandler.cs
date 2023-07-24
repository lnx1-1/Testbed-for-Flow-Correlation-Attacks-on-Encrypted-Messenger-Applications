using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using ProjectCommons.Utils;
using TdLib;

namespace TelegramAPIPlugin.TelegramClient {
    internal class MsgID {
        public string remoteID { get; set; }
        public int uniquieID { get; set; }

        public MsgID(TdApi.File file) {
            uniquieID = file.Id;

            remoteID = file.Remote.Id;
        }

        public override string ToString() {
            return $"remoteID: [{remoteID}] uniqueID: [{uniquieID}]";
        }

        public override bool Equals(object obj) {
            if (obj != null && obj.GetType() == typeof(MsgID)) {
                var other = (MsgID)obj;
                return remoteID.Equals(other.remoteID) && uniquieID.Equals(other.uniquieID);
            } else {
                return false;
            }
        }

        protected bool Equals(MsgID other) {
            return remoteID == other.remoteID && uniquieID == other.uniquieID;
        }

        public override int GetHashCode() {
            unchecked {
                return ((remoteID != null ? remoteID.GetHashCode() : 0) * 397) ^ uniquieID;
            }
        }
    }


    /// <summary>
    /// Message Handler for Telegram Client. Contains the Message handler
    /// </summary>
    public class TelegramClientMessageHandler {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private LoggingState _tgLogState;

        private Dictionary<string, string> _pendingMsgs;
        // private readonly Mutex _pendingMsgMutex = new Mutex();

        private Dictionary<string, int> _pendingMsgMsgIDDict;

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private ClientAbstractionLayer _CAL;

        public TelegramClientMessageHandler(ClientAbstractionLayer cal, LoggingState tgLoggingState) {
            _pendingMsgs = new Dictionary<string, string>();
            _pendingMsgMsgIDDict = new Dictionary<string, int>();
            _CAL = cal;
            this._tgLogState = tgLoggingState;
        }


        public void HandleUpdate(TdApi.Update update) {
            if (!_tgLogState.loggingActive) {
                // Log.Debug($"Skipping msgUpdate. Logging not Active [{_CAL.ClientName}]");
                return;
            }

            switch (update) {
                case TdApi.Update.UpdateNewMessage message:
                    HandleMessageUpdate(message);
                    break;
                case TdApi.Update.UpdateFile fileUpdate: {
                    HandleFileUpdate(fileUpdate);
                    break;
                }
            }
        }

        private void HandleFileUpdate(TdApi.Update.UpdateFile fileUpdate) {
            bool isDownloadCompleted = fileUpdate.File.Local.IsDownloadingCompleted &&
                                       !fileUpdate.File.Remote.IsUploadingActive &&
                                       fileUpdate.File.Local.CanBeDownloaded;
            bool isUploadingComplete = !fileUpdate.File.Local.IsDownloadingActive &&
                                       fileUpdate.File.Local.IsDownloadingCompleted &&
                                       !fileUpdate.File.Remote.IsUploadingActive &&
                                       fileUpdate.File.Remote.IsUploadingCompleted &&
                                       !fileUpdate.File.Remote.Id.Equals("");

            if (isDownloadCompleted) {
                long size = fileUpdate.File.Size;
                double timestampUnixLocal = UnixDateTime.GetUnixTimeNowDouble();
                long expectedSize = fileUpdate.File.ExpectedSize;
                long downloadedSize = fileUpdate.File.Local.DownloadedSize;
                // Log.Info($"\nLocal:\n - Complete: {file.Local.IsDownloadingCompleted}\n - Active: {file.Local.IsDownloadingActive}\nRemote:\n - UpComplete: {file.Remote.IsUploadingCompleted}\n - upActive: {fileUpdate.File.Remote.IsUploadingActive}");
                // MsgID id = new MsgID(fileUpdate.File);

                var uniqueId = fileUpdate.File.Id;


                var uniqueRemoteId = fileUpdate.File.Remote.UniqueId;
                if (uniqueRemoteId.Equals("")) {
                    Log.Warn("Uknown file id for a File!!");
                }

                MessageReceivedEventArgs ev = new MessageReceivedEventArgs() { _Type = "unknown", id = uniqueId.ToString() };

                if (!_pendingMsgs.ContainsKey(fileUpdate.File.Remote.UniqueId)) {
                    Log.Warn($"File {uniqueRemoteId} is not downloading - received Update for File but file id wasnt present in pending list. Skipping file..");
                    return;
                }

                ev._Type = _pendingMsgs[uniqueRemoteId];

                // var diff = timestampUnixLocal - ev._Timestamp;
                ev._Timestamp = timestampUnixLocal;
                ev._Len = downloadedSize;
                ev._Path = fileUpdate.File.Local.Path;

                OnMessageReceived?.Invoke(this, ev);

                Log.Debug($"|{_CAL.ClientName}| Download complete!! of File [{uniqueRemoteId}] Path: {fileUpdate.File.Local.Path}");

                // Log.Debug($"Download took {diff}s");
                if (!File.Exists(ev._Path)) {
                    Log.Warn($"Download wasnt sucessfull.. File is Missing!! [{Path.GetFileName(ev._Path)}]");
                    Log.Warn(
                        $"File.ID {fileUpdate.File.Id}\n" +
                        $"File.Size {fileUpdate.File.Size}\n" +
                        $"ExpectedSize {fileUpdate.File.ExpectedSize}\n" +
                        $"Can be Downloaded: {fileUpdate.File.Local.CanBeDownloaded}\n" +
                        $"File.Remote.UniqueID: {fileUpdate.File.Remote.UniqueId}\n"
                    );
                }
            } else if (isUploadingComplete) {
                Log.Debug(
                    $"|{_CAL.ClientName}| Uploading Complete ID [{fileUpdate.File.Id}] - Size: [{fileUpdate.File.Size}] Path: [{fileUpdate.File.Local.Path}]");
            }
        }


        private async Task StartDownloadFile(int downloadId, string msgType, string uniqueRemoteId) {
            if (_pendingMsgs.TryGetValue(uniqueRemoteId, out var ev)) {
                _pendingMsgMsgIDDict.TryGetValue(uniqueRemoteId, value: out var lastdownId);
                Log.Warn($"++++  ID [{uniqueRemoteId}] Already Present.. Double {msgType} Message Key Detected!! ++++ [{ev}]");
                if (lastdownId.Equals(downloadId)) {
                    Log.Debug($"Download ID sind ebenfalls gleich {downloadId}");
                } else {
                    Log.Debug($"Download ID Diff: Curr: {downloadId}, Last: {lastdownId}");
                }
            } else {
                _pendingMsgs.Add(uniqueRemoteId, msgType);
                _pendingMsgMsgIDDict.Add(uniqueRemoteId, downloadId);
            }


            Log.Debug($"Starting Donwnload of ID: [{uniqueRemoteId}]");
            var file = await _CAL.DownloadFile(downloadId);
            if (file.Local.IsDownloadingCompleted) {
                Log.Debug($"Download completed [{file.Id}");
            } else {
                Log.Debug($"Download active [{file.Id}");
            }
        }


        public async void HandleMessageUpdate(TdApi.Update.UpdateNewMessage msgUpdate) {
            bool isOutgoing = msgUpdate.Message.IsOutgoing; //! Seems like it is the Other way around?!
            long timestampUnix = msgUpdate.Message.Date;
            long timestampUnixLocal = UnixDateTime.GetUnixTimeNow();
            // 

            switch (msgUpdate.Message.Content) {
                case TdApi.MessageContent.MessageText txtMsg:
                    HandleTextMessage(msgUpdate, txtMsg, timestampUnix, timestampUnixLocal, isOutgoing);
                    break;
                case TdApi.MessageContent.MessageAudio audioMessage:
                    await HandleAudioMessage(audioMessage, isOutgoing, timestampUnix);
                    break;
                case TdApi.MessageContent.MessagePhoto messagePhoto:
                    await HandlePhotoMessage(messagePhoto, isOutgoing, timestampUnix);
                    break;
                case TdApi.MessageContent.MessageVideo messageVideo:
                    await HandleVideoMessage(messageVideo, isOutgoing, timestampUnix);
                    break;
                case TdApi.MessageContent.MessageVoiceNote messageVoiceNote:
                    await HandleVoiceNoteMessage(messageVoiceNote, isOutgoing, timestampUnix);
                    break;
                case TdApi.MessageContent.MessageChatAddMembers addMembersMsg:
                    Log.Debug($"New Chat members were added. Extra: [{addMembersMsg.Extra}]");
                    break;
                default:
                    Log.Debug($"Unknwon msg Type: {msgUpdate.Message.Content.DataType}");
                    break;
            }
        }

        private async Task HandleVoiceNoteMessage(TdApi.MessageContent.MessageVoiceNote messageVoiceNote, bool isOutgoing, long timestampUnix) {
            var downloadId = messageVoiceNote.VoiceNote.Voice.Id;

            Log.Debug($"|{_CAL.ClientName}| Received VoiceNote @T[{timestampUnix}]");

            await StartDownloadFile(downloadId, "audio", messageVoiceNote.VoiceNote.Voice.Remote.UniqueId);
        }

        // private async Task DownloadFile(long size, long timestampUnix, int downloadId, string msgType) {
        //     _pendingMsgMutex.WaitOne();
        //     try {
        //         var file = await _CAL.DownloadFile(downloadId);
        //         MsgID id = new MsgID(file);
        //         Log.Debug($"Started Download of ID: [{id}]");
        //
        //         if (_pendingMsgs.TryGetValue(id, out var ev)) {
        //             Log.Warn($"++++ ID [{id}] Already Present.. Double {msgType} Message Key Detected!! ++++ [{ev}]");
        //         } else {
        //             _pendingMsgs.Add(id,
        //                 new MessageReceivedEventArgs()
        //                     { _Len = size, _Timestamp = timestampUnix, _Type = msgType });
        //         }
        //     } finally {
        //         _pendingMsgMutex.ReleaseMutex();
        //     }
        // }


        private async Task HandleVideoMessage(TdApi.MessageContent.MessageVideo messageVideo, bool isOutgoing,
            long timestampUnix) {
            if (isOutgoing) {
                Log.Debug($"Is Outgoing Video Msg.. Skipped");
                return;
            }

            var downloadId = messageVideo.Video.Video_.Id;

            Log.Debug($"|{_CAL.ClientName}| Received Video @T[{timestampUnix}]");

            await StartDownloadFile(downloadId, "video", messageVideo.Video.Video_.Remote.UniqueId);
        }


        private void HandleTextMessage(TdApi.Update.UpdateNewMessage msgUpdate, TdApi.MessageContent.MessageText txtMsg,
            long timestampUnix,
            long timestampUnixLocal, bool isOutgoing) {
            int messageSize = Encoding.UTF8.GetByteCount(txtMsg.Text.Text);
            string msgText = txtMsg.Text.Text;


            Log.Debug("----------------------------------M S G ------------------------------------");
            Log.Debug($"Timestamp in Msg [{timestampUnix}], timestamp here [{timestampUnixLocal}]");
            Log.Debug($"new Msg:[{msgText.Substring(0, Math.Min(msgText.Length, 15))}]");
            Log.Debug($"Size: {messageSize}");
            Log.Debug($"isOutgoing: {isOutgoing}");
            // Log.Debug($"Chat: [{chatName}]");
            Log.Debug($"In Channel: {msgUpdate.Message.IsChannelPost}");
            Log.Debug($"tmstp: {timestampUnix}");
            Log.Debug($"Type: text");
            Log.Debug("---------------------------------------------------------------------------");

            OnMessageReceived?.Invoke(this,
                new MessageReceivedEventArgs(len: messageSize, timestamp: timestampUnix, type: "text",
                    isOutGoing: isOutgoing, msgText));
        }

        private async Task HandlePhotoMessage(TdApi.MessageContent.MessagePhoto messagePhoto, bool isOutgoing,
            long timestampUnix) {
            if (isOutgoing) {
                Log.Debug($"Is Outgoing Photo Msg.. Skipped");
                return;
            }

            var sizesList = messagePhoto.Photo.Sizes.ToList();
            var photoSize = sizesList.OrderBy(photo => photo.Photo.Size).Last();
            Log.Debug($"Received Photo msg [{photoSize.Photo.Id}]");

            await StartDownloadFile(photoSize.Photo.Id, "photo", photoSize.Photo.Remote.UniqueId);
        }

        private async Task HandleAudioMessage(TdApi.MessageContent.MessageAudio audioMessage, bool isOutgoing,
            long timestampUnix) {
            if (isOutgoing) {
                Log.Debug($"Is Outgoing Audio Msg.. Skipped");
                return;
            }

            var id = audioMessage.Audio.Audio_.Id;
            Log.Debug($"Received AudioMsg [{id}]");
            await StartDownloadFile(id, "audio", audioMessage.Audio.Audio_.Remote.UniqueId);
        }
    }
}