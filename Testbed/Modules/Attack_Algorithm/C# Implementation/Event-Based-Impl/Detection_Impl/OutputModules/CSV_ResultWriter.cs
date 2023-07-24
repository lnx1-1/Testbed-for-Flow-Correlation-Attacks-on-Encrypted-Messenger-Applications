using System;
using System.Collections.Generic;
using System.IO;
using Event_Based_Impl.DataTypes;
using ProjectCommons;

namespace Event_Based_Impl.Utility_tools {
    public class CSV_ResultWriter {
        private const char Dlim = ';';
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static string getAsCSVLine(MsgEvent ev) {
            return $"{ev.timestamp}{Dlim} {ev.size}{Dlim} {ev.type}";
        }

        public static string getAsCSVLine(NetworkPacket pc) {
            return $"{pc.timestamp}{Dlim} {pc.size}";
        }

        public static string getCSVHeaderLine() {
            return $"timestamp{Dlim} size{Dlim}";
        }

       

        public static void WriteAsCsv<T>(List<T> burstEventList, string path, bool writeHeader = true) {
            if (IsOutputPathInvalid(path)) return;
            using StreamWriter fileWriteStream = new StreamWriter(path);
            if (writeHeader) {
                fileWriteStream.WriteLine(getCSVHeaderLine());
            }

            foreach (var obj in burstEventList) {
                if (typeof(T) == typeof(MsgEvent)) {
                    fileWriteStream.WriteLine(getAsCSVLine(obj as MsgEvent));
                } else if (typeof(T) == typeof(NetworkPacket)) {
                    fileWriteStream.WriteLine(getAsCSVLine(obj as NetworkPacket));
                }
            }

            fileWriteStream.Flush();
            log.Info($"Wrote a CSV_{typeof(T).Name} file to: {path}");
        }

        private static bool IsOutputPathInvalid(string path) {
            var parentPath = Directory.GetParent(path);
            if (!parentPath.Exists) {
                if (parentPath.Parent != null && parentPath.Parent.Exists) {
                    log.Warn("Creating subfolder that doesn't exist..");
                    parentPath.Create();
                } else {
                    log.Error($"This path and Parent Folder does not exist..Aborting CSV Write Out\n{parentPath}");
                    return true;
                }
            }

            return false;
        }
    }
    //
    // await using StreamWriter fileWriteStream = new StreamWriter(pathWrapper.OutFiles.ResultsFolder);
    // fileWriteStream.WriteLine(CSV_ResultWriter.getCSVHeaderLine());
    // foreach (var packet in packetLists) {
    // 	var outLine = "";
    // 	if (eventDict.ContainsKey(packet.timestamp)) {
    // 		outLine = CSV_ResultWriter.getAsCSVLine(eventDict[packet.timestamp]);
    // 		log.Debug($"Writing EventLine: {outLine}");
    // 	} else {
    // 		outLine = CSV_ResultWriter.getAsCSVLine(packet);
    // 	}
    //
    // 	fileWriteStream.WriteLine(outLine);
    // }
    //
    // await fileWriteStream.FlushAsync();
    //
    // log.Info("Wrote CSV file to: " + pathWrapper.OutFiles.ResultsFolder);
}