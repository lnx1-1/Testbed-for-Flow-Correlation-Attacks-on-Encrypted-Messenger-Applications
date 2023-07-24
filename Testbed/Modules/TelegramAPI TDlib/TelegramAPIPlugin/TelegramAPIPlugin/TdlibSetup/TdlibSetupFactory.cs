using System.Threading.Tasks;
using TdLib;
using TdLib.Bindings;
using TdLogLevel = TdLib.Bindings.TdLogLevel;

namespace TelegramAPIPlugin.TdlibSetup {
	public static class TdlibSetupFactory {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public static async Task<TdClient> SetupTdClient(AuthSetting setting, bool cleanSession) {

			// Creating Telegram client and setting minimal verbosity to Fatal since we don't need a lot of logs :)
			TdClient client = new TdClient();
			client.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);

			// Subscribing to all events
			client.UpdateReceived += async (_, update) => { await SetupUpdateHandler(update); };
			

			TdlibAuthenticator authenticator = new TdlibAuthenticator(setting);
			// Interactively handling authentication
			await authenticator.Authenticate(client, cleanSession);

			
			// if (cleanSession) {
			// 	await client.CloseAsync();
			// 	return await SetupTdClient(setting, false);
			// }
			
			await client.RemoveAllFilesFromDownloadsAsync();
			// await client.ReadAllChatMentionsAsync();
			// await client.ReadAllChatReactionsAsync();
			// await client.ReadAllMessageThreadMentionsAsync();
			// await client.ReadAllMessageThreadReactionsAsync();

			
			return client;
		}

		public static async Task<TdClient> SetupTdClient(AuthSetting setting) {
			return await SetupTdClient(setting, false);
		}


		private static Task SetupUpdateHandler(TdApi.Update update) {
			// Since Tdlib was made to be used in GUI application we need to struggle a bit and catch required events to determine our state.
			// Below you can find example of simple authentication handling.
			// Please note that AuthorizationStateWaitOtherDeviceConfirmation is not implemented.

			switch (update) {
				case TdApi.Update.UpdateConnectionState { State: TdApi.ConnectionState.ConnectionStateReady }:
					// You may trigger additional event on connection state change
					Log.Debug("Connection Ready!");
					break;
				default:
					// Log.Debug(update.GetType().ToString);
					//await Console.Out.WriteLineAsync(update.GetType().ToString());
					// ReSharper disable once EmptyStatement
					;
					// Add a breakpoint here to see other events
					break;
			}

			return Task.CompletedTask;
		}
	}
}