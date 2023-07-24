
## Event Based Algorithm
#pseudocode
#EventBasedAlgorithmen 
#MessageEvent 

	Event{
		timestamp, 
		size
	}

### Loading & Preprocessing

	Event[] channelEvent = loadMessageEvents()
	Event[] userEvent = loadCapturedPackets()

	

### Event Extraction

// Check for Empty EventArray

	startTime  = min(channelEvent[0].time, userEvent[0].time)

	ChannelEvent = setTimestampsRelativToStartTime(channelEvent, startTime)
	UserEvent = setTimestampsRelativToStartTime(channelEvent, startTime)

	channelEvent = ExtractRealMessageEvents(ChannelEvent) //Merge similar to Burst Extraction 
	userEvent = ExtractBurstsEvents(userEvent) // Use Similar Threshold

---
// Select a Observation Interval in which we try finding matches: (Im code werden verschiedene Ausprobiert... Und mehrfach die gleichen Intervals getestet um die daten zu verfizieren)

	interval = 15min
	
	#Filter Events for this interval (So dass wir nur Events innerhalb dieses Intervals haben)
	
	 MatchRatio = Detect_Matches(ChannelEvents, UserEvents)


### Burst Extraction:

- **Sim Events** sind Packete mit sehr kleinem InterPacket Delay (Traffic Bursts)
	- Lassen sich anhand eines [[IPD]] thresholds erkennen
		- Pakete deren [[IPD]] kleiner ist (T_e = 0.5s), werden zusammen als Event gezählt
		- Timestamp des Letzten Pakets ist die Arrival time des Events
		- Summe der Pakete ist größe des Events
- **Protocol Messages** (Updates, Handshakes usw..) sind kleine vereinzelte Nachrichten


> Extraction (Zusammenfassung von Packets) auch auf Logged Events ausführen (Falls Nachrichten direkt nacheinander gesedent wurden) Auch mit T_e


> **SIM-Event**
> "where the arrival time of the last packet in the burst gives the arrival time of the event, and the sum of all packet sizes in the burst gives the size of the event. "

<summary></summary>

-----
CODE

Event Extraction: Each SIM event, e.g., a sent image, produces a burst of MTU-sized packets in the encrypted traffic, i.e., packets with very small inter-packet delays. This is illustrated in Figure 8: SIM events such as images appear as traffic bursts, and scattered packets of small size are SIM protocol messages like notifications, handshakes, updates, etc.

Therefore, the adversary can extract SIM events by looking for bursts of MTU-sized packets, even though she cannot see packet contents due to encryption. 
We use the IPD threshold te to identify bursts.
Any two packets with distance less than te are considered to be part of the same burst. Note that t_e is a hyper-parameter of our model and we discuss its choice later in the paper.

For each burst, the adversary extracts a SIM event, Two SIM messages sent with an IMD less than te are extracted as one event. Similarly, the adversary combines events closer than t_e when capturing them from the target channel.

-----
### Correlation Detection:

 The adversary counts the number of events matches between the user flow f_user and the channel flow f_channel. We say that the ith channel event ei matches
wenn 
$|t_c - t_u| < DELTA$ and $| s_c - s_u | < Γ$

We set ∆ of the event-based algorithm to 3 seconds. 
We also set Γ parameter of the event-based detector to 10Kb

Note that even though the sizes of SIM messages do not change in transmission, the event extraction algorithm introduced earlier may impose size modifications, as network jitter is able to divide/merge event bursts (i.e., a burst can be divided into two bursts due to network jitter or two bursts can be combined due to the small bandwidth of the user)

Finally, the adversary calculates the ratio of the matched events as $r = k/n$, where $k$ is number of matched events and $n$ is the total number of events in the target channel

----
## Calc TruePositive


- Für jede Beobachtungszeiträume
	- werden die Matchrates berechnet
	- Diese werden als liste Pro File( ein File entspricht 1std capture) gespeichert

~~~C#
for(int obsInterval: ObservationIntervals){
	List<int(matchratio)> correlationTruePositives = new List<>;
	split_dataset_into_observation_Intervals
	for(DatasetTuple dataset: Datasets){ //Dataset is one Hour of Recording.. One File
		List<dataset> datasetIntervalList = dataset_split_into_observationIntervals(dataset);
		for(dataset intervalSet: datasetIntervalList){
			var matchRate = detectCorrelation(intervalSet.msgEvents, intervalSet.networkEvents) //Ignoring Text Messages
		}
		
	}
	
	

}

~~~

	
## Calc FalsePositive

- Für jede Beobachtungszeiträume
	- werden nicht passende beobachtungsfiles gematcht (stunde 10 mit Stunde 11. Aber nicht beide miteinander)
