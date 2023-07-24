using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Event_Based_Impl.DataTypes;

namespace Event_Based_Impl {
	/// <summary>
	/// Represents a Parsed JSON PCAP file. Containing NetworkPackets
	/// </summary>
	public static class JsonPacketCaptureParser {
		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		public class PacketCapture {
			public List<NetworkPacket> _packets { get; private set; }
			public PacketCapture(List<NetworkPacket> capture) {
				if (capture == null || capture.Count == 0) {
					throw new ArgumentNullException(nameof(capture), "ist null oder Leer");
				}
				_packets = new List<NetworkPacket>(capture);
			}
		}

		/// <summary>
		/// Reads a pcap JSON file and returns a List Represantation of all Packets as NetworkPackets
		/// </summary>
		/// <param name="path">The Path to read the JSON from</param>
		/// <returns>a Task that provides an NetworkPacket List</returns>
		public static async Task<List<NetworkPacket>> ReadInJSONPackets(string path) {
			using FileStream fileStream = File.OpenRead(path);
			List<NetworkPacket> packetList = await JsonSerializer.DeserializeAsync<List<NetworkPacket>>(fileStream);
			return packetList;
		}

		public static async Task<PacketCapture> LoadJsonPackets(string JSONpath) {
			using FileStream fileStream = File.OpenRead(JSONpath);
			log.Info($"Loading JSON Capture File with {fileStream.Length} bytes ");

			List<NetworkPacket> packetList = await JsonSerializer.DeserializeAsync<List<NetworkPacket>>(fileStream);
			log.Info($"Finished Loading {packetList?.Count} packets");
			return new PacketCapture(packetList);
		}
	}
}