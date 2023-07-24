using System.Text;
using System.Text.Json;
using Event_Based_Impl;
using Event_Based_Impl.DataTypes;
using Event_Based_Impl.InputModules;
using NLog;
using OpenTap;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;
using ProjectCommons.Utils;
using ResultAnalyser;
using ScottPlot;

namespace ResultRunner;

public class Program2 {
    public struct MsgType {
        public string _type { get; set; }
        public int count { get; set; }
        public int totalCount { get; set; }
        public double countProzent { get; set; }
        public double volumeProzent { get; set; }
        public int volumeInByte { get; set; }
        public int smallestMsg { get; set; }
        public int largestMsg { get; set; }
        public double avgSize { get; set; }

        public MsgType(List<MsgEvent> msgEvents, string type) {
            _type = type;
            totalCount = msgEvents.Count;
            count = msgEvents.Count(@event => @event.type.Equals(type));
            if (count == 0) {
                return;
            }

            countProzent = count / (double)totalCount;
            var totalVolume = msgEvents.Select(ev => ev.size).Sum();
            var typeSizes = msgEvents.Where(ev => ev.type.Equals(type)).ToList();
            volumeInByte = typeSizes.Sum(ev => ev.size);
            volumeProzent = volumeInByte / (double)totalVolume;
            smallestMsg = typeSizes.Min(ev => ev.size);
            largestMsg = typeSizes.Max(ev => ev.size);
            avgSize = typeSizes.Average(ev => ev.size);
        }

        public static Dictionary<string, MsgType> GenerateTypeDict(List<MsgEvent> msgEvents, IEnumerable<string> types) {
            var outDict = new Dictionary<string, MsgType>();
            foreach (var type in types) {
                outDict.Add(type, new MsgType(msgEvents, type));
            }

            return outDict;
        }
    }

    private static Logger log = LogManager.GetCurrentClassLogger();


    struct analysePathWrapper {
        public string pcapPath { get; set; }
        public string tracePath { get; set; }
    }

