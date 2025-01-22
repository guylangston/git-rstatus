using System.Diagnostics;

public static class Program
{
    static ConsoleColor StartFg;
    static Stopwatch timer = new();
    static string globalStatus = "";

    public static async Task<int> Main(string[] args)
    {
        StartFg = Console.ForegroundColor;

        timer.Start();
        var lineStart = Console.GetCursorPosition();
        globalStatus = "Started";
        var comp = new GitStatusComponent();

        var process = Process(comp, args.First());
        var frameRate = TimeSpan.FromSeconds(1 / 30f);
        while(!process.IsCompleted)
        {
            Render(comp, lineStart);
            await Task.Delay(frameRate);
        }

        if (process.IsFaulted)
        {
            Console.WriteLine(process.Exception);
            return 1;
        }

        Render(comp, lineStart);
        timer.Stop();

        if (HasFlag("-s"))
        {
            foreach(var x in comp.Roots)
            {
                Console.WriteLine($"{x.PathRelative,30} | {x.Status} | {x.LogFirstLine}");
            }
        }

        return 0;

        bool HasFlag(string flag) => args.Length > 0 && args.Contains(flag);
    }

    private static async Task Process(GitStatusComponent comp, string root)
    {
        try
        {
            globalStatus = "Scanning";
            await comp.Scan(root, 8);

            globalStatus = "Processing";
            await Task.Run(() =>
            {
                var buckets = GeneralHelper.CollectInBuckets(comp.Roots, comp.Roots.Count / 4);
                Task.WaitAll(buckets.Select(x=>ProcessBucket(x)));
            });

            globalStatus = "Completed";
        }
        catch (Exception)
        {
            globalStatus = "Error";
            throw;
        }

        async Task ProcessBucket(GitRoot[] bucket)
        {
            foreach(var dir in bucket)
            {
                await dir.Process();
            }
        }
    }

    static Dictionary<ItemStatus, ConsoleColor> Colors = new()
    {
        {ItemStatus.Discovered, ConsoleColor.Gray},
        {ItemStatus.Checking,   ConsoleColor.DarkCyan},
        {ItemStatus.Ignored,    ConsoleColor.DarkGray},
        {ItemStatus.Clean,      ConsoleColor.DarkGreen},
        {ItemStatus.Dirty,      ConsoleColor.DarkYellow},
        {ItemStatus.Behind,     ConsoleColor.Cyan},
    };

    /// <summary>Render to the console</summary>
    /// - General idea to render all items dynamically if there is space
    /// - If there is no space. Show progress, then output results
    private static void Render(GitStatusComponent comp, (int Left, int Top) lineStart)
    {
        Console.SetCursorPosition(0, lineStart.Top);
        var takeMax = Math.Min(Console.WindowHeight - lineStart.Top - 2, comp.Roots.Count);

        var part1Size = Console.WindowWidth / 2;
        foreach(var item in comp.Roots.OrderBy(x=>x.Path).Take(takeMax))
        {
            var path = "./" + item.PathRelative;
            var part1 = StringHelper.ElipseAtStart(path, part1Size, "__").PadLeft(part1Size);
            var part2 =  item.StatusLine();
            Console.Write(part1);
            Console.Write(" ");
            if (item.Status == ItemStatus.Discovered)
            {
                Console.WriteLine("Pending...");
                continue;
            }
            Console.ForegroundColor = Colors[item.Status];
            Console.Write(item.Status.ToString().PadRight(7));
            Console.ForegroundColor = StartFg;
            Console.Write(" ");
            Console.WriteLine(part2);
        }

        // Status Line
        var donr = comp.Roots.Count(x=>x.IsComplete);
        Console.WriteLine($"[{globalStatus,9}] Items {donr}/{comp.Roots.Count} in {timer.Elapsed.TotalSeconds:0.0} sec");
    }
}
