namespace Event_Based_Impl.Utility_tools {
	//TODO Auslagern in Config file
	public static class Config {
		/// <summary>
		/// The Interpacket Delay Threshold -> Seite 10 unten im Paper erklärt
		/// </summary>
		public static double T_e { get; set; }= 0.5;

		/// <summary>
		/// <b>Value aus dem Code im Extraction prozess. Wahrscheinlich in Bytes. Wird im Paper nicht aufgeführt</b>
		/// 
		/// <remarks> Note that
		/// an SIM communication can include SIM protocol messages as
		/// well (handshakes, notifications, updates, etc.); however, such
		///	messages are comparatively very small as shown in Figure 8,
		/// and thus the detector ignores them in the correlation process.</remarks>
		/// 
		/// </summary>
		public static int relevantPacketSize_MAGICNUM { get; set; } = 40;

		/// <summary>
		/// EDIT// Methode im Code wird nicht benutzt
		/// Mal wieder eine magic Num aus dem Code.. Wird beim Mergen der Channel
		/// 
		/// </summary>
		public static int eventMergeThreshold_MAGICNUM { get; set; }= 2;

		/// <summary>
		/// <b>Maximum Transmission Unit</b> 
		/// </summary>
		public const int MTU = 1500;

		/// <summary>
		/// Ist der Timestamp threshold für Event Detection. Unter dieser Zeit wird es als Match erkannt
		/// We set ∆ of the event-based algorithm to 3 seconds
		/// </summary>
		public static int DELTA { get; set; } = 3; //Seconds


		/// <summary>
		/// Ist der Size threshold innerhalb dessen die Events als Match angesehen werden.
		/// We also set Γ parameter of the event-based detector to 10Kb
		/// </summary>
		public static int GAMMA { get; set; } = 10 * 1000; //Bytes


		public class CorrelationWindowSettings {
			public double stepsize { get; set; }
			public int numberOfSteps { get; set; }
			public int start { get; set; }

			public static CorrelationWindowSettings GetDefault() {
				return new CorrelationWindowSettings() { 
					stepsize = 0.5,
					numberOfSteps = 40,
					start = -20 };
			}
		}
	}
}