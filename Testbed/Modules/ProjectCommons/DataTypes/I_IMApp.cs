using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectCommons.IMAppMessageLogger;

namespace ProjectCommons.DataTypes {
	/// <summary>
	/// The Interface abstracts different Instant Messaging Applications (e.g. Telegram, Signal, WhatsApp)
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public interface I_IMApp {
		/// <summary>
		/// The Client will Join the Chat/Channel
		/// </summary>
		/// <param name="chatID">The ChatID of the Chat to join. Use @LookupChatID to get the correct ID.</param>
		/// <returns>True if Sucessfully Joined the Chat. False otherweise</returns>
		public bool JoinChat(string chatID);

		/// <summary>
		/// The Client will leave the Chat/Channel
		/// </summary>
		/// <param name="chatID">The ChatID of the Chat to leave. Use <see cref="LookupChatId"/> to get the correct ID</param>
		/// <returns>True if Sucesfully Left the Chat. False otherweise</returns>
		public bool LeaveChat(string chatID);

		/// <summary>
		/// Looks up the Correct ChatID for the Given ChatName
		/// </summary>
		/// <param name="chatName"></param>
		/// <returns>The ChatID for the Correct Chat. An Empty string if no chat was found</returns>
		public string LookupChatId(string chatName);

		/// <summary>
		/// Checks if the Client is in the Group or Channel
		/// </summary>
		/// <param name="chatID">the chatID describing the group to check for</param>
		/// <returns>true: when the client is part of this group, false if not</returns>
		public bool IsChatJoined(string chatID);

		/// <summary>
		/// Sends a Message to the Specified ChatID.
		/// </summary>
		/// <param name="chatID">The Chat ID to send the Message to.. Use <see cref="LookupChatId"/> to Lookup the ID of the chatname </param>
		/// <param name="msg">The Message to Send. Can be: <br/>
		/// <see cref="MessageContentAudio">MessageContentAudio</see><br/>
		/// <see cref="MessageContentPhoto">MessageContentPhoto</see><br/>
		/// <see cref="MessageContentText">MessageContentText</see><br/>
		/// <see cref="MessageContentVideo">MessageContentVideo</see>
		/// 
		/// </param>
		public void SendMessage(string chatID, IMessageContent msg);

		/// <summary>
		/// Fetches all Channels, Groups, and Chats from the current UserAccount
		/// </summary>
		/// <returns>A List containing all <b>ChatIDs</b> (max 500)</returns>
		public List<string> GetAllChats();

		public MessageTraceLogger GetMsgLogger();
		public Task<string> GetUserInfoString();
		public void CloseSession();
	}
}