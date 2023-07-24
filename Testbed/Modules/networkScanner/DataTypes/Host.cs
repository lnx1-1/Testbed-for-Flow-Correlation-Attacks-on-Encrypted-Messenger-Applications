using System.Collections;

namespace OpenTap.Plugins.ExamplePlugin.NmapScannerInstrument {
	public class Host{
		public string _ip { get; set; }
		public string _vendor { get; set; }

		public override string ToString() {
			return _ip + " (" + _vendor + ")";
		}

		public Host(string ip, string vendor) {
			_ip = ip;
			_vendor = vendor;
		}
		
	}
}