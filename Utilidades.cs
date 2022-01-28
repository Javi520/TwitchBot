using System;

public static class Utilidades
{
    public static void ConsoleHappyWrite(Tuple<string, ConsoleColor>[] tuple)
    {
        foreach (var item in tuple)
        {
            Console.ForegroundColor = item.Item2;
            Console.WriteLine(item.Item1);
        }
        Console.ResetColor();
    }
}
