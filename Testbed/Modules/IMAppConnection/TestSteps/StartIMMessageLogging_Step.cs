using OpenTap;
using ProjectCommons;

namespace IMAppConnection {
	[Display("Start LoggingIM-App-Messages", Groups: new[] { "Message Logging" })]
	public class StartIMMessageLogging_Step : TestStep {
		#region Settings

		[Display("The IM MessageLoggerInstrument")]
		public ILogIMMessages _Logger { get; set; }

		#endregion

		public StartIMMessageLogging_Step() {
			
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			
		}

		public override void Run() {
			if (_Logger != null) {
				Log.Info("IM App Msg Logging startet");
				_Logger.StartLogging();
			} else {
				Log.Error("No Logger Instrument provided");
				return;
			}

			RunChildSteps(); //If step has child steps.
			UpgradeVerdict(Verdict.Pass);
		}

		public override void PostPlanRun() {
			
			base.PostPlanRun();
		}
	}
}