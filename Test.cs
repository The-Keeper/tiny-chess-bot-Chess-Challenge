using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


using System.IO;
using ChessChallenge.Application;
using System.Runtime.CompilerServices;
#pragma warning disable CS8802

//Test.Run(); // comment this line out to run the challenge program itself
Test.FauxPlay();

public class Test {
    public static void Run() {
        string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "src", "My Bot", "MyBot.cs");
        using System.IO.StreamReader reader = new(path);
        string txt = reader.ReadToEnd();
        (int numTokens, int debugCount) = ChessChallenge.Application.TokenCounter.CountTokens(txt);
        Console.WriteLine($"Bot Brain Capacity: {numTokens - debugCount}");

        MyBot bot = new();
        bot.DEBUG = true;
        bot.MillisecondsAllocatedForSearch = 5000;

        // Board board = Board.CreateBoardFromFEN("4k3/8/8/8/8/8/P3P3/4K3 w - - 0 1");                             // king and two pawns
        Board board = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");     // starting position
        Timer timer = new Timer(60000);
        Move move = bot.Think(board, timer);
        Console.WriteLine(move);

        timer = new Timer(60000);
        board.MakeMove(new Move("e2e4", board));
        move = bot.Think(board, timer);
        Console.WriteLine(move);

    }

    [Serializable]
    public struct HumanBotGameState {
       public int remainingMs;
       public int opponentRemainingMs;
       public int startingMs;
       public int incrementMs;
       public Dictionary<ulong, MyBot.BoardEval> TranspositionTable;

       public HumanBotGameState() {
            remainingMs = 3000;
            startingMs = 3000;
            opponentRemainingMs = 3000;
            incrementMs = 500;
            TranspositionTable = new();
       }
    }

    public static void FauxPlay() {
        Console.Write("Input FEN: ");
        string FEN = Console.ReadLine();  
        if (FEN == "") {
            FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        }
        Board board = Board.CreateBoardFromFEN(FEN);
        Console.Write("Is bot white? (y/n)");
        bool isBotWhite = Console.ReadLine().IndexOfAny("Yy".ToCharArray()) != -1;
        
        string moveString = "";
        do {
        if (isBotWhite == board.IsWhiteToMove)
            board = LoadMakeMoveAndSave(board);
        Console.WriteLine(board);
        Console.Write("Make a move: ");
        moveString = Console.ReadLine();  
        if (moveString == "") 
            break;
        Move move = new(moveString, board); 
        board.MakeMove(move);
        } while (moveString != "");
    }

    public static Board LoadMakeMoveAndSave(Board board, string filePath = "botstate.bin") {

        HumanBotGameState game = new();
        
        if (File.Exists(filePath)) {
            using (Stream stream = File.Open(filePath, FileMode.Open)) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                game = (HumanBotGameState)binaryFormatter.Deserialize(stream);
            }
        } 
        Timer timer = new(game.remainingMs,game.opponentRemainingMs,game.startingMs,game.incrementMs);

        MyBot bot = new();
        bot.TranspositionTable = game.TranspositionTable;
        Move move = bot.Think(board, timer);
        game.remainingMs = timer.MillisecondsRemaining + timer.IncrementMilliseconds;

        if (File.Exists(filePath)) {
            File.Copy(filePath, filePath + ".bak", true);
        }
        using (Stream stream = File.Open(filePath, FileMode.Create)) {
            var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            binaryFormatter.Serialize(stream, game);
        }
        Console.WriteLine($"{move}, Milliseconds remaining: {game.remainingMs}");
        board.MakeMove(move);
        return board;
    }
}