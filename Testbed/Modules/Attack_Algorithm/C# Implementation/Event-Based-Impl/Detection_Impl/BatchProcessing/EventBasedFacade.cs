using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.DataTypes;
using NLog;
using ProjectCommons;

namespace Event_Based_Impl.BatchProcessing {
    public class EventBasedFacade {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static async Task<List<double>> CalcTruePositiveBatch(int numOfFiles, int intervalDurationInSeconds) {
            return await CalcTruePositiveBatch(PathWrapper.pcapTelegramFolder, PathWrapper.MsgTrace_TelegramFolder,
                numOfFiles,
                intervalDurationInSeconds);
        }

        public static async Task<List<double>> CalcTruePositiveSingleFile(string traceFilePath, string pcapFilePath, int intervalDurationInSeconds) {
            var matchRateList = new List<List<double>>();

            var fileWrapper = await Loader.LoadFilesAsync(new List<string>(){pcapFilePath}, new List<string>() {traceFilePath});

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();


            foreach (var tupleFile in fileWrapper.Zipped()) {
                Processing.processOneFile(intervalDurationInSeconds, tupleFile, matchRateList);
            }

            PrintMatchRates(matchRateList);

            return matchRateList.SelectMany(x => x).ToList();
        }
        
        public static async Task<List<double>> CalcTruePositiveBatch(string traceFolderPath, string pcapFolderPath,
            int numOfFiles, int intervalDurationInSeconds) {
            var matchRateList = new List<List<double>>();

            var fileWrapper = await Loader.LoadFilesAsync(pcapFolderPath, traceFolderPath, numOfFiles);

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();


            foreach (var tupleFile in fileWrapper.Zipped()) {
                Processing.processOneFile(intervalDurationInSeconds, tupleFile, matchRateList);
            }

            PrintMatchRates(matchRateList);

            return matchRateList.SelectMany(x => x).ToList();
        }

        public static async Task<List<double>> CalcTruePositiveBatchV2(string traceFolderPath, string pcapFolderPath,
            int numOfFiles, int intervalDurationInSeconds) {
            var matchRateList = new List<List<double>>();

            var fileWrapper = await Loader.LoadFilesAsync(pcapFolderPath, traceFolderPath, numOfFiles);

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();


            foreach (var tupleFile in fileWrapper.Zipped()) {
                var exractedPackets = EventExtractor.extractBurstEvents(tupleFile.netPackets);
                var filteredTraces = EventExtractor.Get(tupleFile.channelTrace)
                    .FilterOutTextMessages()
                    .Results();

                if (exractedPackets.Count == 0 || filteredTraces.Count == 0) {
                    continue;
                }

                Processing.processOneFileV2(intervalDurationInSeconds, exractedPackets, filteredTraces, matchRateList);

                // Processing.processOneFile(intervalDurationInSeconds, tupleFile, matchRateList);
            }

            PrintMatchRates(matchRateList);

            return matchRateList.SelectMany(x => x).ToList();
        }
        
        public static void PrintMatchRates(Dictionary<int, List<double>> matchRateList) {
            foreach (var entry in matchRateList) {
                log.Info($"ObservationLen: {entry.Key}");
                for (int i = 0; i < entry.Value.Count; i++) {
                    log.Info($"     Interval {i} - Matchrate [{entry.Value[i]}");
                }
                double average = entry.Value.Average();
                log.Info($"Average Matchrate: {average}");
            }
        }
        
        public static void PrintMatchRates(List<List<double>> matchRateList) {
            for (int i = 0; i < matchRateList.Count; i++) {
                log.Info($"------- File {i} ------");
                for (int j = 0; j < matchRateList[i].Count; j++) {
                    log.Info($"-	Interval {j}	: Rate: {matchRateList[i][j]} -");
                }
            }

            double average = matchRateList.SelectMany(x => x).Average();
            log.Info($"Average Matchrate: {average}");
        }

        public static async Task<List<double>> FalsePositiveBatchV2(string traceFolder, string pcapFolder,
            int numOfLoadFiles,
            int intervalDurationInSeconds) {
            var fpMatchrate = new List<List<double>>();
            var fileWrapper = await Loader.LoadFilesAsync(pcapFolder, traceFolder, numOfLoadFiles);

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();

            var traceFileList = fileWrapper.TraceFiles;
            var packetFileList = fileWrapper.NetworkFiles;

            for (int t = 0; t < traceFileList.Count; t++) {
                for (int p = 0; p < packetFileList.Count; p++) {
                    if (t == p) {
                        continue;
                    }

                    log.Debug($"-----------------------");
                    log.Debug($"------- File {p} x {t} ------");

                    var exractedPackets = EventExtractor.extractBurstEvents(packetFileList[p]);
                    var filteredTraces = EventExtractor.Get(traceFileList[t])
                        .FilterOutTextMessages()
                        .Results();

                    if (exractedPackets.Count == 0 || filteredTraces.Count == 0) {
                        continue;
                    }
                    
                    Processing.processOneFileV2(intervalDurationInSeconds, exractedPackets, filteredTraces,
                        fpMatchrate);
                }
            }

            return fpMatchrate.SelectMany(x => x).ToList();
        }


        public static async Task<List<double>> FalsePositiveBatch(string traceFolder, string pcapFolder,
            int numOfLoadFiles,
            int intervalDurationInSeconds) {
            var fpMatchrate = new List<List<double>>();
            var fileWrapper = await Loader.LoadFilesAsync(pcapFolder, traceFolder, numOfLoadFiles);

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();

            var traceFileList = fileWrapper.TraceFiles;
            var packetFileList = fileWrapper.NetworkFiles;

            for (int t = 0; t < traceFileList.Count; t++) {
                for (int p = 0; p < packetFileList.Count; p++) {
                    if (t == p) {
                        continue;
                    }

                    log.Debug($"-----------------------");
                    log.Debug($"------- File {p} x {t} ------");

                    Processing.processOneFile(intervalDurationInSeconds, (packetFileList[p], traceFileList[t]),
                        fpMatchrate);
                }
            }

            return fpMatchrate.SelectMany(x => x).ToList();
        }

        public static async Task<List<double>> FalsePositiveBatch(int numOfLoadFiles, int intervalDurationInSeconds) {
            return await FalsePositiveBatch(PathWrapper.MsgTrace_TelegramFolder, PathWrapper.pcapTelegramFolder,
                numOfLoadFiles, intervalDurationInSeconds);
        }

        
    }
}