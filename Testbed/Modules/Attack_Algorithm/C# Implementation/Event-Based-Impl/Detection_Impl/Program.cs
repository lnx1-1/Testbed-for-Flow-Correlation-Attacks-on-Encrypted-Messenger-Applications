using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.BatchProcessing;
using Event_Based_Impl.DataTypes;
using Event_Based_Impl.InputModules;
using Event_Based_Impl.Utility_tools;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;

namespace Event_Based_Impl {
    public class Program {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void Main(string[] args) {
            if (args.Length != 1) {
                Console.WriteLine("Usage:\n detector <packetsAsJSON>");
                // return;
            }
            else {
                Console.WriteLine("Starting Detector");
            }

            log.Info("Startup");

            DateTime start = DateTime.Now;

            // await DoWork();
            //DoAnalyze();
            // DoTestWork_EventExtraction();
            //TP_FP_IntegrationFunc();

            var rootFolder = @"D:\DataUni"; //"E:\\Datasets_uniPaper";
            var prePacketsFolder = Path.Combine(rootFolder, "converted\\telegram\\packets_relTime");
            var preTracesFolder = Path.Combine(rootFolder, "converted\\telegram\\traces_relTime");
            var outFile = Utilitys.GetAbsolutePath("Results/testResult.json");
            var scriptPath =
                Utilitys.GetAbsolutePath("attack algorithms/testCode/Event-Based-IM-V2/DetectionIM_v_2.py");


            if (!Directory.Exists(prePacketsFolder)) {
                Directory.CreateDirectory(prePacketsFolder);
            }

            if (!Directory.Exists(preTracesFolder)) {
                Directory.CreateDirectory(preTracesFolder);
            }

            

            // DoPreprocessingForPythonCode(prePacketsFolder, preTracesFolder).GetAwaiter().GetResult();

            // CmdExec.RunPythonScript(scriptPath, prePacketsFolder, preTracesFolder, outFile);


            // var result = DetectionResult.LoadFromFile(outFile).GetAwaiter().GetResult();
            // log.Info("Result:");
            // log.Info(result.ToString());

            // var tpTask = EventBasedFacade.CalcTruePositiveBatchV2(PathWrapper.MsgTrace_TelegramFolder,
            //     PathWrapper.pcapTelegramFolder, 100, 1800);
            // var fpTask = EventBasedFacade.FalsePositiveBatchV2(PathWrapper.MsgTrace_TelegramFolder,
            //     PathWrapper.pcapTelegramFolder, 100, 1800);
            //
            // var tp = tpTask.GetAwaiter().GetResult();
            // var fp = fpTask.GetAwaiter().GetResult();
            // log.Info($"TP Avg: [{tp.Average()}]");
            // log.Info($"FP Avg: [{fp.Average()}]");


            // var task = ConvertPcapToCsv();
            // ConvertTracesToCsv();
            // task.GetAwaiter().GetResult();

            // log.Info("--------------------");
            // log.Info($"FP [{FP.Average()}]");
            // log.Info($"TP [{TP.Average()}]");
            // log.Info("--------------------");
            TimeSpan diff = DateTime.Now - start;
            Console.WriteLine("Work Took:{0} Seconds", diff);
        }

        private static void TP_FP_IntegrationFunc() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("TP; IntervalLen;\n");

            for (int i = 1; i <= 3; i++) {
                int intervalDurationInSeconds = 10 * i * 60; // 30 Minuten
                int numOfLoadFiles = 5;

                var TP = EventBasedFacade.CalcTruePositiveBatch(numOfLoadFiles, intervalDurationInSeconds).GetAwaiter()
                    .GetResult();
                var FP = EventBasedFacade.FalsePositiveBatch(numOfLoadFiles, intervalDurationInSeconds).GetAwaiter()
                    .GetResult();
                // TrueFalsePositiveWhole().GetAwaiter().GetResult();

                foreach (var tpVal in TP) {
                    stringBuilder.Append($"{tpVal}; {intervalDurationInSeconds}\n");
                }
            }

            var path = Path.Combine(PathWrapper.OutFiles.ResultsFolder, "TP_FP_Values.csv");
            log.Info($"Wrote out File to {path}");
            File.WriteAllText(path, stringBuilder.ToString());
        }

        private static async void DoAnalyze() {
            List<NetworkPacket> packetLists = await PcapParser.ParsePcapFileAsync(PathWrapper.pcapTelegramFile);
            for (int i = 0; i < 20; i++) {
                log.Info(packetLists[i].size);
            }
        }


     
      



        private static async Task DoPreprocessingForPythonCode(string packetsOutFolder, string tracesOutFolder) {
            var fileWrapper = await Loader.LoadFilesAsync(100);

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();


            int fileCounter = 0;
            foreach (var netFile in fileWrapper.NetworkFiles) {
                // var exractedList = EventExtractor.extractBurstEvents(netFile);
                string path = Path.Combine(packetsOutFolder, $"user-{fileCounter}.csv");
                File.WriteAllLines(path, netFile.Select(p => p.timestamp + ";" + p.size));
                log.Info($"wrote to path: {path}");
                fileCounter++;
            }

            fileCounter = 0;
            foreach (var traceFile in fileWrapper.TraceFiles) {
                var filteredTraces = EventExtractor.Get(traceFile).FilterOutTextMessages().Results();

                string path = Path.Combine(tracesOutFolder, $"channel-{fileCounter}.csv");

                File.WriteAllLines(path, filteredTraces.Select(p => p.timestamp + ";" + p.size + ";" + p.type));
                log.Info($"wrote {traceFile.Count}evs to path: {path}");
                fileCounter++;
            }
        }


