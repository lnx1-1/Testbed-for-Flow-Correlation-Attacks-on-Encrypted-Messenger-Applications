using System;
using System.Collections.Generic;
using System.IO;

namespace Event_Based_Impl.Utility_tools {
	public class MissingFile_detector {
		
		private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		
		public static void AnalyzeFolder(string folder) {
			string [] pcapFiles = Directory.GetFiles(folder);
			Dictionary<int, string> dict = new Dictionary<int, string>();
			
			foreach (var filePath in pcapFiles) {
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				int number = Int32.Parse(fileName.Split('-')[1]);
				dict.Add(number,fileName);
			}

			string generalFileName = Path.GetFileNameWithoutExtension(pcapFiles[0]);
			string fileNamePrefix = generalFileName.Split('-')[0];
			string fileNameExtension = Path.GetExtension(pcapFiles[0]);
			
			int missingCounter = 0;
			for (int i = 0; i < dict.Count; i++) {
				if (!dict.ContainsKey(i)) {
					log.Warn($"Missing File: {fileNamePrefix}-{i}{fileNameExtension}");
					missingCounter++;
				}
			}
			log.Warn("+++++++++++++++++++++++++++++++++++++++++");
			log.Warn($"Missing {missingCounter} files in Total\n");
		}
		
	}
}