#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

namespace TicTacToe.Net
{
    public class GameState
    {
        public Played[][] Board;
        public readonly GameState? Parent;
        public Player NextPlayer;
        public PlayPosition? LastPlayed;
        public Played[] BoardResult;
        public Result Result { get; set; }
        public int? Score { get; set; }
        public int NbVisited { get; set; } = 0;

        public List<GameState>? ChildStates { get; set; }

        public GameState(GameState parent, Player nextPlayer, Played[][] board, PlayPosition lastPlayed, Played[] boardResult)
        {
            Parent = parent;
            NextPlayer = nextPlayer;
            Board = board;
            LastPlayed = lastPlayed;
            BoardResult = boardResult;
            SetResult();
        }

        public Tuple<int, int> LastPlayedCoordinate()
        {
            return Coordinate.ToCoordinate(LastPlayed);
        }
        
        private void SetResult()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            if (AlgoHeuristic.IsWinOneBoard(BoardResult, Player.Opponent))
            {
                Result = Result.Opponent;
            }
            else if (AlgoHeuristic.IsWinOneBoard(BoardResult, Player.Self))
            {
                Result = Result.Self;
            }
            else if (AlgoHeuristic.IsFinished(Board, BoardResult))
            {
                Result = Result.Equal;
            }
            else
            {
                Result = Result.NotFinished;
            }
            stopWatch.Stop();
            Monitoring.SetResultTime += stopWatch.Elapsed.TotalSeconds;
        }


        public void GenerateChildrenState(int level)
        {
            if (level == 0 || Result != Result.NotFinished)
            {
                return;
            }

            ChildStates ??= NextGameStates().ToList();

            Monitoring.Children += ChildStates.Count;

            foreach (var childState in ChildStates)
            {
                childState.GenerateChildrenState(level - 1);
            }
        }


        public Player GetNextPlayer(Player currentPlayer)
        {
            return currentPlayer switch
            {
                Player.Self => Player.Opponent,
                Player.Opponent => Player.Self,
                _ => throw new ArgumentOutOfRangeException(nameof(currentPlayer), currentPlayer, null)
            };
        }

        private Played[][] CopyBoard()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var board = new Played[9][];
            for (var i = 0; i < 9; i++)
            {
                var smallBoard = new Played[9];
                for (var j = 0; j < 9; j++)
                {
                    smallBoard[j] = Board[i][j];
                }

                board[i] = smallBoard;
            }
            stopWatch.Stop();
            Monitoring.AllocationTime += stopWatch.Elapsed.TotalSeconds;

