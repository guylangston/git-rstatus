using System.Text;

public interface IHeader
{
    string? Title { get; }
    int? SizeMin { get; }
    int? SizeMax { get; }
}
public class Header : IHeader
{
    public Header() { }

    public Header(string? title, int? sizeMin = null, int? sizeMax = null)
    {
        Title = title;
        SizeMin = sizeMin;
        SizeMax = sizeMax;
    }

    public string? Title { get; set; }
    public int? SizeMin { get; set; }
    public int? SizeMax { get; set; }
}

public class TableRenderer<THeader, TCell> where THeader : IHeader, new()
{
    public List<THeader> Columns { get; } = new();
    public List<List<TCell>> Rows { get; } = new();

    public int RowCount => Rows.Count;
    public virtual int ColumnsCount => Rows.Count == 0 ? 0 : Rows.Max(x=>x.Count);
    public virtual int ColumnMax => Rows.Where(x=>x.Count > 0).Max(row=>row.Count == 0 ? 0 :  row.Max(Measure));

    public void Clear()
    {
        Rows.Clear();
    }

    protected virtual int Measure(TCell obj) => obj?.ToString()?.Length ?? 0;

    public virtual void WriteCell(TCell obj)
    {
        if (Rows.Count == 0) Rows.Add(new());
        Rows.Last().Add(obj);
    }

    public void WriteRow()
    {
        Rows.Add(new());
    }

    public void WriteRow(params TCell[] row) => WriteRow(row.AsSpan());
    public void WriteRow(ReadOnlySpan<TCell> row)
    {
        foreach(var cell in row)
        {
            WriteCell(cell);
        }
        WriteRow();
    }

    public IEnumerable<TCell?> GetColumnCells(int colIdx)
    {
        foreach(var row in Rows)
        {
            if (colIdx < row.Count)
            {
                yield return row[colIdx];
            }
            else
            {
                yield return default(TCell);
            }
        }
    }

    public virtual int[] GetColumnSizes()
    {
        return Enumerable.Range(0, ColumnsCount)
            .Select(colIdx=>
                    {
                        var max = 0;
                        var header =  (colIdx < Columns.Count) ? Columns[colIdx] : (THeader?)default(THeader);
                        if (header != null && header.SizeMin != null) max = header.SizeMin.Value;
                        foreach(var cell in GetColumnCells(colIdx))
                        {
                            var len = cell?.ToString()?.Length ?? 0;
                            if (len > max)
                                max = len;
                        }
                        if (header != null && header.SizeMax != null)
                        {
                            if (max > header.SizeMax.Value) return header.SizeMax.Value;
                        }

                        return max;
                    })
            .ToArray();
    }

    public virtual
        IEnumerable<IReadOnlyList<(THeader Header, string CellText)>>
        RenderCells()
    {
        List<(THeader, string)> line = new ();
        var colSize = GetColumnSizes();

        // Ensure Column definition
        while(Columns.Count < ColumnsCount)
        {
            Columns.Add(new THeader());
        }

        if (Columns.Any())
        {
            var col = 0;
            foreach(var header in Columns)
            {
                var cell = header.Title ?? "";
                if (cell.Length <= colSize[col])
                {
                    var txt = cell.PadRight(colSize[col]);
                    line.Add( (header,txt) );
                }
                else
                {
                    var txt = cell[0..colSize[col]];
                    line.Add( (header,txt) );
                }
                col++;
            }
            yield return line;
        }

        foreach(var row in Rows)
        {
            if (row.Count == 0) continue;
            line.Clear();
            for(var col=0; col<colSize.Length; col++)
            {
                var header = Columns[col];
                if (row.Count < col)
                {
                    // TODO add blank cell
                }
                else
                {
                    var cell = row[col]?.ToString() ?? "";
                    var cellText = (cell.Length <= colSize[col])
                        ? cell.PadRight(colSize[col])
                        : cell[0..colSize[col]];
                    line.Add( (header, cellText) );
                }

            }
            yield return line;
        }
    }
}

