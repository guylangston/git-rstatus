using System.Diagnostics;

public static class ProcessHelper
{
    public static async Task<string[]> RunYieldingStdOut(string prog, string args, string? directory = null)
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

        var res = new List<string>();
        while(await proc.StandardOutput.ReadLineAsync() is {} line)
        {
            res.Add(line);
        }
        while(await proc.StandardError.ReadLineAsync() is {} line)
        {
            res.Add(line);
        }
        return res.ToArray();
    }
}

