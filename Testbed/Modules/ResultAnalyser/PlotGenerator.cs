using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Event_Based_Impl.DataTypes;
using OxyPlot;
using ProjectCommons;
using ScottPlot;

namespace ResultAnalyser {
    public class PlotGenerator {
        public static Plot SignalPlot(List<double> netSignal) {
            var plt = new ScottPlot.Plot(1000, 600);
            var signalPlot = plt.AddSignal(netSignal.ToArray());
            signalPlot.Label = "Network Bytes/seconds";
            return plt;
        }

         public static Plot PlotPacketDetailsHorstVersion(List<NetworkPacket> currPcapFile, List<MsgEvent> currTraceFile,List<MsgEvent> detectedEvents, string combinedOutPath, int lowerBound = 0, int upperBound = int.MaxValue) {
            bool select = true;
            int witdh = 1500;
            int height = 1280;
            int heightSecondGrafik = 300;
            
            // int lowerBound = 2550;
            // int upperBound = 3500;
            
            setRelativeTimestamps(ref currPcapFile, ref currTraceFile);

            if (select) {
                currPcapFile = currPcapFile.Where(e => e.timestamp < upperBound && e.timestamp > lowerBound).ToList();
                currTraceFile = currTraceFile.Where(e => e.timestamp < upperBound && e.timestamp > lowerBound).ToList();
            }
            
            setRelativeTimestamps(ref currPcapFile, ref currTraceFile);
            
            var msgMax = currTraceFile.Max(e => e.timestamp);

            var lollipopPlot = new ScottPlot.Plot(witdh, height-heightSecondGrafik);
            
            // lollipopPlot.XAxis.AxisTicks.IsVisible = false;
            // var lolGraph = lollipopPlot.AddSignalXY(currPcapFile.OrderBy(p=>p.timestamp).Select(p => p.timestamp).ToArray(),currPcapFile.OrderBy(p=>p.timestamp).Select(p => (double)p.size).ToArray());
            var lolGraph = lollipopPlot.AddLollipop(currPcapFile.Select(p => (double)p.size).ToArray(), currPcapFile.Select(p => p.timestamp).ToArray());
            lolGraph.LollipopRadius = 2;
            lolGraph.LineWidth = 3;
            lolGraph.Label = "Paketgröße [Bytes]";
            lollipopPlot.YAxis.Label("Paketgröße [Bytes]");
            lollipopPlot.SetAxisLimitsY(0,1600);
            lollipopPlot.SetAxisLimitsX(0,msgMax, lollipopPlot.XAxis.AxisIndex);
            
            
            string[] types = { "audio", "video", "photo", "text", "Extracted Events" };

            // Erstelle einen neuen Plot für den Scatterplot
            var scatterPlot = new ScottPlot.Plot(witdh, heightSecondGrafik);
            scatterPlot.Style(dataBackground: Color.Transparent);
            scatterPlot.XAxis.Label("Zeit [Sekunden]");

            // Erstelle benutzerdefinierte Y-Achse mit Kategoriewerten
            var categories = types.ToArray(); // Reverse the array to display categories in the correct order
            var yTicks = Enumerable.Range(0, categories.Length).Select((v => (double)v)).ToArray();
            scatterPlot.YAxis.TickLabelStyle(rotation: 0); // Optional: Set the rotation angle of the tick labels
            scatterPlot.YAxis.ManualTickPositions(yTicks, categories);
            scatterPlot.YAxis.ManualTickSpacing(50);
            scatterPlot.SetAxisLimitsY(-1,types.Length,scatterPlot.YAxis.AxisIndex);
            scatterPlot.SetAxisLimitsX(0,msgMax, scatterPlot.XAxis.AxisIndex);
            
            
            // scatterPlot.YAxis.
            // scatterPlot.YAxis.Ticks
            // scatterPlot.YAxis.Ticks(yTicks, categories);

            for (int i = 0; i < types.Length; i++) {
                var src = currTraceFile.Where(p => p.type.Equals(types[i])).ToArray();
                if (src.Length == 0) {
                    continue;
                }

                var yPos = yTicks[i]; // Verwende den Index der Kategorie als Y-Position
                var timeValues = src.Select(p => p.timestamp).ToArray();
                var yAxis = Enumerable.Repeat(yPos, src.Count()).ToArray();

                var trace = scatterPlot.AddScatter(timeValues, yAxis);
                switch (types[i]) {
                    case "audio":
                        trace.Color = Color.Chocolate;
                        break;
                    case "video":
                        trace.Color = Color.Blue;
                        break;
                    case "photo":
                        trace.Color = Color.LimeGreen;
                        break;
                    case "text":
                        trace.Color = Color.Red;
                        break;
                    default:
                        trace.Color = Color.Fuchsia;
                        break;
                }

                trace.LineWidth = 0;
                trace.MarkerShape = MarkerShape.cross;
                trace.MarkerSize = 35;
                trace.MarkerLineWidth = (float)3;
                trace.Label = "Message " + types[i];
            }

            // lollipopPlot.Legend().IsVisible = true;
            // lollipopPlot.Legend().Location = Alignment.MiddleRight;

            // lollipopPlot.XAxis.Label("Time [Seconds]");
            
            
            scalePlot(ref scatterPlot);
            scalePlot(ref lollipopPlot);
            lollipopPlot.XAxis.TickLabelStyle(fontSize: 25);

            var bmpScatter = scatterPlot.Render();
            var bmpLollipop = lollipopPlot.Render();
            using (var bmp = new System.Drawing.Bitmap(witdh, height))
            using (var gfx = System.Drawing.Graphics.FromImage(bmp)) {
                
                gfx.DrawImage(bmpScatter, 0, height-heightSecondGrafik-20);
                gfx.DrawImage(bmpLollipop, 0, 0);
                bmp.Save(combinedOutPath);
            }

            return lollipopPlot;
        }

