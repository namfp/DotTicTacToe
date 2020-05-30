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

        internal static bool IsFinished(Played[][] board, Played[] boardResult)
        {
            for (var i = 0; i < 9; i++)
            {
                if (boardResult[i] == Played.Empty)
                {
                    for (var j = 0; j < 9; j++)
                    {
                        if (board[i][j] == Played.Empty)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
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

            if (IsFinished(board, boardResult))
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
    
    public static class Monitoring
    {
        public static double ChildrenGenerationTime { get; set; }
        public static double HeuristicTime { get; set; }
        public static double AllocationTime { get; set; }
        public static int Children { get; set; }
        public static double ComputeBoardResultTime { get; set; }
        public static double RollOutTime { get; set; }
        public static double SetResultTime { get; set; }

        public static void Init()
        {
            ChildrenGenerationTime = 0;
            HeuristicTime = 0;
            AllocationTime = 0;
            Children = 0;
            ComputeBoardResultTime = 0;
            RollOutTime = 0;
            SetResultTime = 0;
        }
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
    
    
    
}