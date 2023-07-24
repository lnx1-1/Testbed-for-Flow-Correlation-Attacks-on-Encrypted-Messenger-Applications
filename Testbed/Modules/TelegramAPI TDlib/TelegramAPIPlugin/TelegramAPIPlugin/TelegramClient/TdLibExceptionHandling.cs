using System;
using TdLib;

namespace TelegramAPIPlugin.TelegramClient {
	public class TdLibExceptionHandling {
		private static bool MsgEquals(Exception e, string msg) {
			if (e is TdException tdException) {
				return (tdException.Message.Equals(msg));
			} else {
				return false;
			}
		}


		
		public static bool IsSearchQueryEmptyException(Exception e) {
			return MsgEquals(e, "SEARCH_QUERY_EMPTY");
		}

		public static bool IsChatNotFoundException(Exception e) {
			return MsgEquals(e, "Chat not found");
		}

		public static bool IsUserNameNotOccupied(Exception e) {
			return MsgEquals(e, "USERNAME_NOT_OCCUPIED");
		}

		public static bool IsUserNameInvalidException(Exception e) {
			return MsgEquals(e, "USERNAME_INVALID") || MsgEquals(e, "Username is invalid");
		}

		public static bool IsPrivateChatException(TdException tdException) {
			return MsgEquals(tdException, "Chat member status can't be changed in private chats") ||
			       tdException.Message.Contains("private chats");
		}

		public static bool IsTooManyRequests(TdException tdException) {
			return tdException.Message.Contains("Too Many Requests");
		}

		public static bool IsAlreadyParticipant(TdException tdException) {
			return tdException.Message.Contains("USER_ALREADY_PARTICIPANT");
		}
		
		public static bool IsInviteSent(TdException tdException) {
			return tdException.Message.Contains("INVITE_REQUEST_SENT");
		}
		
		

	}
}