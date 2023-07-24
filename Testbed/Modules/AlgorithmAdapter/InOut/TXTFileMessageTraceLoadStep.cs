using System.Collections.Generic;
using System.IO;
using Event_Based_Impl;
using Event_Based_Impl.InputModules;
using OpenTap;
using ProjectCommons;

namespace AlgorithmAdapter.FileLoad {
	[Display("TXTFileMessageTraceLoadStep", Groups: new[] { "FlowCorrelation", "EventBased", "File Load" })]
	public class TxtFileMessageTraceLoadStep : TestStep {
		#region Settings

		[Display("File or Folder Path to load .TXT file(s) from", Group: "Input", Order: 1)]
		public string _path { get; set; } = PathWrapper.MsgTrace_TelegramFile1;

		[Display("Loaded MsgEvents", Group: "Ouput", Order: 2)]
		[Output]
		public List<MsgEvent> _OutMsgEventList { get; set; }


		// property here for each parameter the end user should be able to change

		#endregion

		public TxtFileMessageTraceLoadStep() {
			Name = "Txt File Message Trace Load";
			//Set default values for properties / settings.
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			// ToDo: Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (Directory.Exists(_path) && !File.Exists(_path)) {
				Log.Info("Detected path as Folder\n loading MessageTrace TXT Folder");
				_OutMsgEventList = MessageTraceParser.ParseMessageTraceTxtFolder(_path);
			} else if (File.Exists(_path)) {
				Log.Info("Detected Path as File\n loading MessageTrace TXT file");
				_OutMsgEventList = MessageTraceParser.ParseMessageTraceTxtFile(_path);
			} else {
				Log.Error($"Something went wrong with your path: [{_path}]");
				return;
			}

			RunChildSteps(); //If step has child steps.
			if (_OutMsgEventList != null && _OutMsgEventList.Count > 0) {
				Log.Info($"Successfully loaded [{_OutMsgEventList.Count}] Events");
				UpgradeVerdict(Verdict.Pass);
			} else {
				UpgradeVerdict(Verdict.Fail);
			}
		}

		public override void PostPlanRun() {
			// ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}