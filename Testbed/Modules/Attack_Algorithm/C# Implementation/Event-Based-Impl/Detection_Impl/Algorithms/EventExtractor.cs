using System;
using System.Collections.Generic;
using System.Linq;
using Event_Based_Impl.DataTypes;
using Event_Based_Impl.Utility_tools;
using NLog;
using ProjectCommons;

namespace Event_Based_Impl.Algorithms {
	/// <summary>
	/// Extracts Bursts fromPcap Files: Summarizes and Filters Relevant Packages and creates MsgEvents from That
	/// Aswell as Extracting Bursts from MessageTraces (summarizes Message Events from Message Traces which lay close together)
	/// Both 
	/// </summary>
	public static class EventExtractor {
		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();


		/// <summary>
		/// <b>Represents a Stream of MSgEvents or Networkpackets.</b> <br/>
		/// Wraps the EventExtractors Methods for Fluent API use
		/// </summary>
		public class EventStream {
			private List<MsgEvent> _msgEvent;
			private List<NetworkPacket> _networkPackets;


			private EventStream() {
			}

			public static EventStream Get(List<MsgEvent> msgEvents) {
				return new EventStream() { _msgEvent = msgEvents };
			}

			public static EventStream Get(List<NetworkPacket> networkPackets) {
				return new EventStream() { _networkPackets = networkPackets };
			}

			public EventStream Sort() {
				if (_msgEvent != null) {
					_msgEvent = _msgEvent.OrderBy(p => p.timestamp).ToList();
				}

				if (_networkPackets != null) {
					_networkPackets = _networkPackets.OrderBy(p => p.timestamp).ToList();
				}

				return this;
			}

			public EventStream ExtractBursts() {
				if (_msgEvent != null) {
					_msgEvent = EventExtractor.extractBurstEvents(_msgEvent);
				}

				if (_networkPackets != null) {
					_msgEvent = EventExtractor.extractBurstEvents(_networkPackets);
				}

				return this;
			}

			public EventStream FilterOutTextMessages() {
				_msgEvent = EventExtractor.FilterOutTextMessages(_msgEvent);
				return this;
			}


			public EventStream mergeEventsByT_e() {
				_msgEvent = EventExtractor.mergeEventsByT_e(_msgEvent);
				return this;
			}

			public EventStream FilterEmptyEvents() {
				_msgEvent = EventExtractor.FilterEmptyEvents(_msgEvent);
				return this;
			}

			public List<NetworkPacket> NetResults() {
				return _networkPackets;
			}

			public List<MsgEvent> Results() {
				if (_msgEvent == null) {
					throw new NotSupportedException(
						"You requested the msgEvent Result. But this is not set.. if you got this stream from NetworkPackets, try extracting the Network events first");
				}
				return _msgEvent;
			}
		}


		/// <summary>
		/// Returns the Fluent API EventStream Obj
		/// </summary>
		/// <param name="netEvents">the List to Populate the EventStream with</param>
		/// <returns>A EventStream that allows fluent API use</returns>
		public static EventStream Get(List<NetworkPacket> netEvents) {
			return EventStream.Get(netEvents);
		}
		/// <summary>
		/// Returns the Fluent API EventStream Obj
		/// </summary>
		/// <param name="msgEvents">the List to Populate the EventStream with</param>
		/// <returns>A EventStream that allows fluent API use</returns>
		public static EventStream Get(List<MsgEvent> msgEvents) {
			return EventStream.Get(msgEvents);
		}

		/// <summary>
		/// Filters all Packets that are smaller 40. \ 
		/// 
		/// Summs packets that have a IPD smaller then 0.5seconds
		/// and Returns the Extracted Events
		/// And i think the Burst needs to be at least the size of MTU..  Why that ?? 
		/// </summary>
		/// <param name="msgEvents">The list msgEvents to Extract from</param>
		/// <returns>A List of Burst Events</returns>
		/// <exception cref="NullReferenceException">When there is a Null Packet</exception>
		public static List<MsgEvent> extractBurstEvents(List<MsgEvent> msgEvents) {
			List<UnextractedDataPacket> inList =
				new List<UnextractedDataPacket>(msgEvents.Select(inEv => new UnextractedDataPacket(inEv)));
			return ExtractBurstEvents(inList);
		}

		/// <summary>
		/// Removes all Events with `type == "text"`
		/// </summary>
		/// <param name="inputEvents">The List to Filter from</param>
		/// <returns>a List with all Text events removed</returns>
		public static List<MsgEvent> FilterOutTextMessages(List<MsgEvent> inputEvents) {
			var outList = inputEvents.Where((ev) => !ev.type.Equals("text")).ToList();
			log.Debug($"filtered txt msg. remaining: {outList.Count}");
			return outList;
		}


