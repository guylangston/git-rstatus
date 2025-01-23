public enum ItemStatus
{
    Discovered,
    Checking,
    Ignored,
    Clean,
    Dirty,
    Behind,
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

    public required string Path { get; init;}
    public required string PathRelative { get; init; }
    public ItemStatus Status { get; set; } = ItemStatus.Discovered;
    public RunStatus StatusRunning { get; set; } = RunStatus.Pending;
    public Exception? Error { get; private set; }

    public bool IsComplete => StatusRunning == RunStatus.Complete || StatusRunning == RunStatus.Error;
    public bool IsIgnored() => Path.Contains("linux");
    public string? LogFirstLine => gitLog?.StdOut.FirstOrDefault();

    public string StatusLine()
    {
        if (StatusRunning == RunStatus.Error) return $"<ERROR> {Error?.Message}";
        if (Status == ItemStatus.Discovered) return "Pending...";
        if (Status == ItemStatus.Ignored)  return "";
        if (gitStatus != null)
        {
            if (Status == ItemStatus.Behind) return gitStatus.FirstLineOrError();
            if (Status == ItemStatus.Dirty && gitStatus.StdOut.Count > 1)
            {
                return $"({gitStatus.StdOut.Count-1}) {gitStatus.StdOut[1]}";
            }
        }
        if (Status == ItemStatus.Checking) return "$git fetch";
        if (Status == ItemStatus.Clean)
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
        return $"{Status}";
    }

    public async Task GitStatus()
    {
        gitStatus = await ProcessHelper.RunYieldingProcessResult("git", "status -bs", Path);
    }

    public async Task GitFetch()
    {
        gitFetch = await ProcessHelper.RunYieldingProcessResult("git", "fetch", Path);
    }

    public async Task GitRemote()
    {
        gitRemote = await ProcessHelper.RunYieldingProcessResult("git", "remote -v", Path);
    }

    public async Task GitLog()
    {
        gitLog = await ProcessHelper.RunYieldingProcessResult("git", "log --pretty=\"(%cd) %s\" --date=relative", Path);
    }

    public async Task Process()
    {
        try
        {
            StatusRunning = RunStatus.Running;
            if (IsIgnored())
            {
                Status = ItemStatus.Ignored;
                StatusRunning = RunStatus.Complete;
                return;
            }

            Status = ItemStatus.Checking;
            await GitRemote();
            await GitFetch();
            await GitStatus();
            if (gitStatus != null && gitStatus.StdOut.Count == 1)
            {
                if (gitStatus.StdOut.First().Contains("[behind "))
                {
                    Status = ItemStatus.Behind;
                }
                else
                {
                    // clean
                    await GitLog();
                    Status = ItemStatus.Clean;
                }
            }
            else
            {
                Status = ItemStatus.Dirty;
            }

            StatusRunning = RunStatus.Complete;
        }
        catch(Exception ex)
        {
            StatusRunning = RunStatus.Error;
            Error = ex;
        }
    }
}

