namespace GitRStatus.TUI;
public class Spinner
{
    const string SeqSmall = "⠟⠯⠷⠾⠽⠻";
    const string SeqLarge = "⡿⣟⣯⣷⣾⣽⣻⢿";

    float next = 0;
    float speed = 1;
    string seq;

    public Spinner(string seq, float speed)
    {
        this.seq = seq;
        this.speed = speed;
    }

    public Spinner(float speed) : this(Spinner.SeqLarge, speed) {}
    public Spinner() : this(Spinner.SeqLarge, 1) {}

    public char Next()
    {
        var n = seq[(int)next];
        next += speed;
        if (next >= seq.Length) next = 0;
        return n;
    }
}

