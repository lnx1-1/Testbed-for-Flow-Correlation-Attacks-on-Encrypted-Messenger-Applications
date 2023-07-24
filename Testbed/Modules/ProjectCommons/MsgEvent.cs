using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectCommons {
	public class MsgEvent : ITimedSize {
		public int size { get; set; } = -1; //Size in Bytes
		public static string[] MsgTypes = { "audio", "video", "photo", "text" };
		
		[JsonPropertyName("timeDelay")] public double timeDelay { get; set; } = 0;

		[JsonPropertyName("timestamp")] public double timestamp { get; set; } = -1;

		public string type { get; set; } = "MsgEvent";
		public int id { get; set; } = 0;
		public bool isOutgoing { get; set; }

		public string messageText { get; set; } = "";

		public string path { get; set; } = "";

		public override string ToString() {
			return
				$"MsgEvent(Tstamp: {timestamp}, size: {size}, id: {id}, type: {type}, msgText: {messageText.Substring(0, Math.Min(messageText.Length, 15))})";
		}

		public bool EqualsIgnoreTimeAndPath(MsgEvent other) {
			return size.Equals(other.size) &&
			       type.Equals(other.type) &&
			       id.Equals(other.id) &&
			       isOutgoing == other.isOutgoing &&
			       messageText.Equals(other.messageText);
		}

		public bool EqualsIgnoreDelay(MsgEvent other) {
			return size == other.size &&
			       timestamp.Equals(other.timestamp) &&
			       type == other.type &&
			       id == other.id &&
			       path == other.path;
		}

		public override bool Equals(object obj) {
			if (obj is MsgEvent ev) {
				return Equals(ev);
			}


			return false;
		}

		private bool Equals(MsgEvent other) {
			return size == other.size &&
			       timeDelay.Equals(other.timeDelay) &&
			       timestamp.Equals(other.timestamp) &&
			       type == other.type &&
			       id == other.id &&
			       path == other.path;
		}


		public override int GetHashCode() {
			return new { size, timeDelay, timestamp, type, id }.GetHashCode();
		}

		public MsgEvent() {
			path = "";
		}

		public MsgEvent(MsgEvent ev) {
			this.size = ev.size;
			this.timestamp = ev.timestamp;
			this.type = ev.type;
			this.id = ev.id;
			this.messageText = ev.messageText;
		}
	}
}