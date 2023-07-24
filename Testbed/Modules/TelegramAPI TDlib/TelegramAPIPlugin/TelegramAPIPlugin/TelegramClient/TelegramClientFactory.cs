using System;
using System.Threading.Tasks;
using ProjectCommons.IMAppMessageLogger;
using TelegramAPIPlugin.TdlibSetup;
using TdLib;

namespace TelegramAPIPlugin.TelegramClient {
	/// <summary>
	/// This Factory provides a Instance of the Telegram Client.
	/// </summary>
	public static class TelegramClientFactory {
		/// <summary>
		/// Creates an instance of the TelegramClient.
		/// The Instance will be ready to use
		/// </summary>
		/// <returns>the TelegramClientFacade instance</returns>
		public static async Task<TelegramClientFacade> GetInstance() {
			return await GetInstance(Config.TestAccount01);
		}

		
		
		public static async Task<TelegramClientFacade> GetInstance(AuthSetting account) {
			
			Console.WriteLine($"Using PhoneNr: {account.PhoneNumber}");

			var tgState = new LoggingState();
			
			//Setup Tdlib (Telegram Connector Library)
			var tdClient = await TdlibSetupFactory.SetupTdClient(account);
			
			//Instantiate Abstraction Layer
			var CAL = new ClientAbstractionLayer(tdClient);
			
			//Create and Connect Msg Handler
			var msgHandler = new TelegramClientMessageHandler(CAL, tgState);
			tdClient.UpdateReceived += (_, update) => {msgHandler.HandleUpdate(update);};
			
			//Create and Connect Msg Logger
			MessageTraceLogger msgLog = new MessageTraceLogger(tgState);
			msgHandler.OnMessageReceived += (_, ev) => msgLog.LogMsg(ev);
			
			//Create Facade
			var facade = new TelegramClientFacade(CAL, msgLog);

			return facade;
		}
	}
}