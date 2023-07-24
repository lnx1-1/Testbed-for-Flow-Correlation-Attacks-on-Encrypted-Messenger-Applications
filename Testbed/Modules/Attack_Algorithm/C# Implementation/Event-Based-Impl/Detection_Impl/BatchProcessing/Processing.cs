using System;
using System.Collections.Generic;
using System.Linq;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.DataTypes;
using NLog;
using ProjectCommons;

namespace Event_Based_Impl.BatchProcessing {
    public static class Processing {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void Prepocessing(Loader.FilesWrapper wrapped) {
            wrapped.Zipped()
                .Where(file => file.netPackets.Count != 0 && file.channelTrace.Count != 0)
                .ToList().ForEach((file) => { SetRelativeTimeStamps(file.netPackets, file.channelTrace); });
        }


        public static void processOneFileV2(int intervalDurationInSeconds, List<MsgEvent> netEventList,
            List<MsgEvent> msgTraces,
            List<List<double>> matchRateList) {
            // Determine the Max Timestamp. To Calculate the Intervals
            double maxTimestamp =
                Math.Max(netEventList.Max(p => p.timestamp), msgTraces.Max(p => p.timestamp));

            //File into Intervals specified by intervalDurationInSeconds
            log.Debug("traceIntervals:");
            var traceIntervals = SplitIntoIntervals(msgTraces, intervalDurationInSeconds, maxTimestamp);
            log.Debug("netEventIntervals:");
            var netIntervals = SplitIntoIntervals(netEventList, intervalDurationInSeconds, maxTimestamp);


            var currMatchList = new List<double>();
            matchRateList.Add(currMatchList);
            var intervalTupelList = traceIntervals.Zip(netIntervals, (MsgIval, NetIval) => (MsgIval, NetIval));
            foreach (var interval in intervalTupelList) {
                if (interval.MsgIval.Count == 0) {
                    log.Debug($"Skipped interval.. No TraceEvents");
                    continue;
                }

                if (interval.NetIval.Count == 0 || interval.MsgIval.Count == 0) {
                    log.Debug("No trace or netevents after filtering. Skipping file");
                    continue;
                }

                double matchRate = CorrelationDetector.CalculateCorrelationRate(interval.MsgIval, interval.NetIval);
                currMatchList.Add(matchRate);
            }
        }
        
        public static void processOneFileV2(int intervalDurationInSeconds, List<MsgEvent> netEventList,
            List<MsgEvent> msgTraces,
            Dictionary<int, List<double>> matchRateList) {
            // Determine the Max Timestamp. To Calculate the Intervals
            double maxTimestamp =
                Math.Max(netEventList.Max(p => p.timestamp), msgTraces.Max(p => p.timestamp));
            
            
            //File into Intervals specified by intervalDurationInSeconds
            log.Debug("traceIntervals:");
            var traceIntervals = SplitIntoIntervals(msgTraces, intervalDurationInSeconds, maxTimestamp);
            log.Debug("netEventIntervals:");
            var netIntervals = SplitIntoIntervals(netEventList, intervalDurationInSeconds, maxTimestamp);


            var currMatchList = new List<double>();
            matchRateList.Add(intervalDurationInSeconds,currMatchList);
            var intervalTupelList = traceIntervals.Zip(netIntervals, (MsgIval, NetIval) => (MsgIval, NetIval));
            foreach (var interval in intervalTupelList) {
                if (interval.MsgIval.Count == 0) {
                    log.Debug($"Skipped interval.. No TraceEvents");
                    continue;
                }

                if (interval.NetIval.Count == 0 || interval.MsgIval.Count == 0) {
                    log.Debug("No trace or netevents after filtering. Skipping file");
                    continue;
                }

                double matchRate = CorrelationDetector.CalculateCorrelationRate(interval.MsgIval, interval.NetIval);
                currMatchList.Add(matchRate);
            }
        }

        public static void processOneFile(int intervalDurationInSeconds,
            (List<NetworkPacket> netPackets, List<MsgEvent> channelTrace) tupleFile,
            List<List<double>> matchRateList) {
            log.Debug($"Processing File for {intervalDurationInSeconds}s: Packets: [{tupleFile.netPackets.Count}], Traces: [{tupleFile.channelTrace.Count}");
            // Determine the Max Timestamp. To Calculate the Intervals
            double maxTimestamp =
                Math.Max(tupleFile.netPackets.Max(p => p.timestamp), tupleFile.channelTrace.Max(p => p.timestamp));

            //File into Intervals specified by intervalDurationInSeconds
            log.Debug("traceIntervals:");
            var traceIntervals = SplitIntoIntervals(tupleFile.channelTrace,
                intervalDurationInSeconds, maxTimestamp);
            log.Debug("netEventIntervals:");
            var netIntervals = SplitIntoIntervals(tupleFile.netPackets,
                intervalDurationInSeconds, maxTimestamp);


            var currMatchList = new List<double>();
            matchRateList.Add(currMatchList);
            var intervalTupelList = traceIntervals.Zip(netIntervals, (MsgIval, NetIval) => (MsgIval, NetIval));
            foreach (var interval in intervalTupelList) {
                if (interval.MsgIval.Count == 0) {
                    log.Debug($"Skipped interval.. No TraceEvents");
                    continue;
                }

                var traceEventList = EventExtractor.Get(interval.MsgIval)
                    .Sort()
                    .FilterEmptyEvents()
                    .ExtractBursts()
                    .FilterOutTextMessages()
                    .Results();

                var netEventList = EventExtractor.Get(interval.NetIval)
                    .Sort()
                    .ExtractBursts()
                    .Results();

                if (netEventList.Count == 0 || traceEventList.Count == 0) {
                    log.Debug("No trace or netevents after filtering. Skipping file");
                    continue;
                }

                double matchRate = CorrelationDetector.CalculateCorrelationRate(traceEventList, netEventList);
                currMatchList.Add(matchRate);
            }
        }

