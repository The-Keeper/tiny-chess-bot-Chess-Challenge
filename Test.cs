using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8802

Test.Run(); // comment this line out to run the challenge program itself


public static class Test {
    public static void Run()
    {
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
}
