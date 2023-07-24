using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog.Fluent;
using ProjectCommons;

namespace Event_Based_Impl {
    public static class PathWrapper {
        private static List<string> rootPaths = new List<string>() { @"D:\DataUni", "E:\\Datasets_uniPaper" };

        public static readonly string DefaultPythonDetectionScryptPath =
            Utilitys.GetAbsolutePath("attack algorithms/testCode/Event-Based-IM-V2/DetectionIM_v_2.py");

        private static string GetCurrentlyUsedRootPath() {
            return rootPaths.Where(Directory.Exists).First();
        }

        public static string datasetPath = GetCurrentlyUsedRootPath();

        public static readonly string PacketsJSON_Small =
            datasetPath +
            "\\Signal\\limited_bw\\1Mbps_bw\\packets\\messages_pareto_xm_5000_alpha_0.93_max30delay_signal_1Mbps_061_180_SMALL.JSON";

        public static readonly string PacketsJSON_Big =
            datasetPath +
            "\\Signal\\limited_bw\\1Mbps_bw\\packets\\messages_pareto_xm_5000_alpha_0.93_max30delay_signal_1Mbps_061_180.JSON";

        public static readonly string PacketsJSON_veryBig =
            datasetPath +
            "\\Signal\\limited_bw\\1Mbps_bw\\packets\\messages_pareto_xm_5000_alpha_0.93_max30delay_signal_1Mbps_000_060.JSON";

        /// <summary>
        /// A folder containing generated MessageEvents (Not Network Packet) in .JSON format
        /// </summary>
        public static readonly string msgEventsFolder =
            datasetPath + "\\Signal\\generated_message_traces\\messages_pareto_xm_5000_alpha_0.93_max30delay";

        public static readonly string msgEventFile =
            datasetPath +
            @"\Signal\generated_message_traces\messages_pareto_xm_5000_alpha_0.93_max30delay\converted_messages_period_00_2020_07_09_13_42_53.json";

        public static readonly string msgEventtestFile = @"D:\Users\Linus\git\cryptCorr\dataset\single_testEvent.json";


        public static readonly string timestampFile =
            datasetPath +
            @"\Signal\limited_bw\1Mbps_bw\timestamps\timestamps_converted_messages_period_00_2020_07_09_13_42_53.txt";

        public static readonly string timestampFileFolder = datasetPath + @"\Signal\limited_bw\1Mbps_bw\timestamps";
        public static readonly string pcapTelegramFile = datasetPath + @"\telegram\normal\pcaps\user-0.pcap";
        public static readonly string pcapTelegramFile1 = datasetPath + @"\telegram\normal\pcaps\user-1.pcap";
        public static readonly string pcapTelegramFolder = datasetPath + @"\telegram\normal\pcaps";

        public static readonly string MsgTrace_TelegramFolder =
            @datasetPath + "\\telegram\\normal\\adversary_message_traces";


        public static readonly string packets_PreprocessedFolder = "D:\\DataUni\\converted\\telegram\\packetsPre2";
        public static readonly string msgTraces_PreprocessedFolder = @"D:\DataUni\converted\telegram\TracesPre2";

        public static string MsgTrace_TelegramFileByNum(int fileNumber) {
            return datasetPath + $"\\telegram\\normal\\adversary_message_traces\\channel-{fileNumber}.txt";
        }

        public static List<string> MsgTrace_TelegramFilesByNum(int howManyFiles) {
            return GetFilesByNum(howManyFiles, MsgTrace_TelegramFileByNum);
        }


        public static List<string> GetFilesByNum(string folderPath, string exampleFileName, int howManyFiles) {
            var outFiles = new List<string>();
            int fileIndex = 0;
            int maxNumberOfFiles = Directory.EnumerateFiles(folderPath).Count();
            while (outFiles.Count <= howManyFiles && fileIndex <= maxNumberOfFiles) {
                string path = GetFileByNumPattern(folderPath, exampleFileName, fileIndex);
                if (File.Exists(path)) {
                    outFiles.Add(path);
                }

                fileIndex++;
            }

            return outFiles;
        }

        public static List<string> GetFilesByNum(int howManyFiles, Func<int, string> fileFunc) {
            var outFiles = new List<string>();
            int fileIndex = 0;
            while (outFiles.Count < howManyFiles) {
                string path = fileFunc(fileIndex);
                if (File.Exists(path)) {
                    outFiles.Add(path);
                }

                fileIndex++;
            }

            return outFiles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="fileNameFormat">An Example File from that folder.  eg. example-1.pcap</param>
        /// <param name="fileNumber"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static string GetFileByNumPattern(string folderPath, string fileNameFormat, int fileNumber) {
            if (fileNameFormat.Contains("/") || fileNameFormat.Contains("\\")) {
                fileNameFormat = Path.GetFileName(fileNameFormat);
            }

            if (Directory.Exists(folderPath)) {
                string pattern = @$"(.*)\d+(\..*)";
                var fileNamePart1 = "";
                var fileNamePart2 = "";
                RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;
                var collection = Regex.Matches(fileNameFormat, pattern, options);
                if (collection.Count == 1) {
                    if (collection[0].Groups.Count == 3) {
                        fileNamePart1 = collection[0].Groups[1].Value;
                        fileNamePart2 = collection[0].Groups[2].Value;
                    }
                    else {
                        throw new ArgumentException($"Wrong fileNameFormat specified: [{fileNameFormat}]");
                    }
                }

                return Path.Combine(folderPath, $@"{fileNamePart1}{fileNumber}{fileNamePart2}");
            }
            else {
                throw new FileNotFoundException($"the Path {folderPath} doesn't exist on this System");
            }
        }

        public static string Pcap_TelegramFileByNum(int fileNumber) {
            string folderPath = @"\telegram\normal\pcaps\";
            if (Directory.Exists(datasetPath + folderPath)) {
                return datasetPath + folderPath + $@"user-{fileNumber}.pcap";
            }
            else {
                throw new FileNotFoundException($"the Path {datasetPath + folderPath} doesn't exist on this System");
            }
        }

        public static List<string> Pcap_TelegramFilesByNum(int amountOfFiles) {
            return GetFilesByNum(amountOfFiles, Pcap_TelegramFileByNum);
        }

        public static readonly string MsgTrace_TelegramFile =
            datasetPath + @"\telegram\normal\adversary_message_traces\channel-0.txt";

        public static readonly string MsgTrace_TelegramFile1 =
            datasetPath + @"\telegram\normal\adversary_message_traces\channel-1.txt";


        public struct OutFiles {
            public static readonly string ResultsFolder = Utilitys.GetAbsolutePath("Results");

            public static readonly string CSV_eventFile = Path.Combine(ResultsFolder, "events.csv");
            public static readonly string CSV_packetsFile = Path.Combine(ResultsFolder, "packets.csv");

            public static string csvFile(string filename) {
                return Path.Combine(ResultsFolder, filename + ".csv");
            }
        }
    }
}