using System.Collections.Generic;
using OpenTap;
using OpenTap.Plugins.ExamplePlugin.NmapScannerInstrument;

namespace networkScanner {
	public interface IScannerInstrument : IInstrument {
		List<Host> RunScan(string networkAddr);
	}
}