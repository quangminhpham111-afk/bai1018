using System.Globalization;
using System.Text;

namespace BookManager;

/// <summary>Đọc/ghi CSV thủ công (xử lý đúng dấu phẩy và dấu nháy kép trong dữ liệu).</summary>
public static class CsvHelper
{
    private const string Header = "Id,Title,Author,Price,Quantity,CoverPath";

    public static void Write(string path, IEnumerable<BookModel> books)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var b in books)
        {
            sb.AppendLine(string.Join(",",
                Escape(b.Id),
                Escape(b.Title),
                Escape(b.Author),
                b.Price.ToString(CultureInfo.InvariantCulture),
                b.Quantity.ToString(CultureInfo.InvariantCulture),
                Escape(b.CoverPath)));
        }
        // UTF-8 có BOM để Excel mở không lỗi tiếng Việt
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    public static List<BookModel> Read(string path)
    {
        var result = new List<BookModel>();
        int lineNo = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var f = ParseLine(line);
            if (lineNo == 1 && f.Count > 0 && f[0].Trim().Equals("Id", StringComparison.OrdinalIgnoreCase))
                continue; // bỏ dòng tiêu đề

            if (f.Count < 5)
                throw new FormatException($"Dòng {lineNo} không đủ cột (cần tối thiểu 5 cột).");

            if (!decimal.TryParse(f[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                throw new FormatException($"Dòng {lineNo}: giá không hợp lệ ({f[3]}).");
            if (!int.TryParse(f[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty))
                throw new FormatException($"Dòng {lineNo}: số lượng không hợp lệ ({f[4]}).");

            result.Add(new BookModel
            {
                Id = f[0].Trim(),
                Title = f[1],
                Author = f[2],
                Price = price,
                Quantity = qty,
                CoverPath = f.Count > 5 ? f[5] : ""
            });
        }
        return result;
    }

    private static string Escape(string s)
    {
        s ??= "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cur.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
        }
        fields.Add(cur.ToString());
        return fields;
    }
}

/// <summary>Hộp thoại nhập một dòng văn bản (dựng bằng code).</summary>
public static class InputBox
{
    public static string Show(string title, string prompt, string defaultValue = "")
    {
        using var f = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(360, 110)
        };
        var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        var txt = new TextBox { Text = defaultValue, Location = new Point(12, 36), Width = 336 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(192, 70), Size = new Size(75, 28) };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(273, 70), Size = new Size(75, 28) };
        f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }
}
