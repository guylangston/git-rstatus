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

public class GitRoot
{
    string[]? gitStatus;
    string[]? gitFetch;
    string[]? gitRemote;
    string[]? gitLog;

    public required string Path { get; init;}
    public required string PathRelative { get; init; }
    public ItemStatus Status { get; set; } = ItemStatus.Discovered;
    public RunStatus StatusRunning { get; set; } = RunStatus.Pending;

    public bool IsComplete => StatusRunning == RunStatus.Complete || StatusRunning == RunStatus.Error;
    public bool IsIgnored() => Path.Contains("linux");

    public async Task GitStatus()
    {
        gitStatus = await ProcessHelper.RunYieldingStdOut("git", "status -bs", Path);
    }

    public async Task GitFetch()
    {
        gitFetch = await ProcessHelper.RunYieldingStdOut("git", "fetch", Path);
    }

    public async Task GitRemote()
    {
        gitRemote = await ProcessHelper.RunYieldingStdOut("git", "remote -v", Path);
    }

    public async Task GitLog()
    {
        gitLog = await ProcessHelper.RunYieldingStdOut("git", "log --oneline -10", Path);
    }

    public string? LogFirstLine => gitStatus?.FirstOrDefault();

    public string StatusLine()
    {
        if (gitLog != null && gitLog.Length > 0)
        {
            return gitLog.First();
        }
        if (Status == ItemStatus.Discovered) return "Pending...";
        if (Status == ItemStatus.Behind) return gitStatus?.First() ?? "<ERR>";
        if (Status == ItemStatus.Dirty && gitStatus != null && gitStatus.Length > 1)
        {
            return $"({gitStatus.Length-1}) {gitStatus[1]}";
        }
        if (Status == ItemStatus.Checking) return "$git fetch";
        if (Status == ItemStatus.Ignored)  return "";
        return $"{Status}:{StatusRunning}";
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
            await GitFetch();
            await GitStatus();
            if (gitStatus != null && gitStatus.Length == 1)
            {
                if (gitStatus.First().Contains("[behind "))
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
        catch(Exception)
        {
            StatusRunning = RunStatus.Error;
        }
    }
}