    public static async Task Main(string[] ars) {
        //TODO mal testen welche größen vergleiche zu den Files vorliegen

        var folderPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results";
        var folderPath2 = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-14-12-26-00.json";
        var path = @"C:\Users\Linus\git\cryptCorr\Results\Result_Szenario3_OriginalD_messageTraces-2023-06-14-06-55-29\messageTraces-2023-06-14-20-19-07_RecordedReplay.json";
        var msgTraceTelegramFolderPath = PathWrapper.MsgTrace_TelegramFolder;
        var pcapTelegramFolderPath = PathWrapper.pcapTelegramFolder;
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-10-16-30-18.json";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-09-08-38-36.json";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-08-05-49-02.json";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-10-20-30-34_short.json";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-12-17-53-47.json";
        // var path = "C:\\Users\\Linus\\git\\cryptCorr\\Results\\messageTraces-2023-06-09-08-38-36.json";
        // var path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-10-20-30-34.json";

        // var path1 = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-13-13-52-23.json";
        // var path2 = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\messageTraces-2023-06-13-14-04-29.json";
        // var summaryFile = AnalyseTraceFolderPerFile(TGpath);
        //
        // WriteOutMissingFilesAsCsv(summaryFile, TGpath);

        var TPFile = @"C:\Users\Linus\git\cryptCorr\Results\Results_20230428195008\TP-1800.json";
        var FPFile = @"C:\Users\Linus\git\cryptCorr\Results\Results_20230428195008\FP-1800.json";
        var replay2 = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\2xReplay\messageTraces-2023-06-24-18-15-41.json";
        var replayPath = @"C:\Users\Linus\git\cryptCorr\Results\ReplayDaytime_17_06\messageTraces-2023-06-18-16-28-28_captured.json";
        var origPath = @"C:\Users\Linus\git\cryptCorr\Results\ReplayDaytime_17_06\messageTraces-2023-06-14-12-26-00_cleaned_Source.json";
        var sourcePath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\messageTraces-2023-06-14-12-26-00_cleaned.json";


        var openCloseTracePath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Test_PhoneOpenClose\messageTraces-2023-06-22-19-23-17_Recorded.json";
        var openClosePcapPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Test_PhoneOpenClose\capture-22_06_openClose.pcap";
        var openCloseOutPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Test_PhoneOpenClose\Analyse";
        var trace4Path = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\Replay_4\messageTraces-2023-06-27-07-00-29.json";
        // await CleanMsgTraceFile(@"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\messageTraces-2023-06-14-12-26-00.json");
        // createSummary(@"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\messageTraces-2023-06-14-12-26-00_cleaned_short.json");
        // GenerateSummaryForAllTracesFiles(@"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data");
        // await CompareMessageScripts(sourcePath, replay2);
        // var outFolder = @"C:\\Users\\Linus\\git\\cryptCorr\\MainProject\\IMSniff_Testbed_OpenTAP\\bin\\Debug x64\\Results\\Recording_15_06_Evening\\outFig1_1.png";
        string[] pcapFilesPaths = new string[4];
        pcapFilesPaths[0] = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\ReplayDaytime_17_06\capture-.pcap";
        pcapFilesPaths[1] = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\2xReplay\capture-15_06_Evening.pcap";
        pcapFilesPaths[2] = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\capture-15_06_Evening.pcap";
        pcapFilesPaths[3] = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_15_06_Evening\Replay_4\capture-Replay4.pcap";

        var etherExperimentList = new List<analysePathWrapper> {
            new analysePathWrapper() {
                tracePath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\messageTraces_00_-2023-06-28-03-10-19.json",
                pcapPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\capture-PC_Ether-.pcap",
            },
            new analysePathWrapper() {
                tracePath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\messageTraces_01_-2023-06-28-07-09-22.json",
                pcapPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\capture-PC_Ether-1.pcap",
            },
            new analysePathWrapper() {
                tracePath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\messageTraces_02_-2023-06-28-11-08-25.json",
                pcapPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\capture-PC_Ether-2.pcap",
            }
        };

        var outPath = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\Android_Closed";


        var imagesFolder = @"C:\Users\Linus\git\cryptCorr\Bachelorarbeit\Batschi\Thesis\images";

        var lunaFolder = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Recording_andCapture_LUNA\Data";
        var closeFolder = @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\Android_Closed";

        var trace1 = @"D:\DataUni\telegram\normal\adversary_message_traces\channel-0.txt";
        var pcap1 = @"D:\DataUni\telegram\normal\pcaps\user-0.pcap";

        // GenerateSummaryForAllTracesFiles(closeFolder);
        // GeneratePcapStatsFromFolder(closeFolder);
        // var filesWrapper = Loader.LoadFilesAsync(pcapTelegramFolderPath, msgTraceTelegramFolderPath).GetAwaiter().GetResult();
        // var tracesList = filesWrapper.TraceFiles.SelectMany(list => list).ToList();
        // var sizeZero = tracesList.Count(ev => ev.size == 0);
        // var sizeNeg = tracesList.Count(ev => ev.size <0);
        // var textCount = tracesList.Where(ev=>ev.type.Equals("text")).Count(ev => ev.size <0);
        // var videoCount = tracesList.Where(ev=>ev.type.Equals("video")).Count(ev => ev.size <0);
        // var AverageSize = tracesList.Where(ev=>ev.type.Equals("photo")).Where(ev=>ev.size>0).Average(ev=>ev.size);
        // log.Info($"AvgSizePhoto: {AverageSize}");
        // var calc = MatchrateCalculator.LoadFromFile(TPFile, FPFile);
        // calc.PrintRange(0.5);
        // calc.CalRates(0.6);
        // foreach (var wrap in etherExperimentList) {

        await GenerateDetailedAnalyse(trace1, pcap1, imagesFolder, "noneMsg_detailed");
        // }
        // await GenerateScottPlott(@"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\messageTraces_02_-2023-06-28-11-08-25.json", @"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\PC_Windows_Ethernet\Raw_data\capture-PC_Ether-2.pcap", outPath);
    }

    public struct outStruct {
        public string filePath { get; set; }
        public int totalSize { get; set; }
        public int totalCount { get; set; }
        public double avgSize { get; set; }
        public double captureDuration { get; set; }
    };

    public static void GeneratePcapStatsFromFolder(string folderPath) {
        if (!Directory.Exists(folderPath)) {
            log.Error("Not a valid input folder path");
            return;
        }

        var files = Directory.EnumerateFiles(folderPath).Where(path => Path.GetExtension(path).ToLower().Equals(".pcap"));
        var enumerable = files.ToList();
        if (!enumerable.Any()) {
            log.Error("No pcap files in Provided folder");
            return;
        }

        GeneratePcapStatistics(enumerable.ToList(), folderPath);
    }

