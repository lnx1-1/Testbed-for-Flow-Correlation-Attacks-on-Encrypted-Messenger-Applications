using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Event_Based_Impl.DataTypes;
using OpenTap;
using OxyPlot;
using ProjectCommons;


namespace ResultAnalyser {
    public class DetailedAnalyseStep : TestStep {
        [Display("Figure Output File", Group: "Ouput")]
        [FilePath]
        public string figureOutputPath { get; set; } = "./Results/figure1";

        [Display("In Msg Events", Group: "Input")]
        public Input<List<MsgEvent>> _msgEvents { get; set; }

        [Display("In Parsed Packets", Group: "Input")]
        public Input<List<NetworkPacket>> _rawNetPackets { get; set; }

        [Display("In ExtractedNetEvents", Group: "Input")]
        [EnabledIf(nameof(AddExtractedMsgEvents), true)]
        [Browsable(true)]
        public Input<List<MsgEvent>> _extractedMsgEvents { get; set; }

        [Display("Add Extracted Events to Plot", Group: "Config")]
        public bool AddExtractedMsgEvents { get; set; } = true;

        public bool pdfExport { get; set; } = false;

        public int pdfWith { get; set; } = 800;
        public int pdfHeight { get; set; } = 500;
        public bool svgExport { get; set; } = true;

        public DetailedAnalyseStep() {
            _msgEvents = new Input<List<MsgEvent>>();
            _rawNetPackets = new Input<List<NetworkPacket>>();
            _extractedMsgEvents = new Input<List<MsgEvent>>();
        }

        public void RunScottPlot() {
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
            Log.Info("Generating Plot");

            var plot = PlotGenerator.PlotPacketEventDetails(_rawNetPackets.Value, _msgEvents.Value);
            plot.Title("Extracted Message Events and Observed Packets");

            if (AddExtractedMsgEvents) {
                PlotGenerator.AddExtractedPlot(_extractedMsgEvents.Value, plot);
            }

            var outPath = Utilitys.GetAbsolutePath(figureOutputPath);
            plot.SaveFig(outPath);
        }

        public override void Run() {
            Log.Info("Generating Plot");
            var plot = OxyPlottGenerator.PlotPacketEventDetails(_rawNetPackets.Value, _msgEvents.Value);
            plot.Title = "Extracted Message Events and Observed Packets";

            if (AddExtractedMsgEvents) {
                OxyPlottGenerator.AddExtractedPlot(_extractedMsgEvents.Value, plot);
            }

            if (svgExport) {
                var outPath = Utilitys.GetAbsolutePath(figureOutputPath + ".svg");
                outPath = Path.GetFullPath(outPath);
                using (var stream = File.Create(outPath)) {
                    var exporter = new SvgExporter() { Width = 1000, Height = 600 };
                    exporter.Export(plot, stream);
                }

                Log.Info($"Saved Figure to: {outPath}");
            }

            if (pdfExport) {
                var outPath = Utilitys.GetAbsolutePath(figureOutputPath + ".pdf");
                outPath = Path.GetFullPath(outPath);
                using (var stream = File.Create(outPath)) {
                    var exporter = new PdfExporter() { Width = pdfWith, Height = pdfHeight };
                    exporter.Export(plot, stream);
                }

                Log.Info($"Saved Figure to: {outPath}");
            }
        }
    }
}