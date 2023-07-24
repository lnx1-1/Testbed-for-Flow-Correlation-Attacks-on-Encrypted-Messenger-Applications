using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Event_Based_Impl;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.Utility_tools;
using OpenTap;
using ProjectCommons;

namespace AlgorithmAdapter.Event_Based.PreProcessing {
    public class BatchPreProcessStep : TestStep {
        [Display("Packets Input", "File or Folder Path to load .PCAP file(s) from", Group: "Input", Order: 1)]
        [DirectoryPath]
        public string _PcapINPath { get; set; } = PathWrapper.pcapTelegramFolder;

        [Display("Traces Input", "File or Folder Path to load .TXT file(s) from", Group: "Input", Order: 1)]
        [DirectoryPath]
        public string _TracesINpath { get; set; } = PathWrapper.MsgTrace_TelegramFolder;

        [Display("Files Amount", "Amount of Files to Load. 0 = Load all Files", Group: "Input", Order: 0)]
        public int AmountOfFiles { get; set; } = 0;

        public bool WriteOutputToCSV { get; set; } = true;

        [Display("Pcap Output", "File or Folder Path to Write preProcessed Packet CSV file(s) to", Group: "Output",
            Order: 1)]
        [DirectoryPath]
        [EnabledIf(propertyName: "WriteOutputToCSV")]
        public string _PcapOutPath { get; set; } = PathWrapper.packets_PreprocessedFolder;

        [Display("Traces Output", "File or Folder Path to write preProcessed Traces CSV file(s) to", Group: "Output",
            Order: 1)]
        [DirectoryPath]
        [EnabledIf(propertyName: "WriteOutputToCSV")]
        public string _TracesOutPath { get; set; } = PathWrapper.msgTraces_PreprocessedFolder;


        [Display("Extracted (processed) Network events", Group: "Output")]
        [Browsable(false)]
        [Output]
        public List<MsgEvent> _ExtractedNetworkEvents { get; set; } = new List<MsgEvent>();

        [Display("Extracted (processed) MsgEvents", Group: "Output", Order: 2)]
        [Browsable(false)]
        [Output]
        public List<MsgEvent> _extractedMsgEvents { get; set; } = new List<MsgEvent>();


        public bool PreconditionsMet() {
            if (!Directory.Exists(_PcapINPath)) {
                Log.Error($"Input folder Path: {_PcapINPath} not found");
                return false;
            }

            if (!Directory.Exists(_TracesINpath)) {
                Log.Error($"Input folder Path: {_TracesINpath} not found");
                return false;
            }

            if (!Directory.Exists(_PcapOutPath)) {
                Log.Warning($"Output Path [{_PcapOutPath}] doesnt exist. Trying to Create Path");
                Directory.CreateDirectory(_PcapOutPath);
                if (Directory.Exists(_PcapOutPath)) {
                    Log.Info("Path Successfully created");
                } else {
                    return false;
                }
            }

            if (!Directory.Exists(_TracesOutPath)) {
                Log.Warning($"Output Path [{_TracesOutPath}] doesnt exist. Trying to Create Path");
                Directory.CreateDirectory(_TracesOutPath);
                if (Directory.Exists(_TracesOutPath)) {
                    Log.Info("Path Successfully created");
                } else {
                    return false;
                }
            }

            return true;
        }

        public override void Run() {
            if (!PreconditionsMet()) {
                return;
            }

            Log.Info($"Loading Files from {_PcapINPath} and {_TracesINpath}");
            Loader.FilesWrapper fileWrapper = null;
            if (AmountOfFiles == 0) {
                fileWrapper = Loader.LoadFilesAsync(_PcapINPath, _TracesINpath).GetAwaiter().GetResult();
            } else {
                fileWrapper = Loader.LoadFilesAsync(_PcapINPath, _TracesINpath, AmountOfFiles).GetAwaiter().GetResult();
            }

            Log.Info("Starting Preprocessing and fileWriteout");

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();

            int fileCounter = 0;
            foreach (var netFile in fileWrapper.NetworkFiles) {
                var exractedList = EventExtractor.extractBurstEvents(netFile);
                if (WriteOutputToCSV) {
                    WriteOutNetFile(exractedList, fileCounter);
                } else {
                    _ExtractedNetworkEvents.AddRange(exractedList);
                }

                fileCounter++;
            }

            Log.Info($"Wrote {fileWrapper.NetworkFiles.Count} Preprocessed NetworkEvent Files to {_PcapOutPath}");

            fileCounter = 0;
            foreach (var traceFile in fileWrapper.TraceFiles) {
                var filteredTraces = EventExtractor.Get(traceFile).FilterOutTextMessages().Results();

                if (WriteOutputToCSV) {
                    WriteOutMsgEvents(fileCounter, filteredTraces);
                } else {
                    _extractedMsgEvents.AddRange(filteredTraces);
                }

                fileCounter++;
            }

            Log.Info($"Wrote {fileCounter} Preprocessed MessageTraces Files to {_PcapOutPath}");
        }

        private void WriteOutMsgEvents(int fileCounter, List<MsgEvent> filteredTraces) {
            string path = Path.Combine(_TracesOutPath, $"channel-{fileCounter}.csv");
            CSV_ResultWriter.WriteAsCsv(filteredTraces, path);
            // File.WriteAllLines(path, filteredTraces.Select(p => p.timestamp + ";" + p.size + ";" + p.type));
            // Log.Debug($"wrote {filteredTraces.Count} evs to path: {path}");
        }

        private void WriteOutNetFile(List<MsgEvent> exractedList, int fileCounter) {
            string path = Path.Combine(_PcapOutPath, $"user-{fileCounter}.csv");
            CSV_ResultWriter.WriteAsCsv(exractedList, path);
        }
    }
}