    public static void GeneratePcapStatistics(List<string> paths, string outFolderPath) {
        List<outStruct> outList = new List<outStruct>();

        foreach (var path in paths) {
            var packets = PcapParser.ParsePcapFileAsync(path).GetAwaiter().GetResult();
            var Size = packets.Sum(p => p.size);
            var Count = packets.Count;
            var avgS = packets.Average(p => p.size);
            packets.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            var duration = packets.Last().timestamp - packets.First().timestamp;
            var currStruct = new outStruct() {
                totalSize = Size,
                totalCount = Count,
                avgSize = avgS,
                filePath = path,
                captureDuration = duration
            };
            outList.Add(currStruct);
        }

        var jsonString = JsonSerializer.Serialize(outList, new JsonSerializerOptions() { WriteIndented = true });
        if (Directory.Exists(outFolderPath)) {
            var outPath = Path.Combine(outFolderPath, $"pcapAnalyzeResult_{UnixDateTime.GetTimeString()}.json");
            File.WriteAllText(outPath, jsonString);
            log.Info($"wrote out file to: {outPath}");
        } else {
            log.Error("Dir not existing");
        }
    }


    public static async Task ScottPlottBytesPerSeonds(string pcapPath, string outPath) {
        var pcaps = await PcapParser.ParsePcapFileAsync(pcapPath);
        List<double> sizesPerSecond = pcaps.Zip(pcaps.Skip(1), (current, next) => {
            double timeDiff = next.timestamp - current.timestamp;
            if (timeDiff == 0) {
                timeDiff = 0.00001;
            }

            double dataRate = (double)current.size / timeDiff;
            return dataRate;
        }).ToList();

        var plot = PlotGenerator.SignalPlot(sizesPerSecond);

        // var plot = PlotGenerator.PlotPacketEventDetails(pcaps, msgEvents);


        plot.SaveFig(outPath, 1000, 800, false);
    }

    public static async Task GenerateDetailedAnalyse(string tracePath, string pcapPath, string outPath, string fileName) {
        List<MsgEvent> msgEvents = null;
        if (Path.GetExtension(tracePath).EndsWith("json")) {
            msgEvents = (await JsonMessageEventFile.ReadMessageEventFile(tracePath))._MsgEvents;
        } else if (Path.GetExtension(tracePath).EndsWith("txt")) {
            msgEvents = MessageTraceParser.ParseMessageTraceTxtFile(tracePath);
        }

        var pcaps = await PcapParser.ParsePcapFileAsync(pcapPath);

        log.Info($"loaded {msgEvents.Count} evs - pcaps: {pcaps.Count}");
        var combinedPath = Path.Combine(outPath, $"{fileName}.bmp");
        // var plot = PlotGenerator.PlotPacketEventDetails(pcaps, msgEvents);
        log.Info($"Write img to: {combinedPath}");
        var plot = PlotGenerator.PlotPacketDetailsHorstVersion(pcaps, msgEvents, combinedPath,1000,4000);
        // var plot = PlotGenerator.DetailedExternalAxis(pcaps, msgEvents);

        // var filePath = Path.Combine(outPath, $"IOGraph_{UnixDateTime.GetTimeString()}.png");
        // plot.SaveFig(filePath, 1000, 800, false);
        // log.Info($"Wrote out to: {filePath}");
    }

    public static TraceFileSummary AnalyseTraces(List<MsgEvent> msgTraces) {
        var noneCount = msgTraces.Count(e => e.size == -1);
        var totalEvents = msgTraces.Count();
        var prozentualNone = (double)noneCount / (double)totalEvents;
        Console.WriteLine($"Starting analysis with {totalEvents} events");


        var summaryFile = new TraceFileSummary() {
            NumberOfTraces = totalEvents,
            NumberOfEmptySizes = noneCount,
            ProzentualEmptySizes = prozentualNone,
            TypeAnalysis = MsgType.GenerateTypeDict(msgTraces, MsgEvent.MsgTypes),
        };
        return summaryFile;
    }

    public static TraceFileSummary AnalyseTraces(JsonMessageEventFile eventFile) {
        var summary = AnalyseTraces(eventFile._MsgEvents);
        return AddMetaInformation(summary, eventFile);
    }

    public static TraceFileSummary AddMetaInformation(TraceFileSummary summaryFile, JsonMessageEventFile eventFile) {
        summaryFile.Path = eventFile.Path;
        summaryFile.recoredFromGroups = eventFile._groups;
        summaryFile.startTimestamp = eventFile.StartTimestamp;
        summaryFile.stopTimestamp = eventFile.StopTimestamp;
        return summaryFile;
    }

    public static TraceFileSummary AnalyseMsgScript(string path) {
        var jsonEventFile = JsonMessageEventFile.ReadMessageEventFile(path).GetAwaiter().GetResult();
        int missingFiles = CheckFilePathsOfMsgScript(jsonEventFile);
        var summary = AnalyseTraces(jsonEventFile);
        summary.MissingFiles = missingFiles;
        return summary;
    }

