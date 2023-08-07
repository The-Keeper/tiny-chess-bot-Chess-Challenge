using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    public bool DEBUG = false; // #DEBUG
    public Random rng = new();
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        return moves[rng.Next(moves.Length)];
    }
}