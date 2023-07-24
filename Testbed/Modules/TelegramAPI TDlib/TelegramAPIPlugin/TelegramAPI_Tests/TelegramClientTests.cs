using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ProjectCommons;
using ProjectCommons.DataTypes;
using ProjectCommons.JsonMessageEventFileParsing;using TdLib;
using TelegramAPIPlugin;
using TelegramAPIPlugin.TelegramClient;

namespace TelegramAPI_Tests {
	public class TelegramClientTests {
		private I_IMApp _telegramClient;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private static readonly string FilePathToJsonTestDataFIle = "../../TestData/LogTestData.json";
		private static readonly string AllMediaJsonTestFile = "../../TestData/AllMediaTestData.json.json";
		private static readonly string ImageFolderPath = "../../TestData/TestData/img/";
		private static readonly string TestVideoPath = "../../TestData/media/video_2023-03-28_14-26-34.mp4";
		private static readonly string TestAudioPath = "../../TestData/media/wie_heißt_der_track.mp3";

		[OneTimeSetUp]
		public void Setup() {
			//target
			// ConfigurationItemFactory.Default.Targets
			// 	.RegisterDefinition("NunitLogger", typeof(TestTargetLogger));

			_telegramClient = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
			// Thread.Sleep(10000);
			Log.Info("TestSetup Done");
		}

		[OneTimeTearDown]
		public void Teardown() {
			Log.Info("Tearing Down TestSession");
			((TelegramClientFacade)_telegramClient).CloseSession();
		}

		[Test]
		[NonParallelizable]
		public void TestInstantiate() {
			Assert.IsNotNull(_telegramClient);
		}

		private async Task<IList<TdApi.Chat>> GetSyncedChats(IAsyncEnumerable<TdApi.Chat> asyncChats) {
			List<TdApi.Chat> chatList = new List<TdApi.Chat>();
			await foreach (var chat in asyncChats) {
				chatList.Add(chat);
			}

			return chatList;
		}


		[Test]
		[NonParallelizable]
		public void TestLookupChatId() {
			Assert.AreEqual(Config.Channel_Witzeposten.ID,
				_telegramClient.LookupChatId(Config.Channel_Witzeposten.Name));
			Assert.AreEqual("", _telegramClient.LookupChatId("adsfgjhkgdsfghjdsrtfzhgju"));
			// Assert.AreEqual(Config.Channel_IMTest.ID, _telegramClient.LookupChatId("IM-testChannel"));
			Assert.AreEqual(Config.Group_IMTestGroup.ID, _telegramClient.LookupChatId(Config.Group_IMTestGroup.Name));
		}

		[Test]
		[NonParallelizable]
		public void TestIsChatJoinedMethod() {
			string id = _telegramClient.LookupChatId("witzeposten");
			_telegramClient.JoinChat(id);
			Assert.True(((TelegramClientFacade)_telegramClient).IsChatJoined(id));
		}

		[Test]
		[NonParallelizable]
		public void TestJoinAndLeaveChat() {
			string chatName = "witzeposten";
			string chatId = _telegramClient.LookupChatId(chatName);

			_telegramClient.LeaveChat(chatId);

			Assert.IsTrue(_telegramClient is TelegramClientFacade);
			var tgClient = (TelegramClientFacade)_telegramClient;
			Assert.False(tgClient.IsChatJoined(chatId), "Chats not Leaved Correctly");


			_telegramClient.JoinChat(chatId);

			Thread.Sleep(500);

			Assert.True(tgClient.IsChatJoined(chatId), "chat is not Joined..");
		}

		[Test]
		[NonParallelizable]
		public void TestChatLookupNotFound() {
			string id = _telegramClient.LookupChatId("123456789hfgöojkwnhsreagikljöbsrgedlköjbsvfd");
			Assert.IsEmpty(id, $"The String was: [{id}]. Expected empty String");
			Assert.IsEmpty(_telegramClient.LookupChatId(""));
			Assert.IsEmpty(_telegramClient.LookupChatId("+#+#"));
		}

