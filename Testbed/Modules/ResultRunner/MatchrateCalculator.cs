using System.Text.Json;
using NLog;
using NLog.Fluent;

namespace ResultRunner; 

public class MatchrateCalculator {

    private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    private List<double> _tpIntervalList;
    private List<double> _fpIntervalList;
    
    private MatchrateCalculator(List<double> tpIntervalList, List<double> fpIntervalList) {
        this._fpIntervalList = fpIntervalList;
        this._tpIntervalList = tpIntervalList;
    }

   
    public static MatchrateCalculator LoadFromFile(string TPPath, string FPPath) {
        string jsonStringTp = File.ReadAllText(TPPath);
        string jsonStringFp = File.ReadAllText(FPPath);
        List<double>? tpIntervalList = JsonSerializer.Deserialize<List<double>>(jsonStringTp);
        List<double>? fpIntervalList = JsonSerializer.Deserialize<List<double>>(jsonStringFp);
        if (tpIntervalList == null || fpIntervalList == null) {
            log.Error("Null List");
            return null;
        }

        return new MatchrateCalculator(tpIntervalList, fpIntervalList);

    }

    public void PrintRange(double stepsize) {
        for (double i = 0; i <= 1; i = i + stepsize) {
            PrintValues(i);
        }
    }
    
    public void PrintValues(double threshold) {
        var tuple = CalRates(_tpIntervalList, _fpIntervalList, threshold);
        log.Info($"N={threshold}");
        log.Info($"     TPR: {tuple.TPR}");
        log.Info($"     FPR: {tuple.FPR}");
    }

    public (double FPR, double TPR) CalRates(double threshold) {
        return CalRates(_tpIntervalList, _fpIntervalList, threshold);
    }
    
    public static (double FPR, double TPR) CalRates(List<double> tpIntervalList, List<double> fpIntervalList, double threshold) {
        int totalInterval = tpIntervalList.Count;
        int TP = tpIntervalList.Count(e => e >= threshold);
        int FN = tpIntervalList.Count(e => e < threshold);

        int FP = fpIntervalList.Count(e => e >= threshold);
        int TN = fpIntervalList.Count(e => e < threshold);

        if (TP + FN != totalInterval) {
            log.Error("WRONG");
        }

        double TPR = TP / ((double)TP + FP);
        double FPR = FP / ((double)FP + TN);

        return (FPR, TPR);
    }
}