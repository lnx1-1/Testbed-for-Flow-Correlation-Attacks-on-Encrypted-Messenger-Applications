using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using packageSniffer.RemoteSniffer;
using ProjectCommons;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace packageSniffer {
    internal static class RemoteSnifferLogic {
        private static Logger _log = LogManager.GetCurrentClassLogger();


        public static bool IsReachable(string ipaddress) {
            var ping = new Ping();
            var reply = ping.Send(ipaddress, PING_TIMEOUT);
            return reply != null && reply.Status == IPStatus.Success;
        }

        private const int PING_TIMEOUT = 5 * 1000;
        private const int NUMBER_OF_FILE_WRITE_TRIES = 5;

        public class CmdAsyncState {
            private RemoteSnifferInstrument _instrument;
            private CancellationToken _token;
            private string _cmd;
            private string _userName;

            public CmdAsyncState(RemoteSnifferInstrument sniffer, string cmd, CancellationToken token, string userName) {
                _instrument = sniffer;
                _token = token;
                _userName = userName;
                _cmd = cmd.Split(' ')[0];
            }

            public bool isActive() {
                var result = RunSSHcmd(_instrument, $"pgrep {_cmd}", _token, null, _userName).Trim();
                bool containsOnlyNumbers = !string.IsNullOrEmpty(result) && Regex.IsMatch(result, @"^\d+$");
                return containsOnlyNumbers;
            }

            public string getOutput() {
                return RunSSHcmd(_instrument, "cat cmdAsyncOut.txt", _token, null, _userName);
            }
        }

        public static CmdAsyncState RunSshCmdBackground(RemoteSnifferInstrument remoteSniffer, string cmdString, CancellationToken cancellationToken,
            Action<string> logAction = null, string username = null) {
            if (username == null) {
                username = remoteSniffer.CaptureUsername;
            }

            using (var sshClient =
                   new SshClient(remoteSniffer.IpAddr, username,
                       remoteSniffer.CaptureUsrPassword)) {
                _log.Debug($"Connecting to remote Sniffer: {username}@{remoteSniffer.IpAddr}");
                sshClient.Connect();
                var cmd = sshClient.CreateCommand($"{cmdString} > cmdAsyncOut.txt  2>&1 &");
                _log.Debug($"Executing: Async [{cmdString}] ");
                cmd.Execute();
            }

            CmdAsyncState state = new CmdAsyncState(remoteSniffer, cmdString, cancellationToken, username);
            return state;
        }


        /// <summary>
        /// Runs the provided CMD and returns the Debug Output result of the CMD for evaluation
        /// Command returns when execution finishes
        /// </summary>
        /// <param name="remoteSniffer">The Remote Sniffer Device where the Cmd schould run on</param>
        /// <param name="cmdString">The string conaining the CMD to run on the remote device</param>
        /// <param name="cancelToken"></param>
        /// <param name="logAction">A Lambda expr to execute when there is Output ready. Can be used to log while this cmd is in Progress</param>
        /// <param name="tapThread"></param>
        /// <param name="username"></param>
        /// <returns>The Output string read to finish </returns>
        public static string RunSSHcmd(RemoteSnifferInstrument remoteSniffer, string cmdString, CancellationToken cancelToken,
            Action<string> logAction = null, string username = null) {
            StringBuilder stringOutput = new StringBuilder();

            if (username == null) {
                username = remoteSniffer.CaptureUsername;
            }

            using (var sshClient =
                   new SshClient(remoteSniffer.IpAddr, username,
                       remoteSniffer.CaptureUsrPassword)) {
                _log.Debug($"Connecting to remote Sniffer: {username}@{remoteSniffer.IpAddr}");
                sshClient.Connect();
                var cmd = sshClient.CreateCommand(cmdString);

                _log.Debug($"Executing: [{cmdString}] ");
                var asyncCmd = cmd.BeginExecute();

                
                var readerError = new StreamReader(cmd.ExtendedOutputStream);
                var readerStd = new StreamReader(cmd.OutputStream);

                // _log.Debug("Waiting for cmd end");

                while (!asyncCmd.IsCompleted || !readerError.EndOfStream || !readerStd.EndOfStream) {
                    //This makes it Interruptable
                    try {
                        if (cancelToken.IsCancellationRequested) {
                            throw new OperationCanceledException();
                        }
                    } catch (OperationCanceledException) {
                        _log.Warn("Cancel Requested. Trying to Cancel CMD");
                        cmd.CancelAsync();
                        return "Cancel";
                    }

                    // _log.Debug("read2End");
                    if (!readerStd.BaseStream.CanRead) {
                        _log.Error("Cant read stream");
                        return "";
                    }

                    // _log.Debug($"Peek Returning: {readerError.Peek()}");
                    int maxLoop = 5;
                    while (readerError.Peek() != -1 && maxLoop > 0) {
                        var output = readerError.ReadLine();
                        // _log.Debug($"Out: {output}");
                        // _log.Debug("Read Done");
                        // var stdOutput = readerStd.ReadToEnd().Trim();
                        if (output == null || output.Trim().Length == 0) {
                            continue;
                        }

                        stringOutput.Append(output+"\n");
                        logAction?.Invoke(output+"\n");
                        maxLoop -= 1;
                    }

                    maxLoop = 5;
                    while (readerStd.Peek() != -1 && maxLoop > 0) {
                        var output = readerStd.ReadLine();
                        if (output == null || output.Trim().Length == 0) {
                            continue;
                        }

                        stringOutput.Append(output);
                        logAction?.Invoke(output);
                        maxLoop -= 1;
                    }


                    // _log.Debug("Sleeping");
                    // if (stdOutput.Length > 0) {
                    //     logAction?.Invoke("std: "+stdOutput);
                    // }

                    Thread.Sleep(500);
                    // _log.Debug($"Sleep Done. State {asyncCmd.AsyncState}");
                    // _log.Debug($"Err: {readerError.EndOfStream}, Std: {readerStd.EndOfStream}, complete {asyncCmd.IsCompleted}");
                }

                _log.Debug("cmd Execute done");
                // cmd.EndExecute(asyncCmd); //Would block until end of remoteCall
                readerError.Close();
                readerStd.Close();
                // _log.Debug("Streams Closed");
            }

            // _log.Debug($"Returning stringOutput {stringOutput}");
            return stringOutput.ToString();
        }


        public static void CopyFileToRemote(string localFilePath, string remotePath, string ip, string usrName,
            string pw) {
            _log.Debug("Copying file to Remote");
            if (ip?.Length == 0 || usrName?.Length == 0 || pw?.Length == 0) {
                _log.Error("Settings error. Check IP, Usr, PW");
                return;
            }

            if (!File.Exists(localFilePath)) {
                _log.Error($"Couldnt find local file to Copy to Remote. Path: [{localFilePath}]");
                return;
            }

            using (var scpClient = new ScpClient(ip, usrName, pw)) {
                scpClient.Connect();
                _log.Debug("Connected SCP client");
                try {
                    scpClient.Upload(File.OpenRead(localFilePath), remotePath);
                } catch (ScpException e) {
                    _log.Error($"A directory with the specified path exists on the remote host.: Path: {remotePath}");
                    _log.Error(e.Message);
                    return;
                } catch (SshException e) {
                    _log.Error("Somtehing went wrong with the SSH connection. ");
                    _log.Error(e.Message);
                    return;
                }

                _log.Info($"local file: [{localFilePath}] was Succesfully uploaded to: [{remotePath}]");
            }
        }

        /// <summary>
        /// <b>Copys the PCAP file from the remoteSniffer.</b> <br/>
        /// If LocalFIlePath is currently in Use, a different name will be used
        /// </summary>
        /// <param name="remoteFilePath">the full RemoteFilepath</param>
        /// <param name="localFilePath">the Path where the local should be saved</param>
        /// <param name="ipAddress">The IP of the Remote Sniffer device</param>
        /// <param name="userName">User which was used in the setup process. Needs Copy rights..</param>
        /// <param name="password">password of the user</param>
        /// <returns>The Filepath where the File was saved <br/>
        /// Return Empty string if file wasn't saved correctly
        /// </returns>
        public static string CopyFileToLocal(string remoteFilePath, string localFilePath, string ipAddress,
            string userName, string password) {
            _log.Debug($"Copying capture file from Remote using usr: {userName}");
            if (ipAddress?.Length == 0 || userName?.Length == 0 || password?.Length == 0) {
                _log.Error("Settings error. Check IP, Usr, PW");
                return "";
            }

            using (var scpClient = new ScpClient(ipAddress, userName, password)) {
                scpClient.Connect();
                _log.Debug("Connected SCP client");
                var parentFolder = Utilitys.GetDirectoryPath(localFilePath);
                if (!Directory.Exists(parentFolder)) {
                    Directory.CreateDirectory(parentFolder);
                    _log.Debug("Created folder " + parentFolder);
                }

                bool fileTransferSuccessful = false;


                for (int i = 0; i < NUMBER_OF_FILE_WRITE_TRIES && !fileTransferSuccessful; i++) {
                    try {
                        var file = File.Create(localFilePath);
                        scpClient.Download(remoteFilePath, file);
                        _log.Debug($"Downloaded capture file to: {localFilePath}");
                        fileTransferSuccessful = true;
                    } catch (IOException e) {
                        _log.Debug("IO Exception: " + e.Message);
                        if (Utilitys.IsFileLocked(e)) {
                            _log.Warn("File is Used by another Process..");
                            localFilePath = Utilitys.FormatFilePath(i + 1, Utilitys.GetDirectoryPath(localFilePath),
                                Path.GetFileNameWithoutExtension(localFilePath), Path.GetExtension(localFilePath));
                        }
                    } catch (ScpException scpException) {
                        _log.Error("Error while copying to local: " + scpException.Message);
                        return "";
                    }
                }

                if (fileTransferSuccessful) {
                    _log.Info(".pcap files Succesfully Saved to: " + Path.GetFullPath(localFilePath));
                } else {
                    _log.Error("Couldnt Save .PCAP file");
                    localFilePath = "";
                }
            }

            return localFilePath;
        }
    }
}