        public static Plot PlotPacketDetailsHorstVersion(List<NetworkPacket> currPcapFile, List<MsgEvent> currTraceFile, string combinedOutPath, int lowerBound = 0, int upperBound = int.MaxValue) {
            bool select = true;
            int witdh = 1500;
            int height = 1280;
            int heightSecondGrafik = 300;
            
            // int lowerBound = 2550;
            // int upperBound = 3500;
            
            setRelativeTimestamps(ref currPcapFile, ref currTraceFile);

            if (select) {
                currPcapFile = currPcapFile.Where(e => e.timestamp < upperBound && e.timestamp > lowerBound).ToList();
                currTraceFile = currTraceFile.Where(e => e.timestamp < upperBound && e.timestamp > lowerBound).ToList();
            }
            
            setRelativeTimestamps(ref currPcapFile, ref currTraceFile);
            
            var msgMax = currTraceFile.Max(e => e.timestamp);

            var lollipopPlot = new ScottPlot.Plot(witdh, height-heightSecondGrafik);
            var scatterPlot = new ScottPlot.Plot(witdh, heightSecondGrafik);
            
            var nullTimeStamp = currTraceFile.Where(ev => ev.size <= 0).Select(ev => ev.timestamp).ToArray();
            foreach (var t0 in nullTimeStamp) {
                // scatterPlot.AddArrow(t0, yPos-30, t0 + 10, 10);
                var vline = lollipopPlot.AddVerticalLine(t0);
                vline.LineWidth = 30;
                // vline.PositionLabel = true;
                vline.PositionLabelFont.Size = 25;
                vline.Label = "None";
                vline.Color = Color.FromArgb(50,Color.Chocolate);
                vline.PositionLabelBackground = Color.Chocolate;
                    
                var vlineScatter = scatterPlot.AddVerticalLine(t0);
                vlineScatter.LineWidth = 30;
                // vline.PositionLabel = true;
                vlineScatter.PositionLabelFont.Size = 25;
                vlineScatter.Label = "None";
                vlineScatter.Color = Color.FromArgb(50,Color.Chocolate);
                vlineScatter.PositionLabelBackground = Color.Chocolate;
            }
            
            
            
            // lollipopPlot.XAxis.AxisTicks.IsVisible = false;
            // var lolGraph = lollipopPlot.AddSignalXY(currPcapFile.OrderBy(p=>p.timestamp).Select(p => p.timestamp).ToArray(),currPcapFile.OrderBy(p=>p.timestamp).Select(p => (double)p.size).ToArray());
            var lolGraph = lollipopPlot.AddLollipop(currPcapFile.Select(p => (double)p.size).ToArray(), currPcapFile.Select(p => p.timestamp).ToArray());
            lolGraph.LollipopRadius = 2;
            lolGraph.LineWidth = 4;
            lolGraph.Label = "Paketgröße [Bytes]";
            lolGraph.BorderColor = Color.White;
            
            lolGraph.LollipopColor = Color.SteelBlue;
            lollipopPlot.YAxis.Label("Paketgröße [Bytes]");
            lollipopPlot.SetAxisLimitsY(0,1600);
            lollipopPlot.SetAxisLimitsX(0,msgMax, lollipopPlot.XAxis.AxisIndex);
            
            
            string[] types = { "audio", "video", "photo", "text" };

            // Erstelle einen neuen Plot für den Scatterplot
            
            scatterPlot.Style(dataBackground: Color.Transparent);
            scatterPlot.XAxis.Label("Zeit [Sekunden]");

            // Erstelle benutzerdefinierte Y-Achse mit Kategoriewerten
            var categories = types.ToArray(); // Reverse the array to display categories in the correct order
            var yTicks = Enumerable.Range(0, categories.Length).Select((v => (double)v)).ToArray();
            scatterPlot.YAxis.TickLabelStyle(rotation: 0); // Optional: Set the rotation angle of the tick labels
            scatterPlot.YAxis.ManualTickPositions(yTicks, categories);
            scatterPlot.YAxis.ManualTickSpacing(50);
            scatterPlot.SetAxisLimitsY(-1,types.Length,scatterPlot.YAxis.AxisIndex);
            scatterPlot.SetAxisLimitsX(0,msgMax, scatterPlot.XAxis.AxisIndex);
            
            
            // scatterPlot.YAxis.
            // scatterPlot.YAxis.Ticks
            // scatterPlot.YAxis.Ticks(yTicks, categories);

            
            
            for (int i = 0; i < types.Length; i++) {
                var src = currTraceFile.Where(p => p.type.Equals(types[i])).ToArray();
                if (src.Length == 0) {
                    continue;
                }

               
                var yPos = yTicks[i]; // Verwende den Index der Kategorie als Y-Position
                var timeValues = src.Select(p => p.timestamp).ToArray();
                var yAxis = Enumerable.Repeat(yPos, src.Count()).ToArray();

                
                
                var trace = scatterPlot.AddScatter(timeValues, yAxis);
                switch (types[i]) {
                    case "audio":
                        trace.Color = Color.Chocolate;
                        break;
                    case "video":
                        trace.Color = Color.SteelBlue;
                        break;
                    case "photo":
                        
                        trace.Color = Color.LimeGreen;
                        break;
                    case "text":
                        trace.Color = Color.Red;
                        break;
                    default:
                        trace.Color = Color.Fuchsia;
                        break;
                }

                trace.LineWidth = 0;
                trace.MarkerShape = MarkerShape.cross;
                trace.MarkerSize = 40;
                trace.MarkerLineWidth = (float)3.5;
                trace.Label = "Message " + types[i];
            }

            // lollipopPlot.Legend().IsVisible = true;
            // lollipopPlot.Legend().Location = Alignment.MiddleRight;

            // lollipopPlot.XAxis.Label("Time [Seconds]");
            
            
            scalePlot(ref scatterPlot);
            scalePlot(ref lollipopPlot);
            lollipopPlot.XAxis.TickLabelStyle(fontSize: 25);

            var bmpScatter = scatterPlot.Render();
            var bmpLollipop = lollipopPlot.Render();
            using (var bmp = new System.Drawing.Bitmap(witdh, height))
            using (var gfx = System.Drawing.Graphics.FromImage(bmp)) {
                
                gfx.DrawImage(bmpScatter, 0, height-heightSecondGrafik-20);
                gfx.DrawImage(bmpLollipop, 0, 0);
                bmp.Save(combinedOutPath);
            }

            return lollipopPlot;
        }