		[Test]
		[NonParallelizable]
		public void TestChatJoinEmptyInput() {
			Assert.False(_telegramClient.JoinChat(""));
			Assert.False(_telegramClient.JoinChat("0"));
			Assert.False(_telegramClient.JoinChat("-1"));
		}

		[Test]
		[NonParallelizable]
		public void TestChatLeaveWrongIDs() {
			Assert.False(_telegramClient.LeaveChat(""));
			Assert.False(_telegramClient.LeaveChat("1234"));
			Assert.False(_telegramClient.LeaveChat("-1"));
			Assert.False(_telegramClient.LeaveChat("0"));
		}

		[Test]
		[NonParallelizable]
		public void TestIsChatJoined() {
			Assert.False(_telegramClient.IsChatJoined(""));
			Assert.False(_telegramClient.IsChatJoined("-1"));
			Assert.False(_telegramClient.IsChatJoined("0"));
		}

		[Test]
		[NonParallelizable]
		public void TestSendTextMessage() {
			string message = "hello World";
			string groupID = _telegramClient.LookupChatId(Config.Group_IMTestGroup.Name);

			var _160Acc = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
			_160Acc.GetMsgLogger().StartLogging();

			_telegramClient.SendMessage(groupID, new MessageContentText(message));

			Thread.Sleep(1000);
			_160Acc.GetMsgLogger().StopLogging();
			var log = _160Acc.GetMsgLogger().GetMsgLog();
			Log.Info($"Received {log.Count} msgs");
			Xunit.Assert.True(log.Exists((ev) => {
				if (!ev.messageText.Equals(message)) {
					Log.Info($"no Match: ev: [{ev.messageText}], msg: [{message}]");
				}

				Log.Info(ev);
				return ev.messageText.Equals(message);
			}), $"message isnt contained in Log. LogLen: {log.Count}");
		}


		// [Test]
		// [NonParallelizable]
		// public void TestSendReceivePictureMessage() {
		// 	string path =
		// 		@"B:\Users\Linus\git\cryptCorr\Plugins\TelegramAPI TDlib\TelegramAPIPlugin\TelegramAPI_Tests\testData\test1.png";
		//
		// 	var facade = ((TelegramClientFacade)_telegramClient);
		// 	var _160Acc = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
		// 	string groupID = _160Acc.LookupChatId(Config.Group_IMTestGroup.Name);
		//
		// 	facade.GetMsgLogger().StartLogging();
		//
		// 	_160Acc.SendPhoto(long.Parse(groupID), path).GetAwaiter().GetResult();
		// 	var now = UnixDateTime.GetUnixTimeNowDouble();
		// 	Thread.Sleep(800);
		// 	facade.GetMsgLogger().StopLogging();
		// 	var log = facade.GetMsgLogger().getMsgLog();
		//
		// 	Assert.That(log, Has.Count.EqualTo(1));
		// 	Assert.That(log[0].timestamp, Is.EqualTo(now).Within(0.9));
		// }

		public void TestLogMessagesToDisk() {
			var facade = ((TelegramClientFacade)_telegramClient);
			var _160Acc = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
			var msgEvents = JsonMessageEventFile.ReadMessageEventFile(FilePathToJsonTestDataFIle).GetAwaiter();
			var evs = new List<MsgEvent>();
			var id = 0;

			foreach (var imgPath in Directory.GetFiles(ImageFolderPath)) {
				var fileInfo = new FileInfo(imgPath);
				evs.Add(new MsgEvent()
					{ id = id++, path = imgPath, size = (int)fileInfo.Length, type = "photo", timeDelay = (id + 1) });
			}
		}

