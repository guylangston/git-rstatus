namespace GitRStatus;

/// <summary>DISCLAIMER: I don't want to use `Microsoft.Extensions.Logging.ILogger`, or Nlog, etc.
/// As I want this project to have zero-depenancy</summary>
public interface ILogger
{
    void Log(string txt);
    void Log(Exception ex, string txt);

    // If we need we can add LogLevels, etc later
}

public interface ILoggerFactory
{
    ILogger GetLogger(string name);
    ILogger GetLogger(Type type);
    ILogger GetLogger<T>() => GetLogger(typeof(T));
}

public class LoggerDebug : ILogger
{
    public LoggerDebug(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public void Log(string txt)
    {
        System.Diagnostics.Debug.Write(DateTime.Now.ToString("u"));
        System.Diagnostics.Debug.Write(": ");
        System.Diagnostics.Debug.Write(Name);
        System.Diagnostics.Debug.Write(": ");
        System.Diagnostics.Debug.WriteLine(txt);
    }

    public void Log(Exception ex, string txt)
    {
        Log(txt + Environment.NewLine + ex.ToString());
    }
}

public class SimpleLoggerFactory : ILoggerFactory
{
    public SimpleLoggerFactory()
    {
    }

    public ILogger GetLogger(string name)
    {
        // NOTE: to view the log add `--log` to the config line. See Program.cs:5
        return new LoggerDebug(name);
    }

    public ILogger GetLogger(Type type) => GetLogger(type.FullName!);
}