        private static void scalePlot(ref Plot plt) {
            int label = 28;
            int Ticklabel = 27;
            int grid = 2;
            plt.XAxis.Label(size: label);
            plt.YAxis.Label(size: label);

            plt.XAxis.TickLabelStyle(fontSize: Ticklabel);
            plt.YAxis.TickLabelStyle(fontSize: Ticklabel);

            plt.XAxis.MajorGrid(lineWidth: grid);
            plt.YAxis.MajorGrid(lineWidth: grid);

        }

        private static List<NetworkPacket> setRelativeTimestamps(ref List<NetworkPacket> currPcapFile, ref List<MsgEvent> currTraceFile) {
            var pcapMin = currPcapFile.Min(e => e.timestamp);
            var msgMin = currTraceFile.Min(e => e.timestamp);
            var timeMin = Math.Min(pcapMin, msgMin);
            if (timeMin != 0) {
                currPcapFile = currPcapFile.Select(e => {
                    e.timestamp = e.timestamp - timeMin;
                    return e;
                }).ToList();
                currTraceFile = currTraceFile.Select(e => {
                    e.timestamp = e.timestamp - timeMin;
                    return e;
                }).ToList();
            }

            return currPcapFile;
        }


        public static Plot DetailedExternalAxis(List<NetworkPacket> currPcapFile, List<MsgEvent> currTraceFile) {
            var plt = new ScottPlot.Plot(600, 400);

            var yAxis2 = plt.XAxis2;
            var yAxis3 = plt.AddAxis(ScottPlot.Renderable.Edge.Right);
            var yAxis4 = plt.AddAxis(ScottPlot.Renderable.Edge.Right);

            var hlineA = plt.AddVerticalLine(3);
            hlineA.XAxisIndex = yAxis2.AxisIndex;
            hlineA.PositionLabel = true;
            hlineA.PositionLabelOppositeAxis = true;
            hlineA.PositionLabelBackground = hlineA.Color;

            var hlineB = plt.AddVerticalLine(7);
            hlineB.XAxisIndex = yAxis3.AxisIndex;
            hlineB.PositionLabel = true;
            hlineB.PositionLabelOppositeAxis = true;
            hlineB.PositionLabelBackground = hlineB.Color;

            var hlineC = plt.AddVerticalLine(7);
            hlineC.XAxisIndex = yAxis4.AxisIndex;
            hlineC.PositionLabel = true;
            hlineC.PositionLabelOppositeAxis = true;
            hlineC.PositionLabelBackground = hlineC.Color;

// tell the line which axis to draw the label on

            hlineA.PositionLabelAxis = yAxis2;
            hlineB.PositionLabelAxis = yAxis3;
            hlineC.PositionLabelAxis = yAxis4;

            plt.YAxis2.Ticks(true);
            // plt.SetAxisLimits(yMin: -10, yMax: 10, xAxisIndex: yAxis2.AxisIndex);
            // plt.SetAxisLimits(yMin: -10, yMax: 10, xAxisIndex: yAxis3.AxisIndex);
            // plt.SetAxisLimits(yMin: -10, yMax: 10, xAxisIndex: yAxis4.AxisIndex);
            return plt;
        }

