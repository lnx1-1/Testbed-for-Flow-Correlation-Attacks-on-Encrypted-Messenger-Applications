using System.Collections.Generic;
using System.IO;
using Event_Based_Impl;
using Event_Based_Impl.DataTypes;
using Event_Based_Impl.InputModules;
using OpenTap;

namespace AlgorithmAdapter.FileLoad {
	[Display("PCAP File Load", Groups: new[] {"FlowCorrelation", "EventBased", "File Load"})]
	public class PcapFileLoadStep : TestStep {
		#region Settings

		
		[Display("File or Folder Path to load .PCAP file(s) from", Group:"Input", Order: 1)]
		public string _path { get; set; } = PathWrapper.pcapTelegramFile1;

		[Display("Read NetworkPackets", "The Network Packets which were parsed from the Pcap file", Group: "Output", Order: 2)]
		[Output]
		public List<NetworkPacket> _OutPacketList { get; set; }

		#endregion

		public PcapFileLoadStep() {
			Name = "Pcap File Load";
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			//Optionally add any setup code this step needs to run before the testplan starts
		}

		public override void Run() {
			if (Directory.Exists(_path) && !File.Exists(_path)) {
				Log.Info("Detected path as Folder\n loading Pcap Folder");
				_OutPacketList = PcapParser.ParsePcapFolder(_path);
			} else if (File.Exists(_path)) {
				Log.Info("Detected Path as File\n loading Pcap file");
				_OutPacketList = PcapParser.ParsePcapFileAsync(_path).GetAwaiter().GetResult();
			} else {
				Log.Error($"Something went wrong with your path: [{_path}]");
				return;
			}


			RunChildSteps(); //If step has child steps.
			if (_OutPacketList != null && _OutPacketList.Count > 0) {
				Log.Info($"Successfully loaded [{_OutPacketList.Count}] Packets");
				UpgradeVerdict(Verdict.Pass);
			} else {
				UpgradeVerdict(Verdict.Fail);
			}
		}

		public override void PostPlanRun() {
			// Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}