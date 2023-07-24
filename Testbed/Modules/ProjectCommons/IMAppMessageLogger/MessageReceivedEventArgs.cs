using System;
using ProjectCommons;

namespace TelegramAPIPlugin {
	public class MessageReceivedEventArgs : EventArgs {
		public string _Path { get; set; } = "";

		/**
		 * Timestamp as Unix timestamp. Decimal point at Seconds based
		 */
		public double _Timestamp { get; set; }

		public double _TimeDelay { get; set; }

		/**
		 * Payload size of message in Bytes
		 */
		public long _Len { get; set; }

		/**
		 * Type of Message as String ?! .. text, audio, photo
		 */
		public string _Type { get; set; }

		public bool _isOutGoing { get; set; }

		public string _chatName { get; set; }

		public string _messageText { get; set; } = "";
		
		public string id { get; set; }
		public string remoteID { get; set; }

		public MessageReceivedEventArgs() {
		}

		public MessageReceivedEventArgs(double timestamp, long len, string type) : this() {
			_Timestamp = timestamp;
			_Len = len;
			_Type = type;
		}

		public MessageReceivedEventArgs(long len, double timestamp, string type, bool isOutGoing) : this() {
			_Len = len;
			_Timestamp = timestamp;
			_Type = type;
			_isOutGoing = isOutGoing;
		}

		public MessageReceivedEventArgs(long len, double timestamp, string type, bool isOutGoing, string messageText) :
			this(len, timestamp, type, isOutGoing) {
			_messageText = messageText;
		}

		public override bool Equals(object obj) {
			if (obj is MessageReceivedEventArgs arg) {
				return Equals(arg);
			} else {
				return base.Equals(obj);
			}
		}


		public MsgEvent AsMsgEvent() {
			return new MsgEvent() {
				size = (int)_Len, timestamp = _Timestamp, type = _Type, isOutgoing = _isOutGoing,
				messageText = _messageText, path = _Path, timeDelay = _TimeDelay
			};
		}

		protected bool Equals(MessageReceivedEventArgs other) {
			return _Timestamp.Equals(other._Timestamp) &&
			       _Len == other._Len &&
			       _Type == other._Type &&
			       _isOutGoing == other._isOutGoing &&
			       _Path == other._Path &&
			       _TimeDelay.Equals(other._TimeDelay);
		}

		public bool EqualsIgnoreDelay(MessageReceivedEventArgs other) {
			return _Timestamp.Equals(other._Timestamp) &&
			       _Len == other._Len &&
			       _Type == other._Type &&
			       _isOutGoing == other._isOutGoing &&
			       _Path == other._Path;
		}

		public override int GetHashCode() {
			return new { _Timestamp, _Len, _Type, _isOutGoing, _Path }.GetHashCode();
		}

		public override string ToString() {
			return string.Format($"tmstp: {_Timestamp}, len: {_Len}, Type: [{_Type}], _isOutgoing: {_isOutGoing}");
		}
	}
}