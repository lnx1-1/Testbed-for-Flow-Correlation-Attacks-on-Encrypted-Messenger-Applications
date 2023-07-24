using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TelegramAPIPlugin.Utils {
	/// <summary>
	/// Windows only Prompt. Uses a Windows cmd.exe for the Prompt
	/// So the user gets a Terminal window displayed where he can enter a string and press enter. The window disapppears
	/// </summary>
	public class WindowsBatchUserPrompt : IUserInputPrompt {
		private const string BatchFileName = "prompt.bat";
		private const string IDFileName = "id.txt";

		/// <summary>
		/// Displays a Input prompt where the user can input a String.
		/// The Prompt closes upon completion
		/// </summary>
		/// <param name="promptText">string to show to the user. eg. "please insert something"</param>
		/// <returns>the answer the user gave</returns>
		public string ShowUserPrompt(string promptText) {
			string outString = "";
			string[] promptCode = new[] {
				"@echo off", "set id=NONE", $"set /p id={promptText} ", $"echo %id% > {IDFileName}"
			};

			//First create our batch file to run
			File.WriteAllLines(BatchFileName, promptCode);

			//Prepare our process to run our Batchfile
			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = "cmd.exe",
				WorkingDirectory = Directory.GetCurrentDirectory(),
				Arguments = @"/C .\" + BatchFileName,
				UseShellExecute = true,
				CreateNoWindow = false
			};

			//Starting out Process
			Process proc = Process.Start(startInfo);

			Console.WriteLine("Waiting for userInput");

			proc?.WaitForExit();

			if (File.Exists(IDFileName)) {
				var fileLines = File.ReadLines(IDFileName).ToList();
				if (fileLines.Count > 1) {
					Console.WriteLine("Contains more than one Line.. strange");
				} else if (fileLines.Count == 0) {
					Console.WriteLine("Error  -- no Input from Batch..");
				} else if (fileLines[0].Trim().Equals("NONE")) {
					Console.WriteLine("No User Input Provided");
				} else {
					Console.WriteLine($"Got userInput: [{fileLines[0]}]");
					outString = fileLines[0];
				}

				File.Delete(IDFileName);
				File.Delete(BatchFileName);
			} else {
				Console.WriteLine("Error. Couldn't find file " + IDFileName);
			}

			return outString;
		}
	}
}