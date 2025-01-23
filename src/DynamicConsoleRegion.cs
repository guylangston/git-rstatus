public class DynamicConsoleRegion : IDisposable
{
    int initialLine;
    int skipped;
    int frame;

    public int RequestedHeight { get; private set; }
    public int MinHeight { get; init; } = 3;
    public int FreeLines { get; private set; }
    public int Width => Console.WindowWidth;
    public bool AllowOverflow { get; set; } = false;

    /// <summary>Safe will clear the screen before drawing</summary>
    public bool SafeDraw { get; set; } = true;

    public ConsoleColor StartFg { get; private set; }
    public ConsoleColor StartBg { get; private set; }
    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set => Console.ForegroundColor = value;
    }
    public ConsoleColor BackgroundColor
    {
        get => Console.BackgroundColor;
        set => Console.BackgroundColor = value;
    }

    /// <summary>Number of lines assigned to region</summary>
    public int Height { get; private set; }

    /// <summary>Prepare state. Capture initial cursor size</summary>
    public void Init(int requestedHeight)
    {
        initialLine = Console.CursorTop;
        StartFg = Console.ForegroundColor;
        StartBg = Console.BackgroundColor;

        ReInit(requestedHeight);
    }

    public void ReInit(int newSize)
    {
        if (newSize <= 0) throw new InvalidDataException(newSize.ToString());
        // Q: How much size is available?
        var availableLines = Console.WindowHeight - initialLine - 1;
            // -1 because we never want to use the very last line.
            // as WriteLine() on the last line will cause a automatic newline
            // TODO: In future we could finesse this away

        // Q: How much size can be assigned?
        if (availableLines >= newSize)
        {
            RequestedHeight = newSize;
            FreeLines = Height = newSize;
        }
        else
        {
            RequestedHeight = newSize;
            while(availableLines < newSize && initialLine > 0)
            {
                if ((Console.CursorTop+availableLines) >= Console.WindowHeight-1)
                {
                    Console.WriteLine(); // Add a new line, scrolling the window up
                    initialLine--;
                }
                availableLines++;
            }

            FreeLines = Height = availableLines;
        }
    }

    /// <summary>Clear the region; set cursor to start; track freelines</summary>
    public void StartDraw()
    {
        // Clear and Reset
        Console.SetCursorPosition(0, initialLine);
        Console.CursorVisible = false;

        if (frame == 0 || SafeDraw)
        {
            Console.ForegroundColor = StartFg;
            Console.BackgroundColor = StartBg;
            var emptyLine = new String(' ', Width-1);
            for(int x=0; x<Height; x++)
            {
                Console.WriteLine(emptyLine);
            }
            Console.SetCursorPosition(0, initialLine);
        }
        FreeLines = Height;
        skipped = 0;
        frame++;
    }

    public bool Write(string s)
    {
        // Don't allow wrapping
        var remaining = Console.WindowWidth - Console.CursorLeft;
        if (s.Length >= remaining)
        {
            Console.Write(s[0..(remaining-1)]);
            return false;
        }
        else
        {
            Console.Write(s);
            return true;
        }
    }

    public void FinishLine()
    {
        while(Console.CursorLeft < Console.WindowWidth-1)
        {
            Console.Write(' ');
        }
    }

    public bool WriteLine(string s, bool finish = false)
    {
        if (AllowOverflow || FreeLines > 0)
        {
            Write(s);
            if (finish) FinishLine();
            Console.WriteLine();
            FreeLines--;
            return true;
        }
        skipped++;
        return false;
    }

    public void Dispose()
    {
        Console.CursorVisible = true;
    }
}
