using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Event_Based_Impl.DataTypes;

namespace Event_Based_Impl.InputModules {
    public class DetectionResult {
        public List<Result> _results { get; set; }

        public DetectionResult() {
            _results = new List<Result>();
        }

        public class Result {
            public double TP { get; set; }
            public double FP { get; set; }
            public int IntervalLen { get; set; }

            public override string ToString() {
                return $"\nFor {IntervalLen}-Second Interval:\n   TP: {TP}\n   FP: {FP}\n";
            }
        }

        public override string ToString() {
            return _results.Select((result => result.ToString()))
                .Aggregate((a, b) => a.ToString() + "\n" + b.ToString());
        }

        public static DetectionResult CreateFromDict(Dictionary<int, List<double>> resultList) {
            var detectionResult = new DetectionResult();
            foreach (var pair in resultList) {
                var result = new DetectionResult.Result() {
                    TP = pair.Value.Average(),
                    IntervalLen = pair.Key
                };
                detectionResult._results.Add(result);
            }

            return detectionResult;
        }
         
        
        public static async Task<DetectionResult> LoadFromFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"FilePath {filePath} wasnt found");
            }
            using FileStream fileStream = File.OpenRead(filePath);
            return new DetectionResult() { _results = await JsonSerializer.DeserializeAsync<List<Result>>(fileStream) };
        }
    }
}