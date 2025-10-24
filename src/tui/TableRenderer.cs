namespace GitRStatus.TUI;
public interface IHeader
{
    string? Title { get; }
    int? SizeMin { get; }
    int? SizeMax { get; }
    int Index { get; set; }
    int? Size { get; set; }
    int? SizeContent { get; set; }
}

public interface IHeader<TRowData, TCell> : IHeader
{
    string RenderCellText(TRowData? row, TCell? cell);
}

public class TableColumn : IHeader
{
    public TableColumn() { }

    public TableColumn(string? title, int? sizeMin, int? sizeMax)
    {
        Title = title;
        SizeMin = sizeMin;
        SizeMax = sizeMax;
    }

    public int Index { get; set; }
    public string? Title { get; set; }
    public int? SizeMin { get; set; }
    public int? SizeMax { get; set; }
    public int? Size { get; set;}
    public int? SizeContent { get; set;}
}

public class TableColumn<TRowData, TCell> : TableColumn, IHeader<TRowData, TCell>
{
    public TableColumn() { }
    public TableColumn(string? title, int? sizeMin, int? sizeMax) : base(title, sizeMin, sizeMax) {}

    public virtual string RenderCellText(TRowData? row, TCell? cellObj)
    {
        var cell = cellObj?.ToString() ?? "";
        var size = Size ?? 500;
        var cellTextClipped = (cell.Length <= size)
            ? cell.PadRight(size)
            : cell[0..size];
        return cellTextClipped;
    }
}

public class TableRow<TRowData, TCell> : List<TCell>
{
    public TRowData? RowData { get; set; }

    public bool TryGetCell(IHeader col, out TCell? val)
    {
        if (col.Index < Count)
        {
            val = this[col.Index];
            return true;
        }
        val = default(TCell);
        return false;
    }

    public TableRow<TRowData, TCell> SetRowData(TRowData? data)
    {
        RowData = data;
        return this;
    }
}

public class TableRenderer<THeader,TRow, TCell> where THeader : IHeader, new()
{
    TableRow<TRow, TCell>? currentRow = null;

    public List<THeader> Columns { get; } = new();
    public List<TableRow<TRow, TCell>> Rows { get; } = new();

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
        if (currentRow == null)
        {
            currentRow = new();
            Rows.Add(currentRow);
        }

        currentRow.Add(obj);

    }

    public TableRow<TRow, TCell> WriteRow()
    {
        if (currentRow == null) throw new InvalidOperationException();
        var row = currentRow;
        currentRow = null;
        return row;
    }

    public TableRow<TRow, TCell> WriteRow(params TCell[] row) => WriteRow(row.AsSpan());
    public TableRow<TRow, TCell> WriteRow(ReadOnlySpan<TCell> row)
    {
        foreach(var cell in row)
        {
            WriteCell(cell);
        }
        return WriteRow();
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

    public virtual void CalcColumnSizes()
    {
        // Ensure Column definition
        while(Columns.Count < ColumnsCount)
        {
            Columns.Add(new THeader());
        }
        for(var idx=0; idx<Columns.Count; idx++)
        {
            var col = Columns[idx];
            col.Index = idx;

            var max = 0;
            var header =  (idx < Columns.Count) ? Columns[idx] : (THeader?)default(THeader);
            if (header != null && header.SizeMin != null) max = header.SizeMin.Value;
            foreach(var cell in GetColumnCells(idx))
            {
                var len = cell?.ToString()?.Length ?? 0;
                if (len > max)
                    max = len;
            }

            col.SizeContent = max;
            if (col.Size == null)
            {
                if (col.SizeMin != null && max < col.SizeMin.Value)
                {
                    col.Size = col.SizeMin.Value;
                }
                else if (col.SizeMax != null && max > col.SizeMax.Value)
                {
                    col.Size = col.SizeMax.Value;
                }
                else
                {
                    col.Size = max;
                }
            }
        }
    }

    public int[] GetColumnsSizes()
    {
        CalcColumnSizes();
        return Columns.Select(x=>x.Size ?? 0).ToArray();
    }

    // May be obsolete?
    public virtual
        IEnumerable<IReadOnlyList<(THeader Header, string CellText)>>
        RenderCells()
    {
        List<(THeader, string)> line = new ();
        var colSize = GetColumnsSizes();

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

