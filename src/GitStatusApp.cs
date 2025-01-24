using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

public class GitStatusApp : IDisposable
{
    DynamicConsoleRegion consoleRegion = new()
    {
        SafeDraw = false,
    };
    Stopwatch timer = new();
    bool scanComplete;
    string? globalStatus;
    GitRoot[]? gitRoots;
    Spinner spinner = new(0.2f);
    ILogger logger = Program.LoggerFactory.GetLogger<GitStatusApp>();

    public GitStatusApp(string[] args)
    {
        ArgsRaw = args;
        var flags = new StringBuilder();
        for (int i = 0; i < args.Length; i++)
        {
            string? arg = args[i];

            if (arg.StartsWith("--"))
            {
                if (arg == "--exclude")
                {
                    var next = args[i+1];
                    if (next.StartsWith('-')) throw new InvalidDataException("--exclude must be followed with a path");
                    ArgExclude.AddRange(next.Split(','));
                    i++;
                }
                else if (arg == "--no-fetch")
                {
                    var next = args[i+1];
                    if (next.StartsWith('-')) throw new InvalidDataException("--no-fetch must be followed with a path");
                    ArgNoFetch.AddRange(next.Split(','));
                    i++;
                }
                else if (arg == "--no-fetch-all")
                {
                    ArgNoFetch.Add("*");
                }
                else
                {
                    ArgAllParams.Add(arg);
                }
            }
            else if (arg.StartsWith("-"))
            {
                flags.Append(arg[1..]);
            }
            else
            {
                ArgPath.Add(arg);
            }
        }
        ArgAllFlags = flags.ToString();

        if (ArgPath.Count == 0)
        {
            ArgPath.Add(Environment.CurrentDirectory);
        }
    }

    public string[] ArgsRaw { get;  }
    public string ArgAllFlags { get; init; }
    public List<string> ArgAllParams { get; } = new();
    public List<string> ArgPath { get; } = new();
    public List<string> ArgExclude { get; } = new();
    public List<string> ArgNoFetch { get; } = new();
    public bool ArgRemote { get; set; }
    public bool ArgPull => ArgAllFlags.Contains('p') || ArgAllParams.Contains("--pull");
    public bool ArgHelp => ArgAllFlags.Contains('?') ||  ArgAllFlags.Contains('h') || ArgAllParams.Contains("--help");
    public bool ArgAbs => ArgAllFlags.Contains('a') || ArgAllParams.Contains("--abs");
    public int ArgMaxDepth { get; } = 8;

    public IReadOnlyList<GitRoot> Roots => gitRoots ?? throw new NullReferenceException("gitRoots. Scan expected first");

    public bool ShouldFetch(GitRoot root)
    {
        if (ArgNoFetch.Contains("*")) return false;

        // TODO: Document this VV
        if (ArgNoFetch.Any(x=>root.PathRelative.EndsWith(x))) return false;
        return true;
    }

