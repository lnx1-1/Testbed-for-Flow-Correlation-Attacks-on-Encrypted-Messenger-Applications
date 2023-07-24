using NLog;
using NLog.Targets;
using OpenTap;

namespace LoggingAdapterNlogtoOTAP;

/// <summary>
/// Ist ein Adapter Target welches auf die OpenTAP logging console Loggt.
/// Rules und setup lässt sich in der NLog Config spezifizieren
/// <see href="./example.config">Link Text</see>
/// example: ``target name="TAPLogAdapter" xsi:type="NLogToOTAPAdapter" layout="|${pad:padding=5:inner=${level:uppercase=true}} | ${message} | ${logger}"``
/// Die DLL sollte includiert werden: extensions>
///add assembly="LoggingAdapterNlogtoOTAP"/>
///	extensions
/// </summary>
[Target("NLogToOTAPAdapter")]
public class NlogOpenTapTarget : TargetWithLayout {
	private readonly TraceSource _Log = OpenTap.Log.CreateSource("Nlog");

	protected override void Write(LogEventInfo logEvent) {
		string logMessage = this.Layout.Render(logEvent);
		if (logEvent.Level == LogLevel.Info) {
			_Log.Info(logMessage);
		} else if (logEvent.Level == LogLevel.Warn) {
			_Log.Warning(logMessage);
		} else if (logEvent.Level == LogLevel.Error) {
			_Log.Error(logMessage);
		} else if (logEvent.Level == LogLevel.Debug) {
			_Log.Debug(logMessage);
		}
	}
}