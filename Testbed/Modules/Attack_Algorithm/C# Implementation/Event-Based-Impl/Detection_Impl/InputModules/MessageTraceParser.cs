using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTap;
using ProjectCommons;

namespace Event_Based_Impl.InputModules {
	/// <summary>
	/// A Parser for Telegram Message Traces..
	/// These Traces are normally Produced by a TG Logging App that observes incoming or Outgoing Messages and logs them in the form of a .txt file
	/// <see cref="E:\Datasets_uniPaper\telegram\normal\adversary_message_traces\channel-0.txt"/> 
	/// </summary>
	public static class MessageTraceParser {
		private const int SIZE_TOKEN_POSITION = 5;
		private const int TYPE_TOKEN_POS = 4;
		private const int LEN_TOKEN = 6;
		private const string SIZE_STRING_NOT_SET_CONTENT = "None";

		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		public static List<MsgEvent> ParseMessageTraceTxtFolder(string folderPath) {
			log.Info($"Parsing txt Message Traces from Folder: {folderPath}");
			string[] files = Directory.GetFiles(folderPath);
			return ParseMessageTraceTxtFiles(files.ToList());
		}


		public static List<MsgEvent> ParseMessageTraceTxtFiles(List<string> filePaths) {
			List<MsgEvent> outList = new List<MsgEvent>();
			foreach (var file in filePaths) {
				TapThread.ThrowIfAborted();
				outList = outList.Concat(ParseMessageTraceTxtFile(file)).ToList();
			}

			return outList;
		}


		/// <summary>
		/// Parses A Message Trace file `.txt` to a List of MsgEvents
		/// Timestamps are converted to Seconds since Epoch
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public static List<MsgEvent> ParseMessageTraceTxtFile(string filepath) {
			if (!File.Exists(filepath)) {
				log.Error("Provided Path isnt a File.. Consider choosing ParseMessageTraceFolder");
			}

			var lines = File.ReadLines(filepath).ToList();
			List<MsgEvent> msgEventList = new List<MsgEvent>();

			lines.RemoveAt(0); //Remove First line

			foreach (var line in lines) {
				string[] lineTokens = line.Split(' ');

				if (lineTokens.Length != LEN_TOKEN) {
					log.Warn($"Skipping Line: {line} \nformat not right");
					continue;
				}

				var timestamp = getTimestamp(lineTokens) / 1000; //Convert to Timestamp in Seconds with Decimal point
				var msgType = getMsgType(lineTokens);
				var msgBytes = getSize(lineTokens);

				MsgEvent ev = new MsgEvent() {
					size = msgBytes,
					type = msgType,
					timestamp = timestamp
				};
				msgEventList.Add(ev);

				// log.Debug(ev);
			}

			return msgEventList;
		}

		private static string getMsgType(string[] lineTokens) {
			string msgType = lineTokens[TYPE_TOKEN_POS];
			return msgType;
		}

		private static int getSize(string[] lineTokens) {
			int msgBytes = -1; //! war vorher auf 0
			string sizeString = lineTokens[SIZE_TOKEN_POSITION];

			if (sizeString.Equals(SIZE_STRING_NOT_SET_CONTENT)) {
				return msgBytes;
			}

			try {
				msgBytes = int.Parse(sizeString);
			} catch (Exception e) {
				log.Error(e, $"failed to parse MsgSize: [{sizeString}]: " + e.Message);
			}

			return msgBytes;
		}

		private static double getTimestamp(string[] lineTokens) {
			string timeString = lineTokens[1] + " " + lineTokens[2];
			double timestamp = -1;
			try {
				timestamp = ((DateTimeOffset)DateTime.Parse(timeString)).ToUnixTimeMilliseconds();
			} catch (Exception e) {
				log.Error(e, $"Error while parsing timestamp: {e.Message}");
				throw;
			}

			return timestamp;
		}
	}
}