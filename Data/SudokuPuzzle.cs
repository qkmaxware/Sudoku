using System.Collections;
using System.Collections.Generic;

namespace Sudoku.Data;

public class Cell {
    public int CellX => BlockOffsetX + Block.BlockX * 3;
    public int CellY => BlockOffsetY + Block.BlockY * 3;
    public int BlockOffsetX {get; private set;}
    public int BlockOffsetY {get; private set;}
    public int ActualValue {get; set;}
    public int? EnteredValue {get; set;}
    public bool IsEnteredValueWrong => EnteredValue.HasValue && EnteredValue.Value != ActualValue;
    public HashSet<int> PotentialValues {get; private set;} = new HashSet<int>();

    public Puzzle Puzzle => Block.Puzzle;
    public Block Block {get; private set;}
    public RowEnumerator Row => new RowEnumerator(Puzzle, this.CellY);
    public ColumnEnumerator Column => new ColumnEnumerator(Puzzle, this.CellX);

    public Cell(Block owner, int x, int y) {
        this.Block = owner;
        this.ActualValue = 0;
        this.BlockOffsetX = x;
        this.BlockOffsetY = y;
    }

    public override string ToString() => $"Cell(x: {CellX}, y: {CellY})";
}

public class RowEnumerator : IEnumerable<Cell> {
    private Puzzle Puzzle;
    private int rowIdx;
    public RowEnumerator(Puzzle puz, int row) {
        this.Puzzle = puz; this.rowIdx = row;
    }

    public IEnumerable<int> EnteredDigits {
        get {
            foreach (var cell in this) {
                if (cell.EnteredValue.HasValue)
                    yield return cell.EnteredValue.Value;
            }
        }
    }

    public IEnumerator<Cell> GetEnumerator() {
        for (var i = 0; i < 9; i++) {
            yield return Puzzle.GetCell(i, rowIdx);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }
}

public class ColumnEnumerator : IEnumerable<Cell> {
    private Puzzle Puzzle;
    private int colIdx;
    public ColumnEnumerator(Puzzle puz, int col) {
        this.Puzzle = puz; this.colIdx = col;
    }

    public IEnumerable<int> EnteredDigits {
        get {
            foreach (var cell in this) {
                if (cell.EnteredValue.HasValue)
                    yield return cell.EnteredValue.Value;
            }
        }
    }

    public IEnumerator<Cell> GetEnumerator() {
        for (var i = 0; i < 9; i++) {
            yield return Puzzle.GetCell(colIdx, i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }
}

public class Block : IEnumerable<Cell> {
    private Cell[,] cells;

    public Puzzle Puzzle {get; private set;}
    public int BlockX {get; private set;}
    public int BlockY {get; private set;}

    public int CellOffsetX => BlockX * 3;
    public int CellOffsetY => BlockY * 3;

    public Cell this[int x, int y] => cells[x,y];

    public Block(Puzzle puzzle, int x, int y) {
        this.Puzzle = puzzle;
        this.BlockX = x;
        this.BlockY = y;

        this.cells = new Cell[3,3];
        for (var i = 0; i < 3; i++) {
            for (var j = 0; j < 3; j++) {
                this.cells[i,j] = new Cell(this, i, j);
            }
        }
    }

    public IEnumerable<int> EnteredDigits {
        get {
            foreach (var cell in this) {
                if (cell.EnteredValue.HasValue)
                    yield return cell.EnteredValue.Value;
            }
        }
    }

    public IEnumerator<Cell> GetEnumerator() {
        for (var i = 0; i < 3; i++) {
            for (var j = 0; j < 3; j++) {
                yield return this[i,j];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }
}

public class Puzzle : IEnumerable<Cell> {
    private Block[,] blocks;

    public Puzzle() {
        this.blocks = new Block[3,3];

        for (var i = 0; i < 3; i++) {
            for (var j = 0; j < 3; j++) {
                this.blocks[i,j] = new Block(this, i, j);
            }
        }
    }

    public IEnumerable<Cell> Cells {
        get {
            for (var i = 0; i < 9; i++) {
                for (var j = 0; j < 9; j++) {
                    yield return this.GetCell(i,j);
                }
            }
        }
    }

    public IEnumerable<RowEnumerator> Rows {
        get {
            for (var i = 0; i < 9; i++) {
                yield return new RowEnumerator(this, i);
            }
        }
    }

    public IEnumerable<ColumnEnumerator> Columns {
        get {
            for (var i = 0; i < 9; i++) {
                yield return new ColumnEnumerator(this, i);
            }
        }
    }

    public IEnumerable<Block> Blocks {
        get {
            for (var i = 0; i < 3; i++) {
                for (var j = 0; j < 3; j++) {
                    yield return blocks[i,j];
                }
            }
        }
    } 

    public bool IsSolved() {
        foreach (var cell in this) {
            if (!cell.EnteredValue.HasValue || cell.IsEnteredValueWrong) {
                return false;
            }
        } 
        return true;
    }

    public bool AreEnteredValuesValid() {
        foreach (var cell in this) {
            if (!cell.EnteredValue.HasValue) {
                return false; // Missing value
            }
        }
        foreach (var row in Rows) {
            var distinct = row.EnteredDigits.GroupBy(x => x).All(g => g.Count() == 1);
            if (!distinct)
                return false; // Duplicate value in row
        }
        foreach (var column in Columns) {
            var distinct = column.EnteredDigits.GroupBy(x => x).All(g => g.Count() == 1);
            if (!distinct)
                return false; // Duplicate value in column
        }
        foreach (var block in Blocks) {
            var distinct = block.EnteredDigits.GroupBy(x => x).All(g => g.Count() == 1);
            if (!distinct)
                return false; // Duplicate value in block
        }
        return true;
    }

    public Cell GetCell(int x, int y) {
        var blockX = x / 3;
        var blockY = y / 3;
        var cellX = x - blockX * 3;
        var cellY = y - blockY * 3;
        return GetBlock(blockX, blockY)[cellX, cellY];
    }

    public Block GetBlock(int x, int y) {
        return this.blocks[x, y];
    }

    public IEnumerator<Cell> GetEnumerator() {
        return this.Cells.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }

    public Puzzle DeepCopy() {
        Puzzle x = new Puzzle();

        for (var i = 0; i < 9; i++) {
            for (var j = 0; j < 9; j++) {
                x.GetCell(i,j).ActualValue = this.GetCell(i,j).ActualValue;
                x.GetCell(i,j).EnteredValue = this.GetCell(i,j).EnteredValue;
            }
        }

        return x;
    }
}