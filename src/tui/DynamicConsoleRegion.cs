public class DynamicConsoleRegionWithLogger : DynamicConsoleRegion
{
    public ILogger? Logger { get; set; }
}

public class DynamicConsoleRegion : IDisposable
{
    int initialCursorLine;
    int skipped;
    int frame;

    public int RequestedHeight { get; private set; }
    public int MinHeight { get; init; } = 3;
    public int FreeLines { get; private set; }
    public int Width => Console.WindowWidth;

    public bool AllowOverflow { get; set; } = false;

    /// <summary>Safe will clear the screen before drawing</summary>
    public bool ModeSafeDraw { get; set; } = true;

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

    public override string ToString()
    {
        return $"{nameof(DynamicConsoleRegion)}: Height:{Height}{(ModeSafeDraw ? " +safe": "")}{( AllowOverflow ? " +overflow" : "" )}";
    }

    /// <summary>Prepare state. Capture initial cursor size</summary>
    public void Init(int requestedHeight)
    {
        initialCursorLine = Console.CursorTop;
        StartFg = Console.ForegroundColor;
        StartBg = Console.BackgroundColor;

        ReInit(requestedHeight);
    }

    public void ReInit(int newSize)
    {
        if (newSize <= 0) throw new InvalidDataException(newSize.ToString());

        // Q: How much size is available?
        var availableLines = Console.WindowHeight - initialCursorLine - 1;
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
            Console.CursorTop = initialCursorLine;
            availableLines = 0;
            while(availableLines <= newSize && initialCursorLine > 0)
            {
                var lastLine = Console.WindowHeight-1;
                if (initialCursorLine == 0) break;

                if (Console.CursorTop < lastLine)
                {
                    Console.WriteLine();
                    availableLines++;
                }
                else
                {
                    Console.WriteLine();
                    availableLines++;
                    // Did adding the new line cause the windows to scroll up?
                    // Yes - if the cursor is on the last line
                    initialCursorLine--;
                }
            }
            Console.CursorTop = initialCursorLine;
            FreeLines = Height = availableLines;
        }
    }

    /// <summary>Clear the region; set cursor to start; track freelines</summary>
    public void StartDraw(bool clear)
    {
        // Clear and Reset
        Console.SetCursorPosition(0, initialCursorLine);
        Console.CursorVisible = false;
        Revert();

        if (frame == 0 || clear)
        {
            var emptyLine = new String(' ', Width-1);
            for(int x=0; x<Height; x++)
            {
                Console.WriteLine(emptyLine);
            }
            Console.SetCursorPosition(0, initialCursorLine);
        }
        FreeLines = Height;
        skipped = 0;
        frame++;
    }

    /// <summary>Revert to default colors</summary>
    public void Revert()
    {
        Console.ForegroundColor = StartFg;
        Console.BackgroundColor = StartBg;
    }

    public bool Write(string s)
    {
        if (FreeLines < 1 && !AllowOverflow) return false;

        if (!ModeSafeDraw)
        {
            Console.Write(s);
            return true;
        }

        // Don't allow wrapping
        // WARN: This is slow!
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

    public bool WriteLine() => WriteLine("");
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
