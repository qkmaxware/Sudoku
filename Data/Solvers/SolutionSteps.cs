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
public class HiddenSingleSameRowAndColumn : SolutionStep {
    public HiddenSingleSameRowAndColumn(Cell cell, int value) : base(cell, value) {
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
                return new HiddenSingleSameRowAndColumn(cell, numbers[0]);
            }
        }
        return null;
    }
}

/// <summary>
/// A step where we look at row, column, and block to identify if a given cell can only take on 1 value
/// </summary>
public class HiddenSingleDifferentRowsAndColumns : SolutionStep {
    public HiddenSingleDifferentRowsAndColumns(Cell cell, int value, List<IEnumerable<Cell>> hints) : base(cell, value) {
        foreach (var hint in hints) {
            this.HintCells.AddRange(hint);
        }
    }
    public override string StepName() => "Hidden single";
    public override string StepDescription() => "A block must contain all values between 1 and 9. By looking at each remaining number we can eliminate particular rows and columns from the block which contain that value. There is only one remaining cell in the block that can be the value and so it must be.";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        HashSet<int> available = new HashSet<int>(9);
        HashSet<Cell> all_empty = new HashSet<Cell>(9);
        HashSet<Cell> remaining_empty = new HashSet<Cell>(9);

        foreach (var block in puzzle.Blocks) {
            // Set all values a possible
            for (var i = 1; i <= 9; i++) {
                available.Add(i);
            }

            // Remove used values and mark empty cells
            all_empty.Clear();
            remaining_empty.Clear();
            foreach (var cell in block) {
                if (cell.EnteredValue.HasValue) {
                    // Cell is not empty
                    available.Remove(cell.EnteredValue.Value);   
                } else {
                    // Cell is empty
                    all_empty.Add(cell);
                }
            }

            // Check each intersecting row & column to see if these eliminate cells
            foreach (var value in available) {
                remaining_empty.UnionWith(all_empty);

                // Eliminate cells on rows with matching numbers
                for (var rowNum = 0; rowNum < 3; rowNum++) {
                    var rowId = block.CellOffsetY + rowNum;
                    foreach (var cell in new RowEnumerator(puzzle, rowId)) {
                        if (cell.EnteredValue.HasValue && cell.EnteredValue.Value == value) {
                            remaining_empty.RemoveWhere(c => c.CellY == rowId);
                            break;
                        }
                    }
                }
                // Eliminate cells on columns with matching numbers
                for (var colNum = 0; colNum < 3; colNum++) {
                    var colId = block.CellOffsetX + colNum;
                    foreach (var cell in new ColumnEnumerator(puzzle, colId)) {
                        if (cell.EnteredValue.HasValue && cell.EnteredValue.Value == value) {
                            remaining_empty.RemoveWhere(c => c.CellX == colId);
                            break;
                        }
                    }
                }

                // If there is only one spot left... it's gota be that number
                if (remaining_empty.Count == 1) {
                    // Get and update cell
                    var cell = remaining_empty.First();
                    cell.EnteredValue = value; 
                    // Get all "hints" leading to this step
                    List<IEnumerable<Cell>> hints = new List<IEnumerable<Cell>>();
                    //hints.Add(block.Where(c => c != cell));                 // All of the block except the cell
                    for (var rowNum = 0; rowNum < 3; rowNum++) {
                        var rowId = block.CellOffsetY + rowNum;
                        if (rowId != cell.CellY) {
                            var en = new RowEnumerator(puzzle, rowId);
                            if (en.Where(x => x.EnteredValue.HasValue && x.EnteredValue.Value == value).Any()) {
                                hints.Add(en);    // All rows except the row that contains the cell
                            }
                        }
                    }
                    for (var colNum = 0; colNum < 3; colNum++) {
                        var colId = block.CellOffsetX + colNum;
                        if (colId != cell.CellX) {
                            var en = new ColumnEnumerator(puzzle, colId);
                            if (en.Where(x => x.EnteredValue.HasValue && x.EnteredValue.Value == value).Any()) {
                                hints.Add(en);    // All columns except the col that contains the cell
                            }
                        }
                    }
                    // return step
                    return new HiddenSingleDifferentRowsAndColumns(cell, value, hints); 
                }
            }
        }

        return null;
    }

}
