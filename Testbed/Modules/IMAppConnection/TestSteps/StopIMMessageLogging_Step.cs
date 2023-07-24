using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using OpenTap;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;
using ProjectCommons.Utils;

namespace IMAppConnection.TestSteps {
    [Display("Stop LoggingIM-App-Messages", Groups: new[] { "Message Logging" })]
    public class StopIMMessageLogging_Step : TestStep {
        #region Settings

        [Display("The IM MessageLoggerInstrument", Order: 0, Group: "Setup", Collapsed: true)]
        public ILogIMMessages _Logger { get; set; }

        [Display("Output Result to .csv File?", Order: 1, Group: "Output")]
        public bool _outputToMessageTraceFile { get; set; } = false;

        [Display("Output Result to .JSON", "Output Result to .JSON MessageScript File?", Order: 1.2, Group: "Output")]
        public bool _outputToMessageScriptJSON { get; set; } = false;

        [Display("Result output folder", "Output folder for Result file", Order: 2, Group: "Output")]
        [DirectoryPath]
        public string _outputFolderPath { get; set; } = "Result";

        [Display("LoggedChannelEvents", "The Events Logged through Telegram Client")]
        [Output]
        public List<MsgEvent> _msgTraces { get; set; }

        [Display("Resulting outputfile")]
        [FilePath]
        public string _resultingOutputFile { get; private set; }

        [Display("Recorded Groups")] public Input<List<String>> recordedGroups { get; set; }

        #endregion

        private double _stopTimestamp;
        private double _startTimeStamp;

        public StopIMMessageLogging_Step() {
            recordedGroups = new Input<List<string>>();
        }

        [Display("Write out Events NOW", Group: "Output", Order: 4)]
        [Browsable(true)]
        public void WriteOutEvents() {
            if (!Directory.Exists(_outputFolderPath)) {
                Log.Warning($"Results Directory doesnt exist.. - Creating Directoy {_outputFolderPath}");
                Directory.CreateDirectory(_outputFolderPath);
            }

            if (_msgTraces == null || _msgTraces.Count == 0) {
                Log.Error("No Message Traces for Writeout..");
                return;
            }

            string outFileName = $"messageTraces-{UnixDateTime.GetTimeString()}";

            if (_outputToMessageTraceFile) {
                Log.Debug("Writing Logging output to .csv File");

                string outfile = Path.Combine(_outputFolderPath, $"{outFileName}.csv");
                Log.Info("Writing Message traces .csv to file: " + outfile);
                File.WriteAllLines(outfile, _Logger.GetLogAsCsv());
            }

            if (_outputToMessageScriptJSON) {
                _resultingOutputFile = Path.Combine(_outputFolderPath, $"{outFileName}.json");
                List<string> groupList = new List<string>();
                if (recordedGroups.Step == null) {
                    Log.Info("Group Property not set");
                } else {
                    groupList = recordedGroups.Value;
                }
                var file = new JsonMessageEventFile(_msgTraces,outFileName, groupList) { StartTimestamp = _startTimeStamp, StopTimestamp = _stopTimestamp };
                file.WriteToDisk(_resultingOutputFile, true);
            }
        }

        public override void PrePlanRun() {
            base.PrePlanRun();
            if (_outputToMessageScriptJSON) {
                if (!Directory.Exists(_outputFolderPath)) {
                    Log.Warning(
                        "Message Logging. Result Output Directory doesnt exist..		+++ Skipping JSON file Writeout +++");
                }
            }

            if (_Logger == null) {
                Log.Error("No Logger Instrument provided");
                UpgradeVerdict(Verdict.Aborted);
                return;
            }
        }

        public override void Run() {
            _Logger.StopLogging();
            _stopTimestamp = UnixDateTime.GetUnixTimeNowDouble();
            Log.Info(" ------  Stopped Msg Logging ----");
            if (_Logger.getMsgLog().Count > 0) {
                Log.Debug("Results:");
                _Logger.getMsgLog().ForEach((ev) => Log.Info(ev.ToString()));
            } else {
                Log.Info("No Message received during Logging Period");
            }

            Log.Info("---------------------------");

            _startTimeStamp = _Logger.getStartTime();

            _msgTraces = new List<MsgEvent>(_Logger.getMsgLog());

            WriteOutEvents();


            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }


        public override void PostPlanRun() {
            base.PostPlanRun();
        }
    }
}