using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectCommons;
using ProjectCommons.DataTypes;
using Timer = System.Timers.Timer;

namespace MessageInjector {
	public class MessageInjector {
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		/// <summary>
		/// Sends the given Messages to the TargetChatID using the given MsgApp
		/// Waits until all messages were send (synchronous)
		/// </summary>
		/// <param name="msgList">The List containing the Message to Send</param>
		/// <param name="targetChatID">The ID of the Target chat</param>
		/// <param name="msgApp">The Message application used to Send the Messages</param>
		public static Task ExecMessageScript(List<MsgEvent> msgList, string targetChatID, I_IMApp msgApp) {
			CountdownEvent countdown = new CountdownEvent(msgList.Count);
			foreach (var msgEv in msgList) {
				System.Timers.ElapsedEventHandler evHandle = (o, s) => countdown.Signal(1);
				switch (msgEv.type) {
					case "text":
						evHandle += (obj, sender) => msgApp.SendMessage(targetChatID,new MessageContentText(msgEv.messageText));
						break;
					case "photo":
						evHandle += (o, s) => msgApp.SendMessage(targetChatID,new MessageContentPhoto(msgEv.path));
						break;
					case "audio":
						evHandle += (o, s) => msgApp.SendMessage(targetChatID,new MessageContentAudio(msgEv.path));
						break;
					case "video":
						evHandle += (o, s) => msgApp.SendMessage(targetChatID,new MessageContentVideo(msgEv.path));
						break;
					default:
						Log.Error($"MsgType: [{msgEv.type}] Not Supported Yet!!");
						countdown.Signal(1);
						continue;
				}

				double timeDelay = msgEv.timeDelay * 1000; //Timestamps in MsgEvents are Seconds based. Conversion to ms
				Timer t = new Timer(timeDelay);
				t.Elapsed += evHandle;
				t.AutoReset = false;
				t.Enabled = true;
				
				Log.Debug("Started Timer with T: "+timeDelay);
			}
			return Task.Run(countdown.Wait);
		}
	}
}