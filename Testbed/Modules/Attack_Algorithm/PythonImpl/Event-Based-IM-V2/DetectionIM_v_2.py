import pickle # für Obj serialisation
#import numpy as np
import matplotlib.pyplot as plt
import os
import time
from datetime import datetime
import json
#import csv
from pathlib import Path
import csv
import sys
import DetectionFunctions as func
from typing import List
import numpy
import json
import configParams as conf


#packetsRootPath = "E:/Datasets_uniPaper/converted/telegram/packets"
#tracesRootPath = "E:/Datasets_uniPaper/converted/telegram/traces"

# packetsRootPath = "D://DataUni//converted//telegram//packets_preprocessed"
# tracesRootPath = "D://DataUni//converted//telegram//traces_preprocessed"
# resultFilePath: str = ""

packetsRootPath = r"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\Recording_ShortForReproduciability\RecordedData\prepro\pcaps"
tracesRootPath = r"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\Recording_ShortForReproduciability\RecordedData\prepro\traces"
resultFilePath: str = r"C:\Users\Linus\git\cryptCorr\MainProject\IMSniff_Testbed_OpenTAP\bin\Debug x64\Results\Sammlung\Recording_ShortForReproduciability\Analyse\Results_RefImpl\Results_2023-06-29-17-43-40"

if(len(sys.argv) == 4):
    print("Using csv path Args")
    packetsRootPath = sys.argv[1]
    tracesRootPath = sys.argv[2]
    resultFilePath = sys.argv[3]
elif (len(sys.argv) == 2):
    print("Using resultPath Only")
    resultFilePath = sys.argv[1]
else:
    print ("Usage: DetectionIM.py <processedPacketsFolder> <processedTracesFolder> <resultFilePath>")
    print ("Or: DetectionIM.py <resultFilePath>")
    print ("But Received "+str(len(sys.argv))+"Arguments..")
    for a in sys.argv:
        print(a)
    #exit()

#-------------------------



packetfileList = os.listdir(packetsRootPath)
packetfileList.sort(key=lambda string: int(string.split('-')[1].replace(".csv",""))) # Das ist so eine hässliche sprache das die lösung jetzt dem entsprechend wird.

tracefileList = os.listdir(tracesRootPath)
tracefileList.sort(key=lambda string: int(string.split('-')[1].replace(".csv",""))) # Das ist so eine hässliche sprache das die lösung jetzt dem entsprechend wird.

print("fileListCount {}".format(len(packetfileList)))

packetFiles: list[func.PacketFile_t] = []
traceFiles: list[func.MsgTraceEvFile_t] = []

if(len(packetfileList) != len(tracefileList)):
    print("Different FilePath list lengths")
else:
    print("Loading Files")
    for i in range(0,len(packetfileList)):    
        pFile = func.loadPacketsCSVFile(os.path.join(packetsRootPath,packetfileList[i]))
        tFile = func.loadMsgTracesFromCSVFile(os.path.join(tracesRootPath,tracefileList[i]))
        #print(len(pFile.timestamp))
        packetFiles.append(pFile)
        traceFiles.append(tFile)

#print(traceFiles[0].msgTimestmps[0])
#print("time: {}".format(packetFile[0].timestamp[0]))

#----------------------------------------

message_types_of_all_senders = [] 
for file in traceFiles:
    message_types_of_all_senders.append(file.msgTypes)

#-------------------------------------------


all_user_bursts = [] 
all_channel_bursts = [] # the same as adversary bursts in the case of one-on-one chats

allFiles = packetfileList 

for i in range(0,len(allFiles)):
    channel_timestamps = traceFiles[i].msgTimestmps 
    channel_message_sizes = traceFiles[i].msgSizes 
    user_packet_timestamps = packetFiles[i].timestamps #Timestamp File
    user_packet_sizes = packetFiles[i].sizes 

    if len(user_packet_timestamps) == 0 or len(channel_timestamps) == 0: # Wenn nichts enthält wird weitergegangen
        all_user_bursts.append([])
        all_channel_bursts.append([])
        print('Skipping file '+str(i)+" (no receiver timestamps or ChannelTimestamps)")
        continue 

    #Start time ermitteln
#    start_time = min(channel_timestamps[0], user_packet_timestamps[0]) # Channel timestamps kommen aus dem Timestamp file - User Packet timestamps aus den Packets .. der start wird ermittelt

#    converted_sender_timestamps = convert_time(channel_timestamps, start_time) # Sets all timestamps (from timestamp file) relativ to the first of this file
    
    traces_period_points = func.get_on_periods_of_sender_2(channel_timestamps, channel_message_sizes) # Creates Tupel list with message sizes and Converted Timestamps

    # Does the same with the Timestamps from the Captured Packets
