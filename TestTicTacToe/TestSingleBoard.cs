using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TicTacToe.Net;

namespace TestTicTacToe
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }


        private List<Played[]> Combine(int n)
        {
            if (n == 1)
            {
                var first = new Played[9];
                first[0] = Played.Empty;
                var second = new Played[9];
                second[0] = Played.Opponent;
                var third = new Played[9];
                third[0] = Played.Self;
                return new List<Played[]>(){first, second, third};
            }
            else
            {
                var minusOne = Combine(n - 1);
                var newResult = new List<Played[]>();
                foreach (var previous in minusOne)
                {
                    var first = new Played[9];
                    Array.Copy(previous, 0, first, 0, 9);
                    first[n - 1] = Played.Empty;
                    newResult.Add(first);
                    var second = new Played[9];
                    Array.Copy(previous, 0, second, 0, 9);
                    second[n - 1] = Played.Opponent;
                    newResult.Add(second);
                    var third = new Played[9];
                    Array.Copy(previous, 0, third, 0, 9);
                    third[n - 1] = Played.Self;
                    newResult.Add(third);
                }
                
                return newResult;
            }
        }
        


        [Test]
        public void TestScoringEmpty()
        {
            var board = new Played[9][];
            for (var i = 0; i < 9; i++)
            {
                board[i] = new Played[9];
                for (var j = 0; j < 9; j++)
                {
                    board[i][j] = Played.Empty;
                }
            }
            
            var score = AlgoHeuristic.ComputeScore(board, AlgoHeuristic.GetBoardResult(board));
            Assert.AreEqual(score, 0);
        }
        private void Invert(Played[][] board)
        {
            for (var i = 0; i < 9; i++)
            {
                for (var j = 0; j < 9; j++)
                {
                    board[i][j] = board[i][j] switch
                    {
                        Played.Opponent => Played.Self,
                        Played.Self => Played.Opponent,
                        _ => board[i][j]
                    };
                }
            }
        }

        [Test]
        public void TestTwoWinningCount()
        {
            var board = new[]
            {
                Played.Self, Played.Empty, Played.Empty, 
                Played.Empty, Played.Self, Played.Empty, 
                Played.Empty, Played.Empty, Played.Empty
            };
            Assert.AreEqual(AlgoHeuristic.TwoWinningCount(board, Player.Self), 1);
            
            board = new[]
            {
                Played.Self, Played.Empty, Played.Empty, 
                Played.Empty, Played.Self, Played.Empty, 
                Played.Self, Played.Empty, Played.Empty
            };
            Assert.AreEqual(AlgoHeuristic.TwoWinningCount(board, Player.Self), 3);
            Assert.AreEqual(AlgoHeuristic.TwoWinningCount(board, Player.Opponent), 0);
        }

        [Test]
        public void Test1()
        {
            var first = new[]
            {
                Played.Self, Played.Empty, Played.Empty, 
                Played.Empty, Played.Self, Played.Empty, 
                Played.Empty, Played.Empty, Played.Self, 
            };
            var empty = new[]
            {
                Played.Empty, Played.Empty, Played.Empty,
                Played.Empty, Played.Empty, Played.Empty,
                Played.Empty, Played.Empty, Played.Empty,
            };
            var board = new[]
            {
                first, empty, empty,
                empty, empty, empty,
                empty, empty, empty,
            };
            var score = Constants.WinCorner + Constants.CenterSquareAnyBoard + Constants.SmallBoardWin;
            Assert.AreEqual(AlgoHeuristic.ComputeScore(board, AlgoHeuristic.GetBoardResult(board)), score);
            Invert(board);
            Assert.AreEqual(AlgoHeuristic.ComputeScore(board, AlgoHeuristic.GetBoardResult(board)), score * -1);
        }

        [Test]
        public void Test2()
        {
            var board = new[]
            {
                new[]
                {
                Played.Opponent, Played.Self, Played.Empty, 
                Played.Empty, Played.Empty, Played.Empty, 
                Played.Empty, Played.Empty, Played.Empty, 
                },
                
                new[]
                {
                    Played.Empty, Played.Self, Played.Empty, 
                    Played.Opponent, Played.Empty, Played.Empty, 
                    Played.Empty, Played.Empty, Played.Empty, 
                },
                new[]
                {
                    Played.Empty, Played.Self, Played.Empty, 
                    Played.Empty, Played.Empty, Played.Empty, 
                    Played.Empty, Played.Self, Played.Empty, 
                },
                new[]
                {
                    Played.Self, Played.Empty, Played.Empty, 
                    Played.Opponent, Played.Opponent, Played.Opponent, 
                    Played.Empty, Played.Self, Played.Empty, 
                },
                new[]
                {
                    Played.Self, Played.Opponent, Played.Empty,
                    Played.Self, Played.Empty, Played.Empty,
                    Played.Self, Played.Opponent, Played.Opponent,
                },
                new[]
                {
                    Played.Empty, Played.Empty, Played.Opponent,
                    Played.Empty, Played.Self, Played.Empty,
                    Played.Opponent, Played.Empty, Played.Empty,
                },
                new[]
                {
                    Played.Empty, Played.Empty, Played.Opponent,
                    Played.Opponent, Played.Empty, Played.Empty,
                    Played.Self, Played.Self, Played.Self,
                },
                new[]
                {
                    Played.Empty, Played.Empty, Played.Empty,
                    Played.Empty, Played.Empty, Played.Empty,
                    Played.Opponent, Played.Empty, Played.Empty,
                },
                new[]
                {
                    Played.Opponent, Played.Self, Played.Empty,
                    Played.Empty, Played.Empty, Played.Self,
                    Played.Empty, Played.Opponent, Played.Self,
                }
            };

            Assert.AreEqual(24, AlgoHeuristic.ComputeScore(board, AlgoHeuristic.GetBoardResult(board)));
            Invert(board);    
            Assert.AreEqual(-24,AlgoHeuristic.ComputeScore(board, AlgoHeuristic.GetBoardResult(board)));
        }

        [Test]
        public void GenerateNextStates()
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
            var gameState = new GameState(Player.Self, board, null!, empty);
            gameState.GenerateChildrenState(2);
            Assert.AreEqual(gameState.ChildStates.Count, 81);
            foreach (var gameStateChildState in gameState.ChildStates)
            {
                Assert.AreEqual(gameStateChildState.NextPlayer, Player.Opponent);
            }

            var childState = gameState.ChildStates.First();
            Assert.AreEqual(childState.ChildStates.Count, 8);
            Assert.AreEqual(gameState.ChildStates[1].ChildStates.Count, 9);
        }

        [Test]
        public void TestPlaying()
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
            var gameState = new GameState(Player.Self, board, null!, empty);
            var state = gameState.NextPlay(1);
            Console.WriteLine(Monitoring.HeuristicTime);
            Console.WriteLine(Monitoring.ChildrenGenerationTime);
            Console.WriteLine(state.LastPlayed);
        }
        
    }
}