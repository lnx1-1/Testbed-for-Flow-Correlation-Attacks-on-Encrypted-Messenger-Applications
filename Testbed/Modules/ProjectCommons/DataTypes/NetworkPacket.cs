using System.Text.Json.Serialization;
using ProjectCommons;

namespace Event_Based_Impl.DataTypes {
	public class NetworkPacket : ITimedSize {
		[JsonPropertyName("to_amazon")]
		public bool toGateway { get; set; }
		public string src { get; set; } // Source IP 
		public string dst { get; set; } // Destination IP
		public int size { get; set; }
		
		/// <summary>
		/// As <b>Seconds</b>
		/// </summary>
		public double timestamp { get; set; }
		public int protocol { get; set; }
		[JsonPropertyName("s_port")]
		public int SrcPort { get; set; } //Source Port
		
		[JsonPropertyName("d_port")]
		public int DstPort { get; set; } // Destination Port

		public override string ToString() {
			return $"packet(size:{size}, srcIP:{src}, srcPort:{SrcPort}, tmstp: {timestamp})";
		}

		
	}
}