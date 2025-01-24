public enum ItemStatus
{
    None,
    Found,
    Check,
    Ignore,
    UpToDate,
    Dirty,
    Behind,
    Ahead, // TODO
    Pull,
}

public enum RunStatus
{
    Pending,
    Running,
    Complete,
    Error
}

public static class ProcessResultHelper
{
    public static string FirstLineOrError(this ProcessResult? result)
    {
        if (result is null) return "<ERR>";
        if (result.StdOut.Count == 0) return "<ERR>";
        return result.StdOut.First();
    }
}

public class GitRoot
{
    ProcessResult? gitStatus;
    ProcessResult? gitFetch;
    ProcessResult? gitRemote;
    ProcessResult? gitLog;
    ProcessResult? gitPull;
    ILogger logger;

    public GitRoot(string path, string relPath)
    {
        Path = path;
        PathRelative = relPath;
        logger = Program.LoggerFactory.GetLogger(nameof(GitRoot) + ":" + relPath);
    }

    public string Path { get; }
    public string PathRelative { get; }
    public ItemStatus Status { get; set; } = ItemStatus.Found;
    public RunStatus StatusRunning { get; set; } = RunStatus.Pending;
    public Exception? Error { get; private set; }

    public bool IsComplete => StatusRunning == RunStatus.Complete || StatusRunning == RunStatus.Error;
    public string? LogFirstLine => gitLog?.StdOut.FirstOrDefault();

    public string StatusLine()
    {
        if (StatusRunning == RunStatus.Error) return $"<ERROR> {Error?.Message}";
        if (Status == ItemStatus.Found) return "";
        if (Status == ItemStatus.Ignore)  return "";
        if (gitStatus != null)
        {
            if (Status == ItemStatus.Behind) return gitStatus.FirstLineOrError();
            if (Status == ItemStatus.Ahead) return gitStatus.FirstLineOrError();
            if (Status == ItemStatus.Dirty && gitStatus.StdOut.Count > 1)
            {
                return $"[{gitStatus.StdOut.Count-1} files] {gitStatus.StdOut[1]}";
            }
        }
        if (Status == ItemStatus.Check) return "";
        if (Status == ItemStatus.UpToDate)
        {
            if (gitLog != null)
            {
                if (gitLog.StdOut.Count > 0)
                {
                    return gitLog.FirstLineOrError();
                }
                if (gitLog.StdErr.Count > 0)
                {
                    foreach(var ln in gitLog.StdErr)
                    {
                        Console.Error.WriteLine($"{PathRelative}|{ln}");
                    }
                    return gitLog.StdErr.First();
                }
                if (gitLog.ExitCode != 0) return $"exitcode: {gitLog.ExitCode}";
            }
            return "";
        }
        if (Status == ItemStatus.Pull)
        {
            if (gitPull != null)
            {
                return gitPull.FirstLineOrError();
            }
        }
        return $"{Status}";
    }

    private async Task<ProcessResult> RunYielding(string cmd, string args)
    {
        var res = await ProcessRunner.RunYieldingProcessResult(cmd, args, Path, 50, TimeSpan.FromSeconds(30));
        logger.Log($"CMD: {cmd} {args} ==> ExitCode:{res.ExitCode} in {res.Duration} [std: {res.StdOut.Count}, err: {res.StdErr.Count}]");
        if (res.TimeOutBeforeComplete)
        {
            logger.Log("CMD-TIMEOUT!");
        }
        foreach(var err in res.StdErr)
        {
            logger.Log($"ERR: {err}");
        }
        foreach(var lin in res.StdOut)
        {
            logger.Log(lin);
        }
        return res;
    }

    public async Task GitStatus()
    {
        gitStatus = await RunYielding("git", "status -bs");
    }

    public async Task GitFetch()
    {
        gitFetch = await RunYielding("git", "fetch");
    }

    public async Task GitRemote()
    {
        gitRemote = await RunYielding("git", "remote -v");
    }

    public async Task GitLog()
    {
        gitLog = await RunYielding("git", "log --pretty=\"(%cd) %s\" --date=relative -10");
    }

    public async Task GitPull()
    {
        gitPull = await RunYielding("git", "pull");
    }

    public async Task Process(GitStatusApp app)
    {
        try
        {
            StatusRunning = RunStatus.Running;
            if (Status == ItemStatus.Ignore)
            {
                StatusRunning = RunStatus.Complete;
                return;
            }

            Status = ItemStatus.Check;
            if (app.ArgRemote) await GitRemote();
            if (app.ShouldFetch(this)) await GitFetch();

            await GitStatus();
            if (gitStatus != null && gitStatus.StdOut.Count == 1)
            {
                var lineOne = gitStatus.FirstLineOrError();
                if (lineOne.Contains("[behind "))
                {
                    Status = ItemStatus.Behind;
                    if (app.ArgPull)
                    {
                        Status = ItemStatus.Pull;
                        await GitPull();
                        return;
                    }
                }
                if (lineOne.Contains("[ahead "))
                {
                    Status = ItemStatus.Ahead;
                    return;
                }
                else
                {
                    // clean
                    await GitLog();
                    Status = ItemStatus.UpToDate;
                    return;
                }
            }
            else
            {
                Status = ItemStatus.Dirty;
            }
        }
        catch(Exception ex)
        {
            StatusRunning = RunStatus.Error;
            Error = ex;
        }
        finally
        {
            if (StatusRunning != RunStatus.Error) StatusRunning = RunStatus.Complete;
        }
    }
}

