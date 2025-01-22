using System.Collections.Concurrent;

public class GitStatusComponent
{
    public ConcurrentBag<GitRoot> Roots { get; } = new();

    public Task Scan(string root, int maxDepth = 4)
    {
        Func<string, bool> exclude = x => x.Contains("tmux");

        void Recurse(string path, int depth)
        {
            try
            {
                if (Directory.Exists(Path.Combine(path, ".git/")))
                {
                    Roots.Add(new GitRoot
                    {
                        Path = path,
                        PathRelative = Path.GetRelativePath(root, path)
                    });
                }
                if (depth <= maxDepth)
                {
                    foreach(var kid in Directory.GetDirectories(path))
                    {
                        if (!exclude(kid))
                        {
                            Recurse(kid, depth+1);
                        }
                    }
                }
            }
            catch(UnauthorizedAccessException)
            {
                // Skip
            }
        }
        Recurse(root, 0);

        return Task.CompletedTask;
    }
}

