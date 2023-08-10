using System;
using System.Linq;

namespace Sudoku.Data;

/*
Based on the Python code from here https://www.101computing.net/sudoku-generator-algorithm/


def fillGrid(grid):
  global counter
  #Find next empty cell
  for i in range(0,81):
    row=i//9
    col=i%9
    if grid[row][col]==0:
      shuffle(numberList)      
      for value in numberList:
        #Check that this value has not already be used on this row
        if not(value in grid[row]):
          #Check that this value has not already be used on this column
          if not value in (grid[0][col],grid[1][col],grid[2][col],grid[3][col],grid[4][col],grid[5][col],grid[6][col],grid[7][col],grid[8][col]):
            #Identify which of the 9 squares we are working on
            square=[]
            if row<3:
              if col<3:
                square=[grid[i][0:3] for i in range(0,3)]
              elif col<6:
                square=[grid[i][3:6] for i in range(0,3)]
              else:  
                square=[grid[i][6:9] for i in range(0,3)]
            elif row<6:
              if col<3:
                square=[grid[i][0:3] for i in range(3,6)]
              elif col<6:
                square=[grid[i][3:6] for i in range(3,6)]
              else:  
                square=[grid[i][6:9] for i in range(3,6)]
            else:
              if col<3:
                square=[grid[i][0:3] for i in range(6,9)]
              elif col<6:
                square=[grid[i][3:6] for i in range(6,9)]
              else:  
                square=[grid[i][6:9] for i in range(6,9)]
            #Check that this value has not already be used on this 3x3 square
            if not value in (square[0] + square[1] + square[2]):
              grid[row][col]=value
              if checkGrid(grid):
                return True
              else:
                if fillGrid(grid):
                  return True
      break
*/

public class BacktrackingGenerator : IPuzzleGenerator {
    public Puzzle Generate() {
        int retries = 50;
        for (var i = 0; i < retries; i++) {
            Puzzle p = new Puzzle();
            fill(p);
            if (valid(p)) {
                Console.WriteLine("Generated in " + i + " tries");
                return p;
            } else {
                continue;
            }
        }
        throw new Exception("Maximum retries reached.");
    }

    private bool fill(Puzzle p) {
        List<int> number_options = new List<int>{ 1,2,3,4,5,6,7,8,9 };

        // Find next empty cell
        foreach (var cell in p) {
            // Cell isn't empty, skip it
            if (cell.ActualValue != 0)
                continue;

            // Add randomness
            number_options.Shuffle();

            // Try the options
            foreach (var option in number_options) {
                if (not_in_row(cell.Row, option)) {
                    if (not_in_col(cell.Column, option)) {
                        if (not_in_block(cell.Block, option)) {
                            cell.ActualValue = option;
                            if (check(p)) {
                                return true;
                            } else {
                                if (fill(p)) {
                                    return true;
                                }
                            }
                        } 
                    }
                }
            }

            break;
        }

        return false;
    }

    private bool check(Puzzle p) {
        foreach (var cell in p) {
            if (cell.ActualValue == 0)
                return false;
        }
        return true;
    }

    private bool valid(Puzzle p) {
        foreach (var cell in p) {
            if (cell.ActualValue == 0) {
                Console.WriteLine($"{cell} has no value");
                return false;                           // No value assigned to cell
            }
            foreach (var r in cell.Row) {
                if (r == cell)
                    continue;
                if (cell.ActualValue == r.ActualValue) { // Value shared in row
                    Console.WriteLine($"{cell} has duplicate value of {r}");
                    return false;
                }
            }
            foreach (var c in cell.Column) {
                if (c == cell)
                    continue;
                if (cell.ActualValue == c.ActualValue) { // Value shared in column
                    Console.WriteLine($"{cell} has duplicate value of {c}");
                    return false;
                }
            }
            foreach (var b in cell.Block) {
                if (b == cell)
                    continue;
                if (cell.ActualValue == b.ActualValue) { // Value shared in block
                    Console.WriteLine($"{cell} has duplicate value of {b}");
                    return false;
                }
            }
        }
        return true;
    }

    private bool not_in_row(RowEnumerator row, int option) {
        return !(row.Where(x => x.ActualValue == option).Any());
    }

    private bool not_in_col(ColumnEnumerator col, int option) {
        return !(col.Where(x => x.ActualValue == option).Any());
    }

    private bool not_in_block(Block block, int option) {
        return !(block.Where(x => x.ActualValue == option).Any());
    }

}