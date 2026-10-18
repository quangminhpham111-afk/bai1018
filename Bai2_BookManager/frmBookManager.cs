using System.ComponentModel;
using System.Globalization;

namespace BookManager;

public class frmBookManager : Form
{
    private static readonly CultureInfo Vi = new("vi-VN");

    private readonly BindingList<BookModel> _books = new();
    private readonly BindingSource _bs = new();
    private readonly ErrorProvider _err = new();
    private string _coverPath = "";   // đường dẫn ảnh bìa đang chọn trên form

    private MenuStrip menuStrip;
    private ToolStrip toolStrip;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblCount, lblTotal, lblTime;
    private System.Windows.Forms.Timer timerClock;

    private TextBox txtId, txtTitle, txtAuthor;
    private NumericUpDown numPrice, numQty;
    private PictureBox picBookCover;
    private Button btnLoadCover, btnAdd, btnEdit, btnDelete;
    private DataGridView dgvBooks;

    public frmBookManager()
    {
        BuildUi();
        ConfigureGrid();

        _bs.DataSource = _books;
        dgvBooks.DataSource = _bs;
        _books.ListChanged += (s, e) => UpdateStatus();

        _books.Add(new BookModel { Id = "S001", Title = "Lập trình C# căn bản", Author = "Nguyễn Văn A", Price = 120000, Quantity = 10 });
        _books.Add(new BookModel { Id = "S002", Title = "Cấu trúc dữ liệu và giải thuật", Author = "Trần Thị B", Price = 150000, Quantity = 5 });
        _books.Add(new BookModel { Id = "S003", Title = "Cơ sở dữ liệu", Author = "Lê Văn C", Price = 98000, Quantity = 8 });

        UpdateStatus();
        timerClock.Start();
    }

