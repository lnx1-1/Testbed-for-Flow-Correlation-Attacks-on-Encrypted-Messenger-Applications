import csv
import configParams as conf
def convert_time(timestamps, start_time):
    """
    Converts timestamps to relative timestamps (relative to Startime)
    """
    converted_timestamps = []
    for t in timestamps:
        converted = t - start_time
        if converted < 0:
            print('Error: timestamp is larger than start time with t = {} and start_time = {}'.format(t, start_time))
        converted_timestamps.append(converted)
    return converted_timestamps    

def get_on_periods_of_sender_2(timestamps, message_sizes):
    """
    Returns an Array of timestamp and messagesSizes Tupels\\
    eg.   
     `[(timestamps[0], messagesizes[0]), (timestamps[1], messagesizes[1]).... ]`
    """
    global TransitionPeriod # Macht doch gar nichts ... können wir auch weglöschen?
    bursts = []
    timestamps_Sizes_tupel = zip(timestamps, message_sizes) # Creates a Tupel... (timestamps[0], messagesizes[0])...
    for t, s in timestamps_Sizes_tupel:
        bursts.append((t, s))
    return bursts


def get_bursts_user(timestamps, user_packet_sizes):
    """
    Returnes A List of Tupels containing the Timestamps and the Packet Burstsizes (Filters only Correct burst (40bytes threshold))
    Merges Packets together that belong to one Burst (one Second ?) some Magic constant -> Should be the T_e Parameter
    """
    global TransitionPeriod
    is_in_burst = False
    bursts = []
    burst_size = 0
    last_burst_index = 0
    MTU = 1500 #TODO Constant ist damit wirklich die MTU gemeint ? ... Oder wird damit nur nochmal doppelt geprüft falls leitung mit geringer bandbreite verwendet wird.. Aber dann macht MTU size keinen sinn..

    for time_iter in range(0, len(timestamps)): # Time_iter ist index für jede Nachricht (markiert durch timestamp)
        if time_iter == len(timestamps) - 1 and is_in_burst: # Wenn letzte Msg und wir innerhalb eines Bursts sind ?
            if burst_size > MTU: # Und es ein Burst Paket ist -> Verstehe den MTU parameter nicht so richtig -> Seite 8 unten rechts
                bursts.append((timestamps[last_burst_index], burst_size)) # Speichern wir diesen Burst, so wie auch normal (Nur das hier richtigerweise der LastburstIndex richtig ist)!!!
                break

        if (timestamps[time_iter] - timestamps[last_burst_index]) > 1: # Wenn mehr als eine Sekunde vergangen ist seit ende des letzten Burst -> Sind wir nicht mehr in einem Burst
            if is_in_burst:
                if burst_size > MTU:
                    #TODO is Programmier Fehler hier ? müsste nicht last_burst_index sein? Siehe seite 9 und 6 Zeilen weiter oben
                    bursts.append((timestamps[time_iter], burst_size)) # Speichern des Bursts (als Tupel Timestamp mit der Burstgröße) (Warum nicht den Letzten Burst Timestamp???!!)
                is_in_burst = False #-> Sind wir nicht mehr in einem Burst
                burst_size = 0
        if user_packet_sizes[time_iter] > conf.MAGIC_IsUserPacketARelevantBurstPacket: # magic constant -> Wenn packet size > 40 Bytes (Wahrscheinlich wenn nicht nur steuerungsinformationen) #TODO
            if is_in_burst == False:
                is_in_burst = True # Befinden wir uns in einem Burst
            last_burst_index = time_iter # Setzen des des Bursts (wird immer neu gesetzt -> Markiert das Ende)
            burst_size += user_packet_sizes[time_iter] # Addieren des Bursts
    return bursts

def find_intervals(period_points, minute):
    """
    Splits the Period Points list into Time Intervals. Specified by "minute"
    
    `return:` list of periodPoints (wahrscheinlich msgEvents). Jedes Listenelement ist eine Liste mit msgEvents innerhalb eines Intervals
    """
    LENGTH = conf.LENGTH
    num_of_intervals = int(LENGTH / minute)
    intervals = []
    for i in range(num_of_intervals):
        intervals.append([]) # the Empty element
        startpoint = i * minute
        endpoint = (i + 1) * minute
        for j in range(len(period_points)):
            if startpoint <= period_points[j][0] and period_points[j][0] < endpoint:
                intervals[-1].append(period_points[j])
    return intervals

