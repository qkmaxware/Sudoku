using System.Collections.Generic;

namespace Sudoku.Data;

public abstract class AStarSolver : IPuzzleSolver {

    protected class SearchNode {
        public SolutionStep? StepTakenFromPrevious {get; set;}
        public SearchNode? Previous {get; set;}
        public int GScore {get; set;}
        public int FScore => GScore + HScore;
        public int HScore {get; set;}
        public Queue<SolutionStep> Reductions {get; set;}
        public Puzzle StateAfterReductions {get; set;}

        public SearchNode(Queue<SolutionStep> redux, Puzzle result) {
            this.Reductions = redux;
            this.StateAfterReductions = result;
        }

        // NEED THESE TO WORK WITH PRIORITYQUEUE AND HASHSET
        public override bool Equals(object? other) {
            return other switch {
                // All cells must have the same entered value
                SearchNode node => StateAfterReductions.Select(cell => cell.EnteredValue).SequenceEqual(node.StateAfterReductions.Select(cell => cell.EnteredValue)),
                _ => base.Equals(other)
            };
        }
        public override int GetHashCode() {
            // Count of all cells that are non-empty
            return StateAfterReductions.Where(cell => cell.EnteredValue.HasValue).Count();
        } 
    }

    /// <summary>
    /// Solve the current sudoku puzzle and return the steps required to reach the solution from the current state.
    /// It assumes any entered value is "correct". If this is not the case, please remove wrong values first if they are known.
    /// </summary>
    /// <param name="puzzle">puzzle to solve</param>
    /// <returns>solution steps</returns>
    public IEnumerable<SolutionStep> Solve(Puzzle puzzle) {
        var startNode = MakeNode(puzzle);
        return aStar(startNode, (puzzle) => puzzle.IsSolved());
    }

    protected SearchNode MakeNode(Puzzle start, SearchNode? cameFrom = null) {
        Puzzle reduced = start.DeepCopy();
        var reductions = Reduce(reduced);
        SearchNode node = new SearchNode(reductions, reduced) {
            Previous = cameFrom,
            GScore = cameFrom != null ? cameFrom.GScore + 1 : 0,
            HScore = Heuristic(start)
        };
        return node;
    }

    private IEnumerable<SolutionStep> recursive_reconstruct_path(SearchNode tail) {
        if (ReferenceEquals(tail, null)) {
            yield break;
        } else {
            if (!ReferenceEquals(tail.Previous, null)) {
                foreach (var step in recursive_reconstruct_path(tail.Previous)) {
                    yield return step;
                }
            }
            if (!ReferenceEquals(tail.StepTakenFromPrevious, null)) {
                yield return tail.StepTakenFromPrevious;
            }
            while (tail.Reductions.Count > 0) {
                yield return tail.Reductions.Dequeue();
            }
        }
    }

    // https://en.wikipedia.org/wiki/A*_search_algorithm
    private IEnumerable<SolutionStep> aStar(SearchNode start, Predicate<Puzzle> goal) {
        // If already done, be done
        if (goal(start.StateAfterReductions)) {
            return recursive_reconstruct_path(start);
        }

        // Init        
        var closedSet = new HashSet<SearchNode>();
        var openSet = new PriorityQueue<SearchNode, int>();
        openSet.Enqueue(start, start.FScore);

        // Search
        while (openSet.Count > 0) {
            // Get state with the lowest F-Score (smallest search depth + smallest distance to solution)
            var current = openSet.Dequeue();
            if (goal(current.StateAfterReductions)) {
                return recursive_reconstruct_path(current);
            }
            
            // Generate possible subsequent states ("random" guessing)
            var neighbours = GenerateNeighbours(current);
            foreach (var neighbour in neighbours) {
                // If this neighbour previously recorded
                SearchNode? duplicate;
                if (closedSet.TryGetValue(neighbour, out duplicate)) {
                    // There is a previous path to this state
                    if (neighbour.FScore < duplicate.FScore) {
                        // This path is better than the previous one, record it
                        closedSet.Add(neighbour); // HashSet will replace the old record
                        openSet.Enqueue(neighbour, neighbour.FScore);
                    } else {
                        // This path is not better, discard it
                        continue;
                    }
                } else {
                    // This is a new path
                    openSet.Enqueue(neighbour, neighbour.FScore);
                }
            }

            // Add this to the already explored state list
            closedSet.Add(current);
        }

        // No solution
        throw new Exception("No solution found for puzzle. Maybe there is a technique that I do not know.");
    }

