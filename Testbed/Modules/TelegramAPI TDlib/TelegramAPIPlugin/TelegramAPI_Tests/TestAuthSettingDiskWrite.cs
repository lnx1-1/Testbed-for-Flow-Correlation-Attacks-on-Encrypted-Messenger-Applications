using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NUnit.Framework;
using TelegramAPIPlugin;
using TelegramAPIPlugin.TdlibSetup;
using TelegramAPIPlugin.Utils;

namespace TelegramAPI_Tests {
    public class TestAuthSettingDiskWrite {
        [Test]
        public void TestWriteReadAuthSettings() {
            var filePath = "../../Auth.json";
            var testList = new List<AuthSetting>() { Config.TestAccount00, Config.TestAccount00 };
            AuthSettingsFIleHandler.WriteAuthSettingsToJsonFile(testList,filePath);
            
            Assert.True(File.Exists(filePath));

            var authList = AuthSettingsFIleHandler.LoadAuthSettingsFromJson(filePath);

            // Assert.That(authList, Is.SameAs(testList));

            for (int i = 0; i < Math.Max(testList.Count, authList.Count);i++) {
                Assert.AreEqual(authList[i], testList[i]);
            }
        }
    }
}