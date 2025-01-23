using System.Diagnostics;

public record ProcessResult(int ExitCode, List<string> StdOut, List<string> StdErr)
{
}

public static class ProcessHelper
{
    public static async Task<ProcessResult> RunYieldingProcessResult(string prog, string args, string? directory = null, int maxLines = 100)
    {
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
        proc.Start();

        var stdOut = new List<string>();
        while(await proc.StandardOutput.ReadLineAsync() is {} line)
        {
            if (stdOut.Count < maxLines) stdOut.Add(line);
        }
        var stdErr = new List<string>();
        while(await proc.StandardError.ReadLineAsync() is {} line)
        {
            if (stdErr.Count < maxLines) stdErr.Add(line);
        }
        if (!proc.HasExited)
        {
            await proc.WaitForExitAsync();
        }

        return new ProcessResult(proc.ExitCode, stdOut, stdErr);
    }
}

