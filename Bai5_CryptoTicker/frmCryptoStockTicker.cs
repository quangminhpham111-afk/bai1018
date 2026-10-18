using System.ComponentModel;

namespace CryptoTicker;

public class frmCryptoStockTicker : Form
{
    private const int MaxLogRows = 500;

    private Button btnStart, btnPause;
    private Label lblBTCPrice, lblChange;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblLastUpdate, lblSyncCount, lblState;
    private DataGridView dgvHistoryLog;

    private readonly BindingList<HistoryRow> _history = new();

    private CancellationTokenSource _cts;
    private Task _loopTask = Task.CompletedTask;

    // Chỉ được truy cập từ thread nền đang chạy vòng lặp (mỗi lúc chỉ có 1 vòng lặp)
    private decimal _price = 65000m;
    private readonly Random _rng = new();

    // Chỉ được truy cập trên UI Thread
    private int _syncCount;

    public frmCryptoStockTicker()
    {
        BuildUi();
        ConfigureGrid();
        dgvHistoryLog.DataSource = _history;
        lblBTCPrice.Text = FormatPrice(_price);
        SetRunning(false);
    }

    // ===================== GIAO DIỆN =====================
    private void BuildUi()
    {
        Text = "Giám sát tỷ giá thời gian thực";
        Size = new Size(760, 600);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterScreen;

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 150 };

        btnStart = new Button { Name = "btnStart", Text = "Bắt đầu Giám sát", Location = new Point(15, 12), Size = new Size(150, 32) };
        btnPause = new Button { Name = "btnPause", Text = "Tạm dừng", Location = new Point(175, 12), Size = new Size(120, 32) };
        btnStart.Click += btnStart_Click;
        btnPause.Click += btnPause_Click;

        var lblCaption = new Label { Text = "BTC / USD", Location = new Point(15, 58), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        lblBTCPrice = new Label
        {
            Name = "lblBTCPrice",
            Location = new Point(15, 80),
            AutoSize = true,
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = SystemColors.ControlText
        };
        lblChange = new Label { Location = new Point(300, 100), AutoSize = true, Font = new Font("Segoe UI", 11) };

        pnlTop.Controls.AddRange(new Control[] { btnStart, btnPause, lblCaption, lblBTCPrice, lblChange });

        // StatusStrip
        statusStrip = new StatusStrip();
        lblState = new ToolStripStatusLabel("Đang dừng") { BorderSides = ToolStripStatusLabelBorderSides.Right };
        lblLastUpdate = new ToolStripStatusLabel("Cập nhật lần cuối: --:--:--") { BorderSides = ToolStripStatusLabelBorderSides.Right };
        lblSyncCount = new ToolStripStatusLabel("Số lượt đồng bộ thành công: 0");
        statusStrip.Items.AddRange(new ToolStripItem[] { lblState, lblLastUpdate, lblSyncCount });

        dgvHistoryLog = new DataGridView { Name = "dgvHistoryLog", Dock = DockStyle.Fill };
        dgvHistoryLog.CellFormatting += dgvHistoryLog_CellFormatting;

        // Fill trước, Bottom, rồi Top
        Controls.Add(dgvHistoryLog);
        Controls.Add(statusStrip);
        Controls.Add(pnlTop);

        FormClosing += (s, e) => _cts?.Cancel();   // dừng vòng lặp nền khi đóng form (không chờ để tránh deadlock)
    }