        private static async Task ConvertPcapToCsv() {
            var pcapFilePaths = PathWrapper.Pcap_TelegramFilesByNum(100);
            foreach (var pcapFile in pcapFilePaths) {
                var packetList = await InputModules.PcapParser.ParsePcapFileAsync(pcapFile);
                string path = Path.Combine("E:\\Datasets_uniPaper\\converted\\telegram\\packets",
                    Path.GetFileName(pcapFile).Replace(".pcap", "") + ".csv");
                File.WriteAllLines(path, packetList.Select(p => p.timestamp + ";" + p.size));
                log.Info($"wrote to path: {path}");
            }
        }


        private static void ConvertTracesToCsv() {
            var tracePaths = PathWrapper.MsgTrace_TelegramFilesByNum(100);
            foreach (var tracePath in tracePaths) {
                var traceFile = InputModules.MessageTraceParser.ParseMessageTraceTxtFile(tracePath);
                string path = Path.Combine("E:\\Datasets_uniPaper\\converted\\telegram\\traces",
                    Path.GetFileName(tracePath).Replace(".txt", "") + ".csv");
                File.WriteAllLines(path, traceFile.Select(p => p.timestamp + ";" + p.size + ";" + p.type));
                log.Info($"wrote {traceFile.Count}evs to path: {path}");
            }
        }

        private static void DoTestWork() {
            List<MsgEvent> traceEventList =
                MessageTraceParser.ParseMessageTraceTxtFolder(PathWrapper.MsgTrace_TelegramFolder);
            log.Info($"Loaded {traceEventList.Count}");
            // traceEventList.ForEach(ev =>log.Info(ev));

            List<NetworkPacket> packetLists = PcapParser.ParsePcapFolder(PathWrapper.pcapTelegramFolder);
            log.Info($"Loaded {packetLists.Count} Packets");
            // traceEventList = EventExtractor.mergeEventsByT_e(traceEventList);
            // traceEventList = EventExtractor.filterEmptyEvents(traceEventList);
            traceEventList = EventExtractor.extractBurstEvents(traceEventList);

            var burstEventList = EventExtractor.extractBurstEvents(packetLists);

            CorrelationDetector.CalculateCorrelationRate(traceEventList, burstEventList);

            // log.Info("Writing out Files....\n\n");
            // CSV_ResultWriter.WriteAsCsv(traceEventList,pathWrapper.OutFiles.csvFile("extractedTraces"));
            // CSV_ResultWriter.WriteAsCsv(packetLists,pathWrapper.OutFiles.CSV_packetsFile);
            // CSV_ResultWriter.WriteAsCsv(burstEventList,pathWrapper.OutFiles.CSV_eventFile);
        }

        private static void DoTestWork_traceExtraction() {
            List<MsgEvent> traceEventList =
                MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFile);
            log.Info($"Loaded {traceEventList.Count} events");
            traceEventList.ForEach(ev => log.Info(ev));

            CSV_ResultWriter.WriteAsCsv(traceEventList, PathWrapper.OutFiles.csvFile("RAWTraces"));

            traceEventList = EventExtractor.mergeEventsByT_e(traceEventList);
            log.Info($"\nMerge resultet in {traceEventList.Count} events");
            traceEventList.ForEach(ev => log.Info(ev));

            CSV_ResultWriter.WriteAsCsv(traceEventList, PathWrapper.OutFiles.csvFile("extractedTraces"));
        }

        private static async void DoTestWork_EventExtraction() {
            // List<NetworkPacket> packetLists = await PcapParser.ParsePcapFolderAsync(pathWrapper.pcapTelegramFolder);
            List<NetworkPacket> packetLists = await PcapParser.ParsePcapFileAsync(PathWrapper.pcapTelegramFile);
            // log.Info($"Parsed PCAP Folder. List containing {packetLists.Count} packets");
            //var list = MessageTraceParser.ParseMessageTraceTxtFolder(pathWrapper.MsgTrace_TelegramFolder);

            var burstEventList = EventExtractor.extractBurstEvents(packetLists);


            // CSV_ResultWriter.WriteAsCsv(packetLists,pathWrapper.OutFiles.CSV_packetsFile);
            CSV_ResultWriter.WriteAsCsv(burstEventList, PathWrapper.OutFiles.CSV_eventFile);

            // log.Info($"Loaded {list.Count} MessageEvents");
        }


        private static void AnalyzeFolders() {
            MissingFile_detector.AnalyzeFolder(PathWrapper.pcapTelegramFolder);
            MissingFile_detector.AnalyzeFolder(PathWrapper.MsgTrace_TelegramFolder);
        }


        private static async Task DoWork() {
            Task<JsonPacketCaptureParser.PacketCapture> captureTask =
                JsonPacketCaptureParser.LoadJsonPackets(PathWrapper.PacketsJSON_Small);
            JsonMessageEventFile msgEvents =
                await JsonMessageEventFile.LoadMessageEventFolder(PathWrapper.msgEventsFolder);
            msgEvents.AddTimestampsFromFolder(PathWrapper.timestampFileFolder);
            msgEvents.RemoveIncompleteEvents();
            var captureParser = await captureTask;

            double captureTime = captureParser._packets.First().timestamp;
            double channelTime = msgEvents._MsgEvents.First().timestamp;
            log.Info($"starttime Capture: {captureTime}");
            log.Info($"starttime Channel: {channelTime}");


            double startTime = Math.Min(captureTime, channelTime);
            if (startTime < 0) {
                log.Fatal("Startime is Negativ!");
            }
            else {
                log.Info($"Found StartTime: {startTime}. ");
            }
        }
    }
}