		/// <summary>
		/// MsgTraces müssen auch zusammengefasst werden
		/// For each burst, the adversary extracts a SIM event, Two SIM messages sent with an IMD less than t_e are extracted as one event.
		/// Similarly, the adversary combines events closer than t_e when capturing them from the target channel.
		/// </summary>
		/// <param name="msgEvents">Message Traces</param>
		/// <returns>Merged Traces as Events</returns>
		public static List<MsgEvent> mergeEventsByT_e(List<MsgEvent> msgEvents) {
			List<MsgEvent> outList = new List<MsgEvent>();
			if (msgEvents.Count == 0) {
				log.Error("msgEvents doensnt contain messages");
				return outList;
			}

			double lastEvTimeStamp = 0;
			foreach (var ev in msgEvents) {
				if ((ev.timestamp - lastEvTimeStamp) < Config.T_e) {
					//Event Merged weil timediff < T_e
					outList.Last().size += ev.size;
					lastEvTimeStamp = ev.timestamp;
					log.Debug($"Merged event {ev}");
				} else {
					lastEvTimeStamp = ev.timestamp;
					outList.Add(ev);
				}
			}

			return outList;
		}

		/// <summary>
		/// Removes Events with size == 0
		/// </summary>
		/// <param name="msgEvents">The list of Events to Filter</param>
		/// <returns>The list with where all events with size == 0 are Removed</returns>
		public static List<MsgEvent> FilterEmptyEvents(List<MsgEvent> msgEvents) {
			var outList = msgEvents.Where(ev => ev.size > 0).ToList();
			log.Debug($"Filtering event List. Removing {msgEvents.Count - outList.Count}");
			return outList;
		}

		/// <summary>
		/// Filters all Packets that are smaller 40. \ 
		/// 
		/// Summs packets that have a IPD smaller then 0.5seconds
		/// and Returns the Extracted Events
		/// </summary>
		/// <param name="packetLists">The list of Packets to Extract from</param>
		/// <returns>A List of Burst Events</returns>
		/// <exception cref="NullReferenceException">When there is a Null Packet</exception>
		public static List<MsgEvent> extractBurstEvents(List<NetworkPacket> packetLists) {
			List<UnextractedDataPacket> inList =
				new List<UnextractedDataPacket>(packetLists.Select(p => new UnextractedDataPacket(p)));
			return ExtractBurstEvents(inList);
		}

		/// <summary>
		/// Does the Extraction Itself. Explanation: <see cref="extractBurstEvents(System.Collections.Generic.List{ProjectCommons.MsgEvent})"/>
		/// </summary>
		/// <param name="packetLists"></param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		private static List<MsgEvent> ExtractBurstEvents(List<UnextractedDataPacket> packetLists) {
			log.Debug($"Starting Event Extraction from list with {packetLists.Count} packets");
			var burstEventList = new List<MsgEvent>();
			var filteredPackets = FilterPackets(packetLists);
			if (filteredPackets.Count <= 0) {
				return burstEventList; //Return Empty Event List..
			}

			double lastPacketTimestamp = filteredPackets[0].timestamp;
			int burstSum = 0;
			foreach (var packet in filteredPackets) {
				if (packet == null) {
					throw new NullReferenceException("Packet is Null!!");
				}

				if ((packet.timestamp - lastPacketTimestamp) < Config.T_e) {
					//gehört zum derzeitigen Burst
					burstSum += packet.size; //Addiere Size zum Burst
				} else {
					if (burstSum > Config.MTU) {
						saveBurst(burstSum, lastPacketTimestamp, burstEventList);
					}

					burstSum = packet.size;
				}

				lastPacketTimestamp = packet.timestamp;
			}

			if (burstSum > Config.MTU) {
				// Für wenn wir uns am Ende des Files befinden und wir aktuell in einem Burst sind
				saveBurst(burstSum, lastPacketTimestamp, burstEventList);
			}

			log.Debug($"Extracted {burstEventList.Count} Events");
			return burstEventList;
		}

		private static void saveBurst(int burstSum, double lastPacketTimestamp, List<MsgEvent> burstEventList) {
			//Wir sind in einem Burst und wollen diesen Speichern
			MsgEvent newEvent = new MsgEvent {
				size = burstSum,
				timestamp = lastPacketTimestamp
			};
			burstEventList.Add(newEvent);
		}

		/// <summary>
		/// Filters Packets that are Smaller than the {Config.MAGIC_relevantPacketSize}
		/// </summary>
		/// <param name="packetLists"></param>
		/// <returns></returns>
		private static List<UnextractedDataPacket> FilterPackets(List<UnextractedDataPacket> packetLists) {
			var filteredPackets =
				packetLists.Where(packet => packet.size > Config.relevantPacketSize_MAGICNUM).ToList();
			log.Debug(
				$"Filtered out {packetLists.Count - filteredPackets.Count} elements. {filteredPackets.Count} total remaining elements");
			return filteredPackets;
		}

		/// <summary>
		/// Wrapper type for MsgEvents and Network packets..
		/// Both types get Converted into UnextracktedDataPackets to avoid code duplication
		/// </summary>
		private class UnextractedDataPacket : MsgEvent {
			public UnextractedDataPacket(NetworkPacket packet) {
				size = packet.size;
				timestamp = packet.timestamp;
				type = "NetPacket";
			}

			public UnextractedDataPacket(MsgEvent ev) : base(ev) {
			}
		}
	}
}