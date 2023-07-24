using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenTap;
using OpenTap.Plugins.ExamplePlugin.NmapScannerInstrument;

namespace networkScanner.NetworkScanners {
	[Display("Nmap Scanner Instrument")]
	public class NetworkScannerInstrument : Instrument, IScannerInstrument {

		[Display("Network Hosts", "All Devices on Network", "Result", 2)]
		public string[] ipaddrs { get; private set; }

		public NetworkScannerInstrument() {
			Name = "Nmap Scanner";
		}

		private static void runNmapScan(string projectDirectory, string nmapFileName, string networkAddr) {
			Process scanProc = new Process();
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe"; //"D:\\Program Files (x86)\\Nmap\\nmap.exe";
			startInfo.WorkingDirectory = projectDirectory;
			startInfo.Arguments = $"/C nmap -sn {networkAddr} -oX " + nmapFileName; //"-sL "+ networkAddr+" -oG NmapRunOutPut.txt";
			scanProc.StartInfo = startInfo;
			// startInfo.RedirectStandardOutput = true;
			// startInfo.RedirectStandardInput = true;
			startInfo.UseShellExecute = true;
			startInfo.CreateNoWindow = false;
			scanProc.Start();
			// scanProc.StandardInput.WriteLine("192.168.178.0\\24");
			// scanProc.StandardInput.Flush();
			scanProc.WaitForExit();
			// Console.WriteLine(scanProc.StandardOutput.ReadToEnd());
		}

		private static List<Host> ReadInXml(string inFileName) {
			var xmlDoc = XDocument.Load(inFileName);
			List<Host> outList = new List<Host>();
			foreach (var host in xmlDoc.Root.Descendants("host")) {
				string ip = host.Elements("address")
					.Where(element => element.Attribute("addrtype").Value.Contains("ipv4"))
					.Select(element => element.Attribute("addr")?.Value)
					.FirstOrDefault();
				string vendor = host.Elements("address")
					.Where(element => element.Attribute("addrtype").Value.Contains("mac"))
					.Select(element => element.Attribute("vendor")?.Value).FirstOrDefault();
				outList.Add(new Host(ip, vendor));
			}

			return outList;
		}

		public List<Host> RunScan(string networkAddr) {
			string workingDir = Environment.CurrentDirectory;
			string projectDirectory = "";

			try {
				projectDirectory = Directory.GetParent(workingDir).Parent.Parent.FullName;
			} catch (NullReferenceException e) {
				Console.WriteLine(e);
				throw;
			}

			string nmapFileName = "nmapOut.xml";
			runNmapScan(projectDirectory, nmapFileName, networkAddr);
			List<Host> hostList = ReadInXml(Path.Combine(projectDirectory, nmapFileName));
			Log.Info($"++++ Scan Finished of Network [{networkAddr}] ++++ ");
			foreach (var host in hostList) {
				Log.Info("Host: " + host);
			}

			if (hostList.Count == 0) {
				Log.Info($"No Hosts were found on Network: [{networkAddr}]");
			}

			return hostList;
		}
	}
}