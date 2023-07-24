using System;
using OpenTap;

namespace ProjectCommons.DataTypes {
	public interface ICaptureNetPacketsInstrument : IInstrument {
		/// <summary>
		/// Cleans up any Capture Files or Folders
		/// </summary>
		public void Cleanup();

		/// <summary>
		/// Starts the network Packet Capture on the Specified interface for the specified time 
		/// </summary>
		/// <param name="duration">Duration in Seconds of Capture</param>
		/// <param name="ifaceName">the Interface name to capture on. (eg: wlan0, br0, eth1)</param>
		/// <param name="captureFilter">the Capture filter in tcpdump capture filter format</param>
		/// <param name="logAction">A optional Action that gets a String parameter containing live updates on dump output</param>
		/// <returns>The number of captured Packets.<br/> -1 if there was an error</returns>
		public int RunPacketCapture(int duration, string ifaceName, string captureFilter,
			Action<string> logAction = null);

		/// <summary>
		/// Stores temp Capture file to Disk.
		/// Is the provided path is in Use, alternative filepath gets choosen
		/// </summary>
		/// <param name="localFilePath">the path to store the file to</param>
		/// <returns>
		/// The Filepath where the File was saved <br/>
		/// Return Empty string if file wasn't saved correctly</returns>
		public string StoreCapture(string localFilePath);

		
		/// <summary>
		/// Starts a new Background Capture. Can be Stopped using the StopRunning Capture Method
		/// </summary>
		/// <param name="ifaceName">the Interface name to Capture on</param>
		/// <param name="captureFilter">the TCPdump Capture Filter</param>
		/// <param name="logAction">A Log Action to use as printOut during execution</param>
		public void StartCapture(string ifaceName, string captureFilter, Action<string> logAction = null);

		/// <summary>
		/// Stops the Running Background Capture
		/// </summary>
		/// <returns>Returns the Amount of packets that where Captured.<br/>
		/// -1: Something went wrong<br/>
		/// >=0: Amount of Packets.</returns>
		public int StopRunningCapture();

	}
}