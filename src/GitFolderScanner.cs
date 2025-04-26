using System.Collections.Concurrent;

public class GitFolderScanner
{
    int scanned;

    // Inputs
    public Predicate<string> Exclude { get; init; } = (_)=>false;
    public Action<bool>? UpdateProgress { get;set; } = null;

    // Outputs
    public ConcurrentBag<GitRoot> Roots { get; } = new();
    public int ProgressScanned => scanned;
    public int ProgressFound => Roots.Count;

    public Task Scan(string root, int maxDepth = 4)
    {
        void Recurse(string path, int depth)
        {
            Interlocked.Increment(ref scanned);
            try
            {
                if (Directory.Exists(Path.Combine(path, ".git/")))
                {
                    Roots.Add(new GitRoot(path, Path.GetRelativePath(root, path)));
                    if (UpdateProgress != null) UpdateProgress(true);
                }
                else
                {
                    if (UpdateProgress != null) UpdateProgress(false);
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

