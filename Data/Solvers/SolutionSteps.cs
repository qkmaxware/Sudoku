using System.Collections.Generic;

namespace Sudoku.Data;

/// <summary>
/// A step taken when someone randomly guesses the next value
/// </summary>
public class RandomGuess : SolutionStep {
    public RandomGuess(Cell cell, int value) : base(cell, value) { }
    public override string StepName() => "Random guess";
    public override string StepDescription() => "Randomly guess the value";
}

/// <summary>
/// A step taken when there is only one element left in the given row
/// </summary>
public class LastInRow : SolutionStep {
    public LastInRow(Cell cell, int value) : base(cell, value) {
        this.HintCells.AddRange(cell.Row);
    }
    public override string StepName() => "Last value in row";
    public override string StepDescription() => "There is only one empty cell remaining in the row so it's value is the only one missing";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        foreach (var chunk in puzzle.Rows) {
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Cell? empty = null;
            foreach (var cell in chunk) {
                if (cell.EnteredValue.HasValue) {
                    numbers.Remove(cell.EnteredValue.Value);
                    continue;
                } else {
                    empty = cell;
                }   
            }
            if (numbers.Count == 1 && !ReferenceEquals(empty, null)) {
                empty.EnteredValue = numbers[0];
                return new LastInRow(empty, numbers[0]);
            }
        }
        return null;
    }
}

/// <summary>
/// A step taken when there is only one element left in the given row
/// </summary>
public class LastInColumn : SolutionStep {
    public LastInColumn(Cell cell, int value) : base(cell, value) {
        this.HintCells.AddRange(cell.Column);
    }
    public override string StepName() => "Last value in column";
    public override string StepDescription() => "There is only one empty cell remaining in the column so it's value is the only one missing";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        foreach (var chunk in puzzle.Columns) {
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Cell? empty = null;
            foreach (var cell in chunk) {
                if (cell.EnteredValue.HasValue) {
                    numbers.Remove(cell.EnteredValue.Value);
                    continue;
                } else {
                    empty = cell;
                }   
            }
            if (numbers.Count == 1 && !ReferenceEquals(empty, null)) {
                empty.EnteredValue = numbers[0];
                return new LastInColumn(empty, numbers[0]);
            }
        }
        return null;
    }
}

/// <summary>
/// A step taken when there is only one element left in a given 3x3 block
/// </summary>
public class LastInBlock : SolutionStep {
    public LastInBlock(Cell cell, int value) : base(cell, value) {
        this.HintCells.AddRange(cell.Block);
    }
    public override string StepName() => "Last value in block";
    public override string StepDescription() => "There is only one empty cell remaining in the block so it's value is the only one missing";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        foreach (var chunk in puzzle.Blocks) {
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Cell? empty = null;
            foreach (var cell in chunk) {
                if (cell.EnteredValue.HasValue) {
                    numbers.Remove(cell.EnteredValue.Value);
                    continue;
                } else {
                    empty = cell;
                }   
            }
            if (numbers.Count == 1 && !ReferenceEquals(empty, null)) {
                empty.EnteredValue = numbers[0];
                return new LastInBlock(empty, numbers[0]);
            }
        }
        return null;
    }
}

/// <summary>
/// A step where we look at row, column, and block to identify if a given cell can only take on 1 value
/// </summary>
public class HiddenSingle : SolutionStep {
    public HiddenSingle(Cell cell, int value) : base(cell, value) {
        this.HintCells.AddRange(cell.Row);
        this.HintCells.AddRange(cell.Column);
        this.HintCells.AddRange(cell.Block);
    }
    public override string StepName() => "Hidden single";
    public override string StepDescription() => "Each row, column, and block can only contain the numbers 1 to 9 exactly once";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        foreach (var cell in puzzle) {
            // Skip already done cells
            if (cell.EnteredValue.HasValue)
                continue;

            // Eliminate values
            var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            foreach (var other in cell.Row.Concat(cell.Column).Concat(cell.Block)) {
                if (other.EnteredValue.HasValue) {
                    numbers.Remove(other.EnteredValue.Value);
                }
            }
            
            // If there is only a single value left it's gota be that
            if (numbers.Count == 1) {
                cell.EnteredValue = numbers[0];
                return new HiddenSingle(cell, numbers[0]);
            }
        }
        return null;
    }
}