def find_intervals_channel(period_points, minute, message_types):
    """
    same as find_intervals. Except that it filters out text type Messages.. 
    copy and Paste Code
    """
    global LENGTH #=3600 = Eine stunde! 
    num_of_intervals = int(LENGTH / minute) # Wieviele Intervalle innerhalb einer Stunde. Weil die Period Points pro Stunde sind.. 
    intervals = []
    for i in range(num_of_intervals):
        intervals.append([])
        startpoint = i * minute
        endpoint = (i + 1) * minute
        for j in range(len(period_points)):
            if startpoint <= period_points[j][0] and period_points[j][0] < endpoint and message_types[j] != 'text':
                intervals[-1].append(period_points[j])
    return intervals


def find_matches(channel_interval, user_interval):
    """
    Bekommt einen Award für die Hässlichste Lösung bisher

    Vergleicht channel Intervals mit den Userintervals, zählt alle Matches der Events (time und size)
    Ein Match ist wenn die time (max) +- DELTA sekunden auseinanderliegt, und die Größen abweichung kleiner ist als GAMMA*Size (Also die relative größe des Bursts)

    `Return:` Die Treffer Quote (number_of_matches / all Events)
    """
    number_of_matches = 0
    number_of_nonmatches = 0
    matched_user_intervals = set()
    # Vergleicht jedes Event aus channel_interval mit jedem user_intervall (Versucht diese zu machten. Mit Gamma (Size Threshold) und Delta (Time Threshold))
    # Probiert verschiedene Deltas durch (Zeitliche Abweichungen)
    # Wenn ein Match gefunden wurde, dann wird mit dem nächsten channel_interval fortgeführt
    for j in range(min(len(channel_interval), len(user_interval))):
        time = channel_interval[j][0]
        size = channel_interval[j][1]
        is_matched = False
        for d in range(1, conf.DELTA): # Innerhalb von 1-15 sekunden -> Geht verschiedene Delta werte durch. Zeitliche Differenz. Um uhrzeit drifft auszugleichen
            for i in range(len(user_interval)):
                if user_interval[i] in matched_user_intervals: # Wenn  schon gematcht -> Weitergehen
                    continue
                if abs(user_interval[i][0] - time) < d: # Wenn innerhalb des Deltas (welches schrittweise erhöt wird) Also Differenz der beiden Timestamps
                    if abs(size - user_interval[i][1]) < conf.GAMMA * size: # Hmm hier wird gamma in Abhändigkei der Größe gesetzt. Relative Abweichung... (Im Paper anders )
                        matched_user_intervals.add(user_interval[i])
                        number_of_matches += 1
                        is_matched = True
                        break
            else:
                continue # Wird gemacht wenn kein Match gefunden wurde (also die schleife nicht mit Break verlassen wurde)
            break # Wird gemacht wenn ein Match gefunden wurde..
        if is_matched is False:
            number_of_nonmatches += 1
    return number_of_matches / float(number_of_matches + number_of_nonmatches)



def detect_correlations_from_intervals(channel_intervals, user_intervals):
    """
    `Return` liste mit Treffer Quote für jedes Interval.\\
        `-1` wenn keine Events in einem Interval\\
        `0` wenn nur ein Event in `channel_interval` bzw `user_interval`

    """
    correlation = []
    for i in range(len(channel_intervals)): # Was ist wenn einer der beiden weniger oder mehr Elemente hat ?! o.O
        if len(channel_intervals[i]) == 0 and len(user_intervals[i]) == 0: # no events in either one
            correlation.append(-1)
        elif len(channel_intervals[i]) == 0 or len(user_intervals[i]) == 0: # only one has an event
            correlation.append(0)
        else:
            correlation.append(find_matches(channel_intervals[i], user_intervals[i]))
    return correlation

def detect_correlations_from_bursts(channel_bursts, user_bursts, message_types, j): #Input Data is one Dataset file (one File -> One Hour)
    """
    1. Splits the Bursts into Observation Intervals (Observation Lengths are provided by `j` arg)\\
    2. Detects Matches\\
    `Return`: a Match Ratios for every Interval (as List/Array)
    """
    
    correlations = []
    user_intervals = find_intervals(user_bursts, conf.observation_lengths[j])
    channel_intervals = find_intervals(channel_bursts, conf.observation_lengths[j])
    correlation_of_intervals = detect_correlations_from_intervals(channel_intervals, user_intervals)
    #channel_intervals = find_intervals_channel(channel_bursts, observation_lengths[j], message_types) # Wird wahrscheinlich benutzt weil text messages eh zu klein sind..
    #correlation_of_intervals = #detect_correlations_from_intervals([channel_bursts], [user_bursts]) #detect_correlations_from_intervals(channel_intervals, user_intervals)
    return correlation_of_intervals

