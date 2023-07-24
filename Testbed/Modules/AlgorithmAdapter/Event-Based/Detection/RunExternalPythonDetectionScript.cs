using System.IO;
using Event_Based_Impl;
using OpenTap;
using ProjectCommons;
using ProjectCommons.Utils;

namespace AlgorithmAdapter.Event_Based.Detection {
    public class RunExternalPythonDetectionScript:TestStep {

        
        [Display("Script Path", Group: "Script")]
        public string _scriptPath { get; set; } = PathWrapper.DefaultPythonDetectionScryptPath; 
        
        [Display("Packets Input", "File or Folder Path to load .csv from", Group: "Input", Order: 1)]
        [DirectoryPath]
        public string _PcapINPath { get; set; } = PathWrapper.packets_PreprocessedFolder;

        [Display("Traces Input", "File or Folder Path to load .csv from", Group: "Input", Order: 1)]
        [DirectoryPath]
        public string _TracesINpath { get; set; } = PathWrapper.msgTraces_PreprocessedFolder;

        [Display("Results Folder Path", Group: "Output")]
        [DirectoryPath]
        public string _resultsPath { get; set; } = Path.Combine(PathWrapper.OutFiles.ResultsFolder);

        public bool appendTimeSuffix { get; set; } = true;
        
        
        public override void Run() {
            var outPutPath = _resultsPath;
            if (Directory.Exists(_resultsPath) && appendTimeSuffix) {
                outPutPath =  Path.Combine(_resultsPath, "Results_"+UnixDateTime.GetTimeString());
                Directory.CreateDirectory(outPutPath);
            }
            
            CmdExec.RunPythonScript(_scriptPath,_PcapINPath,_TracesINpath,outPutPath);

        }
    }
}