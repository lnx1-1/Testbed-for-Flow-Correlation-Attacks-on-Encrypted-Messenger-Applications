using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using OpenTap;
using ProjectCommons.DataTypes;

namespace IMAppConnection {
	[Display("SetupMessageChannels")]
	public class SetupMessageChannels : TestStep {
		#region Settings

		[Display("Use Chat Name File", Order: 1.1)]
		public bool UseChannelNameFile { get; set; } = false;

		[Display("Leave Other Channels",
			"True if only the specified Channels should be joined. All other channels would be left.", Order: 1.2)]
		public bool leaveAllOtherChannels { get; set; } = false;

		[Display("Channel/Chat Names to Join", "The Message Channel names to Join and Observe", Order: 2.1)]
		[EnabledIf("UseChannelNameFile", false)]
		[Browsable(true)]
		[Output]
		public List<string> MessageChannels { get; set; }

		[Display("Channel/Chat Name List (.csv)", "A csv file containing a List of Channel names to Join", Order: 2.2)]
		[EnabledIf("UseChannelNameFile", true)]
		[FilePath(FilePathAttribute.BehaviorChoice.Open, fileExtension: "csv")]
		public string ChannelNameFilePath { get; set; } = @"..\..\TestData\channelNames.csv";

		[Display("IMApp Instrument", "The IMApp to use", Group: "Instrument setup", Collapsed: true)]
		public IOpenTapIMApp IMApp { get; set; }

		#endregion

		public SetupMessageChannels() {
			MessageChannels = new List<string>() { "IMTestGroup" };
		}

		public override void PrePlanRun() {
			base.PrePlanRun();
			if (IMApp == null) {
				throw new ArgumentException("Missing IMApp Instrument!!");
			}
		}

		public string ExtractUserName(string usrName) {
			return usrName.Replace("https://t.me/joinchat/","").Replace(@"https://t.me/", "").Replace("@", "");
		}
		
		
		public override void Run() {
			Log.Info("");
			Log.Info("-------------IM Channel Setup----------------");
			
			if (UseChannelNameFile) {
				if (ChannelNameFilePath != null && File.Exists(ChannelNameFilePath)) {
					MessageChannels = File.ReadLines(ChannelNameFilePath)
						.Select((str)=>str.Trim())
						.Where((str) => str.Length > 0)
						.Select(ExtractUserName)
						.ToList();
					Log.Debug($"Read {MessageChannels.Count} Channels from File");
				}
			}

			List<string> allJoinedChatIDs = new List<string>();
			if (leaveAllOtherChannels) {
				allJoinedChatIDs = IMApp.GetAllChats();
			}

			Dictionary<string, string> channelNameIDDict = new Dictionary<string, string>();
			
			if (MessageChannels != null && MessageChannels.Count > 0) {
				foreach (var channelName in MessageChannels) {
					Log.Debug($"Trying to Join Group: {channelName}");
					var channelID = IMApp.LookupChatId(channelName);
					if (channelID.Equals("")) {
						continue;
					}
					channelNameIDDict.Add(channelName,channelID);
					allJoinedChatIDs.Remove(channelID);
					if (!IMApp.IsChatJoined(channelID)) {
						if (!IMApp.JoinChat(channelID)) {
							Log.Warning($"Couldn't join {channelName}");
						} else {
							Log.Info($"Joined Chat {channelName}");
						}
					} else {
						Log.Info($"Already Participant of Group {channelName}");
					}
					TapThread.Sleep(800);
				}
			}

			if (allJoinedChatIDs.Count > 0 || MessageChannels?.Count > 0) {
				UpgradeVerdict(Verdict.Pass);
			}
			
			var postChannelIDs = IMApp.GetAllChats();
			bool everyChannelJoined = true;
			foreach (var channelName in MessageChannels) {
				if (!postChannelIDs.Contains(channelNameIDDict[channelName])) {
					Log.Warning($"Channel [{channelName} is Missing]");
					UpgradeVerdict(Verdict.Inconclusive);
					everyChannelJoined = false;
				} 
			}

			if (everyChannelJoined) {
				Log.Info($"Succesfully Joined all {channelNameIDDict.Count} Channels!");
			}
			
			foreach (var idToLeave in allJoinedChatIDs) {
				if (IMApp.LeaveChat(idToLeave)) {
					Log.Info($"Successfully left chatID [{idToLeave}]");
				} else {
					Log.Debug($"Couldn't leave Chat with ID: {idToLeave}");
				}
			}

			Log.Info("-------------IM Channel Setup Done----------------");
			Log.Info("");
			RunChildSteps(); //If step has child steps.
			
		}

		public override void PostPlanRun() {
			// ToDo: Optionally add any cleanup code this step needs to run after the entire testplan has finished
			base.PostPlanRun();
		}
	}
}