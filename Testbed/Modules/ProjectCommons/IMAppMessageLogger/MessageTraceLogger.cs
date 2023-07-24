using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using ProjectCommons.Utils;
using TelegramAPIPlugin;
using TelegramAPIPlugin.TelegramClient;

namespace ProjectCommons.IMAppMessageLogger {
	public class MessageTraceLogger {
		private readonly ReaderWriterLockSlim _isLoggingLock = new ReaderWriterLockSlim();
		private readonly List<MessageReceivedEventArgs> _eventlog;
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();
		private bool _isLogging = false;

		private double _loggingStartTime;
		private const double PreLoggingTime = 2.0; // 1 Second PreCapture. -> Sometimes the remote Timestamps are a bit earlier. WhyEver

		private static int _idCounter = 0;

		private LoggingState state;
		
		private readonly int _id;
		public MessageTraceLogger() {
			_id = ++_idCounter;
			_eventlog = new List<MessageReceivedEventArgs>();
			state = new LoggingState();
		}

		public MessageTraceLogger(LoggingState state) {
			this.state = state;
			_eventlog = new List<MessageReceivedEventArgs>();
		}
		
		public void LogMsg(MessageReceivedEventArgs ev) {
			_isLoggingLock.EnterReadLock();
			// Log.Warn($">{_id}< Logging Called: isLogging: "+_isLogging);
			if (_isLogging) {
				Log.Debug($"Logging Received msg [{ev._Type}]");
				if (ev._Timestamp + PreLoggingTime >= _loggingStartTime) {
					Log.Info($"Logged msg [{ev._Type}]");
					ev._TimeDelay = UnixDateTime.GetUnixTimeNowDouble() - _loggingStartTime;
					_eventlog.Add(ev);
				} else {
					Log.Debug($"Skipped Old Message. StartTime: {_loggingStartTime}, timeStamp: {ev._Timestamp}");
				}
				Log.Debug($"_Eventlog containing [{_eventlog.Count} elements]");
			} else {
				Log.Debug($"Logging of ev "+ev+" Skipped. Because its disabled");
			}
			
			_isLoggingLock.ExitReadLock();
		}

		public void LogMsg(long timestmp, long len, string type) {
			var ev = new MessageReceivedEventArgs(timestamp: timestmp, len: len, type: type);
			LogMsg(ev);
		}

		public void StartLogging() {
			_isLoggingLock.EnterWriteLock();
			_loggingStartTime = UnixDateTime.GetUnixTimeNowDouble();
			_eventlog.Clear();
			_isLogging = true;
			state.loggingActive = true;
			_isLoggingLock.ExitWriteLock();
			Log.Info($"Start Logging Messages. T: [{_loggingStartTime}]");
		}

		public void StopLogging() {
			_isLoggingLock.EnterWriteLock();
			_isLogging = false;
			state.loggingActive = false;
			_isLoggingLock.ExitWriteLock();
			Log.Info($"Stop Logging Messages");
		}

		public List<MsgEvent> GetMsgLog() {
			Log.Debug($"returning Log. Containing [{_eventlog.Count}] events");
			return _eventlog.Select((ev) => ev.AsMsgEvent()).ToList();
		}

		public void ClearLog() {
			_eventlog.Clear();
		}

		/// <summary>
		/// Returns the log as CSV file with Header!
		/// </summary>
		/// <returns>the log. All CSV lines are contained in the string list</returns>
		public List<string> GetLogAsCsv() {
			var outLines = new List<string> {
				$"timestamp UNIX{MsgLogTraceMarshaller.DELIMITER} len in Bytes{MsgLogTraceMarshaller.DELIMITER} msgType (text audio photo)"
			};

			outLines.AddRange(_eventlog
				.Select(MsgLogTraceMarshaller
					.MarshallMsgReceiveEventArgsToString) //TODO Change That to parse Library function
				.ToList());

			return outLines;
		}

		public double StartTimeStamp() {
			return _loggingStartTime;
		}
	}
}