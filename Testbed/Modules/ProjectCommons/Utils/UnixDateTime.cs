using System;

namespace ProjectCommons.Utils {
	public static class UnixDateTime {
		/// <summary>
		/// Returns the current time as String in the form of: yyyy-MM-dd-HH-mm-ss
		/// </summary>
		/// <returns></returns>
		public static string GetTimeString() {
			return DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
		}
		
		
		/// <summary>
		/// Current Milliseconds Since Epoch as Long
		/// </summary>
		/// <returns>ms since Epoch</returns>
		public static long GetUnixTimeNow() {
			var now = (DateTimeOffset) DateTime.Now;
			return now.ToUnixTimeMilliseconds();
		}

		public static double GetUnixTimeNowDouble() {
			return ((double) GetUnixTimeNow()) / 1000;
		}
	}
}