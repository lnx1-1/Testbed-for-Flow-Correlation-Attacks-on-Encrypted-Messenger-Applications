using System.Collections.Generic;
using OpenTap;

namespace ProjectCommons {
	/// <summary>
	/// Is Capable to Log Instant messaging Application Messages.
	/// </summary>
	public interface ILogIMMessages : IInstrument {
		void StartLogging();
		void StopLogging();
		List<MsgEvent> getMsgLog();
		void ClearLog();

		/// <summary>
		/// Returns the log as CSV file with Header!
		/// </summary>
		/// <returns>the log. All CSV lines are contained in the string list</returns>
		public List<string> GetLogAsCsv();

		public double getStartTime();
	}
}