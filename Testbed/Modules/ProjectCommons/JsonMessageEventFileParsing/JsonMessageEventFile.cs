using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Event_Based_Impl.InputModules.JsonMessageEventFileParsing;

namespace ProjectCommons.JsonMessageEventFileParsing {
    /// <summary>
    /// Represents a Collection of Message Events. (This File Type comes from the Dataset from the Original Paper. Containing the generated Messages traces that should be sended)
    /// Its static Methods can be used to get a Instance, containing the Parsed Elements from the Provided File
    /// A Timestamp file can be added, containing Timestamps for the loaded msgEvents.
    /// <example>
    ///	<code>
    ///	var file = new JsonMessageEventFile(_msgTraces);
    ///	file.WriteToDisk("./messageScript.json", true); 
    /// </code>
    /// </example>
    public class JsonMessageEventFile {
    /// </summary>
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private const string MESSAGE_EVENT_FILE_EXTENSION = "*.json";

        public JsonMessageEventFile(List<MsgEvent> inList, string path = null, List<string> groupList = null ) {
            if (inList == null || inList.Count == 0) {
                throw new ArgumentException("inList is null or Empty");
            }

            _groups = groupList;
            _MsgEvents = inList;
            if (path != null) {
                this.Path = path;
            } else {
                this.Path = "";
            }
        }

        public JsonMessageEventFile() {
            _MsgEvents = new List<MsgEvent>();
        }

        [JsonPropertyName("messages")] public List<MsgEvent> _MsgEvents { get; set; }

        [JsonPropertyName("RecordedGroups")] public List<string> _groups { get; set; }
        [JsonPropertyName("Start_TimeStamp")] public double StartTimestamp { get; set; }

        [JsonPropertyName("Stop_TimeStamp")] public double StopTimestamp { get; set; }
        [JsonPropertyName("Path")] public string Path { get; set; }

        /// <summary>
        /// Loads a whole Folder containing MessageEvent Files (.json) 
        /// </summary>
        /// <param name="path">the Path to Look for the Files</param>
        /// <returns>A New MessageEventFile Object Containing all MessageEvents Extracted from the Files</returns>
        public static async Task<JsonMessageEventFile> LoadMessageEventFolder(string path) {
            List<MsgEvent> inList = await ReadMessageEventFolder(path);
            return new JsonMessageEventFile(inList);
        }

        /// <summary>
        /// Reads a Single MessageEvent File and Returns a new MessageEventFile Object Containing all the Events from the File
        /// </summary>
        /// <param name="pathToMessageEvents">Path to the file to load events from</param>
        /// <returns>The Object containing all read Events</returns>
        public static async Task<JsonMessageEventFile> ReadMessageEventFile(string pathToMessageEvents) {
            if (!File.Exists(pathToMessageEvents)) {
                log.Error($"JSON Message Event FilesPath doesnt exist: {pathToMessageEvents}");
                return null;
            }

            using FileStream fileStream = File.OpenRead(pathToMessageEvents);
            JsonMessageEventFile msgEvents = await JsonSerializer.DeserializeAsync<JsonMessageEventFile>(fileStream);
            return msgEvents;
        }

        /// <summary>
        /// Reads all MessageEvent Files from a Folder and returns a List containing all Message Events
        /// </summary>
        /// <param name="pathToFolder">the Folder to load from </param>
        /// <returns>The List Containing all MsgEvents</returns>
        public static async Task<List<MsgEvent>> ReadMessageEventFolder(string pathToFolder) {
            string[] msgEventPaths = Directory.GetFiles(pathToFolder, MESSAGE_EVENT_FILE_EXTENSION);
            log.Info($"Loading MessageEventFolder with {msgEventPaths.Length} files");
            List<Task<JsonMessageEventFile>> msgEventFilesTasks = new List<Task<JsonMessageEventFile>>();
            foreach (var path in msgEventPaths) {
                msgEventFilesTasks.Add(ReadMessageEventFile(path));
            }

            //Unpack msgEvents
            List<MsgEvent> outList = new List<MsgEvent>();
            foreach (var task in msgEventFilesTasks) {
                JsonMessageEventFile taskResult = await task;
                outList = outList.Concat(taskResult._MsgEvents).ToList();
            }

            return outList;
        }

        /// <summary>
        /// Writes all containing MessageEvents in a JSON file
        /// </summary>
        /// <param name="filePath">the FilePath to the Output JSON file</param>
        /// <param name="prettyPrint">if the file should be formatted</param>
        public void WriteToDisk(string filePath, bool prettyPrint = true) {
            log.Info($"writing {_MsgEvents.Count} events to Disk path: {System.IO.Path.GetFullPath(filePath)}");
            JsonSerializerOptions options = JsonSerializerOptions.Default;
            if (prettyPrint) {
                options = new JsonSerializerOptions() { WriteIndented = true };
            }

            string jsonFile = JsonSerializer.Serialize<JsonMessageEventFile>(this, options);
            File.WriteAllText(filePath, jsonFile);
        }

        /// <summary>
        /// Loads the Timestamp Folder and Tries to add the Timestamps from it to the Corresponding Events
        /// Matching is done by ID..
        /// If there is no Timestamp found for this Event, event is Skipped
        /// </summary>
        /// <param name="folderpath">The Path for Search for timestamp files.. @see TimestampFileParser</param>
        public void AddTimestampsFromFolder(string folderpath) {
            log.Info($"Loading Timestamp Folder: {folderpath}");
            var timestamps = TimestampFileParser.ParseTimeStampFolder(folderpath);
            AddTimestamps(timestamps);
        }

        /// <summary>
        /// Tries to add the Timstamp from the Dictionary to the Correspoding event.
        /// Matching is done by ID..
        /// If there is no Timestamp found for this Event, event is Skipped
        /// </summary>
        /// <param name="idTimestampDict">The dictionary containing the Timestamps and IDs</param>
        public void AddTimestamps(Dictionary<int, double> idTimestampDict) {
            log.Info("Trying to Add {0} timestamps", idTimestampDict.Count);
            foreach (var msgEvent in _MsgEvents) {
                if (idTimestampDict.ContainsKey(msgEvent.id)) {
                    msgEvent.timestamp = idTimestampDict[msgEvent.id];
                } else {
                    log.Warn($"Couldnt find ID event: {msgEvent}");
                }
            }
        }

        /// <summary>
        /// Removes Events with no timestamp set
        /// That means, if [timestamp < 0] 
        /// </summary>
        public void RemoveIncompleteEvents() {
            int counter = _MsgEvents.RemoveAll(eve => eve.timestamp < 0);
            log.Info($"Removed {counter} incomplete events: new Size: {_MsgEvents.Count}");
        }
    }
}