using System;
using System.Collections.Generic;
using System.Threading;
using OpenTap;
using ProjectCommons.DataTypes;
using ProjectCommons.JsonMessageEventFileParsing;
using TelegramAPIPlugin;

namespace IMAppConnection
{
    [Display("MessageSender")]
    public class SendMessages_Step : TestStep
    {
        private readonly TraceSource _Log = OpenTap.Log.CreateSource(nameof(SendMessages_Step));
        #region Settings
        
        [Display("MessageScript", "A .JSON File containing Messages to send. This file can be created by the Logging Teststeps", Order: 2.2)]
        [FilePath(FilePathAttribute.BehaviorChoice.Open, fileExtension: "json")]
        public string messageScriptJSONPath { get; set; } 
        
        
        [Display("IMApp Instrument", "The IMApp to use", Group: "Instrument setup", Collapsed: true)]
        public IOpenTapIMApp ImApp { get; set; }

        [Display("Message Channel", Description: "The Message Channel Name to Send the Messages to")]
        public string MessageChannelName { get; set; } = Config.Group_IMTestGroup.Name;
        #endregion

        private string msgChannelID = "";
        public SendMessages_Step() {
            messageScriptJSONPath = "../../TestData/LogTestData.json";
        }

        public override void PrePlanRun()
        {
            msgChannelID = ImApp.LookupChatId(MessageChannelName);
            if (msgChannelID == null) {
               _Log.Error("Channel ID Error");
                UpgradeVerdict(Verdict.Error);
            }

            if (messageScriptJSONPath == null || messageScriptJSONPath.Trim().Equals("")) {
                _Log.Error("Message Script not set!");
                UpgradeVerdict(Verdict.Error);
            }

            base.PrePlanRun();
        }

        public override void Run() {
            var msgEvents = JsonMessageEventFile.ReadMessageEventFile(messageScriptJSONPath).GetAwaiter()
                .GetResult()._MsgEvents;
            
            _Log.Info($"Preparing Message Send for {msgEvents.Count} Messages, from file {messageScriptJSONPath}");
            
            MessageInjector.MessageInjector.ExecMessageScript(msgEvents,msgChannelID,ImApp, TapThread.Current.AbortToken).GetAwaiter().GetResult();
            RunChildSteps(); //If step has child steps.
            UpgradeVerdict(Verdict.Pass);
        }

        public override void PostPlanRun()
        {
            base.PostPlanRun();
        }
    }
}