    /// <summary>Should not be async - run on main thread</summary>
    public int Run()
    {
        if (ArgHelp)
        {
            DisplayHelp();
            return 0;
        }

        timer.Start();
        logger.Log("Run: Init");
        consoleRegion.Init(3);
        consoleRegion.WriteLine("[git-status] scanning...");

        var process = ScanAndQueryAllRoots();
        var frameRate = TimeSpan.FromSeconds(1 / 30f);
        var resize = false;
        while(!process.IsCompleted)
        {
            if (scanComplete && !resize)
            {
                if (Roots.Count == 0)
                {
                    Console.WriteLine("No `.git` folders found.");
                    return 2;
                }
                logger.Log("Run: ReInit/Resize");
                // First draw after scanning resizes the dynamic console region
                consoleRegion.WriteLine($"[git-status] found {Roots.Count}, fetching...");
                consoleRegion.ReInit(Roots.Count);
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
        if (Roots.Count == 0)
        {
            Console.WriteLine("No `.git` folders found.");
            return 2;
        }

        // Final Render
        timer.Stop();
        consoleRegion.AllowOverflow = true;
        Render();

        var firstError = Roots.FirstOrDefault(x=>x.Error != null);
        if (firstError != null)
        {
            if (firstError.Error != null)
            {
                logger.Log(firstError.Error, "FistError");
                Console.Error.WriteLine(firstError.Error);
            }
        }

        return 0;
    }

    private async Task ScanAndQueryAllRoots()
    {
        try
        {
            logger.Log("IN: "+nameof(ScanAndQueryAllRoots));
            globalStatus = "Scanning";
            var scanResult = new ConcurrentBag<GitRoot>();
            await Parallel.ForEachAsync(ArgPath, async (path, ct) =>
            {
                var comp = new GitFolderScanner()
                {
                    Exclude = (path)=>
                    {
                        foreach(var ex in ArgExclude)
                        {
                            if (path.EndsWith(ex))
                            {
                                logger.Log($"Excluding: {path} (because {ex})");
                                return true;
                            }
                        }
                        return false;
                    }
                };
                await comp.Scan(path, ArgMaxDepth);
                foreach(var r in comp.Roots)
                {
                    scanResult.Add(r);
                }
            });
            gitRoots = scanResult.ToArray();
            scanComplete = true;

            globalStatus = "Processing";
            await Task.Run(() =>
            {
                var buckets = GeneralHelper.CollectInBuckets(Roots, Roots.Count / 4);
                Task.WaitAll(buckets.Select(x=>ProcessBucket(this, x)));
            });

            globalStatus = "Completed";
        }
        catch (Exception)
        {
            globalStatus = "Error";
            throw;
        }
        finally
        {
            logger.Log("OUT: "+nameof(ScanAndQueryAllRoots));
        }

        static async Task ProcessBucket(GitStatusApp app, GitRoot[] bucket)
        {
            foreach(var gitRoot in bucket)
            {
                await gitRoot.Process(app);
            }
        }
    }


    string? GetVersion() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()[..^2];
    string GetProjectDescription() => "Fast recursive git status (with fetch and pull)";

    public void DisplayHelp()
    {
        Console.WriteLine(
        $"""
        git-status: {GetProjectDescription()}
           version: {GetVersion()}

        git-status -switch --param path1 path2 path3
            --no-fetch-all              # dont `git fetch` before `git status`
            --no-fetch path,path        # same as above, but only on matching path
            -p --pull                   # pull (if status is not dirty)
            --exclude path,path         # dont process repos containing these strings
            --depth number              # don't recurse deeper than `number`
            --log                       # create log file (in $PWD)
            -a --abs                    # use absolute paths
        """);
    }

    static Dictionary<ItemStatus, ConsoleColor> Colors = new()
    {
        {ItemStatus.Found,  ConsoleColor.DarkBlue},
        {ItemStatus.Check,  ConsoleColor.DarkCyan},
        {ItemStatus.Ignore, ConsoleColor.DarkGray},
        {ItemStatus.Clean,  ConsoleColor.DarkGreen},
        {ItemStatus.Dirty,  ConsoleColor.Yellow},
        {ItemStatus.Behind, ConsoleColor.Cyan},
        {ItemStatus.Pull,   ConsoleColor.Magenta},
    };

    /// <summary>Render to the console</summary>
    /// - General idea to render all items dynamically if there is space
    /// - If there is no space. Show progress, then output results
    private void Render()
    {
        consoleRegion.StartDraw();

        if (gitRoots == null || gitRoots.Length == 0) return;

        var table = new TableRenderer<TableColumn, GitRoot, object>();
        table.Columns.Add(new TableColumn<GitRoot, object>() { Size = 6 } );
        table.Columns.Add(new TableColumn<GitRoot, object>("Path", 30, 60));
        table.Columns.Add(new TableColumn<GitRoot, object>("Git", 30, 60));
        foreach(var item in Roots.OrderBy(x=>x.Path))
        {
            var path = ArgAbs ? item.Path : item.PathRelative;
            var row = table.WriteRow(item.Status, path, item.StatusLine());
            row.RowData = item;

            if (!consoleRegion.AllowOverflow && table.RowCount >= consoleRegion.Height - 2) break;
        }
        table.CalcColumnSizes();
        foreach(var row in table.Rows)
        {
            if (row.RowData == null) throw new ArgumentNullException();
            foreach(var col in table.Columns)
            {
                if (row.TryGetCell(col, out var cell))
                {
                    if (col is IHeader<GitRoot, object> fullHeader)
                    {
                        if (col.Index == 0)
                        {
                            consoleRegion.ForegroundColor = Colors[row.RowData.Status];
                        }
                        if (col.Index == 2)
                        {
                            if ((int)row.RowData.Status >= 3)
                            {
                                consoleRegion.ForegroundColor = Colors[row.RowData.Status];
                            }
                        }
                        consoleRegion.Write(fullHeader.RenderCellText(row.RowData, cell));
                        consoleRegion.Revert();
                    }
                    else
                    {
                        consoleRegion.Write(cell?.ToString() ?? "");
                    }
                }
            }
            consoleRegion.WriteLine("");
        }

        // Status Line
        var donr = Roots.Count(x=>x.IsComplete);
        consoleRegion.WriteLine($"[{spinner.Next()}] [{globalStatus,9}] Items {donr}/{Roots.Count} in {timer.Elapsed.TotalSeconds:0.0} sec");
    }

    public void Dispose()
    {
        consoleRegion.Dispose();
    }
}

