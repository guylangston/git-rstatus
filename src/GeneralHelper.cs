public static class GeneralHelper
{
    public static IEnumerable<T[]> CollectInBuckets<T>(IEnumerable<T> items, int bucketSize)
    {
        var curr = new List<T>(bucketSize);
        foreach(var item in items)
        {
            curr.Add(item);
            if (curr.Count == bucketSize)
            {
                yield return curr.ToArray();
                curr.Clear();
            }
        }
        if (curr.Any())
        {
            yield return curr.ToArray();
        }
    }
}

