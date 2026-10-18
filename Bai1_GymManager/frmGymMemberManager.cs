using System.ComponentModel;
using System.Text.RegularExpressions;

namespace GymManager;

public class frmGymMemberManager : Form
{
    private readonly BindingList<MemberModel> _members = new();
    private readonly BindingSource _bs = new();
    private readonly ErrorProvider _err = new();
    private bool _isNew; // true: form đang ở chế độ nhập hội viên mới (đã tạm ngắt binding)

    private TextBox txtMemberId, txtFullName, txtPhone, txtFee;
    private ComboBox cboPackage;
    private DateTimePicker dtpStartDate;
    private Button btnAdd, btnEdit, btnDelete, btnRefresh;
    private DataGridView dgvMembers;

    public frmGymMemberManager()
    {
        BuildUi();
        ConfigureGrid();
        SeedData();

        _bs.DataSource = _members;
        dgvMembers.DataSource = _bs;

        BindControls();
        PrepareNew();
    }

    // ===================== GIAO DIỆN (hoàn toàn bằng code) =====================
    private void BuildUi()
    {
        Text = "Quản lý dịch vụ phòng Gym & Hội viên";
        Size = new Size(880, 620);
        MinimumSize = new Size(880, 560);
        StartPosition = FormStartPosition.CenterScreen;
        _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        var grpInfo = new GroupBox { Text = "Thông tin hội viên", Location = new Point(12, 10), Size = new Size(840, 150), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        txtMemberId = new TextBox { Name = "txtMemberId", ReadOnly = true, Location = new Point(120, 28), Width = 220 };
        txtFullName = new TextBox { Name = "txtFullName", Location = new Point(120, 62), Width = 220 };
        txtPhone = new TextBox { Name = "txtPhone", Location = new Point(120, 96), Width = 220, MaxLength = 10 };
        cboPackage = new ComboBox { Name = "cboPackage", Location = new Point(500, 28), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        cboPackage.Items.AddRange(new object[] { "Theo tháng", "6 tháng", "1 năm" });
        dtpStartDate = new DateTimePicker { Name = "dtpStartDate", Location = new Point(500, 62), Width = 220, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
        txtFee = new TextBox { Name = "txtFee", Location = new Point(500, 96), Width = 220 };

        AddLabel(grpInfo, "Mã hội viên:", 15, 31);
        AddLabel(grpInfo, "Họ và tên:", 15, 65);
        AddLabel(grpInfo, "Số điện thoại:", 15, 99);
        AddLabel(grpInfo, "Gói tập:", 395, 31);
        AddLabel(grpInfo, "Ngày đăng ký:", 395, 65);
        AddLabel(grpInfo, "Phí dịch vụ (VNĐ):", 395, 99);
        grpInfo.Controls.AddRange(new Control[] { txtMemberId, txtFullName, txtPhone, cboPackage, dtpStartDate, txtFee });

        var grpActions = new GroupBox { Text = "Chức năng", Location = new Point(12, 166), Size = new Size(840, 62), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        btnAdd = MakeButton("Thêm mới", 15, btnAdd_Click);
        btnEdit = MakeButton("Sửa thông tin", 150, btnEdit_Click);
        btnDelete = MakeButton("Xóa hội viên", 285, btnDelete_Click);
        btnRefresh = MakeButton("Làm mới", 420, btnRefresh_Click);
        grpActions.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });

        dgvMembers = new DataGridView
        {
            Name = "dgvMembers",
            Location = new Point(12, 238),
            Size = new Size(840, 335),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.AddRange(new Control[] { grpInfo, grpActions, dgvMembers });
        dgvMembers.SelectionChanged += dgvMembers_SelectionChanged;
    }

    private static void AddLabel(Control parent, string text, int x, int y)
        => parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });

    private static Button MakeButton(string text, int x, EventHandler onClick)
    {
        var b = new Button { Text = text, Location = new Point(x, 22), Size = new Size(125, 30) };
        b.Click += onClick;
        return b;
    }

    // ===================== CẤU HÌNH LƯỚI BẰNG CODE =====================
    private void ConfigureGrid()
    {
        dgvMembers.AutoGenerateColumns = false;   // Không sinh cột tự động
        dgvMembers.Columns.Clear();
        dgvMembers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvMembers.ReadOnly = true;
        dgvMembers.MultiSelect = false;
        dgvMembers.AllowUserToAddRows = false;
        dgvMembers.AllowUserToDeleteRows = false;
        dgvMembers.RowHeadersVisible = false;
        dgvMembers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMemberId", HeaderText = "Mã HV", DataPropertyName = nameof(MemberModel.MemberId), FillWeight = 12 });
        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFullName", HeaderText = "Tên hội viên", DataPropertyName = nameof(MemberModel.FullName), FillWeight = 28 });
        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "SĐT", DataPropertyName = nameof(MemberModel.Phone), FillWeight = 16 });
        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPackage", HeaderText = "Gói tập", DataPropertyName = nameof(MemberModel.Package), FillWeight = 14 });
        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colStartDate", HeaderText = "Ngày ĐK", DataPropertyName = nameof(MemberModel.StartDate), FillWeight = 14,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
        });
        dgvMembers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFee", HeaderText = "Phí dịch vụ", DataPropertyName = nameof(MemberModel.Fee), FillWeight = 16,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
    }

    private void SeedData()
    {
        _members.Add(new MemberModel { MemberId = "HV001", FullName = "Nguyễn Văn An", Phone = "0912345678", Package = "Theo tháng", StartDate = DateTime.Today.AddDays(-20), Fee = 300000 });
        _members.Add(new MemberModel { MemberId = "HV002", FullName = "Trần Thị Bình", Phone = "0987654321", Package = "6 tháng", StartDate = DateTime.Today.AddDays(-60), Fee = 1500000 });
        _members.Add(new MemberModel { MemberId = "HV003", FullName = "Lê Minh Cường", Phone = "0905123456", Package = "1 năm", StartDate = DateTime.Today.AddDays(-5), Fee = 2800000 });
    }

    // ===================== DATA BINDING 2 CHIỀU =====================
    // DataSourceUpdateMode.Never: Model -> Control tự động (chọn dòng là điền form);
    // Control -> Model chỉ ghi khi bấm "Sửa" (sau khi đã Validate) bằng Binding.WriteValue().
    private void BindControls()
    {
        Bind(txtMemberId, "Text", nameof(MemberModel.MemberId));
        Bind(txtFullName, "Text", nameof(MemberModel.FullName));
        Bind(txtPhone, "Text", nameof(MemberModel.Phone));
        Bind(cboPackage, "SelectedItem", nameof(MemberModel.Package));
        Bind(dtpStartDate, "Value", nameof(MemberModel.StartDate));
        Bind(txtFee, "Text", nameof(MemberModel.Fee));
    }

    private void Bind(Control c, string prop, string member)
        => c.DataBindings.Add(prop, _bs, member, true, DataSourceUpdateMode.Never);

    private void dgvMembers_SelectionChanged(object sender, EventArgs e)
    {
        // Chỉ xử lý khi người dùng chủ động chọn dòng trên lưới
        if (!dgvMembers.Focused || dgvMembers.CurrentRow == null) return;
        LeaveNewMode();
    }

    private void LeaveNewMode()
    {
        if (!_isNew) return;
        _bs.ResumeBinding();
        _bs.ResetCurrentItem();   // đẩy lại dữ liệu của dòng hiện tại lên các Control
        _isNew = false;
        _err.Clear();
    }

    private void PrepareNew()
    {
        _bs.SuspendBinding();
        _isNew = true;
        txtMemberId.Text = GenerateId();
        txtFullName.Clear();
        txtPhone.Clear();
        txtFee.Clear();
        cboPackage.SelectedIndex = 0;
        dtpStartDate.Value = DateTime.Today;
        dgvMembers.ClearSelection();
        _err.Clear();
        txtFullName.Focus();
    }

    private string GenerateId()
    {
        int max = 0;
        foreach (var m in _members)
            if (m.MemberId.StartsWith("HV") && int.TryParse(m.MemberId.Substring(2), out int n) && n > max) max = n;
        return "HV" + (max + 1).ToString("D3");
    }

    // ===================== VALIDATE (ErrorProvider) =====================
    private bool ValidateInput()
    {
        _err.Clear();
        bool ok = true;

        string name = txtFullName.Text.Trim();
        if (name.Length == 0)
        {
            _err.SetError(txtFullName, "Tên hội viên không được để trống.");
            ok = false;
        }
        else if (!Regex.IsMatch(name, @"^[\p{L}\s]+$"))
        {
            _err.SetError(txtFullName, "Tên không được chứa số hoặc ký tự đặc biệt.");
            ok = false;
        }

        if (!Regex.IsMatch(txtPhone.Text.Trim(), @"^[0-9]{10}$"))
        {
            _err.SetError(txtPhone, "Số điện thoại phải đúng 10 chữ số.");
            ok = false;
        }

        if (!int.TryParse(txtFee.Text.Trim(), out int fee) || fee <= 0)
        {
            _err.SetError(txtFee, "Phí dịch vụ phải là số nguyên dương.");
            ok = false;
        }

        if (cboPackage.SelectedIndex < 0)
        {
            _err.SetError(cboPackage, "Vui lòng chọn gói tập.");
            ok = false;
        }
        return ok;
    }

    // ===================== CÁC NÚT CHỨC NĂNG =====================
    private void btnAdd_Click(object sender, EventArgs e)
    {
        // Bấm khi đang xem hội viên cũ: chuyển sang chế độ nhập mới.
        // Bấm khi đang ở chế độ nhập mới: kiểm tra hợp lệ rồi lưu hội viên.
        if (!_isNew)
        {
            PrepareNew();
            return;
        }
        if (!ValidateInput()) return;

        var m = new MemberModel
        {
            MemberId = GenerateId(),
            FullName = txtFullName.Text.Trim(),
            Phone = txtPhone.Text.Trim(),
            Package = cboPackage.SelectedItem.ToString(),
            StartDate = dtpStartDate.Value.Date,
            Fee = int.Parse(txtFee.Text.Trim())
        };
        _members.Add(m);

        LeaveNewMode();
        _bs.Position = _members.Count - 1;
        dgvMembers.ClearSelection();
        dgvMembers.Rows[_bs.Position].Selected = true;
        MessageBox.Show("Đã thêm hội viên " + m.MemberId + ".", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnEdit_Click(object sender, EventArgs e)
    {
        if (_isNew || _bs.Current == null)
        {
            MessageBox.Show("Vui lòng chọn một hội viên trên lưới để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ValidateInput()) return;

        txtFullName.Text = txtFullName.Text.Trim();
        txtPhone.Text = txtPhone.Text.Trim();

        // Ghi thay đổi từ Control xuống BindingList<T>; lưới tự cập nhật nhờ INotifyPropertyChanged
        foreach (var c in new Control[] { txtFullName, txtPhone, cboPackage, dtpStartDate, txtFee })
            foreach (Binding b in c.DataBindings)
                b.WriteValue();
        _bs.EndEdit();

        MessageBox.Show("Đã cập nhật thông tin hội viên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_isNew || _bs.Current is not MemberModel m)
        {
            MessageBox.Show("Vui lòng chọn một hội viên trên lưới để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var rs = MessageBox.Show($"Bạn có chắc muốn xóa hội viên {m.MemberId} - {m.FullName}?",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rs != DialogResult.Yes) return;

        _bs.RemoveCurrent();
        if (_members.Count == 0) PrepareNew();
    }

    private void btnRefresh_Click(object sender, EventArgs e) => PrepareNew();
}
