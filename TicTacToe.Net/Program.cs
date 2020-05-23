#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe.Net
{
    public enum Played : byte
    {
        Self = 0,
        Opponent = 1,
        Empty = 2
    }

    public enum Result : byte
    {
        Self = 0,
        Opponent = 1,
        Equal = 2,
        NotFinished = 3
    }

    public enum Player : byte
    {
        Self = 0,
        Opponent = 1
    }

    public class ScoreCounter
    {
        public int SmallBoardWin { get; set; } = 0;
        public int WinCenter { get; set; } = 0;
        public int WinCorner { get; set; } = 0;
        public int CenterSquareAnyBoard { get; set; } = 0;
        public int SquareInCenterBoard { get; set; } = 0;
        public int TwoBoardWin { get; set; } = 0;
        public int TwoSquareWin { get; set; } = 0;
    }

    public static class Constants
    {
        public const int Win = 10000;
        public const int Lose = -10000;
        public const int SmallBoardWin = 5;
        public const int WinCenter = 10;
        public const int WinCorner = 3;
        public const int CenterSquareAnyBoard = 3;
        public const int SquareInCenterBoard = 3;
        public const int TwoBoardWin = 4;
        public const int TwoSquareWin = 2;

        public static byte[][] WinPositions = new[]
        {
            new byte[] {0, 1, 2},
            new byte[] {3, 4, 5},
            new byte[] {6, 7, 8},
            new byte[] {0, 3, 6},
            new byte[] {1, 4, 7},
            new byte[] {2, 5, 8},
            new byte[] {0, 4, 8},
            new byte[] {2, 4, 6},
        };
    }


    public class PlayPosition
    {
        public readonly byte BoardNum;
        public readonly byte Pos;

        public PlayPosition(byte boardNum, byte pos)
        {
            BoardNum = boardNum;
            Pos = pos;
        }
    }

    internal class GameState
    {
        private Played[][] _board;
        private Player _nextPlayer;
        private PlayPosition? _playPositiion;


        public List<GameState> ChildStates { get; } = new List<GameState>();

        public GameState(Player nextPlayer, Played[][] board, PlayPosition playPositiion)
        {
            _nextPlayer = nextPlayer;
            _board = board;
            _playPositiion = playPositiion;
        }
    }

    public static class AlgoHeuristic
    {
        public static int GetHashCodeOne<T>(T[] values, Func<T, int> toInt)
        {
            var result = 0;
            var shift = 0;
            foreach (var t in values)
            {
                shift = (shift + 11) % 21;
                result ^= ((toInt(t)) + 1024) << shift;
            }

            return result;
        }

        public static int GetHashCode(Played[][] board)
        {
            return GetHashCodeOne(board, p => GetHashCodeOne(p, x => (int) x));
        }


        #region Scoring

        private static bool IsFinished(Played[] board)
        {
            return board.All(c => c != Played.Empty);
        }

        private static bool IsWinOneBoard(Played[] board, Player player)
        {
            var played = (Played) player;
            return Constants.WinPositions.Any(positions =>
                board[positions[0]] == played &&
                board[positions[1]] == played &&
                board[positions[2]] == played);
        }

        private static Played[] GetBoardResult(Played[][] board)
        {
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

            return result;
        }

        private static void WinSmallBoardScore(int pos, ScoreCounter counter)
        {
            counter.SmallBoardWin += 1;
            switch (pos)
            {
                case 4:
                    counter.WinCenter += 1;
                    break;
                case 0:
                case 2:
                case 6:
                case 8:
                    counter.WinCorner += 1;
                    break;
            }
        }


        private static void GetBoardScore(Played[] result, Player player, ScoreCounter counter)
        {
            for (var i = 0; i < 9; i++)
            {
                var r = result[i];
                if ((byte) r == (byte) player)
                {
                    WinSmallBoardScore(i, counter);
                }
            }


            counter.TwoBoardWin += TwoWinningCount(result, player);
        }

        private static void ComputeOneBoardScore(Played[] board, int boardPos, Player player, ScoreCounter counter)
        {
            var played = (Played) player;
            if (board[4] == played)
            {
                counter.CenterSquareAnyBoard += 1;
            }

            if (boardPos == 4)
            {
                foreach (var cell in board)
                {
                    if (cell == played)
                    {
                        counter.SquareInCenterBoard += 1;
                    }
                }
            }

            counter.TwoSquareWin += TwoWinningCount(board, player);
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

        private static ScoreCounter CountOnePlayer(Played[][] board, Player player, Played[] boardResult)
        {
            var counter = new ScoreCounter();
            GetBoardScore(boardResult, player, counter);

            for (var i = 0; i < 9; i++)
            {
                ComputeOneBoardScore(board[i], i, player, counter);
            }

            return counter;
        }

        private static int ComputeScoreOnePlayer(ScoreCounter counter)
        {
            return counter.WinCenter * Constants.WinCenter + counter.WinCorner * Constants.WinCorner +
                   counter.SmallBoardWin * Constants.SmallBoardWin + counter.TwoBoardWin * Constants.TwoBoardWin +
                   counter.TwoSquareWin * Constants.TwoSquareWin +
                   counter.CenterSquareAnyBoard * Constants.CenterSquareAnyBoard +
                   counter.SquareInCenterBoard * Constants.SquareInCenterBoard;
        }

        public static int ComputeScore(Played[][] board)
        {
            var boardResult = GetBoardResult(board);
            if (IsWinOneBoard(boardResult, Player.Opponent))
            {
                return -10000;
            }

            if (IsWinOneBoard(boardResult, Player.Self))
            {
                return 10000;
            }
            if (IsFinished(boardResult))
            {
                return 0;
            }
            var selfScore = CountOnePlayer(board, Player.Self, boardResult);
            var opponentScore = CountOnePlayer(board, Player.Opponent, boardResult);
            return ComputeScoreOnePlayer(selfScore) - ComputeScoreOnePlayer(opponentScore);
        }

        #endregion
    }

    static class Program
    {
        private static void Main(string[] args)
        {
            string[] inputs;

            // game loop
            while (true)
            {
                inputs = Console.ReadLine()?.Split(' ');
                int opponentRow = int.Parse(inputs[0]);
                int opponentCol = int.Parse(inputs[1]);
                int validActionCount = int.Parse(Console.ReadLine());
                for (int i = 0; i < validActionCount; i++)
                {
                    inputs = Console.ReadLine()?.Split(' ');
                    var row = int.Parse(inputs[0]);
                    var col = int.Parse(inputs[1]);
                }

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

                Console.WriteLine("0 0");
            }
        }
    }
}