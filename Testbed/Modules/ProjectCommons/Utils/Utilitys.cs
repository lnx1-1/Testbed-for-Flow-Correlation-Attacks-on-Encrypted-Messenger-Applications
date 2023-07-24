using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectCommons {
    public class Utilitys {
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        /// <summary>
        /// Checks if Exception means that the file is Used by another process and therefore locked. <br/>
        /// Source: <a href="https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use">Stackoverflow : Is there a way to check if a file is in use?</a>
        /// </summary>
        /// <param name="exception">The Exception to check</param>
        /// <returns>True if Exception was thrown because of fileInUse</returns>
        public static bool IsFileLocked(Exception exception) {
            int errorCode = exception.HResult & ((1 << 16) - 1);
            return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
        }


        /// <summary>
        /// Returns the Directory of the Filepath.
        /// </summary>
        /// <param name="filePath">Path to a File</param>
        /// <returns>Path to the Directory where the file lives in</returns>
        /// <exception cref="ArgumentException">If the provided Path isn't correct</exception>
        public static string GetDirectoryPath(string filePath) {
            var info = new FileInfo(filePath);
            return info.DirectoryName;
            // if (filePath.Trim().Length == 0) {
            // 	throw new ArgumentException($"FilePath empty: [{filePath}]");
            // }
            // int index = Math.Max(filePath.LastIndexOf('\\'), filePath.LastIndexOf('/'));
            // return filePath.Substring(0, index);
        }


        /// <summary>
        /// Formats the FilePath using Instance Numbers..
        /// ex: logFile_1.pcap
        /// </summary>
        /// <param name="instanceNumber"></param>
        /// <param name="fileFolder">the Path to the Folder where the file sits in</param>
        /// <param name="fileName">The Filename ex: logFile</param>
        /// <param name="fileEnding">the Fileending ex: .pcap</param>
        /// <returns></returns>
        public static string FormatFilePath(int instanceNumber, string fileFolder, string fileName,
            string fileEnding) {
            if (fileEnding.StartsWith(".")) {
                fileEnding = fileEnding.Substring(1);
            }

            return Path.Combine(fileFolder, String.Format($"{fileName}{instanceNumber}.{fileEnding}"));
        }

        public static int GetFileNumber(string fileName) {
            string pattern = @$".*(\d+)\..*";
            RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;
            var collection = Regex.Matches(fileName, pattern, options);
            int number = 0;
            if (collection.Count == 1) {
                number = int.Parse(collection[0].Groups[1].Value);
            }

            return number;
        }

        /// <summary>
        /// Returns absolute path from relative Path, relative to RootDirectory of Project (CryptCorr)
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static string GetAbsolutePath(string relativePath) {
            return Path.Combine(GetRootDirectory(), relativePath);
        }
        
        /// <summary>
        /// Returns the RootDirectory (cryptCorr) regardless from where it is invoked
        /// </summary>
        /// <returns>the absolute path of rootDirectory</returns>
        public static string GetRootDirectory() {
            var currDir = Directory.GetCurrentDirectory();

            for (int i = 0; i < 10 && currDir != null; i++) {
                var dirName = Path.GetFileName(currDir);
                if (dirName is "cryptCorr") {
                    return currDir;
                }

                currDir = Directory.GetParent(currDir)?.FullName;
            }

            throw new ArgumentException("Couldnt found Dir..");
        }


        public static string GetNextFileNumber(string filePath) {
            var paths = Directory.GetFiles(GetDirectoryPath(filePath));
            string pattern = @$"(.*)(\d+)?(\..*)";
            int number = 0;
            string front = "";
            string back = "";
            RegexOptions options = RegexOptions.Multiline | RegexOptions.RightToLeft;
            var result = Regex.Match(filePath, pattern, options);
            if (result.Success) {
                front = result.Groups[1].Value;
                if (result.Groups.Count == 3) {
                    //contains digits
                    number = int.Parse(result.Groups[2].Value);
                    back = result.Groups[3].Value;
                }
                else {
                    back = result.Groups[2].Value;
                }
            }

            var matchPattern = Regex.Escape(front) + @"(\d+)" + Regex.Escape(back);

            List<int> numList = new List<int>() { 0 };
            foreach (string path in paths) {
                var collection = Regex.Matches(path, matchPattern, options);
                if (collection.Count == 1) {
                    numList.Add(GetFileNumber(path));
                }
            }

            return front + numList.Max() + back;
        }
    }
}