#    converted_user_timestamps = convert_time(user_packet_timestamps, start_time)
    # And gets the Analyzed Bursts with the Timestamps (relativ to the start of this page)
#    user_period_points = get_bursts_user(converted_user_timestamps, user_packet_sizes)

    packet_period_points = func.get_on_periods_of_sender_2(user_packet_timestamps, user_packet_sizes) # Creates Tupel list with message sizes and Converted Timestamps


    all_user_bursts.append(packet_period_points)
    all_channel_bursts.append(traces_period_points)
    #print ('Hour {} is done'.format(i))
    #print ("\n")


#-------------------------------------------

all_correlations_tp = [] # Ist dann eine Liste von Matchratio List pro Obs Interval File
for j in range(len(conf.observation_lengths)): # Test Correlation_Match_Ratios for Different Obsveration Lengths
    print('For {}-second interval:'.format(conf.observation_lengths[j]))
    corrs_tp = [] # 
    corrs_tp_withougNegativ = []
    for i in range(len(all_user_bursts)): # for every hour (every File?)
        corrs = func.detect_correlations_from_bursts(all_channel_bursts[i], all_user_bursts[i], message_types_of_all_senders[i], j)

        if corrs != -1:
            corrs_tp.extend(corrs)
            corrsWithoutNeg = [i for i in corrs if i != -1]     
            #  corrs_tp_withougNegativ.extend(corrs_tp_withougNegativ)
            print ("File {} is done: Rate: {}".format(i,numpy.average(corrsWithoutNeg)))
            # print(corrsWithoutNeg)

    
    print ("Matchrate: {}".format(numpy.average([i for i in corrs_tp if i != -1])))
    all_correlations_tp.append([c for c in corrs_tp if c != -1]) # MAAAAN die haben sich doch extra eine Funktion geschrieben die alle negativen herausfiltert.. Stattdessen machen die hier so eine häßliche scheiße
#with open(os.path.join(results_dir, 'corrs_tp_event_based_delta_{}.pickle'.format(DELTA)), 'wb') as handle: # Abspeichern der Match Ratios
#    pickle.dump(all_correlations_tp, handle, protocol=pickle.HIGHEST_PROTOCOL)


#---------------------------------------------

# calculate correlations for false samples (uncorrelated pairs of flows)

print("Calculating FP Values..")    
all_corrs_fp = func.CorrelationAllFiles(all_channel_bursts,all_user_bursts, message_types_of_all_senders)



 #------------------------------------
TPAvgList = []
TPStdList = []
resultList: List[func.ResultObj_t] = []
for j in range(len(conf.observation_lengths)): # Test Correlation_Match_Ratios for Different Obsveration Lengths        
    TPAvg =numpy.average(all_correlations_tp[j])
    TPAvgList.append(TPAvg)
    TPStdList.append( numpy.std(all_correlations_tp[j]))
    FPAvg = numpy.average(all_corrs_fp[j])
    ObsLen: int = conf.observation_lengths[j]
    print('For {}-second interval:'.format(ObsLen))
    print ("    Matchrate TP: {}".format(TPAvg))    
    print ("    Matchrate FP: {}".format(FPAvg))
    print ("    from {} Total intervals".format(len(all_correlations_tp[j])))

    obj = func.ResultObj_t(TP=TPAvg, FP=FPAvg, IntervalLen=ObsLen)
    resultList.append(obj)


# plt.errorbar(conf.observation_lengths,TPAvgList,yerr=TPStdList,fmt='o')
# currPath = resultFilePath+'\\boxplotTest.png'
# plt.savefig(currPath)
# print("Saved ErrorDiagram to: {}".format(currPath))



results_json = json.dumps([obj.__dict__ for obj in resultList], indent=4)
currPath = resultFilePath+"\\summary.json"
with open(currPath,"w") as f:
    f.write(results_json)
    print ("wrote result summary file to: "+currPath)




for j in range(len(conf.observation_lengths)): # Test Correlation_Match_Ratios for Different Obsveration Lengths    
    results_json = json.dumps(all_correlations_tp[j], indent=4)
    currPath = resultFilePath+"\\TP-"+str(conf.observation_lengths[j])+".json"
    with open(currPath,"w") as f:
        f.write(results_json)
        print ("wrote detailed result file to: "+currPath)

    results_json = json.dumps(all_corrs_fp[j], indent=4)
    currPath = resultFilePath+"\\FP-"+str(conf.observation_lengths[j])+".json"
    with open(currPath,"w") as f:
        f.write(results_json)
        print ("wrote detailed result file to: "+currPath)




