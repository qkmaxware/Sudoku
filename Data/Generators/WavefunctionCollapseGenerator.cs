using System;
using System.Linq;

namespace Sudoku.Data;

public class WavefunctionCollapseGenerator : IPuzzleGenerator {
    private Random rng = new Random();

    public Puzzle Generate() {
        Puzzle p = new Puzzle();

        int retries = 50;
        for (var i = 0; i < retries; i++) {
        
            // Initialize superposition
            InitializeSuperposition(p);

            // Until resolved
            try {
                for (var collapseNum = 0; collapseNum < 9*9; collapseNum++) {
                    // Pick cell with "lowest entropy"
                    //         [only those still in superposition]                               [grouped by their entropy]                     [ordered by their entropy]             
                    if (ReferenceEquals(Collapse(p), null)) {
                        break;
                    }
                    //Console.WriteLine("Cell(" + cell.CellX + ", " + cell.CellY + ") = " + randomState);
                }
            } catch {
                continue;
            }

            // Done
            Console.WriteLine("Generated in " + i + " tries");
            return p;
        }

        throw new Exception("Max retries hit");
    }

    public void InitializeSuperposition(Puzzle p) {
        foreach (var cell in p) {
            cell.ActualValue = 0;
            cell.EnteredValue = null;
            foreach (var value in new int[]{1, 2, 3, 4, 5, 6, 7, 8, 9})
                cell.PotentialValues.Add(value);
        }
    }

    public Cell? Collapse(Puzzle p) {
        // Collapse state 
        var uncollapsed_cells = p.Where((cell) => cell.ActualValue == 0 || cell.PotentialValues.Count > 0).ToList();
        if (uncollapsed_cells.Count == 0)
            return null;

        var group = uncollapsed_cells.GroupBy((cell) => cell.PotentialValues.Count).OrderBy((x) => x.Key).FirstOrDefault()?.ToList();
        if (ReferenceEquals(group, null)) {
            throw new Exception("No cells to collapse");
        }
        var cell = group.Count > 0 ? group.Random() : null;
        if (ReferenceEquals(cell, null)) {
            throw new Exception("Lowest entropy group is empty");
        }
        Collapse(p, cell);
        
        return cell;
    }

    public void Collapse(Puzzle p, Cell cell) {
        var randomState = cell.PotentialValues.Random();
        cell.ActualValue = randomState;
        cell.PotentialValues.Clear();

        // Propagate to other cells
        propagate(p, cell, randomState);
    }

    private void propagate_row(Puzzle p, Cell cell, int value) {
        List<Cell> singletons = new List<Cell>(10);
        foreach (var otherInRow in cell.Row) {
            if (otherInRow == cell)
                continue;
            otherInRow.PotentialValues.Remove(value);
            if (isSingleton(otherInRow)) {
                singletons.Add(otherInRow);
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate_column(Puzzle p, Cell cell, int value) {
        List<Cell> singletons = new List<Cell>(10);
        foreach (var otherInCol in cell.Column) {
            if (otherInCol == cell)
                continue;
            otherInCol.PotentialValues.Remove(value);
            if (isSingleton(otherInCol)) {
                singletons.Add(otherInCol);
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate_block(Puzzle p, Cell cell, int value) {
        List<Cell> singletons = new List<Cell>(10);
        foreach (var otherInBlock in cell.Block) {
            if (otherInBlock == cell)
                continue;
            otherInBlock.PotentialValues.Remove(value);
            if (isSingleton(otherInBlock)) {
                singletons.Add(otherInBlock);
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate_knight(Puzzle p, Cell cell, int value) {
        // Any two cells separated by a knight's move [https://i.stack.imgur.com/4cNvL.jpg] cannot contain the same digit
        List<Cell> singletons = new List<Cell>(10);
        var coordinates = new List<(int, int)>() {
            (cell.CellX - 2, cell.CellY - 1), (cell.CellX - 1, cell.CellY - 2),
            (cell.CellX + 1, cell.CellY - 2), (cell.CellX + 2, cell.CellY - 1),
            (cell.CellX - 2, cell.CellY + 1), (cell.CellX - 1, cell.CellY + 2),
            (cell.CellX + 2, cell.CellY + 1), (cell.CellX + 1, cell.CellY + 2),
        };
        foreach (var coord in coordinates) {
            if (isValidLocn(coord.Item1, coord.Item2)) {
                var other = p.GetCell(coord.Item1, coord.Item2);
                other.PotentialValues.Remove(value);
                if (isSingleton(other)) {
                    singletons.Add(other);
                }
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate_king(Puzzle p, Cell cell, int value) {
        // Any to cell separated by a king's move [https://chessdelta.com/wp-content/uploads/2021/03/king-moves-in-chess-chessdelta.com-2.jpg] cannot contain the same digit
        List<Cell> singletons = new List<Cell>(10);
        var coordinates = new List<(int, int)>() {
            (cell.CellX - 1, cell.CellY - 1), (cell.CellX, cell.CellY - 1), (cell.CellX + 1, cell.CellY - 1),
            (cell.CellX - 1, cell.CellY)    , /*------------------------*/  (cell.CellX + 1, cell.CellY),
            (cell.CellX - 1, cell.CellY + 1), (cell.CellX, cell.CellY + 1), (cell.CellX + 1, cell.CellY + 1),
        };
        foreach (var coord in coordinates) {
            if (isValidLocn(coord.Item1, coord.Item2)) {
                var other = p.GetCell(coord.Item1, coord.Item2);
                other.PotentialValues.Remove(value);
                if (isSingleton(other)) {
                    singletons.Add(other);
                }
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate_orthogonally_adjacent(Puzzle p, Cell cell, int value) {
        // Any two orthogonally adjacent cell cannot contain consecutive digits
        List<Cell> singletons = new List<Cell>(10);
        var coordinates = new List<(int, int)>() {
            (cell.CellX - 1, cell.CellY), (cell.CellX + 1, cell.CellY), 
            (cell.CellX, cell.CellY + 1), (cell.CellX, cell.CellY - 1), 
        };
        foreach (var coord in coordinates) {
            if (isValidLocn(coord.Item1, coord.Item2)) {
                var other = p.GetCell(coord.Item1, coord.Item2);
                other.PotentialValues.Remove(value + 1);
                other.PotentialValues.Remove(value - 1);
                if (isSingleton(other)) {
                    singletons.Add(other);
                }
            }
        }
        foreach (var c in singletons) {
            Collapse(p, c);
        }
    }

    private void propagate(Puzzle p, Cell cell, int value) {
        // No other cell in the same row can contain the same digit
        propagate_row(p, cell, value);
        // No other cell in the same column can contain the same digit
        propagate_column(p, cell, value);
        // No other cell in the same block can contain the same digit
        propagate_block(p, cell, value);
        // Other rules (https://www.youtube.com/watch?v=yKf9aUIxdb4)
        //propagate_knight(p, cell, value);
        //propagate_king(p, cell, value);
        //propagate_orthogonally_adjacent(p, cell, value);
    }

    private bool isValidLocn(int x, int y) {
        return x >= 0 && x < 9 && y >= 0 && y < 9;
    }

    private bool isSingleton(Cell cell) {
        if (cell.PotentialValues.Count == 1) {
            return true;
        } else {
            return false;
        }
    }
}