        public static Plot PlotPacketEventDetails(List<NetworkPacket> currPcapFile, List<MsgEvent> currTraceFile) {
            var plt = new ScottPlot.Plot(1000, 600);


            var scatterPlot = plt.AddLollipop(currPcapFile.Select(p => (double)p.size).ToArray(),
                currPcapFile.Select(p => p.timestamp).ToArray());
            scatterPlot.LollipopRadius = 5;
            scatterPlot.LineWidth = 1;
            scatterPlot.Label = "Packets";

            string[] types = { "audio", "video", "photo", "text" };

            for (int i = 0; i < types.Length; i++) {
                var src = currTraceFile.Where(p => p.type.Equals(types[i])).ToArray();
                if (src.Length == 0) {
                    continue;
                }

                var yPos = -45.0 * (i + 1);
                var timeValues = src.Select(p => p.timestamp).ToArray();
                var yAxis = Enumerable.Repeat(yPos, src.Count()).ToArray();

                var trace = plt.AddScatter(timeValues, yAxis);
                switch (types[i]) {
                    case "audio":
                        trace.Color = Color.Chocolate;
                        break;
                    case "video":
                        trace.Color = Color.Blue;
                        break;
                    case "photo":
                        trace.Color = Color.LimeGreen;
                        break;
                    case "text":
                        trace.Color = Color.Red;
                        break;
                    default:
                        trace.Color = Color.Fuchsia;
                        break;
                }

                plt.AddHorizontalLine(yPos, trace.Color, (float)0.8);

                trace.XAxisIndex = plt.RightAxis.AxisIndex;
                trace.LineWidth = 0;
                trace.MarkerShape = MarkerShape.cross;
                trace.MarkerSize = 12;
                trace.MarkerLineWidth = (float)2;
                trace.Label = "Message " + types[i];
            }

            plt.Legend().IsVisible = true;
            plt.Legend().Location = Alignment.MiddleRight;

            plt.XAxis.Label("Time [Seconds]");
            plt.YAxis.Label("Packet Size [Bytes]");
            return plt;
        }

        public static void AddExtractedPlot(List<MsgEvent> extractedEvents, Plot plt) {
            var extractedTime = extractedEvents.Select(p => p.timestamp).ToArray();
            var extractedSize = extractedEvents.Select(p => p.size).ToArray();
            var extractedyPos = Enumerable.Repeat(-45.0, extractedTime.Count()).ToArray();
            var extracted = plt.AddScatter(extractedTime, extractedyPos);
            extracted.Color = Color.Magenta;
            extracted.LineWidth = 0;
            extracted.MarkerShape = MarkerShape.eks;
            extracted.MarkerLineWidth = (float)1.8;
            extracted.MarkerSize = 10;
            extracted.Label = "Extracted Events";
            plt.AddHorizontalLine(-45.0, extracted.Color);
        }
    }
}