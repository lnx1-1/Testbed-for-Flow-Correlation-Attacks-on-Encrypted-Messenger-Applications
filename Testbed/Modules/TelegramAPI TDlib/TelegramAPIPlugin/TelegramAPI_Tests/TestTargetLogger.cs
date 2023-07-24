using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace TelegramAPI_Tests {
	[Target("NunitLogger")]
	public class TestTargetLogger: TargetWithLayout {
		protected override void Write(LogEventInfo logEvent) {
			TestContext.Progress.WriteLine(Layout.Render(logEvent));
		}
	}
}