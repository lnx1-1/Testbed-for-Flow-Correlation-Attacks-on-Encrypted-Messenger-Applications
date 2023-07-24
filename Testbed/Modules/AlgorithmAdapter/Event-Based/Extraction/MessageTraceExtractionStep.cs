using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Event_Based_Impl.Algorithms;
using OpenTap;
using ProjectCommons;

namespace AlgorithmAdapter.Event_Based.Extraction {
	[Display("Message Trace Extraction", Groups: new[] { "FlowCorrelation", "EventBased", "Extraction" })]
	public class MessageTraceExtractionStep : TestStep {
		#region Settings

		[Display("MsgEvents to Extract", Group: "Input", Order: 1)]
		public Input<List<MsgEvent>> _msgEvents { get; set; }

		[Display("Extracted (processed) MsgEvents", Group: "Output", Order: 2)]
		[Output]
		public List<MsgEvent> _extractedMsgEvents { get; set; }

		public IEnumerable<ExtractionMode> AvailableModes {
			get => AllModes();
			//get => new[] { ExtractionMode.FilterEvents, ExtractionMode.MergeEvents, ExtractionMode.ExtractBurstEvents };
		}

		[Display("Extraction Mode", Group: "Mode", Order: 3)]
		[AvailableValues("AvailableModes")]
		public ExtractionMode _mode { get; set; } = ExtractionMode.ExtractBurstEvents;

		#endregion

		public MessageTraceExtractionStep() {
			_msgEvents = new Input<List<MsgEvent>>();
			Name = "Message Trace Extraction";
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			//Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (_msgEvents.Value == null || _msgEvents.Value.Count == 0) {
				Log.Error("Invalid input @ Extraction");
				return;
			}

			switch (_mode) {
				case ExtractionMode.ExtractBurstEvents:
					_extractedMsgEvents = EventExtractor.extractBurstEvents(_msgEvents.Value);
					break;
				case ExtractionMode.MergeEvents:
					_extractedMsgEvents = EventExtractor.mergeEventsByT_e(_msgEvents.Value);
					break;
				case ExtractionMode.FilterEvents:
					_extractedMsgEvents = EventExtractor.FilterEmptyEvents(_msgEvents.Value);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (_extractedMsgEvents == null) {
				Log.Error("ExtractedMessageEvents are null");
				throw new NullReferenceException();
			}

			if (_extractedMsgEvents.Count <= 0) {
				Log.Warning("No resulting Message Events. (count == 0)");
				UpgradeVerdict(Verdict.Inconclusive);
			} else {
				Log.Info(
					$"Succesfully Extracted [{_extractedMsgEvents.Count}] MsgEvents, from [{_msgEvents.Value.Count}] Msgs");
				UpgradeVerdict(Verdict.Pass);
			}

			RunChildSteps(); //If step has child steps.
		}

		public override void PostPlanRun() {
			//Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}

		private IEnumerable<ExtractionMode> AllModes() {
			return Enum.GetValues(typeof(ExtractionMode)).Cast<ExtractionMode>();
		}

		public enum ExtractionMode {
			ExtractBurstEvents,
			MergeEvents,
			FilterEvents
		}
	}
}