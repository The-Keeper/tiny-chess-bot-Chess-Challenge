using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot {
    public bool DEBUG = false; // #DEBUG

    Timer timeControl;
    Move bestMove;
    public int MillisecondsAllocatedForSearch = 100;

    public bool ContinueSearch;

    public int positionsSearched = 0;       // #DEBUG

    const int INF = 1000000000;
    int GamePly = 0;

    public int scanDepth = 0;
    public int maxDepth = 60;
    public int scanCapturesDepth = 0;

    public Move Think(Board board, Timer timer) {
        int perspective = board.IsWhiteToMove ? 1 : -1;
        ContinueSearch = true;
        timeControl = timer;

        Move[] allMoves = board.GetLegalMoves();

        int TotalNumberOfPieces = board.GetAllPieceLists()
            .Aggregate(0, (sum, next) => sum + next.Count());
        scanDepth = 2;

        //  searchIsToContinue = true;
        Move lastBestMove = allMoves[0];
        positionsSearched = 0;
        GamePly = board.PlyCount;

        if (DEBUG) // #DEBUG
            Console.WriteLine($"Ply: {GamePly}"); // #DEBUG
        while (ContinueSearch) {
            Search(board, scanDepth, -INF, INF, perspective);
            if (ContinueSearch) {
                lastBestMove = bestMove;
            }
            if (DEBUG) // #DEBUG
                Console.WriteLine($"Scan depth: {scanDepth}, Searched:, {positionsSearched}, best move: {bestMove}, time: {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
            scanDepth += 1;
        }

        return lastBestMove;
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

        return score * (node.IsWhiteToMove ? 1 : -1);

    }

    Dictionary<int, Dictionary<Move, int>> moveScoreByPly = new(); 
    public List<Move> OrderMoves(Board board, Move[] moves, int perspective) {
        return moves.OrderByDescending(m => {
            int PlyCount = board.PlyCount;
            if (moveScoreByPly.ContainsKey(PlyCount) && moveScoreByPly[PlyCount].ContainsKey(m)) {
                return moveScoreByPly[PlyCount][m] * perspective;
            }
            return (int)m.CapturePieceType;
        }).ToList();
    }


    int Quiesce(Board node, int alpha, int beta) {
        int stand_pat = Evaluate(node);
        if (stand_pat >= beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        List<Move> moves = node.GetLegalMoves(true).ToList();

        foreach (Move m in moves) {
            node.MakeMove(m);
            int score = -Quiesce(node, -beta, -alpha);
            node.UndoMove(m);

            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }
        return alpha;
    }

    int Search(Board node, int depth, int alpha, int beta, int color) {
        if (!moveScoreByPly.ContainsKey(node.PlyCount))
            moveScoreByPly.Add(node.PlyCount, new());

        if (timeControl.MillisecondsElapsedThisTurn > MillisecondsAllocatedForSearch) {
            ContinueSearch = false;
        }

        ulong zobrist = node.ZobristKey;
        int score = 0;

        positionsSearched++;                                // #DEBUG

        // Move[] legalMoves = node.GetLegalMoves();            // -7 tokens,  ~7.5% slower
        Span<Move> legalMoves = stackalloc Move[256];
        node.GetLegalMovesNonAlloc(ref legalMoves);

        List<Move> moves = OrderMoves(node, legalMoves.ToArray(), color);
        int movesCount = legalMoves.Length;

        if (movesCount == 0) {
            return node.IsInCheckmate() ? -INF : 0;
        }

        if (depth == 0) {
            //return Evaluate(node);
            return Quiesce(node, alpha, beta);
        }

        int index = 0;

        foreach (Move move in moves) {
            if (!ContinueSearch) {
                break;
            }

            node.MakeMove(move);
            index++;
            if (index == 1) {
                score = -Search(node, depth - 1, -beta, -alpha, -color);
            }
            else {
                score = -Search(node, depth - 1, -alpha - 1, -alpha, -color); // (* search with a null window *)
                if (alpha < score && score < beta) {
                    score = -Search(node, depth - 1, -beta, -score, -color); // (* if it failed high, do a full re-search *)
                }
            }
            node.UndoMove(move);

            // alpha = Math.Max(alpha, score);
            if (score > alpha) {
                alpha = score;
                moveScoreByPly[node.PlyCount][move] = score;
                if (node.PlyCount == GamePly)
                    bestMove = move;
            }

            if (alpha >= beta) {
                break; // (* beta cut-off *)
            }
        }

        return alpha;
    }

}