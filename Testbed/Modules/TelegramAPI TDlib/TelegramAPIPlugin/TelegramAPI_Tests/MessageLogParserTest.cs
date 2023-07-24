using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectCommons.IMAppMessageLogger;
using ProjectCommons.JsonMessageEventFileParsing;
using ProjectCommons.Utils;
using TelegramAPIPlugin;

namespace TelegramAPI_Tests {
	public class MessageLogParserTest {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		[Test]
		public void TestMessageTraceLoggerParser() {
			var msgEvents = new List<MessageReceivedEventArgs> {
				new MessageReceivedEventArgs(0, UnixDateTime.GetUnixTimeNowDouble() , "none", false),
				new MessageReceivedEventArgs(1000, UnixDateTime.GetUnixTimeNowDouble()+1, "audio", false),
				new MessageReceivedEventArgs(-1, UnixDateTime.GetUnixTimeNowDouble()+8, "", false),
				new MessageReceivedEventArgs(-1, UnixDateTime.GetUnixTimeNowDouble()+22, "Hello\nduDa", false)
			};


			MessageTraceLogger logger = new MessageTraceLogger();
			logger.StartLogging();
			msgEvents.ForEach(logger.LogMsg);
			var logEvList = logger.GetLogAsCsv()
				.Select(MsgLogTraceMarshaller.UnmarshallCsvMessageEventToMessageReceivedEventArgs)
				.Where(ev => ev != null)
				.ToList();

			Log.Debug("----- Expected--------");
			msgEvents.ForEach(Log.Debug);
			Log.Debug("------But Was:-------");
			logEvList.ForEach(Log.Debug);
			Log.Debug("-------------");

			Assert.That(logEvList.Count,Is.EqualTo(msgEvents.Count));
			for(var i = 0; i< Math.Min(logEvList.Count, msgEvents.Count);i++) {
				Assert.True(logEvList[i].EqualsIgnoreDelay(msgEvents[i]));
			}
		}

		[Test]
		public void TestJsonMessageScript() {
			var msgEventsArgs = new List<MessageReceivedEventArgs> {
				new MessageReceivedEventArgs(0, 0, "none", false){_Path = "TestPath"},
				new MessageReceivedEventArgs(1000, 1551365636, "audio", false){_Path = "TestPath123"},
				new MessageReceivedEventArgs(-1, -1.551365636, "", false),
				new MessageReceivedEventArgs(-1, -155136.5636, "Hello\nduDa", false){_Path = ""}
			};

			var msgEvents = msgEventsArgs.Select(ev => ev.AsMsgEvent()).ToList();
			
			string filePath = "./jsonTestFile.json";
			var jsonEvFile = new JsonMessageEventFile(msgEvents);
			jsonEvFile.WriteToDisk(filePath, true);

			var loadedEvFile = JsonMessageEventFile.ReadMessageEventFile(filePath).GetAwaiter().GetResult();
			var loadedEvs = loadedEvFile._MsgEvents;
			Assert.That(msgEvents,Is.EquivalentTo(loadedEvs));
		}

		
	}
}