# Load
class PacketFile_t:
    sizes: list[int] = []
    timestamps: list[float] = []
    def __init__(self, size: 'list[int]', timestamp: 'list[float]'):
        self.sizes = size
        if(len(timestamp)==0):
            print("TIMESTAMP ZERO")
        self.timestamps = timestamp

class MsgTraceEvFile_t:
    msgTypes: list[str] = []
    msgSizes: list[int] = []
    msgTimestmps: list[float] = []
    def __init__(self, size: 'list[int]',timestamp: 'list[float]', msgType: 'list[str]'):
        self.msgSizes = size
        self.msgTimestmps = timestamp
        self.msgTypes = msgType

class ResultObj_t:
    def __init__(self, TP: float = 0, FP: float = 0, IntervalLen: int = 0):
        self.TP = TP
        self.FP = FP
        self.IntervalLen = IntervalLen
   

def loadPacketsCSVFile(path: str) -> PacketFile_t:
    packetReader = []
    msgSizes = []
    msgTimestmp = []

    with open(path,newline='') as csvfile:
        packetReader = csv.DictReader(csvfile, delimiter=';',fieldnames=['timestamp','size'])                
        
        for p in packetReader: 
            if(p.get('size').replace(' ','') == 'size'):
                print("Skipped Heading Line")
                continue           
            msgSizes.append(int(p.get('size')))
            msgTimestmp.append(float(p.get('timestamp').replace(',','.')))
    print("Loaded {} packets from {}".format(len(msgSizes),path))
    #print("SizeAnfang: {}".format(msgSizes[0]))
    #print("timeAnfang: {}".format(msgTimestmp[0]))
    return PacketFile_t(msgSizes,msgTimestmp)

def loadMsgTracesFromCSVFile(path: str) -> MsgTraceEvFile_t:
    packetReader = []  
    msgTypes = []
    msgSizes = []
    msgTimestmp = []
    with open(path,newline='') as csvfile:
        packetReader = csv.DictReader(csvfile, delimiter=';',fieldnames=['timestamp','size','type'])                
        
        for p in packetReader:
            if(p.get('size').replace(' ','') == 'size'):
                print("Skipped Heading Line")
                continue           
            msgTypes.append(p.get('type'))
            msgSizes.append(int(p.get('size')))
            msgTimestmp.append(float(p.get('timestamp').replace(',','.')))

            
    print("Loaded {} msgEvents from".format(len(msgSizes), path))
    return MsgTraceEvFile_t(msgSizes,msgTimestmp,msgTypes)
    #print("SizeAnfang: {}".format(packetFile[0].size))



def CorrelationAllFiles(all_channel_bursts, all_user_bursts, message_types_of_all_senders):        
    all_corrs_fp = []
    for m in range(len(conf.observation_lengths)):
    #print('For {}-second interval:'.format(conf.observation_lengths[m]))
    
        corrs_fp = []
        
        for i in range(len(all_user_bursts)):
            for j in range(len(all_channel_bursts)):
                if i == j:
                    continue # Skippt wenn gleicher Index (Also Korrelierende Files sind)
                corrs = detect_correlations_from_bursts(all_channel_bursts[j], all_user_bursts[i], message_types_of_all_senders[j], m)
                if len(corrs) == 0:
                    print('warning')
        #             continue
    #             organized_correlations = remove_empty_correlations(corrs)
        #         for k in range(len(organized_correlations)):
        #             corrs_fp[k].extend(organized_correlations[k])
                if corrs != -1:
                    corrs_fp.extend(corrs)
                    
                #print ("user {} with channel {} is done".format(i, j))
        all_corrs_fp.append([c for c in corrs_fp if c != -1])
    return all_corrs_fp
    # 
#with open(os.path.join(results_dir, 'corrs_fp_event_based_delta_{}.pickle'.format(DELTA)), 'wb') as handle:
 #   pickle.dump(all_corrs_fp, handle, protocol=pickle.HIGHEST_PROTOCOL)


def CorrelateFPFilesV2(all_channel_bursts, all_user_bursts,message_types_of_all_senders):
    all_corrs_fp = []
    for m in range(len(conf.observation_lengths)):
        corrs_fp = []

        for i in range(max(len(all_channel_bursts),len(all_user_bursts))):
            j = i+1
            if(j >= len(all_channel_bursts)):
                j = 0
            corrs = detect_correlations_from_bursts(all_channel_bursts[j], all_user_bursts[i], message_types_of_all_senders[j], m)
            if len(corrs) == 0:
                    print('warning')      
            if corrs != -1:
                corrs_fp.extend(corrs)            
        all_corrs_fp.append([c for c in corrs_fp if c != -1])
    return all_corrs_fp