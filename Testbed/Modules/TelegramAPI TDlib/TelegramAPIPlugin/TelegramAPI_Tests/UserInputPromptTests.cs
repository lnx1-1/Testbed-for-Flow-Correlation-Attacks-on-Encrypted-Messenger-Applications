using System;
using NUnit.Framework;
using TelegramAPIPlugin.Utils;

namespace TelegramAPI_Tests {
	public class UserInputPromptTests {
		[Test]
		[NonParallelizable]
		public void TestMsgBox() {
			IUserInputPrompt inputPrompt = new WindowsBatchUserPrompt();
			string input = inputPrompt.ShowUserPrompt("Running Test: Enter [1234]");
			Assert.True(int.TryParse(input, out var result), $"not the right return value. Expected parseable Int but was [{input}]");
			
			Assert.AreEqual(1234, result);
			Console.WriteLine("TestDone");
		}

		[Test]
		[NonParallelizable]
		public void TestMsgBoxNoUserInput() {
			IUserInputPrompt inputPrompt = new WindowsBatchUserPrompt();
			string input = inputPrompt.ShowUserPrompt("Running Test: Just press Enter");
			Assert.AreEqual("",input);
		}
	}
}