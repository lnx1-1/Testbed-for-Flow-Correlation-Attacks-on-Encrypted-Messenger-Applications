using System.Collections.Generic;
using System.IO;
using Event_Based_Impl.Utility_tools;
using OpenTap;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;

namespace AlgorithmAdapter.InOut {
    [Display("Write To File", Groups: new[] { "FlowCorrelation", "EventBased", "File Store" })]
    public class JsonMessagesLoadStep : TestStep {
        #region Settings

      

        [Display("Extracted Message Events", Group: "Input")]
        public Input<List<MsgEvent>> _msgEvents { get; set; }

        [Display("Extracted Network Events", Group: "Input")]
        public Input<List<MsgEvent>> _networkEvents { get; set; }

        [Display("Folder Path to save Events", Group: "Input", Order: 1)]
        public string _path { get; set; }
        
        public string NetEventFileName { get; set; } = "channel-0.csv";
        public string MsgEventFileName { get; set; } = "traces-0.csv";

        // property here for each parameter the end user should be able to change

        #endregion

        public JsonMessagesLoadStep() {
            Name = "Write To File";
            //Set default values for properties / settings.
            _msgEvents = new Input<List<MsgEvent>>();
            _networkEvents = new Input<List<MsgEvent>>();
        }

        public override void PrePlanRun() {
            base.PrePlanRun();
            // ToDo: Optionally add any setup code this step needs to run before the testplan starts
        }

        public override void Run() {
            if (!(_msgEvents.Value != null && _msgEvents.Value.Count > 0 && _networkEvents.Value != null && _networkEvents.Value.Count > 0)) {
                Log.Error("Input value Error");
            }


            string netPath = Path.Combine(_path, NetEventFileName);
            CSV_ResultWriter.WriteAsCsv(_networkEvents.Value,netPath);
            
            string tracePath = Path.Combine(_path, MsgEventFileName);
            CSV_ResultWriter.WriteAsCsv(_msgEvents.Value,tracePath);

            
            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun() {
            // ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
            base.PostPlanRun();
        }
    }
}