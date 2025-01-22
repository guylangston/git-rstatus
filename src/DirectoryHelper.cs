public static class DirectoryHelper
{
    public static void DeleteEverythingRecursive(string dir)
    {
        Directory.Delete(dir, true);
    }

    public static void CleanRecursive(string dir)
    {
        Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
    }

    public static void EnsureCleanRecursive(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }

        Directory.CreateDirectory(dir);
    }
}

