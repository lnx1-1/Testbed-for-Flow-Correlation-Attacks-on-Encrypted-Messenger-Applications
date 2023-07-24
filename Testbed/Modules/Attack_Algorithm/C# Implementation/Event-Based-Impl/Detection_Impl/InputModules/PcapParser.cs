using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Event_Based_Impl.DataTypes;
using NLog;
using OpenTap;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Event_Based_Impl.InputModules {
	/// <summary>
	/// Reads in .PCAP files or Folders containing multiple .PCAP files
	/// Only IPv4 or IPV6 Packets are read. With UDP or TCP protocol
	/// The resulting Size in the <see cref="Event_Based_Impl.DataTypes.NetworkPacket">NetworkPacket</see> is only the IP Packet size. 
	/// </summary>
	public static class PcapParser {
		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		public static Task<List<NetworkPacket>> ParsePcapFolderAsync(string folderpath) {
			string[] filePaths = Directory.GetFiles(folderpath);
			List<NetworkPacket> outList = new List<NetworkPacket>();

			List<Task<List<NetworkPacket>>> taskList = new List<Task<List<NetworkPacket>>>();

			foreach (string filePath in filePaths) {
				var filePacketList = ParsePcapFileAsync(filePath);
				taskList.Add(filePacketList);
			}

			return taskList.Aggregate(async (eleA, eleB) => (await eleA).Concat(await eleB).ToList());
			;
		}

		public static List<NetworkPacket> ParsePcapFolder(string folderpath) {
			string[] filePaths = Directory.GetFiles(folderpath);
			log.Info($"Parsing PCAP Folder, containing {filePaths.Length} Files. This may take some time");
			return ParsePcapFiles(filePaths.ToList()).GetAwaiter().GetResult();
		}

		public static async Task<List<NetworkPacket>> ParsePcapFiles(List<string> filePaths) {
			var outList = new List<NetworkPacket>();
			foreach (string filePath in filePaths) {
				TapThread.ThrowIfAborted();
				var netPacketList = await ParsePcapFileAsync(filePath);
				outList = outList.Concat(netPacketList).ToList();
			}

			return outList;
		}


		public static Task<List<NetworkPacket>> ParsePcapFileAsync(string filePath) {
			log.Debug($"Loading PCAP file: {filePath} ");
			if (!File.Exists(filePath)) {
				log.Error("File does not Exist.. If you are trying to open a folder, select <ParsePcapFolder> instead");
				return null;
			}

			ICaptureDevice captureDevice;
			try {
				captureDevice = new CaptureFileReaderDevice(filePath);
				captureDevice.Open();
			} catch (Exception e) {
				log.Error(e, $"Error while opening pcap File Device\n {e.Message}");
				throw;
			}

			List<NetworkPacket> netPacketList = RegisterNetworkPacketReader(captureDevice);


			log.Debug($"Starting FileDevice Capture");
			captureDevice.Capture();
			captureDevice.Close();

			return Task.FromResult(netPacketList);
		}

		private static List<NetworkPacket> RegisterNetworkPacketReader(ICaptureDevice captureDevice) {
			log.Debug($"Registering NetPacketReader on Packet Device");
			List<NetworkPacket> packets = new List<NetworkPacket>();

			captureDevice.OnPacketArrival += (sender, capture) => {
				RawCapture rawCapture = capture.GetPacket();
				Packet packet = PacketDotNet.Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

				if (LinkLayers.Ethernet != rawCapture.LinkLayerType) {
					log.Warn("Packet is kein Ethernet Packet - Skipping");
					return;
				}

				EthernetPacket ethernetPacket = packet.Extract<EthernetPacket>();

				if (ethernetPacket == null) {
					log.Error("EthernetPacket is null..");
					return;
				}

				try {
					if (ethernetPacket.Type != EthernetType.IPv4 && ethernetPacket.Type != EthernetType.IPv6) {
						log.Warn("Ethernet Packet doesnt Contain IPv4 or IPv6 payload.. Skipping");
						return;
					}
				} catch (IndexOutOfRangeException e) {
					log.Error("The Provided PCAP file seems Malformed.. \nCheck for Malformed .pcap File!!!");
					log.Error(e);
					throw;
				}


				IPPacket ipPacket = packet.Extract<IPPacket>();


				int capDestPort = 0;
				int capSrcPort = 0;


				switch (ipPacket.Protocol) {
					case ProtocolType.Tcp: {
						TcpPacket tcpPacket = ipPacket.Extract<TcpPacket>();
						capDestPort = tcpPacket.DestinationPort;
						capSrcPort = tcpPacket.SourcePort;
						break;
					}
					case ProtocolType.Udp: {
						UdpPacket udpPacket = ipPacket.Extract<UdpPacket>();
						capDestPort = udpPacket.DestinationPort;
						capSrcPort = udpPacket.SourcePort;
						break;
					}
					default: {
						log.Warn("IP packet has no TCP or UDP payload - Skipping");
						return;
					}
				}

				var networkPacket = CraftNetworkPacket(rawCapture, ipPacket, capSrcPort, capDestPort, ethernetPacket);
				packets.Add(networkPacket);
			};
			return packets;
		}

		private static NetworkPacket CraftNetworkPacket(RawCapture rawCapture, IPPacket ipPacket, int capSrcPort,
			int capDestPort, EthernetPacket ethernetPacket) {
			NetworkPacket networkPacket = new NetworkPacket {
				timestamp = Decimal.ToDouble(rawCapture.Timeval
					.Value), //As Seconds based Decimal number (Hat nachkommastellen aber ist Sekunde)
				src = ipPacket.SourceAddress.ToString(),
				dst = ipPacket.DestinationAddress.ToString(),
				protocol = (int)rawCapture.LinkLayerType,
				SrcPort = capSrcPort,
				DstPort = capDestPort,
				size = ethernetPacket.PayloadPacket
					.TotalPacketLength //! Note.. Ist Es wird die IP Packet Size verwendet. Ohne Ethernet Packet Length
			};
			// log.Debug($"Packet Created.. Len: {networkPacket}");
			return networkPacket;
		}
	}
}