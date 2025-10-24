using System.Diagnostics;

namespace GitRStatus;

public record ProcessResult
{
    public required int ExitCode { get; init; }
    public required List<string> StdOut { get; init; }
    public required List<string> StdErr { get; init; }
    public required string Command { get; init; }
    public required string CommandArgs { get; init; }
    public required DateTime Started { get; init; }

    public string? Name { get; set; }
    public TimeSpan Duration { get; set; }
    public bool TimeOutBeforeComplete { get; set; }

    public void ThrowOnBadExitCode(string? errorMessage)
    {
        if (ExitCode != 0)
        {
            throw new RecoverableException($"Bad ExitCode({ExitCode}) {errorMessage} | {StdErr.FirstOrDefault()}");
        }
    }
}

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunYieldingProcessResult
            (
                string prog, string args,
                string? directory = null, int maxLines = 100, TimeSpan? timeout = null
            )
    {
        var stdOut = new List<string>();
        var stdErr = new List<string>();
        var timer = new Stopwatch();

        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo(prog, args)
            {
               WorkingDirectory       = directory,
               RedirectStandardOutput = true,
               RedirectStandardError  = true,
               UseShellExecute        = false,
            }
        };
        timer.Start();
        var start = DateTime.Now;
        proc.Start();
        async Task ReadStdOut()
        {
            while(await proc.StandardOutput.ReadLineAsync() is {} line)
            {
                stdOut.Add(line);
            }
        }
        async Task ReadStdError()
        {
            while(await proc.StandardError.ReadLineAsync() is {} line)
            {
                stdErr.Add(line);
            }
        }

        var tokenSource = timeout != null
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource();
        var scheduler = TaskScheduler.Default;
        var t1 = Task.Factory.StartNew(ReadStdOut, tokenSource.Token, TaskCreationOptions.LongRunning, scheduler);
        var t2 = Task.Factory.StartNew(ReadStdError, tokenSource.Token, TaskCreationOptions.LongRunning, scheduler);
        var t3 =  proc.WaitForExitAsync(tokenSource.Token);

        await Task.WhenAll(t1, t2, t3);

        if (tokenSource.IsCancellationRequested)
        {
            if (!proc.HasExited)
            {
                proc.Kill();
                timer.Stop();
                return new ProcessResult
                {
                    ExitCode = -99,
                     Command = prog,
                     CommandArgs = args,
                     StdOut = stdOut,
                     StdErr = stdErr,
                     Started = start,
                     Duration = timer.Elapsed,
                     TimeOutBeforeComplete = true
                };
            }
        }

        timer.Stop();
        var exitCode = proc.ExitCode;
        proc.Close();

        return new ProcessResult
        {
            ExitCode = exitCode,
            StdOut = stdOut,
            StdErr = stdErr,
            Command = prog,
            CommandArgs = args,
            Duration = timer.Elapsed,
            Started = start,
            TimeOutBeforeComplete = false
        };
    }
}

