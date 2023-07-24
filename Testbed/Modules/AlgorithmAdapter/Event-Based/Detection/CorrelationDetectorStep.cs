using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.BatchProcessing;
using Event_Based_Impl.InputModules;
using OpenTap;
using ProjectCommons;
using ProjectCommons.Utils;

namespace AlgorithmAdapter.Event_Based.Detection {
    [Display("CorrelationDetector", Groups: new[] { "FlowCorrelation", "EventBased", "Detection" })]
    public class CorrelationDetectorStep : TestStep {
        #region Settings

        [Display("Extracted Message Events", Group: "Input")]
        public Input<List<MsgEvent>> _msgEvents { get; set; }

        [Display("Extracted Network Events", Group: "Input")]
        public Input<List<MsgEvent>> _networkEvents { get; set; }

        public List<int> ObservationLenghts { get; set; }

        [Browsable(true)]
        [Display("Resulting Matchrate", Group: "Output")]
        [Output]
        [Result]
        public double _matchRate { get; private set; } = 0;

        public string outFolderPath { get; set; }
        public bool writeOutAsJSON { get; set; } = true;

        public bool useZeroBasedTimestamps { get; set; } = true;

        #endregion

        public CorrelationDetectorStep() {
            ObservationLenghts = new List<int>() { 300, 600, 1800, 3600 };
            _networkEvents = new Input<List<MsgEvent>>();
            _msgEvents = new Input<List<MsgEvent>>();
            Name = "Correlation Detector";
        }

        public override void PrePlanRun() {
            if (writeOutAsJSON && string.IsNullOrEmpty(outFolderPath)) {
                Log.Error("OutFolderPath is not set!");
                writeOutAsJSON = false;
            }

            base.PrePlanRun();
        }

        public override void Run() {
            if (_msgEvents.Value == null || _networkEvents.Value == null || _networkEvents.Value.Count <= 0 ||
                _msgEvents.Value.Count <= 0) {
                Log.Error("Wrong Input Values");
                return;
            }

            if (useZeroBasedTimestamps) {
                Processing.SetRelativeTimeStampsZeroBased(_networkEvents.Value, _msgEvents.Value);
            } else {
                Processing.SetRelativeTimeStamps(_networkEvents.Value, _msgEvents.Value);
            }

            Dictionary<int, List<double>> resultList = new Dictionary<int, List<double>>();

            foreach (var interval in ObservationLenghts) {
                Processing.processOneFileV2(interval, _networkEvents.Value, _msgEvents.Value, resultList);
            }


            EventBasedFacade.PrintMatchRates(resultList);
            _matchRate = resultList.Average((k) => k.Value.Average());
            // _matchRate = CorrelationDetector.CalculateCorrelationRate(_msgEvents.Value, _networkEvents.Value);
            Log.Info("\n--------------------------------------------");
            Log.Info($"-		Matchrate:	[{_matchRate}]");
            Log.Info("-----------------------------------------------\n");

            Tuple<DetectionResult, Dictionary<int, List<double>>> combinedList = new Tuple<DetectionResult, Dictionary<int, List<double>>>(DetectionResult.CreateFromDict(resultList), resultList);

            if (writeOutAsJSON) {
                var outPath = Path.Combine(outFolderPath, $"ResultMatchRates_{UnixDateTime.GetTimeString()}.json");
                var jsonString = JsonSerializer.Serialize(combinedList, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(outPath, jsonString);
                Log.Info($"Wrote Result file to: {outPath}");
            }

            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(_matchRate != 0 ? Verdict.Pass : Verdict.Inconclusive);
        }


        public override void PostPlanRun() {
            base.PostPlanRun();
        }
    }
}