		[NonParallelizable]
		[Test]
		public void TestSendingAllMediaTypes() {
			var facade = ((TelegramClientFacade)_telegramClient);
			var _160Acc = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
			var chatID = _160Acc.LookupChatId(Config.Group_IMTestGroup.Name);
			var msgEvents = new List<MsgEvent>();
			var audioInfo = new FileInfo(TestAudioPath);
			var videoInfo = new FileInfo(TestVideoPath);
			var testText = "Hello This is TestMessage";
			msgEvents.Add(new MsgEvent()
				{ size = (int)audioInfo.Length, id = 0, path = TestAudioPath, type = "audio", timeDelay = 2 });
			msgEvents.Add(new MsgEvent()
				{ size = (int)videoInfo.Length, id = 0, path = TestVideoPath, type = "video", timeDelay = 8 });
			msgEvents.Add(new MsgEvent() {
				size = Encoding.UTF8.GetBytes(testText).Length, id = 0, messageText = testText, type = "text",
				timeDelay = 13
			});
			facade.GetMsgLogger().StartLogging();
			MessageInjector.MessageInjector.ExecMessageScript(msgEvents, chatID, _160Acc, new CancellationToken()).GetAwaiter().GetResult();
			Thread.Sleep(5000);
			facade.GetMsgLogger().StopLogging();
			var loggedLog = facade.GetMsgLogger().GetMsgLog();
			Assert.That(msgEvents.Count, Is.EqualTo(loggedLog.Count));

			for (int i = 0; i < msgEvents.Count; i++) {
				var currLoggedEvent = loggedLog[i];
				var currInjectedEv = msgEvents[i];

				Assert.True(currInjectedEv.EqualsIgnoreTimeAndPath(currLoggedEvent),
					"msgEvent: " + currInjectedEv + $" loggedEvs[{i}]: " + currLoggedEvent);
				Assert.That(currInjectedEv.timeDelay, Is.EqualTo(currLoggedEvent.timeDelay).Within(5),
					"msgEvent: " + currInjectedEv + $" loggedEvs[{i}]: " + currLoggedEvent);
			}
		}


		[NonParallelizable]
		[Test]
		public void TestSendingMessageScript() {
			var facade = ((TelegramClientFacade)_telegramClient);
			var _160Acc = TelegramClientFactory.GetInstance(Config.TestAccount00).GetAwaiter().GetResult();
			var msgEvents = JsonMessageEventFile.ReadMessageEventFile(FilePathToJsonTestDataFIle).GetAwaiter()
				.GetResult()._MsgEvents;
			var chatID = _160Acc.LookupChatId(Config.Group_IMTestGroup.Name);
			var testChatID = facade.LookupChatId(Config.Group_IMTestGroup.Name);
			facade.JoinChat(testChatID);
			Thread.Sleep(800);
			facade.GetMsgLogger().StartLogging();

			MessageInjector.MessageInjector.ExecMessageScript(msgEvents, chatID, _160Acc, new CancellationToken()).GetAwaiter().GetResult();

			Thread.Sleep(1800);
			facade.GetMsgLogger().StopLogging();

			var loggedEvs = facade.GetMsgLogger().GetMsgLog();
			Log.Info("Count of Events:" + loggedEvs.Count);
			Assert.That(msgEvents, Has.Count.EqualTo(loggedEvs.Count));
			for (int i = 0; i < msgEvents.Count; i++) {
				var currLoggedEvent = loggedEvs[i];
				var currInjectedEv = msgEvents[i];

				Assert.True(currInjectedEv.EqualsIgnoreTimeAndPath(currLoggedEvent),
					"msgEvent: " + currInjectedEv + $" loggedEvs[{i}]: " + currLoggedEvent);
				Assert.That(currInjectedEv.timeDelay, Is.EqualTo(currLoggedEvent.timeDelay).Within(3),
					"msgEvent: " + currInjectedEv + $" loggedEvs[{i}]: " + currLoggedEvent);
			}
		}

		[Test]
		[NonParallelizable]
		public void TestGetAllChatsFromUser() {
			var facade = (TelegramClientFacade)_telegramClient;
			var channelIDs = facade.GetAllChats();
			Log.Info("Fetched Channels...");
			channelIDs.ForEach(Log.Info);
			Assert.That(channelIDs.Count, Is.Positive);
		}
	}
}