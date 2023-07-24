using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjectCommons;
using ProjectCommons.IMAppMessageLogger;
using ProjectCommons.JsonMessageEventFileParsing;
using TdLib;
using TelegramAPIPlugin.TelegramClient;
using Timer = System.Timers.Timer;

namespace TelegramAPIPlugin {
	public class Program {
		public const string ApplicationVersion = "1.0.0";

		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public static async Task Main(string[] args) {
			MessageTraceLogger msgLogger = new MessageTraceLogger();
			TelegramClientFacade telegramClient = TelegramClientFactory.GetInstance(Config.TestAccount01)
				.GetAwaiter().GetResult();
			await telegramClient.PrintUserInfo();

			Console.WriteLine("Press ENTER to sendMsg");
			Console.ReadLine();

			
			// var chatID = telegramClient.LookupChatId(Config.Group_IMTestGroup.Name);

			// await telegramClient.SendPhoto(long.Parse(chatID), @"C:\Users\Linus\Pictures\static_5a6ce73712abd97a057586f2_5a6e02c2bc337941cb750576_5a6e02e4bc337941cb750a0d_1517159540558_sj3f.png");

			Thread.Sleep(1000);
			Console.WriteLine("Press ENTER to exit from application");
			Console.ReadLine();


			// createMessageLogData(telegramClient);
		}

		
		private static void createMessageLogData(TelegramClientFacade telegramClient) {
			var log = telegramClient.GetMsgLogger().GetMsgLog();
			var msgJsonFile = new JsonMessageEventFile(log);
			msgJsonFile.WriteToDisk("../../LogTestData.json");
		}

		private static void playground1(MessageTraceLogger msgLogger) {
			string path = "testMsgLog.csv";
			Console.WriteLine(Path.GetFullPath(path));
			File.WriteAllLines(path, msgLogger.GetLogAsCsv());

			List<string> lines = new List<string>(File.ReadLines(path));

			if (lines.Count < 1) {
				Console.WriteLine("ERROR");
				throw new ArgumentException("file not properly formatted. No Content");
			}

			lines.RemoveAt(0);
			var eventList = lines.Select(MsgLogTraceMarshaller.UnmarshallCsvMessageEventToMessageReceivedEventArgs)
				.Where(ev => (ev != null)).ToList();
		}


		private static string MarshallMsgEventToString(MsgEvent ev) {
			if (ev != null) {
				return string.Format($"{ev.timestamp}, {ev.size}, {ev.type}");
			} else {
				throw new ArgumentException("MessageReceivedEventsArgs is Null");
			}
		}
	}

	
}