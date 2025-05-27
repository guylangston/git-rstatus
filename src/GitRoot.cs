using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

public enum GitStatus
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
    Error,
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

public record GitRootSummary(
        string Path, GitStatus Status,
        int Behind, int Ahead, int NewFiles,
        string SummaryOneLine) {}

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

    public string     Path          { get; }
    public string     PathRelative  { get; }
    public GitStatus  Status        { get; set;    }    = global::GitStatus.Found;
    public RunStatus  StatusRunning { get; set;    }    = RunStatus.Pending;
    public string?    Branch        { get; set;    }
    public string?    BranchStatus  { get; set;    }
    public Exception? Error         { get; private set; }
    public TimeSpan   Duration      { get; private set; }

    public bool IsComplete => StatusRunning == RunStatus.Complete || StatusRunning == RunStatus.Error;
    public string? LogFirstLine => gitLog?.StdOut.FirstOrDefault();

    public DateTime Started { get; private set; }

    public GitRootSummary ToSummary()
        => new GitRootSummary(Path, Status, 0, 0, 0, StatusLine());

    private async Task<ProcessResult> RunYielding(string cmd, string args, bool checkStdOut = true)
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
        if (checkStdOut && res.ExitCode == 0 && res.StdOut.Count == 0)
        {
            logger.Log("WARN: No error (ExitCode=0), but no std out. {cmd} {arg}");
        }

        return res;
    }

    public IEnumerable<ProcessResult> GetProcessResults()
    {
        if (gitFetch != null) yield return gitFetch;
        if (gitStatus != null) yield return gitStatus;
        if (gitLog != null) yield return gitLog;
        if (gitRemote != null) yield return gitRemote;
        if (gitPull != null) yield return gitPull;
    }

    public async Task GitStatus()
    {
        gitStatus = await RunYielding("git", "status -bs");
        gitStatus.ThrowOnBadExitCode(nameof(gitStatus)); // check after assignement so we still record erros
        if (gitStatus.StdOut.Count > 0)
        {
            // Expect: "## main...origin/main [behind 1]"
            var l1 = gitStatus.StdOut.First();
            var until = l1.IndexOf("...");
            if (until > 0)
            {
                Branch = l1[3..until];
            }

            var b1 = l1.IndexOf('[');
            var b2 = l1.IndexOf(']');
            if (b1 > 0 && b2 > b1)
            {
                var aheadBehind = l1[(b1+1)..b2].Replace("behind ", "-").Replace("ahead ", "+");
                BranchStatus = aheadBehind;
            }
        }
    }

    public async Task GitFetch()
    {
        gitFetch = await RunYielding("git", "fetch", false);
        gitFetch.ThrowOnBadExitCode(nameof(gitFetch)); // check after assignement so we still record erros
    }

    public async Task GitRemote()
    {
        gitRemote = await RunYielding("git", "remote -v");
        gitRemote.ThrowOnBadExitCode(nameof(gitRemote)); // check after assignement so we still record erros
    }

    public async Task GitLog()
    {
        gitLog = await RunYielding("git", "log --pretty=\"(%cd) %s\" --date=relative -10");
        gitLog.ThrowOnBadExitCode(nameof(gitLog)); // check after assignement so we still record erros
    }

    public async Task GitPull()
    {
        gitPull = await RunYielding("git", "pull");
        gitPull.ThrowOnBadExitCode(nameof(gitPull)); // check after assignement so we still record erros
    }

    public string StatusLine()
    {
        if (Status == global::GitStatus.Error)
        {
            foreach(var proc in GetProcessResults())
            {
                if (proc.StdErr != null && proc.StdErr.FirstOrDefault() is {} firstError)
                {
                    return firstError;
                }
            }
            return "Unknown error";
        }
        if (StatusRunning == RunStatus.Error) return $"<ERROR> {Error?.Message}";
        if (Status == global::GitStatus.Found) return "";
        if (Status == global::GitStatus.Ignore)
        {
            if (gitLog != null)
            {
                return gitLog.FirstLineOrError();
            }
            if (gitStatus != null)
            {
                return gitStatus.FirstLineOrError();
            }
            return "";
        }
        if (gitStatus != null)
        {
            if (Status == global::GitStatus.Behind) return gitStatus.FirstLineOrError();
            if (Status == global::GitStatus.Ahead) return gitStatus.FirstLineOrError();
            if (Status == global::GitStatus.Dirty && gitStatus.StdOut.Count > 1)
            {
                return $"[{gitStatus.StdOut.Count-1} files] {gitStatus.StdOut[1]}";

            }
        }
        if (Status == global::GitStatus.Check) return "";
        if (Status == global::GitStatus.UpToDate)
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
        if (Status == global::GitStatus.Pull)
        {
            if (gitPull != null)
            {
                return gitPull.FirstLineOrError();
            }
        }
        return $"{Status}";
    }

    public async Task Process(GitStatusApp app)
    {
        Started = DateTime.Now;
        var timer = new Stopwatch();
        try
        {
            timer.Start();
            StatusRunning = RunStatus.Running;
            if (Status == global::GitStatus.Ignore)
            {
                StatusRunning = RunStatus.Complete;
                return;
            }

            Status = global::GitStatus.Check;
            if (app.ArgRemote) await GitRemote();
            var fetch = app.ShouldFetch(this);
            if (fetch)
            {
                await GitFetch();
            }

            await GitStatus();
            if (gitStatus != null && gitStatus.StdOut.Count <= 1)
            {
                var lineOne = gitStatus.FirstLineOrError();
                if (lineOne.Contains("[behind "))
                {
                    Status = global::GitStatus.Behind;
                    if (app.ArgPull)
                    {
                        Status = global::GitStatus.Pull;
                        await GitPull();
                        return;
                    }
                    return;
                }
                else if (lineOne.Contains("[ahead "))
                {
                    Status = global::GitStatus.Ahead;
                    return;
                }
                else
                {
                    await GitLog();
                    if (fetch)
                    {
                        Status = global::GitStatus.UpToDate;
                        return;
                    }
                    else
                    {
                        // did not fetch, do not sure if we are up to date
                        Status = global::GitStatus.Ignore;
                        return;
                    }
                }
            }
            else
            {
                Status = global::GitStatus.Dirty;
            }
        }
        catch(RecoverableException)
        {
            Status = global::GitStatus.Error;
            StatusRunning = RunStatus.Error;
        }
        catch(Exception ex)
        {
            Status = global::GitStatus.Error;
            StatusRunning = RunStatus.Error;
            Error = ex;
        }
        finally
        {
            timer.Stop();
            Duration = timer.Elapsed;
            if (StatusRunning != RunStatus.Error) StatusRunning = RunStatus.Complete;
        }
    }
}

