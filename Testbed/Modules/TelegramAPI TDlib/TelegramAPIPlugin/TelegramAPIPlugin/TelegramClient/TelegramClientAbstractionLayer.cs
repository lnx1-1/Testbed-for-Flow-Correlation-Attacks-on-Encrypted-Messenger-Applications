using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TdLib;

namespace TelegramAPIPlugin.TelegramClient {
    /// <summary>
    /// Is The Abstraction Layer for the Telegram Client. Handles all the Communication with TdClient
    /// </summary>
    public class ClientAbstractionLayer {
        private TdApi.Client _client;
        internal string ClientName { get; }
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private const int TOO_MANY_REQUEST_SLEEP = 500; // in ms

        public ClientAbstractionLayer(TdClient client) {
            _client = client;
            ClientName = getInstanceName();
            
        }


        /// <summary>
        /// Sends a Message with the Specified Content
        /// </summary>
        /// <param name="target">the target ChatID to send the message to</param>
        /// <param name="content">the Content to send. Can be some type</param>
        internal async Task SendMessage(long target, TdApi.InputMessageContent content) {
            await _client.SendMessageAsync(target, 0, 0, null, null, content);
        }

        internal async Task SendAudioMessage(long longChatID, string audiomsgPath) {
            var audioMsg = new TdApi.InputMessageContent.InputMessageAudio();
            audioMsg.Audio = new TdApi.InputFile.InputFileLocal() { Path = audiomsgPath };
            await SendMessage(longChatID, audioMsg);
        }

        internal async Task SendVideoMessage(long longChatID, string videoPath) {
            var videoMsg = new TdApi.InputMessageContent.InputMessageVideo();
            videoMsg.Video = new TdApi.InputFile.InputFileLocal() { Path = videoPath };
            await SendMessage(longChatID, videoMsg);
        }

        internal async Task SendPhotoMessage(long target, string filePath) {
            var newMsg = new TdApi.InputMessageContent.InputMessagePhoto();
            newMsg.Photo = new TdApi.InputFile.InputFileLocal() { Path = filePath };
            await SendMessage(target, newMsg);
        }

        internal async Task SendTextMessage(long target, string message) {
            var newMsg = new TdApi.InputMessageContent.InputMessageText();
            newMsg.Text = new TdApi.FormattedText() { Text = message };
            await SendMessage(target, newMsg);
        }


        /// <summary>
        /// Leaves chat. Returns true if Sucess.. Returns true regardless if you were previously in the Chat.
        /// </summary>
        /// <param name="chatID">the ChatID to leave from</param>
        /// <returns>False on an Error e.g. Chat Not Found, True on succes (doesn't show if you were previously there)</returns>
        internal async Task<bool> LeaveChat(long chatID) {
            bool retry;
            do {
                //Retry Loop if we have to many API request
                retry = false; // is for Resetting. So next try can be successful
                try {
                    await _client.LeaveChatAsync(chatID);
                } catch (TdException e) {
                    if (TdLibExceptionHandling.IsChatNotFoundException(e)) {
                        Console.WriteLine($"Chat [{chatID}], not Found");
                        return false;
                    } else if (TdLibExceptionHandling.IsPrivateChatException(e)) {
                        Log.Info($"Couldn't leave private Chat [{chatID}]");
                        return false;
                    } else if (TdLibExceptionHandling.IsTooManyRequests(e)) {
                        Log.Debug("Too Many API Request. Trying to Sleep for 500ms");
                        Thread.Sleep(TOO_MANY_REQUEST_SLEEP);
                        retry = true;
                    } else {
                        throw;
                    }
                }
            } while (retry);

            return true;
        }

        private async Task<TdException> tryJoin(long chatID) {
            try {
                var result = await _client.JoinChatAsync(chatID);
            } catch (TdException e) {
                return e;
            }

            return null;
        }

        internal async Task<bool> JoinChat(long chatID) {
            Task<string> chatTitle = GetChatTitle(chatID);
            await Console.Out.WriteLineAsync("Trying to Join chat: " + (ulong)chatID + $" [{await chatTitle}]");
            TdException e = null;
            int retryTimeOut = 500;
            do {
                e = await tryJoin(chatID);
                if (e == null) {
                    return true;
                }
                if (TdLibExceptionHandling.IsChatNotFoundException(e)) {
                    return false;
                } else if (TdLibExceptionHandling.IsAlreadyParticipant(e)) {
                    Log.Info("User already Participant of Group");
                    return false;
                } else if (TdLibExceptionHandling.IsTooManyRequests(e)) {
                    Log.Warn($"Too Many Requests [{e.Message}]. Retrying in {retryTimeOut}ms");
                    Thread.Sleep(retryTimeOut);
                    retryTimeOut += 500;
                } else if (TdLibExceptionHandling.IsInviteSent(e)) {
                    Log.Warn("Invite was sent");
                }
                else {
                    throw e;
                }
            } while (e != null && TdLibExceptionHandling.IsTooManyRequests(e));

            return true;
        }

