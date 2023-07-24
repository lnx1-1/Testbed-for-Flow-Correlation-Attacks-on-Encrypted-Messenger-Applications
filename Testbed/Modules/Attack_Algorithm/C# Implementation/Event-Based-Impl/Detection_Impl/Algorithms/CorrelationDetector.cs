using System;
using System.Collections.Generic;
using System.Linq;
using Event_Based_Impl.Utility_tools;
using NLog;
using ProjectCommons;

namespace Event_Based_Impl.Algorithms {
	public static class CorrelationDetector {
		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		private static double[] GetdriftWindow(Config.CorrelationWindowSettings settings) {
			var windowDrift = new double[settings.numberOfSteps];
			for (int i = settings.start; i < (settings.numberOfSteps + settings.start); i++) {
				windowDrift[i + Math.Abs(settings.start)] = settings.stepsize * i;
			}

			return windowDrift;
		}

		public static double CalcCorrelationRateWitSlidingWindow(List<MsgEvent> channelTraces,
			List<MsgEvent> observedUserPackets) {
			return CalcCorrelationRateWitSlidingWindow(channelTraces, observedUserPackets,
				Config.CorrelationWindowSettings.GetDefault());
		}

		public static double CalcCorrelationRateWitSlidingWindow(List<MsgEvent> channelTraces,
			List<MsgEvent> observedUserPackets, Config.CorrelationWindowSettings settings) {
			var windowDrift = GetdriftWindow(settings);
			List<double> matchrates = new List<double>();
			foreach (double drift in windowDrift) {
				var moddedChannelTraces = channelTraces.Select(ev => new MsgEvent()
					{ id = ev.id, size = ev.size, timestamp = ev.timestamp + drift, type = ev.type }).ToList();
				double match = CalculateCorrelationRate(moddedChannelTraces, observedUserPackets);
				matchrates.Add(match);
			}

			log.Info($"Best Matchrate at drift: {windowDrift[matchrates.IndexOf(matchrates.Max())]}");
			return matchrates.Max();
		}

		/// <summary>
		/// For Each ChannelEvent (ChannelTrace) all the corresponding NetworkEvents (<paramref name="observedUserPackets"/>) will be count that match the 
		///	condition: <c> timestamp_difference &lt; </c><see cref="Config.DELTA">Delta</see> <c> AND size_difference &lt; (</c><see cref="Event_Based_Impl.Utility_tools.Config.GAMMA">Gamma</see><c> * trace.size) </c>
		/// <br/>
		/// Finally, the adversary calculates the ratio of the matched events as <c>r = k/n</c> where <br/> <c>k</c> is number of matched events and <b/><c>n</c> is the total number of events in the target channel
		/// </summary>
		/// <param name="channelTraces">The Extracted Channel Events e.g. new TextMessageReceived (Logged through IM App)</param>
		/// <param name="observedUserPackets">Extracted NetworkEvents. (Observed by Sniffing the NetworkTraffic of the TargetedUser.. (Logging the NetworkPackets)</param>
		/// <returns>The Resulting MatchRatio 0-1 </returns>
		public static double
			CalculateCorrelationRate(List<MsgEvent> channelTraces, List<MsgEvent> observedUserPackets) {
			int matches = 0;
			int nonMatch = 0;
			List<MsgEvent> matchedEvs = new List<MsgEvent>();
			foreach (var trace in channelTraces) {
				var matchingEvs = observedUserPackets
					.Where(packetEv => !matchedEvs.Contains(packetEv))
					.Where(packetEv => (Math.Abs(packetEv.timestamp - trace.timestamp) < Config.DELTA))
					.Where(packetEv => (Math.Abs(packetEv.size - trace.size) < (Config.GAMMA * trace.size))).ToList();

				if (matchingEvs.Count > 1) {
					//bringt scheinbar nichts an Verbesserung..
					var bestMatchingEv = GetBestMatchingEvent(matchingEvs, trace);
					matches++;
					matchedEvs.Add(bestMatchingEv);
				} else if (matchingEvs.Count == 1) {
					matches++;
					matchedEvs.Add(matchingEvs.First());
				} else {
					nonMatch++;
				}
			}

			log.Debug(
				$"From {channelTraces.Count} msgTraces and {observedUserPackets.Count} burstEvents. - Matches Found: {matches} - MatchRatio: {(double)matches / channelTraces.Count}");
			// log.Info($"{matches} matches where Detected. If Multiple Matches woul be count, {sumMatches} would it be");
			// log.Info(
			//     $"Calculating MatchRatio: {matches}/{channelTraces.Count}={(double)matches / channelTraces.Count}");
			if (channelTraces.Count != matches + nonMatch) {
				throw new ArithmeticException("Count Missmatch at Matchrate..");
			}

			if (channelTraces.Count == 0) {
				return 0;
			}

			return (double)matches / channelTraces.Count;
		}


		private static MsgEvent GetBestMatchingEvent(List<MsgEvent> matchingEvs, MsgEvent currTestingTrace) {
			return matchingEvs.OrderBy(packetEv => {
				double sizeDiff = (Math.Abs(packetEv.size - currTestingTrace.size) /
				                   (double)(Config.GAMMA * currTestingTrace.size));
				double timeDiff = (Math.Abs(packetEv.timestamp - currTestingTrace.timestamp) / Config.DELTA);
				return sizeDiff + timeDiff;
			}).Last();
		}
	}
}