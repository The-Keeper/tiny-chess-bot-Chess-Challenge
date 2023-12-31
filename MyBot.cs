﻿using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot {
    public bool DEBUG = false; // #DEBUG

    Timer timeControl;
    Move bestMove;
    public int MillisecondsAllocatedForSearch = 600;

    public bool ContinueSearch;

    public int positionsSearched = 0;       // #DEBUG

    const int INF = 1000000000;
    int GamePly = 0;

    public int scanDepth = 0;
    public int maxDepth = 60;
    public int scanCapturesDepth = 0;

    bool isEndGame = false;

    public Move Think(Board board, Timer timer) {
        int perspective = board.IsWhiteToMove ? 1 : -1;
        ContinueSearch = true;
        timeControl = timer;

        Move[] allMoves = board.GetLegalMoves();

        MillisecondsAllocatedForSearch = timer.MillisecondsRemaining <  timer.GameStartTimeMilliseconds/3 ? timer.MillisecondsRemaining/20 : timer.MillisecondsRemaining/40+timer.IncrementMilliseconds;

        scanDepth = 2;

        //  searchIsToContinue = true;
        Move previousLastBestMove = Move.NullMove;
        int confidenceOfSearch = 0;
        Move lastBestMove = bestMove = allMoves[0];
        positionsSearched = 0;
        GamePly = board.PlyCount;

        var pl = board.GetAllPieceLists();
        if (board.IsWhiteToMove) {

        }
        isEndGame = pl[4].Count == 0 && pl[10].Count == 0 
            && board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove).Any(p => board.IsWhiteToMove ? p.Square.Rank < 4 : p.Square.Rank > 3); // or if there are dangerous pawns
        

        if (DEBUG) // #DEBUG
            Console.WriteLine($"Ply: {GamePly}, Board:\n{board}, Timer: {timer.MillisecondsRemaining}"); // #DEBUG
        while (ContinueSearch) {
            Search(board, scanDepth, -INF, INF, perspective);
            if (ContinueSearch) {
                lastBestMove = bestMove;
                if (previousLastBestMove == lastBestMove) {
                    confidenceOfSearch ++;
                    if (confidenceOfSearch == 3) {
                        ContinueSearch = false;
                    }
                } else {
                    previousLastBestMove = lastBestMove;
                    confidenceOfSearch = 0;
                }
            }
            if (DEBUG) // #DEBUG
                Console.WriteLine($"Scan depth: {scanDepth}, Searched:, {positionsSearched}, best move: {bestMove}, Transposition Table Length: {TranspositionTable.Count}, time: {timer.MillisecondsElapsedThisTurn}"); // #DEBUG
            scanDepth += 1;
        }
        if (DEBUG) // #DEBUG
            Console.WriteLine($"Moves: {string.Join(", ", TranspositionTable[board.ZobristKey].moves.OrderBy(m => m.Value).Select(m => $"{m.Key}: {m.Value}"))}"); // #DEBUG

        return lastBestMove;
    }

    readonly static int[][][] pieceScores = new int[6 * 32]
    { // pawn      
 0,  0,  0,  0,
50, 50, 50, 50,
10, 10, 20, 30,
 5,  5, 10, 25,
 0,  0,  0, 20,
 5, -5,-10,  0,
 5, 10, 10,-20,
 0,  0,  0,  0,
//},{
 // knight      
-50,-40,-30,-30,
-40,-20,  0,  0,
-30,  0, 10, 15,
-30,  5, 15, 20,
-30,  0, 15, 20,
-30,  5, 10, 15,
-40,-20,  0,  5,
-50,-40,-30,-30,
//},{
 // bishop 
-20,-10,-10,-10,
-10,  0,  0,  0,
-10,  0,  5, 10,
-10,  5,  5, 10,
-10,  0, 10, 10,
-10, 10, 10, 10,
-10,  5,  0,  0,
-20,-10,-10,-10,
//},{

 // rook
  0,  0,  0,  0,
  5, 10, 10, 10,
 -5,  0,  0,  0,
 -5,  0,  0,  0,
 -5,  0,  0,  0,
 -5,  0,  0,  0,
 -5,  0,  0,  0,
  0,  0,  0,  5,
        //}
//queen
-20,-10,-10, -5,
-10,  0,  0,  0,
-10,  0,  5,  5,
 -5,  0,  5,  5,
  0,  0,  5,  5,
-10,  5,  5,  5,
-10,  0,  5,  0,
-20,-10,-10, -5,

//king middle game
-30,-40,-40,-50,
-30,-40,-40,-50,
-30,-40,-40,-50,
-30,-40,-40,-50,
-20,-30,-30,-40,
-10,-20,-20,-20,
 20, 20,  0,  0,
 20, 30, 10,  0,
    }.Chunk(4)
        .Select(a => a.Concat(a.Reverse()).ToArray()).ToArray() // concat
        .Chunk(8)    //Split to pieces
        .Select(a => new int[][] {
        a.Select(b => b.Select(c => -c)).SelectMany(a => a).ToArray(), // reverse values for black
        a.Reverse().SelectMany(a => a).ToArray()})                              // turn the board for white
        .ToArray();

    // Piece values in centipawns: null, pawn, knight, bishop, rook, queen, king
    readonly static int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    /// <summary>
    /// Simple evaluation funtion.
    /// Sums of values of pieces of the board.
    /// </summary>
    /// <param name="node">Current state of the board.</param>
    /// <returns>Approximated value of current position in centipawns.</returns>
    public int Evaluate(Board node) {
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

            foreach (Piece piece in pl[i])
            {
                int sideIdx = piece.IsWhite ? 1 : 0;
                if (isEndGame && piece.IsKing) {
                    score += 40-30*(Math.Abs(piece.Square.Rank - (3+sideIdx)) + Math.Abs(piece.Square.File - (3+sideIdx)));
                    continue;
                }
                score += pieceScores[pieceTypeIndex][sideIdx][piece.Square.Index];
            }
        }

        return score * (node.IsWhiteToMove ? 1 : -1);

    }

        [Serializable]              // #DEBUG
       public struct BoardEval {
           public Dictionary<Move, int> moves; // moves and their scores;
        }
       public Dictionary<ulong, BoardEval> TranspositionTable = new();
        
        public List<Move> OrderMoves(Board board, Move[] moves, int perspective) {
            ulong zobrist = board.ZobristKey;
            return moves.OrderByDescending(m =>
               (TranspositionTable.ContainsKey(zobrist) && TranspositionTable[zobrist].moves.ContainsKey(m)) ? TranspositionTable[zobrist].moves[m] + 8192 : (int)m.CapturePieceType + (int)m.PromotionPieceType
            ).ToList();
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
        if (timeControl.MillisecondsElapsedThisTurn > MillisecondsAllocatedForSearch) {
            ContinueSearch = false;
        }

        ulong zobrist = node.ZobristKey;
        int score = -INF;

        positionsSearched++;                                // #DEBUG

        // Move[] legalMoves = node.GetLegalMoves();            // -7 tokens,  ~7.5% slower
        Span<Move> legalMoves = stackalloc Move[256];
        node.GetLegalMovesNonAlloc(ref legalMoves);

        List<Move> moves = OrderMoves(node, legalMoves.ToArray(), color);

        if (node.IsInCheckmate())
            return -INF;
            
        if (node.IsDraw())
            return 0;

        if (depth == 0) {
            //return Evaluate(node);
            return Quiesce(node, alpha, beta);
        }

        if (!TranspositionTable.ContainsKey(zobrist))
            TranspositionTable[zobrist] = new() {moves = new()};

        int index = 0;

        foreach (Move move in moves) {
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
                TranspositionTable[zobrist].moves[move] = score;
                if (node.PlyCount == GamePly)
                    bestMove = move;
            }

            if (!ContinueSearch || alpha >= beta) {
                break; // (* beta cut-off *)
            }
        }

        return alpha;
    }

}