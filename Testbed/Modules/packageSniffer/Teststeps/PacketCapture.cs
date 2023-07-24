using System.ComponentModel;
using OpenTap;
using ProjectCommons.DataTypes;
using TraceSource = OpenTap.TraceSource;

namespace packageSniffer.Teststeps {
	[Display("Capture Packets")]
	public class PacketCapture : TestStep {
		private const string TelegramServerFilter =
			"net 91.105.192.0/23 or net 91.108.4.0/22 or net 91.108.8.0/22 or net 91.108.12.0/22 or net 91.108.16.0/22 or net 91.108.20.0/22 or net 91.108.56.0/22 or net 149.154.160.0/20 or net 185.76.151.0/24";


		private TraceSource log = OpenTap.Log.CreateSource("PacketCapture");

		#region Settings

		[Display("Capture Instrument")]
		public ICaptureNetPacketsInstrument _captureInstrument { get; set; }


		[Display("Capture Interface",
			"Capture Interface of Remote device e.g. br0, eth0, wlan0.\nbr0 would be both interfaces",
			Group: "Capture Settings")]
		public string _iface { get; set; } = "br0";

		[Display("Capture Filter",
			Description:
			"Specify a Capture Filter for TCPDump. For Syntax see here: https://www.tcpdump.org/manpages/pcap-filter.7.html",
			Group: "Capture Settings")]
		public string _CaptureFilter { get; set; } = TelegramServerFilter;

		[Display("Capture Duration hours")]
		[Unit("h")]
		public int _CaptureDurationHour { get; set; }

		[Display("Capture Duration minutes")]
		[Unit("m")]
		public int _captureDurationMin { get; set; }

		[Display("Capture Duration seconds")]
		[Unit("s")]
		public int _captureDurationSecs { get; set; }

		[Display("Resulting Capture Duration in seconds")]
		[Unit("s")]
		[Browsable(true)]
		public int _captureDuration { get; private set; }

		// [Display("Split .pcaps into Hour files",
		// 	"If the .pcap should contain only one hour of messages and should be split into multiple ones")]
		// public bool splitIntoHourFiles { get; set; }

		[Display("Clean Capture Folder", "Whether to clean the Internal Capture Folder before creating new Capture Files")]
		public bool cleanInternalCaptureFolderBeforeCopy { get; set; } = true;

		// Add property here for each parameter the end user should be able to change

		#endregion

		private void UpdateDuration() {
			_captureDuration = _captureDurationSecs + (_captureDurationMin * 60) + (_CaptureDurationHour * 60 * 60);
			if (_captureDuration < 0) {
				_captureDuration = 0;
			}
		}

		public PacketCapture() {
			_captureDuration = 0;
			//Set default values for properties / settings.
		}


		public override void PrePlanRun() {
			if (_captureInstrument == null) {
				Log.Error("Remote Sniffer instrument is not set");
			}

			UpdateDuration();


			if (!_captureInstrument.IsConnected) {
				Log.Error("Device is not Connected");
				UpgradeVerdict(Verdict.Error);
				UserInput.Request(new ErrorDialog("Device is not Connected"), true);
				return;
			}


			if (cleanInternalCaptureFolderBeforeCopy) {
				_captureInstrument.Cleanup();
			}


			base.PrePlanRun();
			//Optionally add any setup code this step needs to run before the testplan starts
		}

		[Display("Error Occured")]
		public class ErrorDialog {
			[Browsable(true)]
			[Layout(LayoutMode.FullRow, rowHeight: 2)]
			[Display("Message")]
			public string erroMsg { get; }

			public ErrorDialog(string msg) {
				erroMsg = msg;
			}
		}

		public override void Run() {
			Log.Info("Starting Capture");
			var result = _captureInstrument.RunPacketCapture(_captureDuration, _iface, _CaptureFilter, (t) => log.Info(t));

			if(result == 0){
				Log.Warning("No Packets Captured");
				UpgradeVerdict(Verdict.Inconclusive);
			} else if (result > 0) {
				Log.Info($"Captured {result} packets");
				UpgradeVerdict(Verdict.Pass);
			} else {
				Log.Error("Something went wrong while Capturing");
				UpgradeVerdict(Verdict.Error);
			}
			
			RunChildSteps(); //If step has child steps.
		}


		public override void PostPlanRun() {
			//Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}