using System.Diagnostics;

public static class Program
{
    public static int Main(string[] args)
    {
#if DEBUG
        string logFilePath = "git-status.log";
        using var traceListener= new TextWriterTraceListener(logFilePath);
        Trace.Listeners.Add(traceListener);
#endif

        var app = new GitStatusApp(args);
        return app.Run();
    }

    public static readonly ILoggerFactory LoggerFactory = new SimpleLoggerFactory();
}
