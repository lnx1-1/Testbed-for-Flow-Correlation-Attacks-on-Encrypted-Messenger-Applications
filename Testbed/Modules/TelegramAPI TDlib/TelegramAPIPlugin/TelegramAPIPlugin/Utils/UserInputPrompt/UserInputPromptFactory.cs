using System;
using System.Runtime.InteropServices;

namespace TelegramAPIPlugin.Utils.UserInputPrompt {
	
	/// <summary>
	/// Creates UserInputPrompts.. Determines which implementation is suitable for the current plattform
	/// </summary>
	public static class UserInputPromptFactory {
		
		
		/// <summary>
		/// Returns a Platform Specific Prompt 
		/// </summary>
		/// <returns>The UserInput Prompt</returns>
		/// <exception cref="NotImplementedException">Currently only windows is Supported</exception>
		public static IUserInputPrompt GetPrompt() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				return new WindowsBatchUserPrompt();
			} else {
				throw new NotImplementedException("Only Windows is implemented");
			}
		}
	}
}