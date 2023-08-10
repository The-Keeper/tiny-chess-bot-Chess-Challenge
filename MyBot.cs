using ChessChallenge.API;
using System;

using System.Diagnostics; // #DEBUG
public class MyBot : IChessBot {
    public bool DEBUG = false; // #DEBUG

    public Timer timeControl;
    public int MillisecondsAllocatedForSearch = 1000;
    public int maxDepth = 60;   // Should be removed in optimised version 
    public bool ContinueSearch;
    public Move moveToPlay;
    public int positionsSearched = 0;       // #DEBUG
    public int INF = 1000000000;
    int GamePly = 0;

    public Move Think(Board board, Timer timer) {
        ContinueSearch = true;
        timeControl = timer;
        GamePly = board.PlyCount;
        positionsSearched = 0;             // #DEBUG
        int depth = 0;
        while (ContinueSearch) {
            Search(board, depth, board.IsWhiteToMove ? 1 : -1);
            depth++;

            // Depth termination is to be removed in optimised version
            if (depth > maxDepth) {                
                ContinueSearch = false;
                depth -= 1;
            }
        }
        Console.WriteLine($"Depth reached: {depth}, Searched: {positionsSearched}, Time it took: {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
        return moveToPlay;
    }

    /// <summary>
    /// Recusive search function based on Negamax method.
    /// </summary>
    /// <param name="board">current state of the board.</param>
    /// <param name="depth">depth to search to.</param>
    /// <returns>Transient score of the given move.</returns>
    int Search(Board board, int depth, int color) {
        positionsSearched++;                                // #DEBUG

        // Move[] moves = board.GetLegalMoves();            // -7 tokens,  ~7.5% slower
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);

        if (depth == 0)
            return Evaluate(board) * (board.IsWhiteToMove ? 1 : -1);  // sign depends of the side to move ;
        
        int score = board.IsDraw() ? 0 : -INF;

        foreach (Move move in moves) {
            board.MakeMove(move);
            int childScore = -Search(board, depth - 1, -color);
            board.UndoMove(move);
            if (childScore>score) {
                score = childScore;
                if (board.PlyCount == GamePly)
                    moveToPlay = move;
            }
            if (!ContinueSearch) {
                break;
            }
        }

        if (timeControl.MillisecondsElapsedThisTurn > MillisecondsAllocatedForSearch) {
            ContinueSearch = false;
        }
        return score;
    }


    // Piece values in centipawns: null, pawn, knight, bishop, rook, queen, king
    readonly static int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    /// <summary>
    /// Simple evaluation funtion.
    /// Sums of values of pieces of the board.
    /// </summary>
    /// <param name="node">Current state of the board.</param>
    /// <returns>Approximated value of current position in centipawns.</returns>
    public static int Evaluate(Board node) {
        // Based on Claude Shannon evaluation function :

        // f(p) = 200(K-K')
        //        + 9(Q-Q')
        //        + 5(R-R')
        //        + 3(B-B' + N-N')
        //        + 1(P-P')
        //        - 0.5(D-D' + S-S' + I-I')
        //        + 0.1(M-M') + ...

        // KQRBNP = number of kings, queens, rooks, bishops, knights and pawns
        // D,S,I = doubled, blocked and isolated pawns
        // M = Mobility (the number of legal moves)

        int score = 0;
        PieceList[] pl = node.GetAllPieceLists(); // i<6 = white, %6 = piece type
        for (int i = 0; i < pl.Length; i++) {
            int isWhitePieceFactor = pl[i].IsWhitePieceList ? 1 : -1;
            int pieceTypeIndex = i % 6;
            score += pl[i].Count * pieceValues[pieceTypeIndex + 1] * isWhitePieceFactor;
        }

        return score;
    }

}