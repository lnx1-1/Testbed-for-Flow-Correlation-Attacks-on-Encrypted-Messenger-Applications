using System.Collections.Generic;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.DataTypes;
using OpenTap;
using ProjectCommons;

namespace AlgorithmAdapter.Event_Based.Extraction {
	[Display("Network Event Extraction", Groups: new[] {"FlowCorrelation", "EventBased", "Extraction"})]
	public class NetworkEventExtractionStep : TestStep {
		#region Settings

		[Display("Input NetworkPackets", "The Network Packets to Extract Network Events from", Group: "Input")]
		public Input<List<NetworkPacket>> _netPackets { get; set; }

		[Display("Extracted (processed) Network events", Group: "Output")]
		[Output]
		public List<MsgEvent> _ExtractedNetworkEvents { get; set; }

		[Display("Set Relative Timestamps", Group: "Processing")]
		public bool SetRelativeTimeStamps { get; set; } = false;

		// [Display("Remove Inconsistent Pairs", Group: "Processing")]
		// public bool RemoveInconsistentPairs { get; set; } = false;
		
		#endregion

		public NetworkEventExtractionStep() {
			_netPackets = new Input<List<NetworkPacket>>();
			Name = "Network Event Extraction";
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			// ToDo: Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (_netPackets == null || _netPackets.Value == null || _netPackets.Value.Count == 0) {
				Log.Error("No valid input provided..");
				return;
			}


			_ExtractedNetworkEvents = EventExtractor.extractBurstEvents(_netPackets.Value);
			RunChildSteps(); //If step has child steps.

			if (_ExtractedNetworkEvents == null || _ExtractedNetworkEvents.Count == 0) {
				Log.Error("Something went wrong while Extracting NetworkEvents");
				UpgradeVerdict(Verdict.Fail);
			} else {
				Log.Info(
					$"Succesfully Extracted [{_ExtractedNetworkEvents.Count}] Network Events, from [{_netPackets.Value.Count}] NetworkPackets");
				UpgradeVerdict(Verdict.Pass);
			}
		}

		public override void PostPlanRun() {
			// ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}