    public static TraceFileSummary AnalyseTracesFolder(string path) {
        return AnalyseTraces(MessageTraceParser.ParseMessageTraceTxtFolder(path));
    }

    public static List<TraceFileSummary> AnalyseTraceFolderPerFile(string folderPath) {
        List<TraceFileSummary> summaries = new List<TraceFileSummary>();
        foreach (var filePath in Directory.EnumerateFiles(folderPath)) {
            summaries.Add(AnalyseTraces(MessageTraceParser.ParseMessageTraceTxtFile(filePath)));
        }

        return summaries;
    }

    public static int CheckFilePathsOfMsgScript(JsonMessageEventFile eventFile) {
        int missingCounter = 0;
        foreach (var ev in eventFile._MsgEvents.Where(ev => !ev.type.Equals("text"))) {
            if (!File.Exists(ev.path)) {
                missingCounter++;
                Console.WriteLine($"Path: {ev.path} is Missing.. Trace: {ev}");
            }
        }

        return missingCounter;
    }

    public static void Compare2TracesFiles(List<MsgEvent> eventsA, List<MsgEvent> eventsB, string outFolderPath) {
        if (eventsA.Count != eventsB.Count) {
            Console.WriteLine($"Count missmatch.. Err: A: [{eventsA.Count}], B: [{eventsB.Count}] ");
            return;
        }

        Dictionary<string, double> summaryDict = new Dictionary<string, double>();

        foreach (var type in MsgEvent.MsgTypes) {
            var typListA = eventsA.Where(ev => ev.type.Equals(type)).ToList();
            var typListB = eventsB.Where(ev => ev.type.Equals(type)).ToList();

            List<Tuple<int, double>> diffList = new List<Tuple<int, double>>();

            if (typListA.Count() != typListB.Count) {
                Console.Out.WriteLine("Count Diff!");
                continue;
            }

            if (typListA.Count == 0) {
                log.Info($"No Elements for {type}");
                continue;
            }

            double timeRange = 250.0;
            foreach (var evA in typListA) {
                var sortedList = typListB.Where(ev => Math.Abs(ev.timeDelay - evA.timeDelay) < timeRange).ToList();
                sortedList.Sort((ev1, ev2) => {
                    var ev1SizeDiff = Math.Abs(ev1.size - evA.size);
                    var ev2SizeDiff = Math.Abs(ev2.size - evA.size);

                    return ev1SizeDiff.CompareTo(ev2SizeDiff);
                });
                if (sortedList.Count == 0) {
                    log.Error($"No events in {timeRange}s range for Type {type}");
                    log.Error($"currentEV: {evA}");
                } else {
                    var selectedEle = sortedList.First();
                    Compare2Events(evA, selectedEle, diffList);
                    typListB.Remove(selectedEle);
                }
            }


            StringBuilder csvOut = new StringBuilder("SizesDiff (bytes); TimeDiff (seconds);\n");
            var sizesList = new List<int>();
            var timeDiffList = new List<double>();
            foreach (var tuple in diffList) {
                sizesList.Add(tuple.Item1);
                timeDiffList.Add(tuple.Item2);
                csvOut.Append(tuple.Item1 + "; " + tuple.Item2 + ";\n");
            }

            if (sizesList.Count > 0) {
                summaryDict.Add($"AvgSizeDiff_{type}", sizesList.Average());
                summaryDict.Add($"AvgTimeDiff_{type}", timeDiffList.Average());
            }


            string pathDetailed = Path.Combine(outFolderPath, $"DiffAnalyse_{type}.csv");
            File.WriteAllText(pathDetailed, csvOut.ToString());
            Console.Out.WriteLine($"Wrote file to Path: {pathDetailed}");
        }

        string pathSummary = Path.Combine(outFolderPath, $"SummaryAnalyse.json");
        string jsonString = JsonSerializer.Serialize(summaryDict, new JsonSerializerOptions() { WriteIndented = true });
        log.Info(jsonString);
        File.WriteAllText(pathSummary, jsonString);
        log.Info($"Write out Summary to: {pathSummary}");
    }

    private static void Compare2Events(MsgEvent a, MsgEvent b, List<Tuple<int, double>> diffList) {
        if (!a.type.Equals(b.type)) {
            Console.Out.WriteLine($"Type missmatch: [{a.type}, {b.type}]");
        }

        int sizeDiff = a.size - b.size;
        double delayDiff = a.timeDelay - b.timeDelay;

        diffList.Add(new Tuple<int, double>(sizeDiff, delayDiff));
    }

