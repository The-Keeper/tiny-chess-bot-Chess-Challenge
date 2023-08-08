using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    public bool DEBUG = false; // #DEBUG
    public Random rng = new();

    public Timer timeControl;
    public int MillisecondsAllocatedForSearch = 1000; 
    public int maxDepth = 40;
    public bool ContinueSearch = true;
    public Move moveToPlay;

    public Move Think(Board board, Timer timer)
    {
        ContinueSearch = true;
        timeControl = timer;
        int depth = 2;
        while ( ContinueSearch || depth < maxDepth) {
            moveToPlay = Search(board, depth);
            depth += 2;
        }
        Console.WriteLine($"Final Depth: {depth}, Time it took: {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
        return moveToPlay;
    }

    Move Search(Board board, int depth) {  
        Move[] moves = board.GetLegalMoves();
        
        foreach (Move m in moves) {
            if (timeControl.MillisecondsElapsedThisTurn > MillisecondsAllocatedForSearch) {
                ContinueSearch = false;
                break;
            }
            board.MakeMove(m);
            System.Threading.Thread.Sleep(50);          // #DEBUG
            Search(board, depth - 1);
            board.UndoMove(m);

        }

        return moves[rng.Next(moves.Length)];
    }
}