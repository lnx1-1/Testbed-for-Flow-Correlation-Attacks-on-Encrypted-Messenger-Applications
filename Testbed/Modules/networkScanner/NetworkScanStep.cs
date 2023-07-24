using System.Collections.Generic;
using OpenTap;
using OpenTap.Plugins.ExamplePlugin;

namespace networkScanner {
	[Display("Scanner Step")]
	public class UiTestStep : TestStep {
		[Display("Network to Scan (<NetAdress>/<subnetBits>", "The Network to scan from ex: 192.168.1.0/24", "Settings",
			1, false)]
		public string networkAddress { get; set; }

		private List<string> _hostList = new List<string>();

		public List<string> hostList {
			get { return _hostList; }
			set {
				_hostList = value;
				OnPropertyChanged("hostList");
			}
		}

		[AvailableValues(nameof(hostList))] public List<string> selectedValues { get; set; } = new List<string>();


		[Display("ScanResult", "Dies ist ein Tolles setting", "Pre", 1, false)]
		[AvailableValues(nameof(hostList))]
		public string ScanResult { get; set; }

		[Display("ScanInstrument", "Interface for Scanner Instrument","Pre", 1)]
		public IScannerInstrument MyScanner { get; set; }

		public override void Run() {
			if (networkAddress == null || networkAddress.Trim().Length == 0) {
				Log.Error("No Network specified");
			}

			List<string> tempHostList = MyScanner.RunScan(networkAddress).ConvertAll(host => host.ToString());
			foreach (var host in tempHostList) {
				selectedValues.Add(host);
				hostList.Add(host);
				Log.Info(host);
			}

			Log.Info("Size: " + hostList.Count);
		}
	}
}