    // ===================== GIAO DIỆN =====================
    private void BuildUi()
    {
        Text = "Quản lý sách & Xuất báo cáo nhà sách";
        Size = new Size(940, 700);
        MinimumSize = new Size(900, 640);
        StartPosition = FormStartPosition.CenterScreen;
        _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        // ---- Lưới (Fill) ----
        dgvBooks = new DataGridView { Name = "dgvBooks", Dock = DockStyle.Fill };
        dgvBooks.SelectionChanged += dgvBooks_SelectionChanged;

        // ---- Khu vực chi tiết (Top) ----
        var pnlDetail = new Panel { Dock = DockStyle.Top, Height = 215 };
        var grp = new GroupBox { Text = "Thông tin sách", Location = new Point(10, 8), Size = new Size(560, 200) };
        txtId = new TextBox { Name = "txtId", Location = new Point(110, 25), Width = 180 };
        txtTitle = new TextBox { Name = "txtTitle", Location = new Point(110, 55), Width = 430 };
        txtAuthor = new TextBox { Name = "txtAuthor", Location = new Point(110, 85), Width = 430 };
        numPrice = new NumericUpDown { Name = "numPrice", Location = new Point(110, 115), Width = 180, Maximum = 1_000_000_000, Increment = 1000, ThousandsSeparator = true };
        numQty = new NumericUpDown { Name = "numQty", Location = new Point(400, 115), Width = 140, Maximum = 1_000_000 };
        AddLabel(grp, "Mã sách:", 15, 28);
        AddLabel(grp, "Tên sách:", 15, 58);
        AddLabel(grp, "Tác giả:", 15, 88);
        AddLabel(grp, "Đơn giá (VNĐ):", 15, 118);
        AddLabel(grp, "Số lượng:", 330, 118);

        btnAdd = MakeButton("Thêm", 110, btnAdd_Click);
        btnEdit = MakeButton("Sửa", 215, btnEdit_Click);
        btnDelete = MakeButton("Xóa", 320, btnDelete_Click);
        grp.Controls.AddRange(new Control[] { txtId, txtTitle, txtAuthor, numPrice, numQty, btnAdd, btnEdit, btnDelete });

        var grpCover = new GroupBox { Text = "Ảnh bìa", Location = new Point(580, 8), Size = new Size(200, 200) };
        picBookCover = new PictureBox { Name = "picBookCover", Location = new Point(25, 22), Size = new Size(150, 135), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
        btnLoadCover = new Button { Name = "btnLoadCover", Text = "Tải ảnh bìa", Location = new Point(25, 164), Size = new Size(150, 28) };
        btnLoadCover.Click += btnLoadCover_Click;
        grpCover.Controls.AddRange(new Control[] { picBookCover, btnLoadCover });

        pnlDetail.Controls.AddRange(new Control[] { grp, grpCover });

        // ---- StatusStrip (Bottom) ----
        statusStrip = new StatusStrip();
        lblCount = new ToolStripStatusLabel { BorderSides = ToolStripStatusLabelBorderSides.Right };
        lblTotal = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        lblTime = new ToolStripStatusLabel { Alignment = ToolStripItemAlignment.Right };
        statusStrip.Items.AddRange(new ToolStripItem[] { lblCount, lblTotal, lblTime });

        timerClock = new System.Windows.Forms.Timer { Interval = 1000 };
        timerClock.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        lblTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        // ---- ToolStrip ----
        toolStrip = new ToolStrip { ImageScalingSize = new Size(24, 24) };
        toolStrip.Items.Add(MakeToolButton("Mở file", MakeIcon("O", Color.SteelBlue), (s, e) => OpenCsv()));
        toolStrip.Items.Add(MakeToolButton("Lưu file", MakeIcon("S", Color.SeaGreen), (s, e) => SaveCsv()));
        toolStrip.Items.Add(MakeToolButton("Thêm mới", MakeIcon("+", Color.DarkOrange), (s, e) => NewBook()));

        // ---- MenuStrip ----
        menuStrip = new MenuStrip();
        var mFile = new ToolStripMenuItem("File");
        mFile.DropDownItems.Add(MakeMenu("Mở file CSV", Keys.Control | Keys.O, (s, e) => OpenCsv()));
        mFile.DropDownItems.Add(MakeMenu("Lưu file CSV", Keys.Control | Keys.S, (s, e) => SaveCsv()));
        mFile.DropDownItems.Add(new ToolStripSeparator());
        mFile.DropDownItems.Add(MakeMenu("Thoát", Keys.Alt | Keys.F4, (s, e) => Close()));
        var mEdit = new ToolStripMenuItem("Edit");
        mEdit.DropDownItems.Add(MakeMenu("Thêm", Keys.Control | Keys.N, (s, e) => NewBook()));
        mEdit.DropDownItems.Add(MakeMenu("Xóa", Keys.Delete, (s, e) => DeleteBook()));
        mEdit.DropDownItems.Add(MakeMenu("Tìm kiếm", Keys.Control | Keys.F, (s, e) => SearchBook()));
        menuStrip.Items.AddRange(new ToolStripItem[] { mFile, mEdit });

        // Thứ tự Add quan trọng với Dock: control add sau cùng được dock ngoài cùng
        Controls.Add(dgvBooks);
        Controls.Add(pnlDetail);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
    }

    private static void AddLabel(Control parent, string text, int x, int y)
        => parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });

    private static Button MakeButton(string text, int x, EventHandler onClick)
    {
        var b = new Button { Text = text, Location = new Point(x, 155), Size = new Size(95, 30) };
        b.Click += onClick;
        return b;
    }

    private static ToolStripButton MakeToolButton(string text, Image icon, EventHandler onClick)
    {
        var b = new ToolStripButton(text, icon, onClick) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, TextImageRelation = TextImageRelation.ImageBeforeText };
        return b;
    }

    private static ToolStripMenuItem MakeMenu(string text, Keys shortcut, EventHandler onClick)
        => new(text, null, onClick) { ShortcutKeys = shortcut };

    /// <summary>Tạo icon vuông bo tròn có ký tự ở giữa (không cần file ảnh ngoài).</summary>
    private static Bitmap MakeIcon(string glyph, Color color)
    {
        var bmp = new Bitmap(24, 24);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 22, 22);
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        var size = g.MeasureString(glyph, font);
        g.DrawString(glyph, font, Brushes.White, (24 - size.Width) / 2, (24 - size.Height) / 2);
        return bmp;
    }

    private void ConfigureGrid()
    {
        dgvBooks.AutoGenerateColumns = false;
        dgvBooks.Columns.Clear();
        dgvBooks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvBooks.ReadOnly = true;
        dgvBooks.MultiSelect = false;
        dgvBooks.AllowUserToAddRows = false;
        dgvBooks.RowHeadersVisible = false;
        dgvBooks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã sách", DataPropertyName = nameof(BookModel.Id), FillWeight = 10 });
        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên sách", DataPropertyName = nameof(BookModel.Title), FillWeight = 32 });
        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tác giả", DataPropertyName = nameof(BookModel.Author), FillWeight = 20 });
        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Đơn giá", DataPropertyName = nameof(BookModel.Price), FillWeight = 12,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "SL", DataPropertyName = nameof(BookModel.Quantity), FillWeight = 7,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ảnh bìa", DataPropertyName = nameof(BookModel.CoverFileName), FillWeight = 19 });
    }

    // ===================== STATUS STRIP =====================
    private void UpdateStatus()
    {
        lblCount.Text = $"Tổng số đầu sách: {_books.Count}";
        decimal total = _books.Sum(b => b.TotalValue);
        lblTotal.Text = $"Tổng giá trị kho: {total.ToString("N0", Vi)} VNĐ";
    }

    // ===================== CHỌN DÒNG -> ĐIỀN FORM =====================
    private void dgvBooks_SelectionChanged(object sender, EventArgs e)
    {
        if (dgvBooks.CurrentRow?.DataBoundItem is not BookModel b) return;
        txtId.Text = b.Id;
        txtTitle.Text = b.Title;
        txtAuthor.Text = b.Author;
        numPrice.Value = Math.Min(b.Price, numPrice.Maximum);
        numQty.Value = Math.Min(b.Quantity, numQty.Maximum);
        _coverPath = b.CoverPath ?? "";
        ShowCover(_coverPath);
        _err.Clear();
    }

    private void ShowCover(string path)
    {
        picBookCover.Image?.Dispose();
        picBookCover.Image = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            // Đọc qua MemoryStream để không khóa file ảnh trên đĩa
            using var ms = new MemoryStream(File.ReadAllBytes(path));
            using var img = Image.FromStream(ms);
            picBookCover.Image = new Bitmap(img);
        }
        catch { /* file ảnh hỏng: bỏ qua */ }
    }

    // ===================== ẢNH BÌA (OpenFileDialog) =====================
    private void btnLoadCover_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn ảnh bìa sách",
            Filter = "Ảnh (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        _coverPath = ofd.FileName;
        ShowCover(_coverPath);

        // Nếu đang chọn đúng một sách trong danh sách thì lưu luôn đường dẫn vào đối tượng sách
        if (_bs.Current is BookModel cur && cur.Id == txtId.Text.Trim())
        {
            cur.CoverPath = _coverPath;
            _bs.ResetCurrentItem();
        }
    }

    // ===================== THÊM / SỬA / XÓA =====================
    private bool ValidateInput(bool isAdd)
    {
        _err.Clear();
        bool ok = true;
        string id = txtId.Text.Trim();

        if (id.Length == 0)
        {
            _err.SetError(txtId, "Mã sách không được để trống.");
            ok = false;
        }
        else
        {
            var current = _bs.Current as BookModel;
            bool duplicate = _books.Any(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && (isAdd || !ReferenceEquals(b, current)));
            if (duplicate)
            {
                _err.SetError(txtId, "Mã sách đã tồn tại.");
                ok = false;
            }
        }
        if (txtTitle.Text.Trim().Length == 0)
        {
            _err.SetError(txtTitle, "Tên sách không được để trống.");
            ok = false;
        }
        return ok;
    }

    /// <summary>Menu Edit/Thêm và nút ToolStrip: xóa trắng form để nhập sách mới.</summary>
    private void NewBook()
    {
        _err.Clear();
        txtId.Text = "S" + (NextNumber() + 1).ToString("D3");
        txtTitle.Clear();
        txtAuthor.Clear();
        numPrice.Value = 0;
        numQty.Value = 0;
        _coverPath = "";
        ShowCover("");
        dgvBooks.ClearSelection();
        txtTitle.Focus();
    }

    private int NextNumber()
    {
        int max = 0;
        foreach (var b in _books)
            if (b.Id.Length > 1 && b.Id[0] == 'S' && int.TryParse(b.Id.AsSpan(1), out int n) && n > max) max = n;
        return max;
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        if (!ValidateInput(true)) return;
        var book = new BookModel
        {
            Id = txtId.Text.Trim(),
            Title = txtTitle.Text.Trim(),
            Author = txtAuthor.Text.Trim(),
            Price = numPrice.Value,
            Quantity = (int)numQty.Value,
            CoverPath = _coverPath
        };
        _books.Add(book);
        _bs.Position = _books.Count - 1;
        dgvBooks.ClearSelection();
        dgvBooks.Rows[_bs.Position].Selected = true;
    }

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (_bs.Current is not BookModel b || dgvBooks.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn một sách trên lưới để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ValidateInput(false)) return;

        b.Id = txtId.Text.Trim();
        b.Title = txtTitle.Text.Trim();
        b.Author = txtAuthor.Text.Trim();
        b.Price = numPrice.Value;
        b.Quantity = (int)numQty.Value;
        b.CoverPath = _coverPath;
        _bs.ResetCurrentItem();   // làm tươi dòng trên lưới
        UpdateStatus();           // ResetCurrentItem không đi qua BindingList nên cập nhật thủ công
    }

    private void btnDelete_Click(object sender, EventArgs e) => DeleteBook();

    private void DeleteBook()
    {
        if (_bs.Current is not BookModel b || dgvBooks.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn một sách trên lưới để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var rs = MessageBox.Show($"Xóa sách \"{b.Title}\"?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rs != DialogResult.Yes) return;
        _bs.RemoveCurrent();
    }

    // ===================== TÌM KIẾM =====================
    private string _lastKeyword = "";

    private void SearchBook()
    {
        string kw = InputBox.Show("Tìm kiếm", "Nhập mã sách, tên sách hoặc tác giả:", _lastKeyword);
        if (string.IsNullOrWhiteSpace(kw)) return;
        _lastKeyword = kw.Trim();

        int start = _bs.Position + 1;
        for (int k = 0; k < _books.Count; k++)
        {
            int i = (start + k) % _books.Count;
            var b = _books[i];
            if (Contains(b.Id, _lastKeyword) || Contains(b.Title, _lastKeyword) || Contains(b.Author, _lastKeyword))
            {
                _bs.Position = i;
                dgvBooks.ClearSelection();
                dgvBooks.Rows[i].Selected = true;
                dgvBooks.CurrentCell = dgvBooks.Rows[i].Cells[0];
                return;
            }
        }
        MessageBox.Show("Không tìm thấy kết quả phù hợp.", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool Contains(string s, string kw)
        => (s ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase);

    // ===================== FILE CSV (OpenFileDialog / SaveFileDialog) =====================
    private void SaveCsv()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Lưu danh sách sách",
            Filter = "CSV (*.csv)|*.csv",
            DefaultExt = "csv",
            FileName = "danh_sach_sach.csv",
            OverwritePrompt = true
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            CsvHelper.Write(sfd.FileName, _books);
            MessageBox.Show($"Đã lưu {_books.Count} đầu sách vào:\n{sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenCsv()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Mở file danh sách sách",
            Filter = "CSV (*.csv)|*.csv",
            CheckFileExists = true
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var list = CsvHelper.Read(ofd.FileName);   // đọc hết rồi mới thay đổi dữ liệu hiện tại
            _books.Clear();
            foreach (var b in list) _books.Add(b);
            UpdateStatus();
            if (_books.Count > 0)
            {
                _bs.Position = 0;
                dgvBooks.ClearSelection();
                dgvBooks.Rows[0].Selected = true;
            }
            else NewBook();
        }
        catch (Exception ex)
        {
            MessageBox.Show("File CSV không hợp lệ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timerClock?.Stop();
        base.OnFormClosed(e);
    }
}
