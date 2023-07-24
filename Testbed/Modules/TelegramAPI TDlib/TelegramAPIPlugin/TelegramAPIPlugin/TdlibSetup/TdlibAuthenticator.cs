using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TdLib;
using TelegramAPIPlugin.Utils.UserInputPrompt;

namespace TelegramAPIPlugin.TdlibSetup {
	/// <summary>
	/// Handles all the Nessessary Authentication
	/// </summary>
	public class TdlibAuthenticator {
		private AuthSetting _setting;
		private bool _PasswordNeeded { get; set; }
		private bool _authNeeded;

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		public TdlibAuthenticator(AuthSetting setting) {
			_setting = setting;
		}

		private ManualResetEventSlim _ReadyToClose { get; } = new ManualResetEventSlim();
		private ManualResetEventSlim _ReadyToAuthenticate { get; } = new ManualResetEventSlim();


		~TdlibAuthenticator() {
			_ReadyToClose.Wait();
		}
		
		private async Task AuthenticationUpdatesHandler(TdApi.Update update, TdClient client) {
			switch (update) {
				case TdApi.Update.UpdateAuthorizationState {
					AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters
				}:
					
					// TdLib creates database in the current directory.
					// so create separate directory and switch to that dir.
					var filesLocation = _setting.DBlocation;
					await client.ExecuteAsync(new TdApi.SetTdlibParameters {
						ApiId = _setting.ApiId,
						ApiHash = _setting.ApiHash,
						DeviceModel = "PC",
						SystemLanguageCode = "en",
						ApplicationVersion = Program.ApplicationVersion,
						DatabaseDirectory = filesLocation,
						FilesDirectory = filesLocation,
						// More parameters available!
					});
					break;

				case TdApi.Update.UpdateAuthorizationState {
					AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber
				}:
				case TdApi.Update.UpdateAuthorizationState {
					AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitCode
				}:
					_authNeeded = true;
					_ReadyToAuthenticate.Set();
					break;

				case TdApi.Update.UpdateAuthorizationState {
					AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPassword
				}:
					_authNeeded = true;
					_PasswordNeeded = true;
					_ReadyToAuthenticate.Set();
					break;
				case TdApi.Update.UpdateAuthorizationState {
					AuthorizationState: TdApi.AuthorizationState.AuthorizationStateClosed
				}:
					_ReadyToClose.Set();
					Log.Info("Session ready Close!");
					break;

				case TdApi.Update.UpdateUser:
					_ReadyToAuthenticate.Set();
					break;
				default:
					// Log.Debug(update.GetType().ToString());
					// ReSharper disable once EmptyStatement
					;
					// Add a breakpoint here to see other events
					break;
			}
		}

		/// <summary>
		/// Uses a TDClient Instance on which all the Authentication will be handled
		/// </summary>
		/// <param name="client">A TDclient Instance to Authenticate on</param>
		public async Task Authenticate(TdClient client, bool cleanSession) {
			client.UpdateReceived += async (_, update) => {
				await AuthenticationUpdatesHandler(update, client);
			};

			Log.Debug("Waiting for Auth");
			_ReadyToAuthenticate.Wait();
			Log.Debug("Auth waiting done");

			if (!(_authNeeded || cleanSession)) {
				// We may not need to authenticate since TdLib persists session in 'td.binlog' file.
				// See 'TdlibParameters' class for more information, or:
				// https://core.telegram.org/tdlib/docs/classtd_1_1td__api_1_1tdlib_parameters.html
				Log.Debug("Session is still ready. Authentication Skipped");
				Log.Debug("Session DB Path: "+_setting.DBlocation);
				return;
			}

			Log.Debug($"Auth using PhoneNum: [{_setting.PhoneNumber}]");
			// Setting phone number
			await client.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber {
				
				PhoneNumber = _setting.PhoneNumber
			});

			// Telegram servers will send code to us
			string result = UserInputPromptFactory.GetPrompt().ShowUserPrompt(
				"Telegram Requires an authentication Code. Please enter the Code send to your Telegram Applications");


			await client.ExecuteAsync(new TdApi.CheckAuthenticationCode {
				Code = result
			});

			if (!_PasswordNeeded) {
				return;
			}

			// 2FA may be enabled. Cloud password is required in that case.
			Console.Write("Password Input Required");
			var password = UserInputPromptFactory.GetPrompt().ShowUserPrompt("2FA Enabled. Insert Cloud password:");

			await client.ExecuteAsync(new TdApi.CheckAuthenticationPassword {
				Password = password
			});
		}
	}
}