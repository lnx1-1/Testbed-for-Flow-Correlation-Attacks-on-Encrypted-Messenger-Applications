using System.Collections.Generic;
using System.IO;
using Event_Based_Impl;
using Event_Based_Impl.InputModules;
using OpenTap;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;

namespace AlgorithmAdapter.FileLoad {
	[Display("Json Messages Load Step", Groups: new[] { "FlowCorrelation", "EventBased", "File Load" })]
	public class JsonMessagesLoadStep : TestStep {
		#region Settings

		[Display("File or Folder Path to load .json file(s) from", Group: "Input", Order: 1)]
		public string _path { get; set; }

		[Display("Loaded MsgEvents", Group: "Ouput", Order: 2)]
		[Output]
		public List<MsgEvent> _OutMsgEventList { get; set; }


		// property here for each parameter the end user should be able to change

		#endregion

		public JsonMessagesLoadStep() {
			Name = "Json Messages Load";
			//Set default values for properties / settings.
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			// ToDo: Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (Directory.Exists(_path) && !File.Exists(_path)) {
				Log.Info("Detected path as Folder\n Loading MessageEvent folder");
				_OutMsgEventList = JsonMessageEventFile.LoadMessageEventFolder(_path).GetAwaiter().GetResult()._MsgEvents;
			} else if (File.Exists(_path)) {
				Log.Info("Detected Path as File\n loading MessageEvent json file");
				_OutMsgEventList =JsonMessageEventFile.ReadMessageEventFile(_path).GetAwaiter().GetResult()._MsgEvents;
			} else {
				Log.Error($"Something went wrong with your path: [{_path}]");
				return;
			}

			RunChildSteps(); //If step has child steps.
			if (_OutMsgEventList == null) {
				Log.Error("EventList is Null.. Something went wrong while loading the file from disk");
				UpgradeVerdict(Verdict.Error);
			} else if (_OutMsgEventList.Count <= 0) {
				Log.Error($"Loaded Empty List.. Count: {_OutMsgEventList.Count}");
				UpgradeVerdict(Verdict.Inconclusive);
			} else {
				Log.Info($"Successfully loaded [{_OutMsgEventList.Count}] Events");
				UpgradeVerdict(Verdict.Pass);
			}
		}

		public override void PostPlanRun() {
			// ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}