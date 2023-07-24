using System.Collections.Generic;
using OpenTap;
using OpenTap.Plugins.ExamplePlugin.NmapScannerInstrument;

namespace networkScanner {
	[Display("Dummy Instrument")]
	public class DummyScannerInstrument : Instrument, IScannerInstrument {

		public DummyScannerInstrument() {
			Name = "Dummy Scanner";
		}
		public List<Host> RunScan(string netaddr) {
			Log.Info("Running Dummy Scan on NetAddr"+netaddr);
			return null;
		}
	}
}