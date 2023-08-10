using ChessChallenge.API;
using System;

using System.Diagnostics; // #DEBUG
public class MyBot : IChessBot
{
    public bool DEBUG = false; // #DEBUG

    public Timer timeControl;
    public int MillisecondsAllocatedForSearch = 2000;
    public int finalPly = 2;
    public bool ContinueSearch;
    public Move moveToPlay;
    public int positionsSearched = 0;

    public Move Think(Board board, Timer timer)
    {
        ContinueSearch = true;
        timeControl = timer;
        positionsSearched = 0;
        while ( ContinueSearch ) {
            int score = Search(board, finalPly);
            finalPly += 1;
        }
        Console.WriteLine($"Final Ply: {finalPly}, Searched: {positionsSearched}, Time it took: {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
        return moveToPlay;
    }

    int Search(Board board, int finalPly) { 
        positionsSearched ++;

        // Move[] moves = board.GetLegalMoves();            // -7 tokens,  ~7.5% slower
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        
        

        int score = board.PlyCount;
        if (board.PlyCount == finalPly) {
            return score;
        }

        foreach (Move m in moves) {
            board.MakeMove(m);
            score = Search(board, finalPly);
            board.UndoMove(m);
            if (!ContinueSearch) {
                break;
            }
        }

        if (timeControl.MillisecondsElapsedThisTurn>MillisecondsAllocatedForSearch) {
            ContinueSearch = false;
        } 
        return score;
    }
}