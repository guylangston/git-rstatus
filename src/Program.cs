public static class Program
{
    public static int Main(string[] args)
    {
        var app = new GitStatusApp
        {
            Args = args
        };
        return app.Run();
    }
}
