using System.Collections.Generic;
using OpenTap;
using ProjectCommons;

namespace IMAppConnection {
	[Display("receiveResultTest")]
	public class receiveResultTest : TestStep {
		#region Settings

		[Display("Input Value")] 
		public Input<List<MsgEvent>> msgTraces { get; set; }
		// ToDo: Add property here for each parameter the end user should be able to change

		#endregion

		public receiveResultTest() {
			msgTraces = new Input<List<MsgEvent>>();
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			// ToDo: Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (msgTraces == null) {
				Log.Warning("No Input received..");
				return;
			}

			msgTraces.Value.ForEach((@event => Log.Info("EV: " + @event.ToString())));

			// ToDo: Add test case code here
			RunChildSteps(); //If step has child steps.
			UpgradeVerdict(Verdict.Pass);
		}

		public override void PostPlanRun() {
			// ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}