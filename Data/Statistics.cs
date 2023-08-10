using System;
using System.Collections.Generic;

namespace Sudoku.Data;

public class StatsRow {
    public float PercentExposed {get; set;}
    public double FastestTimeSeconds {get; set;}
    public double SumOfTimeSeconds {get; set;}
    public double AverageTimeSeconds() => SumOfTimeSeconds / GamesPlayed;
    public int GamesPlayed {get; set;}
}

public class Statistics {
    public List<StatsRow>? Games {get; set;}
    public void CompleteGame(float percentExposed, TimeSpan elapsed) {
        if (Games == null)
            Games = new List<StatsRow>();

        var row = Games.Where(row => row.PercentExposed == percentExposed).FirstOrDefault();
        if (row == null) {
            row = new StatsRow();
            row.PercentExposed = percentExposed;
            Games.Add(row);
        }

        var seconds = elapsed.TotalSeconds;
        if (row.GamesPlayed == 0 || seconds < row.FastestTimeSeconds) {
            row.FastestTimeSeconds = seconds;
        }
        row.SumOfTimeSeconds += seconds;
        row.GamesPlayed += 1;
    }
}