using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Event_Based_Impl.InputModules.JsonMessageEventFileParsing {
	/// <summary>
	/// Reads in Timestamp Files which containing the ID and Timestamps of some Message Files.
	/// </summary>
	public static class TimestampFileParser {
		private const int TIMESTAMP_FILE_NUMBER_OF_TOKENS = 4;
		private const int ID_POSITION = 3;
		private const int TIMESTAMP_POSITION = 2;
		private const string TIMESTAMP_FILE_ENDING = "*.txt";
		private const char SPLIT_TOKEN = ' ';

		/// <summary>
		/// Reads in Timestamp File and returns Dictionary with ID and Timestamp pairs
		/// </summary>
		/// <param name="timeStampFilePath"> the Path to the Timestamp file</param>
		/// <returns>A Dictionary with ID as Key (int) and timestamp as Value (double) </returns>
		/// <exception cref="ArgumentException">When the File is malformed..</exception>
		public static Dictionary<int, double> ParseTimeStampFile(string timeStampFilePath) {
			IEnumerable<string> lines = File.ReadLines(timeStampFilePath);
			var dict = new Dictionary<int, double>();

			foreach (string line in lines) {
				string[] lineTokens = line.Split(SPLIT_TOKEN);
				if (lineTokens.Length != TIMESTAMP_FILE_NUMBER_OF_TOKENS) {
					throw new ArgumentException("malformed Line: " + line);
				}

				int id = Int32.Parse(lineTokens[ID_POSITION]);
				double timestamp = (double.Parse(lineTokens[TIMESTAMP_POSITION]))/(1000*1000); //Convert it to Seconds based
				dict.Add(id, timestamp);
			}

			return dict;
		}

		/// <summary>
		/// parses a whole Timestamp folder
		/// </summary>
		/// <param name="folderPath"></param>
		/// <returns></returns>
		public static Dictionary<int, double> ParseTimeStampFolder(string folderPath) {
			var outDict = new Dictionary<int, double>();
			string[] files = Directory.GetFiles(folderPath, TIMESTAMP_FILE_ENDING);
			foreach (var file in files) {
				outDict = outDict.Concat(ParseTimeStampFile(file)).ToDictionary(s => s.Key, k => k.Value);
			}

			return outDict;
		}
	}
}