        public static void SetRelativeTimeStampsZeroBased<T, TR>(List<T> netPackets, List<TR> traceList)
            where T : ITimedSize where TR : ITimedSize {

            double netStartTime = netPackets.Min(p => p.timestamp);
            double traceStartTime = traceList.Min(p => p.timestamp);
            netPackets.ForEach(packet => packet.timestamp -= netStartTime);
            traceList.ForEach(ev => ev.timestamp -= traceStartTime);


            if (netPackets.Exists(p => p.timestamp < 0) || traceList.Exists(p => p.timestamp < 0)) {
                foreach (var networkPacket in netPackets.Where(p => p.timestamp < 0)) {
                    log.Warn("negative Timestamp:");
                    log.Warn(networkPacket);
                    log.Warn("Index: " + netPackets.FindIndex(p => p.timestamp < 0));
                }
                throw new ArgumentException("Event Ordering not Correct. Timestamp Missmatch!! Negativ Timestamp");
            }
        }
        

        /// <summary>
        /// Sets the Timestamps relative to the start of the first packet- or Trace Event 
        /// </summary>
        /// <param name="netPackets"></param>
        /// <param name="traceList"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TR"></typeparam>
        /// <exception cref="ArgumentException">If the Events are not ordered correctly</exception>
        public static void SetRelativeTimeStamps<T, TR>(List<T> netPackets, List<TR> traceList)
            where T : ITimedSize where TR : ITimedSize {
            double startTime = Math.Min(netPackets.Min(p => p.timestamp), traceList.Min(p => p.timestamp));
            netPackets.ForEach(packet => packet.timestamp -= startTime);
            traceList.ForEach(ev => ev.timestamp -= startTime);


            if (netPackets.Exists(p => p.timestamp < 0) || traceList.Exists(p => p.timestamp < 0)) {
                foreach (var networkPacket in netPackets.Where(p => p.timestamp < 0)) {
                    log.Warn("negative Timestamp:");
                    log.Warn(networkPacket);
                    log.Warn("Index: " + netPackets.FindIndex(p => p.timestamp < 0));
                }

                throw new ArgumentException("Event Ordering not Correct. Timestamp Missmatch!! Negativ Timestamp");
            }
        }

        /// <summary>
        /// When all the Events are not overlapping. The Offset is used to Align them again.
        /// </summary>
        /// <param name="netPackets"></param>
        /// <param name="traceList"></param>
        private static void ArrangeNotOverlappingEventLists(List<MsgEvent> netPackets, List<MsgEvent> traceList) {
            double startNetPackets = netPackets.Min(p => p.timestamp);
            double stopNetPackets = netPackets.Max(p => p.timestamp);
            double startTrace = traceList.Min(p => p.timestamp);
            double stopTraces = traceList.Max(p => p.timestamp);

            if (stopTraces < startNetPackets || stopNetPackets < startTrace) {
                double offset = Math.Abs(startTrace - startNetPackets);
                if (stopTraces < startNetPackets) {
                    netPackets.ForEach(p => p.timestamp -= offset);
                }
                else {
                    traceList.ForEach(p => p.timestamp -= offset);
                }
            }
        }

        /// <summary>
        /// Splits the inlist into Intervals
        /// </summary>
        /// <param name="inList"> the InList to Split</param>
        /// <param name="intervalDuration"> the Duration of each interval</param>
        /// <param name="maxRelTime">the max Realtive Timestamp.. Relativ to the minTimestamp</param>
        /// <returns>interval List</returns>
        public static List<List<T>> SplitIntoIntervals<T>(List<T> inList, int intervalDuration,
            double maxRelTime) where T : ITimedSize {
            int numOfIntervals = (int)Math.Ceiling((maxRelTime + 1.0) / intervalDuration);
            return SplitIntoIntervals(inList, numOfIntervals, intervalDuration);
        }


        public static List<List<T>> SplitIntoIntervals<T>(List<T> inList, int numberOfIntervals,
            int intervalDuration) where T : ITimedSize {
            var intervalList = new List<List<T>>();
            log.Debug($"Creating {numberOfIntervals} intervals.");
            for (int i = 0; i < numberOfIntervals; i++) {
                var currList = new List<T>();
                intervalList.Add(currList);
                int start = i * intervalDuration;
                int stop = (i + 1) * intervalDuration;
                foreach (var msgEvent in inList) {
                    if (msgEvent.timestamp >= start && msgEvent.timestamp < stop) {
                        currList.Add(msgEvent);
                    }
                }

                log.Debug($"	adding {currList.Count} events on interval {i}");
            }


            return intervalList;
        }
    }
}