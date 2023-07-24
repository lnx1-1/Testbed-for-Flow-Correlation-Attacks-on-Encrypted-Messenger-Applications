using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Event_Based_Impl;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.DataTypes;
using OpenTap;
using ProjectCommons;

namespace AlgorithmAdapter.Event_Based.PreProcessing {
    public class SinglePreProcessStep : TestStep {
        [Display("MsgEvents to Extract", Group: "Input", Order: 1)]
        public Input<List<MsgEvent>> _msgEvents { get; set; }

        [Display("Input NetworkPackets", "The Network Packets to Extract Network Events from", Group: "Input")]
        public Input<List<NetworkPacket>> _netPackets { get; set; }

        [Display("Filter out Text Messages")] public bool FilterOutTextMessages { get; set; } = false;

        [Display("Extracted (processed) Network events", Group: "Output")]
        [Browsable(false)]
        [Output]
        public List<MsgEvent> _ExtractedNetworkEvents { get; set; } = new List<MsgEvent>();

        [Display("Extracted (processed) MsgEvents", Group: "Output", Order: 2)]
        [Browsable(false)]
        [Output]
        public List<MsgEvent> _extractedMsgEvents { get; set; } = new List<MsgEvent>();

        public SinglePreProcessStep() {
            _msgEvents = new Input<List<MsgEvent>>();
            _netPackets = new Input<List<NetworkPacket>>();
        }

        public override void Run() {
            Loader.FilesWrapper fileWrapper = new Loader.FilesWrapper(new List<List<MsgEvent>>() { _msgEvents.Value },
                new List<List<NetworkPacket>>() { _netPackets.Value });

            Log.Info("Starting Preprocessing and ");

            fileWrapper = Preprocessor.get(fileWrapper)
                .RemoveInconsistendPairs()
                .SetRelativeTimestamps()
                .Result();


            _ExtractedNetworkEvents = EventExtractor.extractBurstEvents(fileWrapper.NetworkFiles[0]);

            if (FilterOutTextMessages) {
                _extractedMsgEvents = EventExtractor.Get(fileWrapper.TraceFiles[0]).FilterOutTextMessages().Results();
            } else {
                _extractedMsgEvents = fileWrapper.TraceFiles[0];
            }
        }
    }
}