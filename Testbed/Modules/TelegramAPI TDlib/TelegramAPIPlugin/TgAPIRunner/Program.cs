using TelegramAPI_Tests;

namespace TgAPIRunner // Note: actual namespace depends on the project name.
{
	internal class Program
	{
		static void Main(string[] args) {
			var testClass = new TelegramClientTests();
			testClass.Setup();
			testClass.TestSendingAllMediaTypes();
			//TelegramAPIPlugin.Program.Main(args).GetAwaiter().GetResult();
		}
		
		
	}
}