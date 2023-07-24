using System;
using System.Collections.ObjectModel;
using NLog;
using NLog.Fluent;
using ProjectCommons;

namespace TelegramAPIPlugin {
	public class MsgLogTraceMarshaller {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public const char DELIMITER = ';';
		
		public static string MarshallMsgReceiveEventArgsToString(MessageReceivedEventArgs args) {
			if (args != null) {
				return string.Format($"{args._Timestamp}{DELIMITER} {args._Len}{DELIMITER} {args._Type}");
			} else {
				throw new ArgumentException("MessageReceivedEventsArgs is Null");
			}
		}

		public static MsgEvent UnmarshallCsvMessageEventToMsgEvent(string line) {
			if (!LineParse(line, out var timestampStr, out var lenStr, out var typeStr)) {
				return null;
			}

			return new MsgEvent() { size = lenStr, timestamp = timestampStr, type = typeStr };
		}

		public static MessageReceivedEventArgs UnmarshallCsvMessageEventToMessageReceivedEventArgs(string line) {
			if (!LineParse(line, out var timestampStr, out var lenStr, out var typeStr)) {
				return null;
			}

			return new MessageReceivedEventArgs { _Len = lenStr, _Timestamp = timestampStr, _Type = typeStr };
		}

		private static bool LineParse(string line, out double timestampStr, out int lenStr, out string typeStr) {
			string[] parts = line.Split(DELIMITER);
			timestampStr = 0;
			lenStr = 0;
			typeStr = null;


			if (parts.Length != 3) {
				Log.Warn($"Skipping Line: {line} \nformat not right");

				return false;
			} else {
				if (!double.TryParse(parts[0], out timestampStr)) {
					Log.Warn($"Errro While Parsing Timestamp Double: {parts[0]}");
					return false;
				}

				if (!int.TryParse(parts[1], out lenStr)) {
					Log.Warn($"Errro While Parsing int Length: {parts[1]}");
					return false;
				}

				typeStr = parts[2].Trim();
				return true;
			}
		}
	}
}