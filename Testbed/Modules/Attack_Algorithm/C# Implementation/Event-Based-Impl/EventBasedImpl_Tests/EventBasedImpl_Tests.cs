using Event_Based_Impl;
using Event_Based_Impl.Algorithms;
using Event_Based_Impl.BatchProcessing;
using Event_Based_Impl.InputModules;
using ProjectCommons;
using NUnit.Framework;

namespace EventBasedImpl;

public class GeneralTests {
	[SetUp]
	public void Setup() {
	}

	[Test]
	public void TestParseMessageTraceFile() {
		var f0 = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(0));
		var f1 = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(1));
		var f2 = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(3));

		Assert.That(f0, Has.Count.EqualTo(12));
		Assert.That(f1, Has.Count.EqualTo(6));
		Assert.That(f2, Has.Count.EqualTo(4));

		Assert.That(f2.Last().timestamp, Is.EqualTo(1564735507));
		Assert.That(f2.Last().size, Is.EqualTo(552612));

		var summedList = new List<MsgEvent>();
	}


	[Test]
	public void TestFilterOutTextMessages() {
		List<MsgEvent> ev = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(0));
		ev = EventExtractor.FilterOutTextMessages(ev);
		Assert.That(ev, Has.Count.EqualTo(5));

		ev = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(1));
		ev = EventExtractor.FilterOutTextMessages(ev);
		Assert.That(ev, Has.Count.EqualTo(4));

		ev = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(18));
		ev = EventExtractor.FilterOutTextMessages(ev);
		Assert.That(ev, Has.Count.EqualTo(1));
	}


	[Test]
	public void TestFilterOutSize0Event() {
		List<MsgEvent> ev = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(1));
		ev = EventExtractor.FilterOutTextMessages(ev);
		Assert.That(ev, Has.Count.EqualTo(4));


		ev = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(18));
		ev = EventExtractor.FilterOutTextMessages(ev);
		Assert.That(ev, Has.Count.EqualTo(1));
	}


	[Test]
	public void TestSplitIntervals() {
		List<MsgEvent> events = new List<MsgEvent>();
		events.Add(new MsgEvent() { timestamp = 10 });
		events.Add(new MsgEvent() { timestamp = 25 });
		events.Add(new MsgEvent() { timestamp = 45 });

		events.Add(new MsgEvent() { timestamp = 55 });
		events.Add(new MsgEvent() { timestamp = 80 });

		var result = Processing.SplitIntoIntervals(events, 2, 50);

		Assert.That(result, Has.Count.EqualTo(2));
		Assert.That(result[0], Has.Count.EqualTo(3));
		Assert.That(result[1], Has.Count.EqualTo(2));
	}
