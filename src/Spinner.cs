public class Spinner
{
    const string SeqSmall = "⠟⠯⠷⠾⠽⠻";
    const string SeqLange = "⡿⣟⣯⣷⣾⣽⣻⢿";

    int next = 0;
    string seq;

    public Spinner()
    {
        seq = SeqLange;
    }

    public char Next()
    {
        var n = seq[next];
        next = ((next+1) % seq.Length);
        return n;
    }
}