    protected virtual int Heuristic(Puzzle puzzle) {
        // Number of cells that are empty is the "distance from the goal"
        return puzzle.Where(cell => !cell.EnteredValue.HasValue).Count();
    }


    private static List<Func<Puzzle, SolutionStep?>> reductions = new List<Func<Puzzle, SolutionStep?>>{
        // Check for "last possible value scenarios"
        LastInRow.TryReduce,
        LastInColumn.TryReduce,
        LastInBlock.TryReduce,

        // Check "basic" techniques
        Scanning.TryReduce,
        OnlyCandidate.TryReduce,
        // SingleElimination.TryReduce, // TODO

        // TODO more
    };
    protected virtual Queue<SolutionStep> Reduce(Puzzle puzzle) {
        Queue<SolutionStep> steps = new Queue<SolutionStep>();

        // Continue applying different reduction techniques until you can't anymore
        SolutionStep? found = null;
        do {
            found = null;
            foreach (var reducer in reductions) {
                found = LastInRow.TryReduce(puzzle) ?? found;
            }
            if (!ReferenceEquals(found, null)) { 
                steps.Enqueue(found); 
                continue; 
            }
        } while (found != null);

        // Return the reductions that have been applied, in order
        return steps;
    }

    protected abstract IEnumerable<SearchNode> GenerateNeighbours(SearchNode current);
}

/// <summary>
/// A* Solver that knows what the actual value of each cell is (from the puzzle game data itself)
/// </summary>
public class InsightfulAStarSolver : AStarSolver {
    protected override IEnumerable<SearchNode> GenerateNeighbours(SearchNode current) {
        // For any "open" cell, pick a valid value to place in the cell
        foreach (var cell in current.StateAfterReductions) {
            // If cell is already filled with a value AND the value is correct
            if (cell.EnteredValue.HasValue && cell.EnteredValue.Value == cell.ActualValue) {
                continue;
            }

            // Create a new node for this puzzle by setting it to it's correct value
            Puzzle next = current.StateAfterReductions.DeepCopy();
            var cellInNext = next.GetCell(cell.CellX, cell.CellY);
            var guess = cellInNext.ActualValue;
            cellInNext.EnteredValue = guess;

            // Create the node
            var node = MakeNode(next, current);
            node.StepTakenFromPrevious = new RandomGuess(cellInNext, guess);
            yield return node;
        }
    }
}

/// <summary>
/// A* Solver that makes no assumptions as to the value of each cell and works only off of entered data
/// </summary>
public class BlindAStarSolver : AStarSolver {
    protected override IEnumerable<SearchNode> GenerateNeighbours(SearchNode current) {
        // For any "open" cell, pick a valid value to place in the cell
        foreach (var cell in current.StateAfterReductions) {
            // If cell is already filled with a value 
            if (cell.EnteredValue.HasValue) {
                continue;
            }

            // Get a list of all numbers that this cell "COULD" be
            var numbers = new List<int>{ 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            foreach (var other in cell.Row) {
                if (other.EnteredValue.HasValue) {
                    numbers.Remove(other.EnteredValue.Value);
                }
            }
            foreach (var other in cell.Column) {
                if (other.EnteredValue.HasValue) {
                    numbers.Remove(other.EnteredValue.Value);
                }
            }
            foreach (var other in cell.Block) {
                if (other.EnteredValue.HasValue) {
                    numbers.Remove(other.EnteredValue.Value);
                }
            }
            
            foreach (var guess in numbers) {
                // Create a new node for this puzzle assuming the correct value was inserted
                Puzzle next = current.StateAfterReductions.DeepCopy();
                var cellInNext = next.GetCell(cell.CellX, cell.CellY);
                cellInNext.EnteredValue = guess;

                // Create the node
                var node = MakeNode(next, current);
                node.StepTakenFromPrevious = new RandomGuess(cellInNext, guess);
                yield return node;
            }
        }
    }
}