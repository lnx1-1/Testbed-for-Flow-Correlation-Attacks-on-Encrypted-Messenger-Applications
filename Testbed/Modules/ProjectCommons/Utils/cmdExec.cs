using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ProjectCommons.Utils {
    public class CmdExec {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        
        public static void RunPythonScript(string scriptPath, params string[] args) {
            log.Info($"Launching Python Script: [{scriptPath}]");
            // log.Info($"Current Directory: {Directory.GetCurrentDirectory()}");
            var argString = "";
            if (args.Length > 0) {
                argString = args.ToList().Aggregate((a, b) => a + " " + b);
            }

            var cmdString = $"/C python -u \"{scriptPath}\" {argString}";
            log.Info($"cmdString [{cmdString}]");
            
            ProcessStartInfo startInfo = new ProcessStartInfo {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                WorkingDirectory = Utilitys.GetRootDirectory(),
                Arguments = cmdString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

// Starting our Process
            Process proc = new Process {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            proc.OutputDataReceived += (sender, e) => log.Info("Py: "+e.Data);
            proc.ErrorDataReceived += (sender, e) => {
                if (e != null && e.Data != null && e.Data.Trim().Length != 0) {
                    log.Error("Py: " + e.Data);
                }
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
        }
        
      }
}