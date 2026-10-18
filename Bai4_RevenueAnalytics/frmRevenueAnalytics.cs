using System.ComponentModel;
using System.Globalization;

namespace RevenueAnalytics;

public class frmRevenueAnalytics : Form
{
    private static readonly CultureInfo Vi = new("vi-VN");

    private DateTimePicker dtpFrom, dtpTo;
    private ComboBox cboBranch;
    private Button btnAnalyze, btnCancel;
    private ProgressBar pgbLoading;
    private Label lblProcessingStatus, lblTotal;
    private TextBox txtTestInput;
    private DataGridView dgvRevenueData;

    private CancellationTokenSource _cts;

    public frmRevenueAnalytics()
    {
        BuildUi();
        ConfigureGrid();
        SetBusy(false);
    }

    // ===================== GIAO DIỆN =====================
    private void BuildUi()
    {
        Text = "Bảng điều khiển thống kê doanh thu";
        Size = new Size(960, 640);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;

        // ---- Tiêu chí ----
        var grpFilter = new GroupBox { Text = "Tiêu chí thống kê", Dock = DockStyle.Top, Height = 105, Padding = new Padding(8) };
        dtpFrom = new DateTimePicker { Location = new Point(85, 28), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
        dtpTo = new DateTimePicker { Location = new Point(300, 28), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        cboBranch = new ComboBox { Location = new Point(520, 28), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        cboBranch.Items.Add("Tất cả");
        cboBranch.Items.AddRange(RevenueService.Branches);
        cboBranch.SelectedIndex = 0;
        btnAnalyze = new Button { Name = "btnAnalyze", Text = "Thống Kê Doanh Thu", Location = new Point(15, 62), Size = new Size(160, 30) };
        btnCancel = new Button { Name = "btnCancel", Text = "Hủy Thao Tác", Location = new Point(185, 62), Size = new Size(130, 30) };
        btnAnalyze.Click += btnAnalyze_Click;
        btnCancel.Click += btnCancel_Click;

        // Ô nhập tự do: dùng để kiểm chứng UI không bị đơ trong lúc xử lý
        txtTestInput = new TextBox { Location = new Point(520, 66), Width = 380, PlaceholderText = "Gõ thử vào đây khi đang thống kê..." };
        AddLabel(grpFilter, "Từ ngày:", 15, 32);
        AddLabel(grpFilter, "Đến ngày:", 230, 32);
        AddLabel(grpFilter, "Chi nhánh:", 445, 32);
        AddLabel(grpFilter, "Ô nhập thử:", 445, 69);
        // ô nhập thử nằm cạnh nhãn; đẩy sang phải một chút cho khỏi chồng chữ
        txtTestInput.Left = 525;
        grpFilter.Controls.AddRange(new Control[] { dtpFrom, dtpTo, cboBranch, btnAnalyze, btnCancel, txtTestInput });

        // ---- Trạng thái ----
        var pnlStatus = new Panel { Dock = DockStyle.Top, Height = 62 };
        pgbLoading = new ProgressBar { Name = "pgbLoading", Location = new Point(12, 8), Size = new Size(920, 22), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Minimum = 0, Maximum = 100 };
        lblProcessingStatus = new Label { Name = "lblProcessingStatus", Location = new Point(12, 36), AutoSize = true, Text = "Sẵn sàng." };
        pnlStatus.Controls.AddRange(new Control[] { pgbLoading, lblProcessingStatus });

        // ---- Tổng kết (Bottom) ----
        lblTotal = new Label { Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10, FontStyle.Bold), Padding = new Padding(10, 0, 0, 0) };

        // ---- Lưới (Fill) ----
        dgvRevenueData = new DataGridView { Name = "dgvRevenueData", Dock = DockStyle.Fill };

        // Thứ tự Add: Fill trước, rồi Bottom, rồi các Top (cuối cùng nằm trên cùng)
        Controls.Add(dgvRevenueData);
        Controls.Add(lblTotal);
        Controls.Add(pnlStatus);
        Controls.Add(grpFilter);

        FormClosing += (s, e) => _cts?.Cancel();   // đóng form giữa chừng thì dừng tiến trình ngầm
    }

    private static void AddLabel(Control parent, string text, int x, int y)
        => parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });

