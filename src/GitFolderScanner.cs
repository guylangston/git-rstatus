using System.Collections.Concurrent;

public class GitFolderScanner
{
    // Inputs
    public ConcurrentBag<GitRoot> Roots { get; } = new();
    public Predicate<string> Exclude { get; init; } = (_)=>false;

    // Outputs
    public int ProgressDirectories { get; private set; }

    public Task Scan(string root, int maxDepth = 4)
    {
        void Recurse(string path, int depth)
        {
            ProgressDirectories++;
            try
            {
                if (Directory.Exists(Path.Combine(path, ".git/")))
                {
                    Roots.Add(new GitRoot(path, Path.GetRelativePath(root, path)));
                }

                if (depth <= maxDepth)
                {
                    foreach (var kid in Directory.GetDirectories(path))
                    {
                        if (!Exclude(kid))
                        {
                            Recurse(kid, depth + 1);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip
            }
#if DEBUG
            catch (Exception) { throw; }
#endif
        }
        Recurse(root, 0);

        return Task.CompletedTask;
    }
}

