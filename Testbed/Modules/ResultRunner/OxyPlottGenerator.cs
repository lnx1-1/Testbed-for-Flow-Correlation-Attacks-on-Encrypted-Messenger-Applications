using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Event_Based_Impl.DataTypes;
using OxyPlot.Annotations;
using ProjectCommons;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using ScottPlot.Plottable;
using BarSeries = OxyPlot.Series.BarSeries;

namespace ResultAnalyser {
    public class OxyPlottGenerator {
        private static readonly double lineDistance = 60.0;



        public static PlotModel PlotPacketEventDetailsSizes(List<MsgEvent> extractedEvent, List<MsgEvent> currTraceFile) {
            var model = new PlotModel { Title = "Packet Event Size Details", PlotType = PlotType.Cartesian };
            var extractedSeries = new ScatterSeries() {
                MarkerType = MarkerType.Cross,
                MarkerFill = OxyColors.Magenta,
                MarkerSize = 7,
                MarkerStroke = OxyColors.Magenta,
                MarkerStrokeThickness = 1.7,
                YAxisKey = "YAxis1"
            };
            foreach (var exEv in extractedEvent) {
                extractedSeries.Points.Add(new ScatterPoint(exEv.timestamp,exEv.size));
            }
            
            model.Series.Add(extractedSeries);
            
            foreach (var type in MsgEvent.MsgTypes) {
                var currEvents = currTraceFile.Where(ev => ev.type.Equals(type)).ToList();
                if (!currEvents.Any()) {
                    continue;
                }

                // BarSeries barSeries = new BarSeries() {
                //     FillColor = GetMarkerColor(type),
                //     Title = "Message " + type,
                //     // YAxisKey = "YAxis2"
                // };

                var lineSeries = new LineSeries {
                    MarkerType = MarkerType.None,
                    Color = GetMarkerColor(type),
                    BrokenLineColor = OxyColors.Automatic,
                    StrokeThickness = 1.8,
                    LineStyle = LineStyle.Solid,
                    Title = "Message " + type,
                    YAxisKey = "YAxis1"
                };

                var scatterSeries = new ScatterSeries() {
                    MarkerType = MarkerType.Plus,
                    MarkerFill = GetMarkerColor(type),
                    MarkerSize = 6,
                    MarkerStroke = GetMarkerColor(type),
                    MarkerStrokeThickness = 1.8,
                    YAxisKey = "YAxis1"
                };
                
                foreach (var ev in currEvents) {
                    scatterSeries.Points.Add(new ScatterPoint(ev.timestamp,ev.size));
                    // barSeries.Items.Add(new BarItem(ev.timestamp,ev.size));
                    if (ev.size <= 0) {
                        Console.WriteLine("Error Size. "+ev);
                    }
                    lineSeries.Points.Add(new DataPoint(ev.timestamp, 0));
                    lineSeries.Points.Add(new DataPoint(ev.timestamp, ev.size));
                    lineSeries.Points.Add(new DataPoint(double.NaN, double.NaN)); // Leerpunkt, um Lücke zu erzeugen
                }
                // model.Series.Add(barSeries);
                model.Series.Add(scatterSeries);
                model.Series.Add(lineSeries);
            }
            
            // model.Axes.Add(new CategoryAxis(){Key = "YAxis2"});
            model.Axes.Add(new LinearAxis
                { Position = AxisPosition.Bottom, Title = "Time [Seconds]", AxisTitleDistance = 8 });
            model.Axes.Add(new LogarithmicAxis() {
                Position = AxisPosition.Left, Title = "Packet Size [Bytes]", AxisTitleDistance = 8,
                // AbsoluteMinimum = -300, Maximum = 1600
                Maximum = Math.Max(extractedEvent.Max(ev=>ev.size),currTraceFile.Max(ev=>ev.size))+10000,
                Key = "YAxis1",
                AbsoluteMaximum = Math.Max(extractedEvent.Max(ev=>ev.size),currTraceFile.Max(ev=>ev.size))+10000, 
            });
            

            return model;
        }

