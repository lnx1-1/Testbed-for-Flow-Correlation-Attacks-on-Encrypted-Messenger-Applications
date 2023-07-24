using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Event_Based_Impl.DataTypes;
using Event_Based_Impl.InputModules;
using ProjectCommons;
using ProjectCommons.JsonMessageEventFileParsing;

namespace Event_Based_Impl {
    public class Loader {
        public class FilesWrapper {
            public List<List<MsgEvent>> TraceFiles { get; set; }
            public List<List<NetworkPacket>> NetworkFiles { get; set; }

            public FilesWrapper() {
            }

            public FilesWrapper(List<(List<NetworkPacket> netPackets, List<MsgEvent> channelTrace)> zipped) {
                TraceFiles = new List<List<MsgEvent>>();
                NetworkFiles = new List<List<NetworkPacket>>();
                foreach (var tupel in zipped) {
                    TraceFiles.Add(tupel.channelTrace);
                    NetworkFiles.Add(tupel.netPackets);
                }
            }

            public FilesWrapper(List<List<MsgEvent>> traceFiles, List<List<NetworkPacket>> networkFiles) {
                TraceFiles = traceFiles;
                NetworkFiles = networkFiles;
            }

            public List<(List<NetworkPacket> netPackets, List<MsgEvent> channelTrace)> Zipped() {
                return NetworkFiles.Zip(TraceFiles,
                    (netPackets, channelTrace) => (netPackets: netPackets, channelTrace: channelTrace)).ToList();
            }
        }

        public static async Task<FilesWrapper> LoadFilesAsync(string pcapFolderPath, string msgTracePath,
            int numberOfFiles) {
            var examplePcapFileName = Directory.EnumerateFiles(pcapFolderPath).First();
            var exampleTraceFileName = Directory.EnumerateFiles(msgTracePath).First();

            var pcapFileNames = PathWrapper.GetFilesByNum(pcapFolderPath, examplePcapFileName, numberOfFiles);
            var msgTraceFileNames = PathWrapper.GetFilesByNum(msgTracePath, exampleTraceFileName, numberOfFiles);
            return await LoadFilesAsync(pcapFileNames, msgTraceFileNames);
        }


        public static async Task<FilesWrapper> LoadFilesAsync(string pcapFolderPath, string msgTracePath) {
            var netPathList = Directory.EnumerateFiles(pcapFolderPath).ToList();
            var tracesPathList = Directory.EnumerateFiles(msgTracePath).ToList();
            return await LoadFilesAsync(netPathList, tracesPathList);
        }

        public static async Task<FilesWrapper> LoadFilesAsync(List<string> pcapPaths, List<string> traceFilePaths) {
            //parse files
            var packetFileListTask = pcapPaths.Select(PcapParser.ParsePcapFileAsync).ToList();
            List<List<MsgEvent>> traceFileList = null;
            if (Path.GetExtension(traceFilePaths.First()).Equals(".json")) {
                
                traceFileList = traceFilePaths.Select((path) => {
                    var msgEvFile = JsonMessageEventFile.ReadMessageEventFile(path).GetAwaiter().GetResult();
                    return msgEvFile._MsgEvents;
                }).ToList();
            } else {
                traceFileList = traceFilePaths.Select(MessageTraceParser.ParseMessageTraceTxtFile).ToList();
            }

            var packetFileList = new List<List<NetworkPacket>>(await Task.WhenAll(packetFileListTask));
            return new FilesWrapper() { TraceFiles = traceFileList, NetworkFiles = packetFileList };
        }

        /// <summary>
        /// Loads Pcap files and Trace Files from Default folder specified by PathWrapper
        /// </summary>
        /// <param name="numOfFiles">Number of Files to Load</param>
        /// <returns>A FileWrapper Object that contains The Pcap Events and the Corresponding Trace files</returns>
        public static async Task<FilesWrapper> LoadFilesAsync(int numOfFiles) {
            //Get Paths
            var pcapFiles = PathWrapper.Pcap_TelegramFilesByNum(numOfFiles);
            var traceFiles = PathWrapper.MsgTrace_TelegramFilesByNum(numOfFiles);
            return await LoadFilesAsync(pcapFiles, traceFiles);
        }
    }
}