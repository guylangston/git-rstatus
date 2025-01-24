using System.Diagnostics;

public record ProcessResult(int ExitCode, List<string> StdOut, List<string> StdErr)
{
    public TimeSpan Duration { get; init; }
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
        proc.OutputDataReceived += (o, e) =>
        {
            if (e.Data != null) stdOut.Add(e.Data);
        };
        proc.ErrorDataReceived += (o, e) =>
        {
            if (e.Data != null) stdErr.Add(e.Data);
        };
        proc.EnableRaisingEvents = true;
        timer.Start();
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (timeout != null)
        {
            proc.WaitForExit(timeout.Value);
            if (!proc.HasExited)
            {
                proc.Kill();
                timer.Stop();
                return new ProcessResult(-99, stdOut, stdErr)
                {
                    Duration = timer.Elapsed,
                    TimeOutBeforeComplete = true
                };
            }
        }
        else
        {
            await proc.WaitForExitAsync();
        }
        timer.Stop();
        return new ProcessResult(proc.ExitCode, stdOut, stdErr)
        {
            Duration = timer.Elapsed,
            TimeOutBeforeComplete = false
        };

    }
}