        public static PlotModel PlotPacketEventDetails(List<NetworkPacket> currPcapFile, List<MsgEvent> currTraceFile) {
            var model = new PlotModel { Title = "Packet Event Details", PlotType = PlotType.Cartesian };

            // var scatterSeries = new ScatterSeries {
            //     MarkerType = MarkerType.Circle,
            //     MarkerFill = OxyColors.Blue,
            //     MarkerSize = 3,
            //     MarkerStroke = OxyColors.Blue,
            //     MarkerStrokeThickness = 0.5
            // };
            //
            // foreach (var packet in currPcapFile) {
            //     scatterSeries.Points.Add(new ScatterPoint(packet.timestamp, packet.size));
            //     scatterSeries.Title = "Packets                               ";
            // }
            //
            // model.Series.Add(scatterSeries);


            var lineSeries = new LineSeries {
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                Title = "Paket"
            };

            for (int i = 0; i < currPcapFile.Count; i++) {
                var packet = currPcapFile[i];
                
                lineSeries.Points.Add(new DataPoint(packet.timestamp, 0));
                lineSeries.Points.Add(new DataPoint(packet.timestamp, packet.size));
                lineSeries.Points.Add(new DataPoint(double.NaN, double.NaN)); // Leerpunkt, um Lücke zu erzeugen
            }

            model.Series.Add(lineSeries);


            for (int i = 0; i < MsgEvent.MsgTypes.Length; i++) {
                var src = currTraceFile.Where(p => p.type.Equals(MsgEvent.MsgTypes[i])).ToArray();
                if (src.Length == 0) {
                    continue;
                }


                var yPos = -lineDistance * (i + 1);
                var scatterSeriesTrace = new ScatterSeries {
                    MarkerType = MarkerType.Plus,
                    MarkerFill = GetMarkerColor(MsgEvent.MsgTypes[i]),
                    MarkerSize = 6,
                    MarkerStroke = GetMarkerColor(MsgEvent.MsgTypes[i]),
                    MarkerStrokeThickness = 1.8,
                    Title = "Message " + MsgEvent.MsgTypes[i]
                };


                foreach (var traceEvent in src) {
                    scatterSeriesTrace.Points.Add(new ScatterPoint(traceEvent.timestamp, yPos));
                }

                model.Series.Add(scatterSeriesTrace);

                var lineAnnotation = new LineAnnotation {
                    Type = LineAnnotationType.Horizontal,
                    Color = GetMarkerColor(MsgEvent.MsgTypes[i]),
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 0.6,
                    Y = yPos
                };

                model.Annotations.Add(lineAnnotation);
            }

            model.Legends.Add(new Legend() {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.RightMiddle,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBackground = OxyColors.LightGray,
                LegendBorder = OxyColors.Black,
                LegendBorderThickness = 1,
                // LegendPadding = 8,
                LegendLineSpacing = 8,
                // LegendSize = new OxySize(500,300),
            });

            model.Axes.Add(new LinearAxis
                { Position = AxisPosition.Bottom, Title = "Time [Seconds]", AxisTitleDistance = 8 });
            model.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left, Title = "Packet Size [Bytes]", AxisTitleDistance = 8,
                AbsoluteMinimum = -300, Maximum = 1600
            });

            return model;
        }

        public static void AddExtractedPlot(List<MsgEvent> extractedEvents, PlotModel model) {
            var extractedScatterSeries = new ScatterSeries {
                MarkerType = MarkerType.Cross,
                MarkerFill = OxyColors.Magenta,
                MarkerSize = 6,
                MarkerStroke = OxyColors.Magenta,
                MarkerStrokeThickness = 1.8,
                Title = "Detected Event"
            };

            foreach (var extractedEvent in extractedEvents) {
                extractedScatterSeries.Points.Add(new ScatterPoint(extractedEvent.timestamp, -lineDistance+5));
            }


            model.Series.Add(extractedScatterSeries);

            var extractedLineAnnotation = new LineAnnotation {
                Type = LineAnnotationType.Horizontal,
                Color = OxyColors.Magenta,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Y = -55.0
            };

            model.Annotations.Add(extractedLineAnnotation);
        }

        private static OxyColor GetMarkerColor(string type) {
            switch (type) {
                case "audio":
                    return OxyColors.Chocolate;
                case "video":
                    return OxyColors.Gold;
                case "photo":
                    return OxyColors.LimeGreen;
                case "text":
                    return OxyColors.Red;
                default:
                    return OxyColors.Fuchsia;
            }
        }
    }
}