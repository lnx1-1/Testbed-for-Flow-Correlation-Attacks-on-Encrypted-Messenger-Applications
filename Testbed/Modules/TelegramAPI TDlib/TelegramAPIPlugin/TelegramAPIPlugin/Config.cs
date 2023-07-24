// ReSharper disable InconsistentNaming

using System;
using TelegramAPIPlugin.TdlibSetup;

namespace TelegramAPIPlugin {
	public static class Config {
		public static readonly AuthSetting TestAccount00 =
			new AuthSetting(0000, "TESTVALUE", "0123456789", "private449");

		// ReSharper disable once InconsistentNaming
		public static readonly AuthSetting TestAccount01 =
			new AuthSetting(0000, "TESTVALUE", "012345678", "Lycamobile");
		
		
		public struct TgChannel {
			public readonly string Name;
			public readonly string ID;

			public long getID() {
				if (long.TryParse(ID, out var longID)) {
					return longID;
				} else {
					Console.WriteLine("Error! while getting ID von TgChannel");
					return 0;
				}
			}

			public TgChannel(string name, string id) {
				Name = name;
				ID = id;
			}
		}

		
		public static TgChannel Group_IMTestGroup = new TgChannel(name: "IMTestGroup", id: "-625670864");
		public static TgChannel Channel_IMTest = new(name: "IM-testChannel", id: "-1001881517875");
		public static TgChannel Channel_Witzeposten = new(name: "witzeposten", id: "-1001257402591");
		public static readonly string configPath = "../../config/TelegramClient_AuthSettings.json";
	}
}