    // public static void compare2SummaryFiles(TraceFileSummary fileA, TraceFileSummary fileB) {
    //     int totalTracesDiff = fileA.NumberOfTraces - fileB.NumberOfTraces;
    //
    //     StringBuilder outstr = new StringBuilder();
    //     outstr.Append("Property; FileA; FileB; Diff;");
    //     outstr.Append($"Total Traces; {fileA.}")
    // }

    public static void createSummary(string path) {
        var summaryFile = AnalyseMsgScript(path);

        // Console.WriteLine(summaryFile);

        var fullName = Directory.GetParent(path)?.FullName;
        if (fullName != null) {
            var writeOutPath = Path.Combine(fullName, "SummaryFile__" + Path.GetFileName(path));
            File.WriteAllText(writeOutPath, summaryFile.ToString());
            Console.WriteLine($"Wrote out File to {writeOutPath}");
        }
    }


    public static void GenerateSummaryForAllTracesFiles(string folderPath) {
        var filePaths = Directory.EnumerateFiles(folderPath).Where(path => Path.GetFileName(path).StartsWith("messageTraces-") && path.EndsWith(".json")).ToList();
        Console.Out.WriteLine($"Number Of Files: {filePaths.Count}");
        foreach (var path in filePaths) {
            if (File.Exists(path)) {
                createSummary(path);
            } else {
                Console.Out.WriteLine($"File {path} doesnt exist. Skipping");
            }
        }
    }


    private static async Task CheckFileSize_toRealSize(string folderPath) {
        var file = await JsonMessageEventFile.LoadMessageEventFolder(folderPath);
        var events = file._MsgEvents.Where(ev => !ev.type.Equals("text")).ToList();
        Console.Out.WriteLine(events.Count + " Evs");
        StringBuilder builder = new StringBuilder("SizeDiff\n");
        foreach (var ev in events) {
            FileInfo info = new FileInfo(ev.path);
            if (info.Exists) {
                var sizeDiff = info.Length - (long)ev.size;
                if (sizeDiff != 0) {
                    builder.Append($"Diff: {sizeDiff}\n");
                }
            } else {
                builder.Append("NAN\n");
            }
        }

        Console.Out.WriteLine(builder);
    }

    private static void WriteOutMissingFilesAsCsv(List<TraceFileSummary> summaryFile, string TGpath) {
        StringBuilder builder = new StringBuilder("TotalFiles;MissingFiles\n");
        foreach (var sum in summaryFile) {
            builder.Append($"{sum.NumberOfTraces};{sum.NumberOfEmptySizes}\n");
        }

        File.WriteAllText(Path.Combine(TGpath, "summaryCSV.csv"), builder.ToString());
    }

    private static async Task CleanMsgTraceFile(string path) {
        var file = await JsonMessageEventFile.ReadMessageEventFile(path);
        int beforeCount = file._MsgEvents.Count;
        file._MsgEvents.RemoveAll(ev => !ev.type.Equals("text") && !File.Exists(ev.path));
        int failCount = beforeCount - file._MsgEvents.Count;
        if (CheckFilePathsOfMsgScript(file) == 0) {
            Console.Out.WriteLine($"Successfully removed [{failCount}]");
            file.WriteToDisk(path.Replace(".json", "_cleaned.json"));
        } else {
            Console.Out.WriteLine("Still files to remove Present");
        }
    }

    private static async Task CompareMessageScripts(string path1, string path2) {
        var fileA = await JsonMessageEventFile.ReadMessageEventFile(path1);
        var fileB = await JsonMessageEventFile.ReadMessageEventFile(path2);
        var fullName = Directory.GetParent(path1)?.FullName;
        if (fullName != null) {
            var info = Directory.CreateDirectory(Path.Combine(fullName, "diffcmp" + Path.GetFileName(fileA.Path) + "-" + Path.GetFileName(fileB.Path)));
            Compare2TracesFiles(fileA._MsgEvents, fileB._MsgEvents, info.FullName);
        } else {
            Console.Out.WriteLine("PathError");
        }
    }
}

public struct TraceFileSummary {
    public int NumberOfTraces { get; set; }
    public int NumberOfEmptySizes { get; set; }
    public double ProzentualEmptySizes { get; set; }
    public int MissingFiles { get; set; }
    public string Path { get; set; }
    public Dictionary<string, Program2.MsgType> TypeAnalysis { get; set; }
    public double startTimestamp { get; set; }
    public double stopTimestamp { get; set; }

    public override string ToString() {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
    }

    public List<string> recoredFromGroups { get; set; }
}