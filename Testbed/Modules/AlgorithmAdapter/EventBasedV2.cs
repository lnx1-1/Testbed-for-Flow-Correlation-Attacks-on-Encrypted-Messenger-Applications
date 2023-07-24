using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Event_Based_Impl;
using Event_Based_Impl.InputModules;
using Event_Based_Impl.Utility_tools;
using OpenTap;
using ProjectCommons;
using ProjectCommons.Utils;
using static Event_Based_Impl.BatchProcessing.EventBasedFacade;

namespace AlgorithmAdapter {
    [Display("Event-Based Batch Detection")]
    public class EventBasedV2 : TestStep {
        #region Settings

        [Display("Number of Files")] public int NumberOfFiles { get; set; } = 10;

        [Display("Interval duration in Seconds")]
        public int IntervalDurationInSeconds { get; set; } = 30 * 60;

        public bool BatchPerFile { get; set; } = false;

        public List<int> IntervalList { get; set; } = new List<int>() { 300, 600, 900, 1800, 3600 };

        [Display("Message Events Folder", Group: "Input")]
        [DirectoryPath]
        public string _msgEventsFolderPath { get; set; } = PathWrapper.MsgTrace_TelegramFolder;

        [Display("Pcap Folder", Group: "Input")]
        [DirectoryPath]
        public string _PcapFolderPath { get; set; } = PathWrapper.pcapTelegramFolder;


        [Display("Result Root Folder", Group: "Output")]
        public string _outFolder { get; set; } = Path.Combine(Utilitys.GetRootDirectory(), "Results");

        [Display("Name Appendix for the ResultFile", Group: "Output")]
        public string _nameAppendix { get; set; } = "";

        [Display("WriteOut", Group: "Output")] public bool _writeOut { get; set; } = true;

        #endregion

        public EventBasedV2() {
        }

        public override void PrePlanRun() {
            if (!Directory.Exists(_msgEventsFolderPath)) {
                Log.Error($"Directory Doesnt exist.. [{_msgEventsFolderPath}]");
            }

            if (!Directory.Exists(_PcapFolderPath)) {
                Log.Error($"Directory Doesnt exist.. [{_PcapFolderPath}]");
            }
        }

        public class ResultWrapper {
            public double FPStdDev { get; set; }
            public double TPStdDev { get; set; }
            [Display("TP")] public double TP { get; set; }
            [Display("ObservationLength [s]")] public int ObservationLen { get; set; }
            [Display("FP")] public double FP { get; set; }
        }


        public override void Run() {
            Log.Info("-----------------------------------------");
            Log.Info("---------Starting EventBased V2----------\n");


            if (BatchPerFile) {
                var pcapPaths = Directory.EnumerateFiles(_PcapFolderPath).ToList();
                var tracePaths = Directory.EnumerateFiles(_msgEventsFolderPath).ToList();

                if (pcapPaths.Count != tracePaths.Count) {
                    Log.Error("File Count diff Detected. CHeck root folders");
                    return;
                }

                Log.Debug("Using Files: \n");
                for (int i = 0; i < pcapPaths.Count; i++) {
                    Log.Debug(pcapPaths[i]);
                    Log.Debug(tracePaths[i]);
                    Log.Debug("----");
                }


                for (int i = 0; i < pcapPaths.Count; i++) {
                    string currPcapFile = pcapPaths[i];
                    string currTraceFile = tracePaths[i];
                    string currName = _nameAppendix + ($"-{i}");
                    List<DetectionResult.Result> resultList = new List<DetectionResult.Result>();
                    foreach (var intervalDuration in IntervalList) {
                        Log.Info($"---- Calculating for {intervalDuration}s interval ---");

                        var tpTask = CalcTruePositiveSingleFile(currTraceFile, currPcapFile, intervalDuration);
                        // var fpTask = CalcFalsePositive(NumberOfFiles, IntervalDurationInSeconds);

                        var tp = tpTask.GetAwaiter().GetResult();
                        var TPAvg = tp.Average();
                        var TPstd = Math.Sqrt(tp.Average(v => Math.Pow(v - TPAvg, 2))); // siehe: https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list


                        var tmpResult = new DetectionResult.Result() { FP = 0, TP = TPAvg, IntervalLen = intervalDuration };
                        resultList.Add(tmpResult);
                        Log.Info(tmpResult.ToString());
                    }

                    var jsonStr = JsonSerializer.Serialize(resultList, new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true });

                    if (_writeOut) {
                        var resultPath = Path.Combine(_outFolder, $"BatchV2_{NumberOfFiles}_Files_{UnixDateTime.GetTimeString()}_{currName}.json");
                        File.WriteAllText(resultPath, jsonStr);
                        Log.Info($"Wrote out ResultJSON to: {resultPath}");
                    } else {
                        Log.Info(jsonStr);
                    }
                }
            } else {
                runDetectionOnBatch();
            }

            // var tpColumn = new ResultColumn("TP", tp.ToArray());
            // var obsInterval = new ResultColumn("Interval",
            //     Enumerable.Repeat(IntervalDurationInSeconds, tp.Count).ToArray());
            //
            // var Table = new ResultTable("TP and Interval", new[] { tpColumn, obsInterval });
            // Results.Publish(Table);

            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
            Log.Info("---------Ended EventBased V2----------\n");
        }

        private void runDetectionOnBatch() {
            List<ResultWrapper> resultList = new List<ResultWrapper>();


            foreach (var intervalDuration in IntervalList) {
                Log.Info($"---- Calculating for {intervalDuration}s interval ---");
                var fpTask = FalsePositiveBatch(_msgEventsFolderPath, _PcapFolderPath, NumberOfFiles, intervalDuration);
                var tpTask = CalcTruePositiveBatch(_msgEventsFolderPath, _PcapFolderPath, NumberOfFiles, intervalDuration);
                // var fpTask = CalcFalsePositive(NumberOfFiles, IntervalDurationInSeconds);

                var tp = tpTask.GetAwaiter().GetResult();
                var fp = fpTask.GetAwaiter().GetResult();

                var TPAvg = tp.Average();

                var TPstd = Math.Sqrt(tp.Average(v => Math.Pow(v - TPAvg, 2))); // siehe: https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list


                var FPAvg = fp.Average();

                var FPstd = Math.Sqrt(fp.Average(v => Math.Pow(v - FPAvg, 2))); // siehe: https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list


                var wrapper = new ResultWrapper {
                    ObservationLen = intervalDuration,
                    TP = TPAvg,
                    FP = FPAvg,
                    FPStdDev = FPstd,
                    TPStdDev = TPstd
                };
                resultList.Add(wrapper);
            }

            var optione = new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true };

            var jsonStr = JsonSerializer.Serialize(resultList, optione);

            if (_writeOut) {
                var resultPath = Path.Combine(_outFolder, $"BatchV2_{NumberOfFiles}_Files_{UnixDateTime.GetTimeString()}_{_nameAppendix}.json");
                File.WriteAllText(resultPath, jsonStr);
                Log.Info($"Wrote out ResultJSON to: {resultPath}");
            } else {
                Log.Info(jsonStr);
            }
        }

        public override void PostPlanRun() {
            base.PostPlanRun();
        }
    }
}