            return board;
        }

        private static bool SingleBoardFinished(Played[] board)
        {
            return board.All(c => c != Played.Empty);
        }


        private IEnumerable<GameState> NextGameStates()
        {
            var allPossibilities = NextPossibilities();
            return allPossibilities.Select(CreateGameState);
        }

        public GameState CreateGameState(PlayPosition played)
        {
            var nextBoard = CopyBoard();
            nextBoard[played.BoardNum][played.Pos] = NextPlayer switch
            {
                Player.Self => Played.Self,
                Player.Opponent => Played.Opponent,
                _ => throw new Exception("Next player invalid")
            };
            var boardResult = GetNextBoardResult(nextBoard, BoardResult, played);
            var nextGameState = new GameState(this, GetNextPlayer(NextPlayer), nextBoard,
                played, boardResult);
            return nextGameState;
        }

        public GameState CloneGameState()
        {
            var clonedBoard = CopyBoard();
            var clonedBoardResult = new Played[9];
            for (var i = 0; i < 9; i++)
            {
                clonedBoardResult[i] = BoardResult[i];
            }

            var clonedGameState = new GameState(null!, NextPlayer, clonedBoard, LastPlayed!, BoardResult);
            return clonedGameState;
        }

        public void SimulateMove(PlayPosition played)
        {
            LastPlayed = played;
            Board[played.BoardNum][played.Pos] = NextPlayer switch
            {
                Player.Opponent => Played.Opponent,
                Player.Self => Played.Self,
                _ => throw new Exception("Never happened")
            };
            NextPlayer = GetNextPlayer(NextPlayer);
            BoardResult = GetNextBoardResult(Board, BoardResult, played);
            SetResult();
            
        }
        

        public IEnumerable<PlayPosition> NextPossibilities()
        {
            var possibilities = new List<PlayPosition>();

            if (LastPlayed == null || BoardResult[LastPlayed.Pos] == Played.Opponent ||
                BoardResult[LastPlayed.Pos] == Played.Self || SingleBoardFinished(Board[LastPlayed.Pos]))
            {
                for (var i = 0; i < 9; i++)
                {
                    if (BoardResult[i] != Played.Empty) continue;
                    for (var j = 0; j < 9; j++)
                    {
                        if (Board[i][j] != Played.Empty) continue;
                        possibilities.Add(new PlayPosition(i, j));
                    }
                }
            }
            else
            {
                for (var j = 0; j < 9; j++)
                {
                    if (Board[LastPlayed.Pos][j] != Played.Empty) continue;
                    possibilities.Add(new PlayPosition(LastPlayed.Pos, j));
                }
            }
            return possibilities;
        }


        private Played[] GetNextBoardResult(Played[][] nextBoard, Played[] lastResult, PlayPosition? nextPlay)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = new Played[9];
            for (var boardPos = 0; boardPos < 9; boardPos++)
            {
                if (nextPlay != null && boardPos == nextPlay.BoardNum)
                {
                    var sBoard = nextBoard[boardPos];
                    if (AlgoHeuristic.IsWinOneBoard(sBoard, Player.Self))
                    {
                        result[boardPos] = Played.Self;
                    }
                    else if (AlgoHeuristic.IsWinOneBoard(sBoard, Player.Opponent))
                    {
                        result[boardPos] = Played.Opponent;
                    }
                    else
                    {
                        result[boardPos] = Played.Empty;
                    }
                }
                else
                {
                    result[boardPos] = lastResult[boardPos];
                }
            }
            stopWatch.Stop();
            Monitoring.ComputeBoardResultTime +=  stopWatch.Elapsed.TotalSeconds;
            return result;

        }

        public bool IsTerminated()
        {
            return (Result != Result.NotFinished);
        }

        public GameState Move(PlayPosition played)
        {
            if (ChildStates == null)
            {
                GenerateChildrenState(1);
            }

            return ChildStates?.Find(c => Equals(c.LastPlayed, played))
                   ?? throw new Exception("Cannot find child state");
        }

        public GameState Move(int row, int col)
        {
            var played = Coordinate.FromCoordinate(row, col);
            return Move(played);
        }
    }

    public static class MCTS
    {
        public static void BestMove(GameState gameState, int duration)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
           
            

            while (stopWatch.Elapsed.TotalMilliseconds <= duration)
            {
                var selected = Selection(gameState, gameState.NbVisited);
                if (selected == null)
                {
                    break;
                }
                stopWatch.Start();
                var rollOutWatch = new Stopwatch();
                rollOutWatch.Start();
                var score = RollOut(gameState);
                rollOutWatch.Stop();
                Monitoring.RollOutTime += rollOutWatch.Elapsed.TotalSeconds;
                BackPropagation(score, selected);
            }
        }

        public static double UCB(GameState gameState, int n0)
        {
            if (gameState.Score != null)
            {
                return ((double) gameState.Score.Value / gameState.NbVisited +
                        2 * Math.Sqrt(Math.Log(n0) / gameState.NbVisited));
            }

            throw new Exception("score cannot be null here");
        }


        public static void BackPropagation(int score, GameState leafGameState)
        {
            leafGameState.Score ??= 0;
            leafGameState.Score += score;
            leafGameState.NbVisited += 1;
            var parent = leafGameState.Parent;
            while (parent != null)
            {
                parent.Score ??= 0;
                parent.Score += score;
                parent.NbVisited += 1;
                parent = parent.Parent;
            }
        }
        

        public static int RollOut(GameState gameState)
        {
         
            var first = gameState.CloneGameState();
            while (true)
            {
                switch (first.Result)
                {
                    case Result.Self:
                        return 1;
                    case Result.Opponent:
                        return -1;
                    case Result.Equal:
                        return 0;
                    default:
                    {
                        var allPossibilites = first.NextPossibilities();
                        var random = new Random();
                        var playPositions = allPossibilites.ToList();
                        if (!playPositions.Any()) throw new Exception("There must be at least one next move");
                        var next = playPositions.ToList()[random.Next(playPositions.Count())];
                        first.SimulateMove(next);
                        break;
                    }
                        
                }
            }
            
        }

        private static GameState? Selection(GameState gameState, int n0)
        {
            GameState result = gameState;
            while (true)
            {
                if (result.IsTerminated()) return null;
                
                if (result.ChildStates == null)
                {
                    if (result.NbVisited == 0)
                    {
                        return result;
                    }

                    result.GenerateChildrenState(1);
                    return result.ChildStates.First();
                }

                var found = result.ChildStates.Find(c => c.NbVisited == 0);
                if (found != null)
                {
                    return found;
                }

                result = result.ChildStates.Select(c => Tuple.Create(c, UCB(c, n0)))
                    .Aggregate((agg, next) => next.Item2 > agg.Item2 ? next : agg).Item1;
            }
        }
    }


    public static class NegaMaxAlgo
    {
        private static int NegaMax(GameState gameState, int depth, int alpha, int beta, int color)
        {
            if (depth == 0 || gameState.IsTerminated())
            {
                if (gameState.Score != null) return gameState.Score.Value;
                var score = color * AlgoHeuristic.ComputeScore(gameState.Board, gameState.BoardResult);
                gameState.Score = score;
                return score;
            }

            if (gameState.ChildStates == null)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                gameState.GenerateChildrenState(1);
                stopWatch.Stop();
                Monitoring.ChildrenGenerationTime += stopWatch.Elapsed.TotalSeconds;
            }

            var value = int.MinValue;

            foreach (var childState in gameState.ChildStates)
            {
                value = Math.Max(value, -1 * NegaMax(childState, depth - 1, -beta, -alpha, -color));
                alpha = Math.Max(alpha, value);
                if (alpha >= beta) break;
            }

            gameState.Score = value;
            return value;
        }

        public static GameState NextPlay(GameState gameState, int depth)
        {
            Monitoring.Init();
            var score = NegaMax(gameState, depth, int.MinValue, int.MaxValue, 1);
            if (gameState.ChildStates == null) throw new Exception("Children cannot be null here");
            var child = gameState.ChildStates.Find(x => x.Score == -score);
            if (child == null) throw new Exception("Cannot find child node");
            return child;
        }
    }

    static class Program
    {
        private static void Main(string[] args)
        {
            string[] inputs;

            GameState gameState = null;
            var firstMove = true;

            // game loop
            while (true)
            {
                inputs = Console.ReadLine()?.Split(' ');
                var opponentRow = int.Parse(inputs[0]);
                var opponentCol = int.Parse(inputs[1]);
                var validActionCount = int.Parse(Console.ReadLine());
                for (int i = 0; i < validActionCount; i++)
                {
                    inputs = Console.ReadLine()?.Split(' ');
                    var row = int.Parse(inputs[0]);
                    var col = int.Parse(inputs[1]);
                }

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

                if (firstMove)
                {
                    var empty = new[]
                    {
                        Played.Empty, Played.Empty, Played.Empty,
                        Played.Empty, Played.Empty, Played.Empty,
                        Played.Empty, Played.Empty, Played.Empty,
                    };
                    var board = new[]
                    {
                        empty, empty, empty,
                        empty, empty, empty,
                        empty, empty, empty,
                    };


                    if (opponentRow == -1 && opponentCol == -1)
                    {
                        gameState = new GameState(null!, Player.Self, board, null!, empty);
                    }
                    else
                    {
                        gameState = new GameState(null!, Player.Opponent, board, null!, empty);
                        gameState = gameState.Move(Coordinate.FromCoordinate(opponentRow, opponentCol));
                    }

                    firstMove = false;
                }
                else
                {
                    gameState = gameState.Move(Coordinate.FromCoordinate(opponentRow, opponentCol));
                }
                
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                gameState = NegaMaxAlgo.NextPlay(gameState, 3);
                if (gameState.LastPlayed == null) throw new Exception("Last Played cannot be null");
                var played = gameState.LastPlayedCoordinate();
                stopWatch.Stop();
                
                Console.Error.WriteLine($"Heuristic Time {Monitoring.HeuristicTime}");
                Console.Error.WriteLine($"Generation time {Monitoring.ChildrenGenerationTime}");
                Console.Error.WriteLine($"Allocation time {Monitoring.AllocationTime}");
                Console.Error.WriteLine($"Children {Monitoring.Children}");
                Console.Error.WriteLine($"ComputeBoardResultTime {Monitoring.ComputeBoardResultTime}");
                Console.Error.WriteLine($"Total time {stopWatch.Elapsed.TotalSeconds}");
                Console.Error.WriteLine($"Score {gameState.Score}");
                Console.WriteLine($"{played.Item1} {played.Item2}");
            }
        }
    }
}