        internal async Task<bool> IsChatJoined(long chatId) {
            TdApi.Chat chat = null;
            try {
                chat = await _client.GetChatAsync(chatId);
            } catch (TdException e) {
                if (e.Message.Equals("Chat not found")) {
                    Console.WriteLine($"IsChatJoined: Chat [{chatId}] not found");
                    return false;
                }
            }

            if (chat == null) {
                return false;
            }

            TdApi.ChatMemberStatus status = new TdApi.ChatMemberStatus();
            switch (chat.Type) {
                case TdApi.ChatType.ChatTypeSupergroup supergroup: {
                    long id = supergroup.SupergroupId;
                    var supergroupObj = await _client.GetSupergroupAsync(id);
                    status = supergroupObj.Status;
                    break;
                }
                case TdApi.ChatType.ChatTypeBasicGroup basic: {
                    var id = basic.BasicGroupId;
                    var basicgroupObj = await _client.GetBasicGroupAsync(id);
                    status = basicgroupObj.Status;
                    break;
                }
            }

            return status is not (TdApi.ChatMemberStatus.ChatMemberStatusBanned
                or TdApi.ChatMemberStatus.ChatMemberStatusLeft);
        }

        private async Task<TdApi.Chat> GetChat(long messageChatID) {
            return await _client.ExecuteAsync(new TdApi.GetChat { ChatId = messageChatID });
        }

        internal async Task<string> GetChatTitle(long messageChatId) {
            string returnValue;
            if (await IsChatJoined(messageChatId)) {
                var chat = await GetChat(messageChatId);
                returnValue = chat.Title;
            } else {
                Log.Debug("Chat not found");
                // Console.WriteLine("GetChatTitle: Chat not found");
                returnValue = "Chat not found";
            }

            return returnValue;
        }

        internal string getInstanceName() {
            return GetMyself().GetAwaiter().GetResult().Usernames.ActiveUsernames.First();
        }

        internal async Task<TdApi.User> GetMyself() {
            return await _client.GetMeAsync();
        }


        /// <summary>
        /// Searchs for Public Chats
        /// </summary>
        /// <param name="chatName">chatName</param>
        /// <returns>returns 0 if no Chat was found, otherwise the ChatID</returns>
        internal long SearchPublicChat(string chatName) {
            long outID = 0;
            try {
                var chat = _client.SearchPublicChatAsync(chatName).GetAwaiter().GetResult();
                outID = chat.Id;
            } catch (TdException e) {
                if (TdLibExceptionHandling.IsUserNameInvalidException(e)) {
                    Log.Warn($"No Chats found during public lookup. [{chatName}]");
                } else if (TdLibExceptionHandling.IsUserNameNotOccupied(e)) {
                    Log.Warn($"UserName Not Occupied during Public Lookup [{chatName}]");
                } else {
                    throw;
                }
            }

            return outID;
        }

        /// <summary>
        /// Searches in the List of known Chats in Chat Title and Username for the ChatName
        /// </summary>
        /// <param name="chatName">the Chat or User Name to Search for</param>
        /// <returns>the ChatID of the first Match</returns>
        internal async Task<long> LookupChatIDAsLong(string chatName) {
            var chats = await _client.SearchChatsAsync(chatName, 1);
            if (chats.TotalCount != 0) {
                return chats.ChatIds[0];
            }

            try {
                chats = await _client.SearchChatsOnServerAsync(chatName, 1);
                if (chats.TotalCount != 0) {
                    return chats.ChatIds[0];
                }
            } catch (Exception e) {
                if (TdLibExceptionHandling.IsSearchQueryEmptyException(e)) {
                    Log.Warn($"ChatName not Valid: [{chatName}] during OnlineSearch");
                }
            }


            Log.Info($"No Chats found during lookup of [{chatName}]. Searching Public chats");
            return SearchPublicChat(chatName);
        }


        internal async Task<List<long>> GetChannelIDs(int limit) {
            var chats = await _client.GetChatsAsync(limit: limit);
            return chats.ChatIds.ToList();
        }

        internal async IAsyncEnumerable<TdApi.Chat> GetChannels(int limit) {
            var chats = await _client.ExecuteAsync(new TdApi.GetChats {
                Limit = limit
            });

            foreach (var chatId in chats.ChatIds) {
                var chat = await _client.ExecuteAsync(new TdApi.GetChat {
                    ChatId = chatId
                });

                if (chat.Type is TdApi.ChatType.ChatTypeSupergroup or TdApi.ChatType.ChatTypeBasicGroup or TdApi
                        .ChatType
                        .ChatTypePrivate) {
                    yield return chat;
                }
            }
        }

        private async void PrintGroupName(long chatID) {
            var chat = await _client.ExecuteAsync(new TdApi.GetChat { ChatId = chatID });

            switch (chat.Type) {
                case TdApi.ChatType.ChatTypeSupergroup:
                    Console.WriteLine($"[Supergroup]: [{chat.Title}]");
                    break;
                case TdApi.ChatType.ChatTypeBasicGroup:
                    Console.WriteLine($"[BasicGroup]: [{chat.Title}]");
                    break;
            }
        }


        public async Task<TdApi.File> DownloadFile(int fileID) {
            return await _client.DownloadFileAsync(fileID, 10);
        }


        public async Task CloseSession() {
            Log.Debug("Close Session called");
            await _client.CloseAsync();
        }
    }
}