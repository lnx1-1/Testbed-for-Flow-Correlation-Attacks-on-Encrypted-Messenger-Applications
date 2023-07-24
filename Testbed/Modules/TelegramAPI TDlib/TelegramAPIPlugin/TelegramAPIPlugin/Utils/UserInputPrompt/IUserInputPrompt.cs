namespace TelegramAPIPlugin.Utils {
	/// <summary>
	/// This Interface provides a UserPrompt (some sort of Window where the user gets a Text displayed and can answer with a string)
	/// the Interface hides different types of Prompts. The are specified by the Factory and are often platform dependent
	/// </summary>
	public interface IUserInputPrompt {
		/// <summary>
		/// Displays a Input prompt where the user can input a String.
		/// The Prompt closes upon completion
		/// </summary>
		/// <param name="promptText">string to show to the user. eg. "please insert something"</param>
		/// <returns>the answer the user gave</returns>
		public string ShowUserPrompt(string promptText);
	}
}