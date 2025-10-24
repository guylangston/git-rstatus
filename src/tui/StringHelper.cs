namespace GitRStatus.TUI;
public static class StringHelper
{
    public static string ElipseAtEnd(string text, int max, string elipse = "...")
    {
        if (text.Length <= max) return text;
        return text[..(max-elipse.Length)] + elipse;
    }

    public static string ElipseAtStart(string text, int max, string elipse = "...")
    {
        if (text.Length <= max) return text;
        return elipse + text[(text.Length-max+elipse.Length)..];
    }
}


