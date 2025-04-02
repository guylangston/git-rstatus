using System.Diagnostics;

public static class Program
{
    public static int Main(string[] args)
    {
        TextWriterTraceListener? traceListener = null;
        try
        {
            if (args.Contains("--log"))
            {
                string logFilePath = "git-rstatus.log";
                traceListener = new TextWriterTraceListener(logFilePath);
                Trace.Listeners.Add(traceListener);
            }
            if (args.Contains("--bench"))
            {
                return Benchmark.Run(args);
            }

            using var app = new GitStatusApp(args);
            return app.Run();
        }
        finally
        {
            if (traceListener != null)
            {
                traceListener.Flush();
                traceListener.Dispose();
            }
        }
    }

    public static readonly ILoggerFactory LoggerFactory = new SimpleLoggerFactory();
}
