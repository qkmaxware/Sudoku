namespace Sudoku.Data;

/// <summary>
/// Interface for generators that can create puzzles
/// </summary>
public interface IPuzzleGenerator {
    /// <summary>
    /// Create a new puzzle
    /// </summary>
    /// <returns>completed puzzle</returns>
    public Puzzle Generate();
}