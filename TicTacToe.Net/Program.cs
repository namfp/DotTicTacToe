#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TicTacToe.Net
{
    public enum Played : int
    {
        Self = 0,
        Opponent = 1,
        Empty = 2
    }

    public enum Result : int
    {
        Self = 0,
        Opponent = 1,
        Equal = 2,
        NotFinished = 3
    }

    public enum Player : int
    {
        Self = 0,
        Opponent = 1
    }

    public static class Constants
    {
        public const int Win = 10000;
        public const int Lose = -10000;
        public const int SmallBoardWin = 10;
        public const int WinCenter = 10;
        public const int WinCorner = 3;
        public const int CenterSquareAnyBoard = 1;
        public const int SquareInCenterBoard = 1;
        public const int TwoBoardWin = 20;
        public const int TwoSquareWin = 1;

        public static readonly int[][] WinPositions = new[]
        {
            new[] {0, 1, 2},
            new[] {3, 4, 5},
            new[] {6, 7, 8},
            new[] {0, 3, 6},
            new[] {1, 4, 7},
            new[] {2, 5, 8},
            new[] {0, 4, 8},
            new[] {2, 4, 6},
        };
    }


    public class PlayPosition
    {
        public readonly int BoardNum;
        public readonly int Pos;

        public PlayPosition(int boardNum, int pos)
        {
            BoardNum = boardNum;
            Pos = pos;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is PlayPosition other)) return false;
            return (other.BoardNum == BoardNum && other.Pos == Pos);
        }

        protected bool Equals(PlayPosition other)
        {
            return BoardNum == other.BoardNum && Pos == other.Pos;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BoardNum, Pos);
        }
    }

    public class GameState
    {
        private Played[][] _board;
        public readonly Player NextPlayer;
        public readonly PlayPosition? LastPlayed;
        private Played[] _boardResult;
        private Result _result;
        public int? Score { get; private set; }

        public List<GameState>? ChildStates { get; set; }

        public GameState(Player nextPlayer, Played[][] board, PlayPosition lastPlayed, Played[] boardResult)
        {
            NextPlayer = nextPlayer;
            _board = board;
            LastPlayed = lastPlayed;
            _boardResult = boardResult;
            SetResult();
        }

        public Tuple<int, int> LastPlayedCoordinate()
        {
            return Coordinate.ToCoordinate(LastPlayed);
        }
        
        private void SetResult()
        {
            if (AlgoHeuristic.IsWinOneBoard(_boardResult, Player.Opponent))
            {
                _result = Result.Opponent;
            }
            else if (AlgoHeuristic.IsWinOneBoard(_boardResult, Player.Self))
            {
                _result = Result.Self;
            }
            else if (AlgoHeuristic.IsFinished(_boardResult))
            {
                _result = Result.Equal;
            }
            else
            {
                _result = Result.NotFinished;
            }
        }


        public void GenerateChildrenState(int level)
        {
            if (level == 0 || _result != Result.NotFinished)
            {
                return;
            }

            ChildStates ??= NextGameStates();

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
                    smallBoard[j] = _board[i][j];
                }

                board[i] = smallBoard;
            }
            stopWatch.Stop();
            Monitoring.AllocationTime += stopWatch.Elapsed.TotalSeconds;

            return board;
        }

        private bool SingleBoardFinished(Played[] board)
        {
            return board.All(c => c != Played.Empty);
        }


        private List<GameState> NextGameStates()
        {
            var children = new List<GameState>();

            if (LastPlayed == null || _boardResult[LastPlayed.Pos] == Played.Opponent ||
                _boardResult[LastPlayed.Pos] == Played.Self || SingleBoardFinished(_board[LastPlayed.Pos]))
            {
                for (var i = 0; i < 9; i++)
                {
                    if (_boardResult[i] != Played.Empty) continue;
                    children.AddRange(GenerateOne(i));
                }
            }
            else
            {
                children.AddRange(GenerateOne(LastPlayed.Pos));
            }

            return children;
        }

        private IEnumerable<GameState> GenerateOne(int i)
        {
            List<GameState> children = new List<GameState>();
            for (int j = 0; j < 9; j++)
            {
                if (_board[i][j] != Played.Empty) continue;
                var nextBoard = CopyBoard();
                nextBoard[i][j] = NextPlayer switch
                {
                    Player.Self => Played.Self,
                    Player.Opponent => Played.Opponent,
                    _ => throw new Exception("Next player invalid")
                };

                var boardResult = GetNextBoardResult(nextBoard, _boardResult, new PlayPosition(i, j));
                var test = AlgoHeuristic.GetBoardResult(nextBoard);
                for (var x = 0; x < 9; x++)
                { 
                    Debug.Assert(boardResult[x] == test[x]);
                }
                var nextGameState = new GameState(GetNextPlayer(NextPlayer), nextBoard,
                    new PlayPosition(i, j), boardResult);
                children.Add(nextGameState);
            }

            return children;
        }

        private Played[] GetNextBoardResult(Played[][] nextBoard, Played[] lastResult, PlayPosition? nextPlay)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = new Played[9];
            for (var boardPos = 0; boardPos < 9; boardPos++)
            {
                if (nextPlay != null && boardPos == nextPlay.Pos)
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

        private bool IsTerminated()
        {
            return (_result != Result.NotFinished);
        }

        private int NegaMax(int depth, int alpha, int beta, int color)
        {
            if (depth == 0 || IsTerminated())
            {
                if (Score != null) return Score.Value;
                var score = color * AlgoHeuristic.ComputeScore(_board, _boardResult);
                Score = score;
                return score;
            }

            if (ChildStates == null)
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                GenerateChildrenState(1);
                stopWatch.Stop();
                Monitoring.ChildrenGenerationTime += stopWatch.Elapsed.TotalSeconds;
            }

            var value = int.MinValue;

            foreach (var childState in ChildStates)
            {
                value = Math.Max(value, -1 * childState.NegaMax(depth - 1, -beta, -alpha, -color));
                alpha = Math.Max(alpha, value);
                if (alpha >= beta) break;
            }

            Score = value;
            return value;
        }

        public GameState NextPlay(int depth)
        {
            Monitoring.Init();
            var score = NegaMax(depth, int.MinValue, int.MaxValue, 1);
            if (ChildStates == null) throw new Exception("Children cannot be null here");
            var child = ChildStates.Find(x => x.Score == -score);
            if (child == null) throw new Exception("Cannot find child node");
            return child;
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

    public static class Monitoring
    {
        public static double ChildrenGenerationTime { get; set; }
        public static double HeuristicTime { get; set; }
        public static double AllocationTime { get; set; }
        public static int Children { get; set; }
        public static double ComputeBoardResultTime { get; set; }

        public static void Init()
        {
            ChildrenGenerationTime = 0;
            HeuristicTime = 0;
            AllocationTime = 0;
            Children = 0;
            ComputeBoardResultTime = 0;
        }
    }


    public static class AlgoHeuristic
    {
        private class BoardEquality : EqualityComparer<Played[]>
        {
            public override bool Equals(Played[] x, Played[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }

                return !x.Where((t, i) => t != y[i]).Any();
            }

            public override int GetHashCode(Played[] obj)
            {
                var result = 0;
                var shift = 0;
                foreach (var t in obj)
                {
                    shift = (shift + 11) % 21;
                    result ^= (((int) (t)) + 1024) << shift;
                }

                return result;
            }
        }

        private static Dictionary<Played[], int> _memoResult = new Dictionary<Played[], int>(new BoardEquality());
        private static Dictionary<Played[], int> _memoBoard = new Dictionary<Played[], int>(new BoardEquality());


        #region Scoring

        internal static bool IsFinished(Played[] board)
        {
            return board.All(c => c != Played.Empty);
        }

        public static bool IsWinOneBoard(Played[] board, Player player)
        {
            var played = (Played) player;
            var result = Constants.WinPositions.Any(positions =>
                board[positions[0]] == played &&
                board[positions[1]] == played &&
                board[positions[2]] == played);
            return result;
        }

        public static Played[] GetBoardResult(Played[][] board)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = new Played[9];
            for (var boardPos = 0; boardPos < 9; boardPos++)
            {
                var sBoard = board[boardPos];
                if (IsWinOneBoard(sBoard, Player.Self))
                {
                    result[boardPos] = Played.Self;
                }
                else if (IsWinOneBoard(sBoard, Player.Opponent))
                {
                    result[boardPos] = Played.Opponent;
                }
                else
                {
                    result[boardPos] = Played.Empty;
                }
            }
            stopWatch.Stop();
            Monitoring.ComputeBoardResultTime +=  stopWatch.Elapsed.TotalSeconds;
            return result;
        }

        private static int WinSmallBoardScore(int pos)
        {
            var score = Constants.SmallBoardWin;
            switch (pos)
            {
                case 4:
                    score += Constants.WinCenter;
                    break;
                case 0:
                case 2:
                case 6:
                case 8:
                    score += Constants.WinCorner;
                    break;
            }

            return score;
        }


        private static int GetBoardScore(Played[] result, Player player)
        {
            var score = 0;
            for (var i = 0; i < 9; i++)
            {
                var r = result[i];
                if ((int) r == (int) player)
                {
                    score += WinSmallBoardScore(i);
                }
            }


            score += TwoWinningCount(result, player) * Constants.TwoBoardWin;
            return score;
        }

        private static int ComputeOneBoardScore(Played[] board, Player player)
        {
            var played = (Played) player;
            var score = 0;
            if (board[4] == played)
            {
                score += Constants.CenterSquareAnyBoard;
            }

            score += TwoWinningCount(board, player) * Constants.TwoSquareWin;
            return score;
        }

        public static int TwoWinningCount(Played[] board, Player player)
        {
            var count = 0;
            foreach (var winPosition in Constants.WinPositions)
            {
                var sum = 0;
                var empty = 0;
                foreach (var pos in winPosition)
                {
                    if (board[pos] == (Played) player)
                    {
                        sum += 1;
                    }
                    else if (board[pos] == Played.Empty)
                    {
                        empty += 1;
                    }
                }

                if (sum == 2 && empty == 1)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static int ComputeSquareInCenterBoard(Played[] board, Player player)
        {
            var played = (Played) player;
            return board.Where(cell => cell == played).Sum(cell => Constants.SquareInCenterBoard);
        }

        private static int Score(Played[][] board, Played[] boardResult)
        {
            if (!_memoResult.TryGetValue(boardResult, out var boardScore))
            {
                boardScore = GetBoardScore(boardResult, Player.Self) - GetBoardScore(boardResult, Player.Opponent);
                _memoResult[boardResult] = boardScore;
            }

            var allSmallBoardScore = board.Sum(b =>
            {
                if (_memoBoard.TryGetValue(b, out var smallBoardScore)) return smallBoardScore;
                smallBoardScore = ComputeOneBoardScore(b, Player.Self) - ComputeOneBoardScore(b, Player.Opponent);
                _memoBoard[b] = smallBoardScore;
                return smallBoardScore;
            });
            var squareInCenterBoardScore = ComputeSquareInCenterBoard(board[4], Player.Self) -
                                           ComputeSquareInCenterBoard(board[4], Player.Opponent);
            return boardScore + allSmallBoardScore + squareInCenterBoardScore;
        }


        public static int ComputeScore(Played[][] board, Played[] boardResult)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            if (IsWinOneBoard(boardResult, Player.Opponent))
            {
                return Constants.Lose;
            }

            if (IsWinOneBoard(boardResult, Player.Self))
            {
                return Constants.Win;
            }

            if (IsFinished(boardResult))
            {
                return 0;
            }

            var score = Score(board, boardResult);
            stopWatch.Stop();
            Monitoring.HeuristicTime += stopWatch.Elapsed.TotalSeconds;
            return score;
        }

        #endregion
    }

    public static class Coordinate
    {
        public static PlayPosition FromCoordinate(int row, int col)
        {
            var boardi = col / 3;
            var boardj = row / 3;
            var boardCoordinate = boardj * 3 + boardi;
            var cellCoordinate = (row % 3) * 3 + col % 3;
            return new PlayPosition(boardCoordinate, cellCoordinate);
        }

        public static Tuple<int, int> ToCoordinate(PlayPosition? played) // row col
        {
            var boardX = played.BoardNum % 3;
            var boardY = played.BoardNum / 3;
            var i = played.Pos % 3;
            var j = played.Pos / 3;
            return new Tuple<int, int>(boardY * 3 + j, boardX * 3 + i);
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
                        gameState = new GameState(Player.Self, board, null!, empty);
                    }
                    else
                    {
                        gameState = new GameState(Player.Opponent, board, null!, empty);
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
                gameState = gameState.NextPlay(3);
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