using System.Collections.Generic;

namespace Sudoku.Data;

/// <summary>
/// Base class for each step towards the solution of a sudoku puzzle
/// </summary>
public abstract class SolutionStep {
    /// <summary>
    /// The cell the step applies to
    /// </summary>
    /// <value>cell</value>
    public Cell Cell {get; private set;}
    /// <summary>
    /// The value that should be input into the cell
    /// </summary>
    /// <value>value between 1 and 9</value>
    public int InputValue {get; private set;}
    /// <summary>
    /// The cells that can be used to direct the user to the solution
    /// </summary>
    /// <typeparam name="Cell"></typeparam>
    /// <returns>list of cells</returns>
    public List<Cell> HintCells {get; private set;} = new List<Cell>();

    public SolutionStep(Cell cell, int value) {
        this.Cell = cell;
        this.InputValue = value;
    }

    /// <summary>
    /// A name to display for the step
    /// </summary>
    /// <returns>name</returns>
    public abstract string StepName();
    /// <summary>
    /// A description of the step or how it is performed
    /// </summary>
    /// <returns>description</returns>
    public abstract string StepDescription();
}

/// <summary>
/// Interface for any sudoku puzzle solvers
/// </summary>
public interface IPuzzleSolver {
    /// <summary>
    /// Solve the current sudoku puzzle and return the steps required to reach the solution from the current state
    /// </summary>
    /// <param name="puzzle">puzzle to solve</param>
    /// <returns>solution steps</returns>
    public IEnumerable<SolutionStep> Solve(Puzzle puzzle);
} 