    private void ConfigureGrid()
    {
        dgvHistoryLog.AutoGenerateColumns = false;
        dgvHistoryLog.Columns.Clear();
        dgvHistoryLog.ReadOnly = true;
        dgvHistoryLog.AllowUserToAddRows = false;
        dgvHistoryLog.RowHeadersVisible = false;
        dgvHistoryLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvHistoryLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var right = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight };
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "Thời gian", DataPropertyName = nameof(HistoryRow.Time), FillWeight = 20 });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colPrice", HeaderText = "Giá BTC/USD", DataPropertyName = nameof(HistoryRow.Price), FillWeight = 25,
            DefaultCellStyle = new DataGridViewCellStyle(right) { Format = "N2" }
        });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colChange", HeaderText = "Biến động", DataPropertyName = nameof(HistoryRow.Change), FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle(right) { Format = "+#,##0.00;-#,##0.00;0.00" }
        });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colPercent", HeaderText = "% thay đổi", DataPropertyName = nameof(HistoryRow.ChangePercent), FillWeight = 17,
            DefaultCellStyle = new DataGridViewCellStyle(right) { Format = "+0.00\\%;-0.00\\%;0.00\\%" }
        });
        dgvHistoryLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTrend", HeaderText = "Xu hướng", DataPropertyName = nameof(HistoryRow.Trend), FillWeight = 18 });
    }

    private void dgvHistoryLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _history.Count) return;
        string col = dgvHistoryLog.Columns[e.ColumnIndex].Name;
        if (col is "colChange" or "colPercent" or "colTrend")
        {
            decimal change = _history[e.RowIndex].Change;
            e.CellStyle.ForeColor = change > 0 ? Color.Green : change < 0 ? Color.Red : Color.Black;
        }
    }

    private static string FormatPrice(decimal p) => p.ToString("N2") + " USD";

    private void SetRunning(bool running)
    {
        btnStart.Enabled = !running;
        btnPause.Enabled = running;
        lblState.Text = running ? "Đang giám sát..." : "Đang dừng";
    }

    // ===================== ĐIỀU KHIỂN =====================
    private void btnStart_Click(object sender, EventArgs e)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        SetRunning(true);
        // Vòng lặp vô hạn chạy trên Background Thread
        _loopTask = Task.Run(() => MonitorLoop(token), token);
    }

    private async void btnPause_Click(object sender, EventArgs e)
    {
        btnPause.Enabled = false;
        _cts?.Cancel();
        try
        {
            await _loopTask;              // await không chặn UI Thread nên không gây deadlock với Invoke
        }
        catch (OperationCanceledException) { }

        _cts?.Dispose();
        _cts = null;
        SetRunning(false);
    }

    // ===================== BACKGROUND THREAD =====================
    private void MonitorLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Sinh tỷ giá ngẫu nhiên: biến động tối đa ±1% mỗi lần quét
            decimal old = _price;
            decimal pct = (decimal)(_rng.NextDouble() * 2 - 1) / 100m;
            _price = Math.Round(Math.Max(1000m, old * (1 + pct)), 2);

            var tick = new PriceTick(DateTime.Now, _price, _price - old, old == 0 ? 0 : (_price - old) / old * 100m);

            // TUYỆT ĐỐI không đụng vào Control ở đây: chuyển hết sang UI Thread
            UpdateUi(tick);

            // Chờ 2 giây; trả về true ngay khi bị hủy -> thoát vòng lặp lập tức
            if (token.WaitHandle.WaitOne(2000)) break;
        }
    }

    // ===================== CẬP NHẬT UI AN TOÀN (CROSS-THREAD) =====================
    private void UpdateUi(PriceTick tick)
    {
        if (IsDisposed || !IsHandleCreated) return;

        if (this.InvokeRequired)
        {
            try
            {
                // Đẩy hành động cập nhật giao diện về Main UI Thread
                this.Invoke(new Action(() => UpdateUi(tick)));
            }
            catch (ObjectDisposedException) { }       // form vừa đóng
            catch (InvalidOperationException) { }     // handle đã bị hủy khi đang Invoke
            return;
        }

        // ---- Từ đây chắc chắn đang ở UI Thread ----
        lblBTCPrice.Text = FormatPrice(tick.Price);
        if (tick.Change > 0) lblBTCPrice.ForeColor = Color.Green;       // tăng: xanh
        else if (tick.Change < 0) lblBTCPrice.ForeColor = Color.Red;    // giảm: đỏ

        lblChange.Text = $"{tick.Change:+#,##0.00;-#,##0.00;0.00} ({tick.ChangePercent:+0.00;-0.00;0.00}%)";
        lblChange.ForeColor = lblBTCPrice.ForeColor;

        _history.Insert(0, new HistoryRow
        {
            Time = tick.Time.ToString("HH:mm:ss"),
            Price = tick.Price,
            Change = tick.Change,
            ChangePercent = tick.ChangePercent,
            Trend = tick.Change > 0 ? "▲ Tăng" : tick.Change < 0 ? "▼ Giảm" : "■ Đi ngang"
        });
        while (_history.Count > MaxLogRows) _history.RemoveAt(_history.Count - 1);

        _syncCount++;
        lblLastUpdate.Text = "Cập nhật lần cuối: " + tick.Time.ToString("HH:mm:ss");
        lblSyncCount.Text = "Số lượt đồng bộ thành công: " + _syncCount;
    }
}
