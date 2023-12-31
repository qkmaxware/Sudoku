using System.Linq;
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
public class OnlyCandidate : SolutionStep {
    public OnlyCandidate(Cell cell, int value) : base(cell, value) {
        this.HintCells.AddRange(cell.Row);
        this.HintCells.AddRange(cell.Column);
        this.HintCells.AddRange(cell.Block);
    }
    public override string StepName() => "Only candidate";
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
                return new OnlyCandidate(cell, numbers[0]);
            }
        }
        return null;
    }
}

/// <summary>
/// A step where we look at row, column, and block to identify if a given cell can only take on 1 value
/// </summary>
public class Scanning : SolutionStep {
    public Scanning(Cell cell, int value, List<IEnumerable<Cell>> hints) : base(cell, value) {
        foreach (var hint in hints) {
            this.HintCells.AddRange(hint);
        }
    }
    public override string StepName() => "Scanning";
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
                    return new Scanning(cell, value, hints); 
                }
            }
        }

        return null;
    }

}

/// <summary>
/// Eliminating numbers from rows, columns, and boxes
/// </summary>
/// 
public class SingleElimination : SolutionStep {

    public SingleElimination(Cell cell, int value, IEnumerable<Cell> hints) : base(cell, value) {
        this.HintCells.AddRange(hints);
    }
    public override string StepName() => "Single Elimination";
    public override string StepDescription() => "Slowly remove possible values from a cell eliminating these possibilities using the rows and columns from other parts of the puzzle";

    public static SolutionStep? TryReduce(Puzzle puzzle) {
        // Create "initial" set of possibilities
        HashSet<int> all_possible = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        foreach (var cell in puzzle.Cells) {
            // Cell already has a value
            if (cell.EnteredValue.HasValue)
                continue;

            // Cell can be any of the following values
            cell.PotentialValues.Clear();
            foreach (var possible in all_possible) {
                if (cell.Block.EnteredDigits.Contains(possible)) {
                    continue;
                }
                if (cell.Row.EnteredDigits.Contains(possible)) {
                    continue;
                }
                if (cell.Column.EnteredDigits.Contains(possible)) {
                    continue;
                }
                cell.PotentialValues.Add(possible);
            }

            // Safety check, same as "OnlyCandidate"
            if (isSingleton(cell)) {
                cell.EnteredValue = cell.PotentialValues.First();
                return new OnlyCandidate(cell, cell.EnteredValue.Value);
            }
        }

        // Perform more "in-depth" eliminations to create singletons
        Dictionary<Cell, List<Cell>> eliminations_leading_to_singleton = new Dictionary<Cell, List<Cell>>();
        
        // If a value only can exist in 1 column/row then eliminate that value from the column/row in all blocks except the current one
        HashSet<int> remaining_digits_in_block = new HashSet<int>(9);
        HashSet<int> columnIndices = new HashSet<int>(3);
        HashSet<int> rowIndices = new HashSet<int>(3);
        foreach (var block in puzzle.Blocks) {
            remaining_digits_in_block.Clear();
            foreach (var digit in block.Where(x => !x.EnteredValue.HasValue).SelectMany(x => x.PotentialValues)) {
                remaining_digits_in_block.Add(digit);
            }

            foreach (var digit in remaining_digits_in_block) {
                columnIndices.Clear();
                rowIndices.Clear();

                // Record what rows/columns this digit can appear in for this block
                foreach (var cell in block) {
                    if (!cell.EnteredValue.HasValue && cell.PotentialValues.Contains(digit)) {
                        rowIndices.Add(cell.CellY);
                        columnIndices.Add(cell.CellX);
                    }
                }

                // If it only appears in 1 row, remove it as an option from the rest of the row
                if (rowIndices.Count == 1) {
                    var rowIndex = rowIndices.First();
                    var row = puzzle.Rows.ElementAt(rowIndex);
                    foreach (var cell in row) {
                        if (cell.Block != block) {
                            cell.PotentialValues.Remove(digit);
                            addCell(eliminations_leading_to_singleton, cell, row.Where(c => c.Block == block));
                        }
                    }
                }
                // If it only appears in 1 column, remove it as an option from the rest of the col
                else if (columnIndices.Count == 1) {
                    var colIndex = columnIndices.First();
                    var col = puzzle.Columns.ElementAt(colIndex);
                    foreach (var cell in col) {
                        if (cell.Block != block) {
                            cell.PotentialValues.Remove(digit);
                            addCell(eliminations_leading_to_singleton, cell, col.Where(c => c.Block == block));
                        }
                    }
                }
                
            }
        }

        foreach (var cell in puzzle.Cells) {
            if (isSingleton(cell)) {
                cell.EnteredValue = cell.PotentialValues.First();
                return new SingleElimination(cell, cell.EnteredValue.Value, tryGetOrEmpty(eliminations_leading_to_singleton, cell));
            }
        }

        return null;
    }

    private static void addCell(Dictionary<Cell, List<Cell>> src, Cell deletedOn, IEnumerable<Cell> deletedCuz) {
        List<Cell>? lst = null;
        if (!src.TryGetValue(deletedOn, out lst)) {
            lst = new List<Cell>();
            src[deletedOn] = lst;
        }
        foreach (var cell in deletedCuz)
            lst.Add(cell);
    }
    private static List<Cell> tryGetOrEmpty(Dictionary<Cell, List<Cell>> src, Cell key) {
        List<Cell>? lst = null;
        if (!src.TryGetValue(key, out lst)) {
            lst = new List<Cell>();
        }
        return lst;
    }

    private static bool isSingleton(Cell cell) {
        return cell.PotentialValues.Count == 1;
    }
}