    private void ConfigureGrid()
    {
        dgvRevenueData.AutoGenerateColumns = false;
        dgvRevenueData.Columns.Clear();
        dgvRevenueData.ReadOnly = true;
        dgvRevenueData.AllowUserToAddRows = false;
        dgvRevenueData.RowHeadersVisible = false;
        dgvRevenueData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvRevenueData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgvRevenueData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã GD", DataPropertyName = nameof(TransactionModel.TransactionId), FillWeight = 14 });
        dgvRevenueData.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày giao dịch", DataPropertyName = nameof(TransactionModel.Date), FillWeight = 22,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });
        dgvRevenueData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chi nhánh", DataPropertyName = nameof(TransactionModel.Branch), FillWeight = 16 });
        dgvRevenueData.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Khách hàng", DataPropertyName = nameof(TransactionModel.Customer), FillWeight = 28 });
        dgvRevenueData.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Doanh thu (VNĐ)", DataPropertyName = nameof(TransactionModel.Amount), FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
    }

    private void SetBusy(bool busy)
    {
        btnAnalyze.Enabled = !busy;
        btnCancel.Enabled = busy;
        dtpFrom.Enabled = !busy;
        dtpTo.Enabled = !busy;
        cboBranch.Enabled = !busy;
        // txtTestInput và bản thân Form luôn hoạt động để chứng minh UI không bị khóa
    }

    // ===================== THỐNG KÊ BẤT ĐỒNG BỘ =====================
    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        if (dtpFrom.Value.Date > dtpTo.Value.Date)
        {
            MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DateTime from = dtpFrom.Value.Date, to = dtpTo.Value.Date;   // đọc Control trên UI Thread, truyền giá trị thuần xuống thread nền
        string branch = cboBranch.SelectedItem.ToString();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Progress<T> được tạo trên UI Thread nên callback luôn chạy trên UI Thread
        var progress = new Progress<(int Percent, string Message)>(p =>
        {
            if (_cts == null) return;   // callback đến muộn sau khi tác vụ đã kết thúc
            pgbLoading.Value = p.Percent;
            lblProcessingStatus.Text = p.Message;
        });

        dgvRevenueData.DataSource = null;
        lblTotal.Text = "";
        pgbLoading.Value = 0;
        lblProcessingStatus.Text = "Đang truy xuất dữ liệu...";
        SetBusy(true);

        try
        {
            // Đẩy toàn bộ việc nặng xuống Background Thread; UI Thread được giải phóng ở await
            List<TransactionModel> data = await Task.Run(
                () => RevenueService.Generate(from, to, branch, progress, token), token);

            // Quay lại UI Thread sau await: được phép đụng vào Control
            dgvRevenueData.DataSource = new BindingList<TransactionModel>(data);
            decimal total = data.Sum(t => t.Amount);
            lblTotal.Text = $"Tổng doanh thu: {total.ToString("N0", Vi)} VNĐ  |  {data.Count:N0} giao dịch";
            lblProcessingStatus.Text = "Hoàn tất thống kê!";
        }
        catch (OperationCanceledException)
        {
            pgbLoading.Value = 0;
            lblProcessingStatus.Text = "Đã hủy thao tác!";
            MessageBox.Show("Đã hủy thao tác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            pgbLoading.Value = 0;
            lblProcessingStatus.Text = "Có lỗi xảy ra.";
            MessageBox.Show("Lỗi khi thống kê: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        btnCancel.Enabled = false;   // tránh bấm nhiều lần
        _cts?.Cancel();
    }
}
