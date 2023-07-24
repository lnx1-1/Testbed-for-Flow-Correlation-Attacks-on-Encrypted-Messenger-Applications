// See https://aka.ms/new-console-template for more information


using Event_Based_Impl;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.InputModules;
using OxyPlot;
using ProjectCommons;
using ResultAnalyser;
using ResultRunner;


Console.WriteLine("Hello, World!");
await Program2.Main(null);
return;


var _rawNetPackets = PcapParser.ParsePcapFileAsync(PathWrapper.pcapTelegramFile1).GetAwaiter().GetResult();
var _msgEvents = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFile1);
var _extractedMsgEvents = EventExtractor.extractBurstEvents(_rawNetPackets);

var plot = OxyPlottGenerator.PlotPacketEventDetails(_rawNetPackets, _msgEvents);
plot.Title = "Extracted Message Events and Observed Packets";

OxyPlottGenerator.AddExtractedPlot(_extractedMsgEvents, plot);

var outPath = Export(plot, "Results/FigureTest.pdf");

Console.WriteLine($"Saved Figure to: {outPath}");


var plot2 = OxyPlottGenerator.PlotPacketEventDetailsSizes(_extractedMsgEvents, _msgEvents);
plot.Title = "Size Analysis";

outPath = Export(plot2, "Results/FigureTest2.pdf");
Console.WriteLine($"Saved Figure to: {outPath}");


string Export(PlotModel plotModel, string outPath) {
    var s = Utilitys.GetAbsolutePath(outPath);
    s = Path.GetFullPath(s);
    using (var stream = File.Create(s)) {
        var exporter = new PdfExporter() { Width = 800, Height = 400 };
        exporter.Export(plotModel, stream);
    }

    return s;
}