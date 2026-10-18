namespace HospitalMDI;

/// <summary>Hộp thoại nhập tên Bác sĩ/Sĩ quan trực (hiển thị lên StatusStrip).</summary>
public class frmLogin : Form
{
    private readonly TextBox _txtName = new() { Location = new Point(15, 38), Width = 290 };
    public string DutyName => _txtName.Text.Trim();

    public frmLogin()
    {
        Text = "Đăng nhập ca trực";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(320, 115);

        var lbl = new Label { Text = "Tên Bác sĩ / Sĩ quan trực:", Location = new Point(15, 15), AutoSize = true };
        _txtName.Text = "BS. Nguyễn Hồng Phúc";
        var ok = new Button { Text = "Vào hệ thống", Location = new Point(110, 72), Size = new Size(100, 28) };
        var cancel = new Button { Text = "Thoát", DialogResult = DialogResult.Cancel, Location = new Point(220, 72), Size = new Size(85, 28) };
        ok.Click += (s, e) =>
        {
            if (DutyName.Length == 0)
            {
                MessageBox.Show("Vui lòng nhập tên người trực.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult = DialogResult.OK;
        };
        Controls.AddRange(new Control[] { lbl, _txtName, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
