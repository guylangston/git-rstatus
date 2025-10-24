using System.Diagnostics;
using GitRStatus.TUI;

namespace GitRStatus;

#if DEBUG
public static class Benchmark
{
    public static int Run(string[] args)
    {
        DynamicConsoleRegion console = new();
        console.Init(20);
        var timer = new Stopwatch();
        timer.Start();
        var cc = 0;
        ConsoleColor[] clrs = [ConsoleColor.DarkBlue, ConsoleColor.DarkCyan, ConsoleColor.DarkGray, ConsoleColor.Cyan, ConsoleColor.Yellow,
                               ConsoleColor.Magenta, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.Red, ConsoleColor.White];
        for(cc=0; cc<4*60; cc++)
        {
            console.StartDraw(false);
            do
            {
                foreach(var c in clrs)
                {
                    console.ForegroundColor = c;
                    console.Write(c.ToString());
                    console.Write(" ");
                }

            } while (console.WriteLine(""));
            console.Revert();
        }
        timer.Stop();
        var fps = (float)cc / timer.Elapsed.TotalSeconds;
        Console.WriteLine($"{cc} frames in {timer} = {fps} fps");

        return 0;
    }
}
#endif