[Test]

	public void TestPreprocessing() {
		var filesWrapper = Loader.LoadFilesAsync(2).GetAwaiter().GetResult();

		Processing.Prepocessing(filesWrapper);

		Assert.That(filesWrapper.NetworkFiles[0][0].timestamp,Is.EqualTo(0));

	}

	[Test]
	public void TestSplitRealIntervals() {
		var msgTraceFile = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(0));
		var traceEventList = EventExtractor.Get(msgTraceFile)
			.FilterEmptyEvents()
			.FilterOutTextMessages()
			.Results();

		Processing.SetRelativeTimeStamps(
			new List<MsgEvent>() { new() { timestamp = traceEventList.First().timestamp } }, traceEventList);

		var intervals = Processing.SplitIntoIntervals(msgTraceFile, 2, 60 * 30);

		Assert.That(intervals[0], Has.Count.EqualTo(3));
		Assert.That(intervals[1], Has.Count.EqualTo(1));

		//------------------------------------------------

		var parsedFiles = PathWrapper.MsgTrace_TelegramFilesByNum(5).Select(MessageTraceParser.ParseMessageTraceTxtFile)
			.ToList();

		Assert.That(parsedFiles, Has.Count.EqualTo(5), "Incorrect number of loaded files");
		
		List<int[]> expectedIntervalCount = new List<int[]>()
			{ new[] { 3, 1 }, new[] { 2 }, new[] { 1 }, new[] { 1 }, new[] { 1 } };

		int i = 0;
		foreach (var traces in parsedFiles) {
			Assert.That(i, Is.LessThan(expectedIntervalCount.Count));
			traceEventList = EventExtractor.Get(traces)
				.Sort()
				.FilterEmptyEvents()
				.FilterOutTextMessages()
				.Results();

			Processing.SetRelativeTimeStamps(
				new List<MsgEvent>() { new() { timestamp = traceEventList.Min(p => p.timestamp) } },
				traceEventList);

			intervals = Processing.SplitIntoIntervals(traceEventList, 60 * 30, traceEventList.Max(p => p.timestamp));

			Assert.That(intervals, Has.Count.EqualTo(expectedIntervalCount[i].Length));
			for (int j = 0; j < intervals.Count; j++) {
				Assert.That(intervals[j], Has.Count.EqualTo(expectedIntervalCount[i][j]));
			}

			i++;
		}
	}


	[Test]
	public void TestSplitRealIntervals2() {
		var msgTraceFile = MessageTraceParser.ParseMessageTraceTxtFile(PathWrapper.MsgTrace_TelegramFileByNum(13));
		var traceEventList = EventExtractor.Get(msgTraceFile)
			.Sort()
			.FilterEmptyEvents()
			.FilterOutTextMessages()
			.Results();

		Assert.DoesNotThrow(() => Processing.SetRelativeTimeStamps(
			new List<MsgEvent>() { new() { timestamp = traceEventList.First().timestamp } }, traceEventList));


		double max = traceEventList.Max(p => p.timestamp);
		double intervalDurationInSeconds = 60 * 30;

		var intervals = Processing.SplitIntoIntervals(msgTraceFile, (int)intervalDurationInSeconds, max);

		Assert.That(intervals, Has.Count.EqualTo(11));

		Assert.That(intervals[0], Has.Count.EqualTo(5)); //6:45 - 0715 
		Assert.That(intervals[1], Has.Count.EqualTo(12)); // 0715 - 0745 
		Assert.That(intervals[2], Has.Count.EqualTo(1)); // 0745 - 0815
		Assert.That(intervals[3], Has.Count.EqualTo(0)); // 0815- 0845
		Assert.That(intervals[4], Has.Count.EqualTo(0)); // 0845 - 0915
		Assert.That(intervals[5], Has.Count.EqualTo(0)); // 0915 - 0945
		Assert.That(intervals[6], Has.Count.EqualTo(0)); // 0945 - 1015
		Assert.That(intervals[7], Has.Count.EqualTo(0)); // 1015 - 1045
		Assert.That(intervals[8], Has.Count.EqualTo(2)); // 1045 - 1115
		Assert.That(intervals[9], Has.Count.EqualTo(0)); // 1115 - 1145
		Assert.That(intervals[10], Has.Count.EqualTo(1)); // 1145 - 1215
	}

	[Test]
	public void TestIntervalCount() {
		List<MsgEvent> events = new List<MsgEvent>();
		events.Add(new MsgEvent() { timestamp = 10 });
		events.Add(new MsgEvent() { timestamp = 25 });
		events.Add(new MsgEvent() { timestamp = 45 });

		events.Add(new MsgEvent() { timestamp = 55 });

		events.Add(new MsgEvent() { timestamp = 80.05 });

		// 20 40 60 80 100 120

		var intervals = Processing.SplitIntoIntervals(events, 20, events.Max(e => e.timestamp));

		Assert.That(intervals.Count, Is.EqualTo(5));

		events.RemoveAt(4);

		intervals = Processing.SplitIntoIntervals(events, 20, events.Max(e => e.timestamp));

		Assert.That(intervals.Count, Is.EqualTo(3));

		//---------------
		events = new List<MsgEvent>();
		events.Add(new MsgEvent() { timestamp = 0 });
		events.Add(new MsgEvent() { timestamp = 10 });
		events.Add(new MsgEvent() { timestamp = 20 });
		events.Add(new MsgEvent() { timestamp = 25 });
		events.Add(new MsgEvent() { timestamp = 45 });

		events.Add(new MsgEvent() { timestamp = 55 });

		events.Add(new MsgEvent() { timestamp = 80 });

		intervals = Processing.SplitIntoIntervals(events, 20, events.Max(e => e.timestamp));

		Assert.That(intervals.Count, Is.EqualTo(5));
		Assert.That(intervals[0].Count, Is.EqualTo(2));
		Assert.That(intervals[1].Count, Is.EqualTo(2));
		Assert.That(intervals[2].Count, Is.EqualTo(2));
		Assert.That(intervals[3].Count, Is.EqualTo(0));
		Assert.That(intervals[4].Count, Is.EqualTo(1));
	}


	[Test]
	public void TestSortEvents() {
		List<MsgEvent> events = new List<MsgEvent>();
		events.Add(new MsgEvent() { timestamp = 80 });
		events.Add(new MsgEvent() { timestamp = 10 });
		events.Add(new MsgEvent() { timestamp = 45 });
		events.Add(new MsgEvent() { timestamp = 25 });

		events.Add(new MsgEvent() { timestamp = 55 });

		Assert.That(events[0].timestamp, Is.EqualTo(80));
		Assert.That(events[2].timestamp, Is.EqualTo(45));

		var resultList = EventExtractor.Get(events).Sort().Results();

		Assert.That(resultList[0].timestamp, Is.EqualTo(10));
		Assert.That(resultList[2].timestamp, Is.EqualTo(45));
		Assert.That(resultList[4].timestamp, Is.EqualTo(80));
	}
	[Test]
	public void TestIntervalDisjointEvents() {
		List<MsgEvent> traceEvents = new List<MsgEvent> {
			new MsgEvent() { timestamp = 10 }, 
			new MsgEvent() { timestamp = 25 },
			new MsgEvent() { timestamp = 45 },
			new MsgEvent() { timestamp = 55 },
			new MsgEvent() { timestamp = 80 },
		};
		List<MsgEvent> netEvents = new List<MsgEvent> {
			new MsgEvent() { timestamp = 88 },
			new MsgEvent() { timestamp = 90 },
			new MsgEvent() { timestamp = 100 },
			new MsgEvent() { timestamp = 115 },
			new MsgEvent() { timestamp = 120 },
		};
		var traceExpected = new double[] {0,15,35,45,70 };
		// var netExpected = new double[] {0,2,12,27,32};
		var netExpected = new double[] {78,80,90,105,110};
		Processing.SetRelativeTimeStamps(traceEvents, netEvents);
		Assert.That(traceEvents.Select(p=>p.timestamp), Is.EquivalentTo(traceExpected.ToList()));
		Assert.That(netEvents.Select(p=>p.timestamp), Is.EquivalentTo(netExpected.ToList()));
	}
}