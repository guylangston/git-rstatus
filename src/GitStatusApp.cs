using System.Diagnostics;

public class GitStatusApp
{
    GitStatusComponent comp = new();
    DynamicConsoleRegion consoleRegion = new()
    {
        SafeDraw = false,
    };
    Stopwatch timer = new();
    bool scanComplete;
    string? globalStatus;

    public required string[] Args { get; init; }

    /// <summary>Should not be async - run on main thread</summary>
    public int Run()
    {
        consoleRegion.Init(3);
        consoleRegion.WriteLine("[git-status] scanning...");

        var process = Process(comp, Args.First());
        var frameRate = TimeSpan.FromSeconds(1 / 30f);
        var resize = false;
        while(!process.IsCompleted)
        {
            if (scanComplete && !resize)
            {
                // First draw after scanning resizes the dynamic console region
                consoleRegion.WriteLine($"[git-status] found {comp.Roots.Count}, fetching...");
                consoleRegion.ReInit(comp.Roots.Count);
                resize = true;
            }
            Render();
            Thread.Sleep(frameRate);
        }
        if (process.IsFaulted)
        {
            Console.Error.WriteLine(process.Exception);
            return 1;
        }

        // Final Render
        timer.Stop();
        consoleRegion.AllowOverflow = true;
        Render();

        if (HasFlag("-s"))
        {
            foreach(var x in comp.Roots)
            {
                Console.WriteLine($"{x.PathRelative,30} | {x.Status} | {x.LogFirstLine}");
            }
        }

        var firstError = comp.Roots.FirstOrDefault(x=>x.Error != null);
        if (firstError != null)
        {
            Console.Error.WriteLine(firstError.Error);
        }

        return 0;

    }

    bool HasFlag(string flag) => Args.Length > 0 && Args.Contains(flag);

    private async Task Process(GitStatusComponent comp, string root)
    {
        try
        {
            globalStatus = "Scanning";
            await comp.Scan(root, 8);
            scanComplete = true;

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

        static async Task ProcessBucket(GitRoot[] bucket)
        {
            foreach(var dir in bucket)
            {
                await dir.Process();
            }
        }
    }

    static Dictionary<ItemStatus, ConsoleColor> Colors = new()
    {
        {ItemStatus.Discover,   ConsoleColor.DarkBlue},
        {ItemStatus.Checking,   ConsoleColor.DarkCyan},
        {ItemStatus.Ignored,    ConsoleColor.DarkGray},
        {ItemStatus.Clean,      ConsoleColor.DarkGreen},
        {ItemStatus.Dirty,      ConsoleColor.Yellow},
        {ItemStatus.Behind,     ConsoleColor.Cyan},
    };

    /// <summary>Render to the console</summary>
    /// - General idea to render all items dynamically if there is space
    /// - If there is no space. Show progress, then output results
    private void Render()
    {
        consoleRegion.StartDraw();

        var maxPath = comp.Roots.Max(x=>x.PathRelative.Length);
        var sizePath = Math.Min(maxPath, Math.Min(80, consoleRegion.Width / 2));
        int cc = 0;
        foreach(var item in comp.Roots.OrderBy(x=>x.Path))
        {
            /* var path = "./" + item.PathRelative; */
            var path = item.PathRelative;
            var txtPath = StringHelper.ElipseAtStart(path, sizePath, "__").PadRight(sizePath);
            var txtStatusLine =  item.StatusLine();

            consoleRegion.ForegroundColor = Colors[item.Status];
            consoleRegion.Write(item.Status.ToString().PadRight(8));
            consoleRegion.ForegroundColor = consoleRegion.StartFg;
            consoleRegion.Write(" ");
            consoleRegion.Write(txtPath);
            consoleRegion.Write(" ");
            if (item.Status == ItemStatus.Dirty || item.Status == ItemStatus.Behind)
            {
                consoleRegion.ForegroundColor = Colors[item.Status];
            }
            consoleRegion.WriteLine(txtStatusLine);
            consoleRegion.ForegroundColor = consoleRegion.StartFg;
            cc++;
            if (!consoleRegion.AllowOverflow && cc >= consoleRegion.Height - 2) break;

        }

        // Status Line
        var donr = comp.Roots.Count(x=>x.IsComplete);
        consoleRegion.WriteLine($"[{globalStatus,9}] Items {donr}/{comp.Roots.Count} in {timer.Elapsed.TotalSeconds:0.0} sec");
    }
}

