namespace HospitalMDI;

public class frmHospitalMain : Form
{
    private MenuStrip menuMain;
    private StatusStrip statusMain;
    private ToolStripStatusLabel lblDoctor, lblInfo;
    private ToolStripProgressBar progressBar;
    private ToolStripMenuItem mnuReload, mnuManage, mnuWindow;
    private System.Windows.Forms.Timer timerProgress;

    public frmHospitalMain()
    {
        BuildUi();
        Shown += (s, e) => DoLogin(firstTime: true);
    }

    // ===================== GIAO DIỆN =====================
    private void BuildUi()
    {
        Text = "Hệ thống quản lý bệnh viện (MDI)";
        IsMdiContainer = true;                 // Form cha MDI
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(800, 560);

        // ---- MenuStrip ----
        menuMain = new MenuStrip();

        var mnuSystem = new ToolStripMenuItem("Hệ thống");
        mnuSystem.DropDownItems.Add(new ToolStripMenuItem("Đăng xuất", null, (s, e) => Logout()));
        mnuReload = new ToolStripMenuItem("Nạp lại dữ liệu", null, (s, e) => StartReload()) { ShortcutKeys = Keys.F5 };
        mnuSystem.DropDownItems.Add(mnuReload);
        mnuSystem.DropDownItems.Add(new ToolStripSeparator());
        mnuSystem.DropDownItems.Add(new ToolStripMenuItem("Thoát", null, (s, e) => Close()));

        mnuManage = new ToolStripMenuItem("Quản lý");
        mnuManage.DropDownItems.Add(new ToolStripMenuItem("Hồ sơ Bệnh nhân", null, (s, e) => OpenChild<frmPatientProfiles>()));
        mnuManage.DropDownItems.Add(new ToolStripMenuItem("Danh mục Bác sĩ", null, (s, e) => OpenChild<frmDoctorSchedule>()));
        mnuManage.DropDownItems.Add(new ToolStripMenuItem("Danh mục Khoa/Phòng", null, (s, e) => OpenChild<frmDepartments>()));

        mnuWindow = new ToolStripMenuItem("Cửa sổ");
        mnuWindow.DropDownItems.Add(new ToolStripMenuItem("Sắp xếp Xếp tầng (Cascade)", null, (s, e) => LayoutMdi(MdiLayout.Cascade)));
        mnuWindow.DropDownItems.Add(new ToolStripMenuItem("Sắp xếp Ngang (Tile Horizontal)", null, (s, e) => LayoutMdi(MdiLayout.TileHorizontal)));
        mnuWindow.DropDownItems.Add(new ToolStripMenuItem("Sắp xếp Dọc (Tile Vertical)", null, (s, e) => LayoutMdi(MdiLayout.TileVertical)));
        mnuWindow.DropDownItems.Add(new ToolStripSeparator());
        mnuWindow.DropDownItems.Add(new ToolStripMenuItem("Đóng tất cả", null, (s, e) => CloseAllChildren()));

        menuMain.Items.AddRange(new ToolStripItem[] { mnuSystem, mnuManage, mnuWindow });
        menuMain.MdiWindowListItem = mnuWindow;   // tự liệt kê các form con đang mở ở cuối menu Cửa sổ

        // ---- StatusStrip ----
        statusMain = new StatusStrip();
        lblDoctor = new ToolStripStatusLabel("Bác sĩ trực: (chưa đăng nhập)") { BorderSides = ToolStripStatusLabelBorderSides.Right };
        lblInfo = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        progressBar = new ToolStripProgressBar { Minimum = 0, Maximum = 100, Width = 180 };
        statusMain.Items.AddRange(new ToolStripItem[] { lblDoctor, lblInfo, progressBar });

        Controls.Add(statusMain);
        Controls.Add(menuMain);
        MainMenuStrip = menuMain;

        timerProgress = new System.Windows.Forms.Timer { Interval = 60 };
        timerProgress.Tick += timerProgress_Tick;
    }

    // ===================== QUẢN LÝ FORM CON MDI (chống mở trùng) =====================
    private T OpenChild<T>() where T : Form, new()
    {
        // 1. Duyệt qua danh sách MdiChildren
        foreach (Form child in MdiChildren)
        {
            if (child is T existing)
            {
                // 2. Đã mở: đưa lên trên cùng
                if (existing.WindowState == FormWindowState.Minimized)
                    existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                return existing;
            }
        }

        // 3. Chưa mở: tạo Instance mới, gán MdiParent và Show
        var form = new T { MdiParent = this };
        form.Show();
        return form;
    }

    private void CloseAllChildren()
    {
        foreach (Form child in MdiChildren.ToArray())
            child.Close();
    }

    // ===================== NẠP LẠI DỮ LIỆU + PROGRESS BAR =====================
    private void StartReload()
    {
        if (timerProgress.Enabled) return;
        mnuReload.Enabled = false;
        progressBar.Value = 0;
        lblInfo.Text = "Đang nạp lại dữ liệu...";
        timerProgress.Start();
    }

    private void timerProgress_Tick(object sender, EventArgs e)
    {
        progressBar.Value = Math.Min(100, progressBar.Value + 10);
        if (progressBar.Value < 100) return;

        timerProgress.Stop();
        foreach (Form child in MdiChildren)
            if (child is IReloadable r) r.ReloadData();

        lblInfo.Text = "Đã nạp lại dữ liệu lúc " + DateTime.Now.ToString("HH:mm:ss");
        progressBar.Value = 0;
        mnuReload.Enabled = true;
    }

    // ===================== ĐĂNG NHẬP / ĐĂNG XUẤT =====================
    private void DoLogin(bool firstTime)
    {
        using var dlg = new frmLogin();
        if (dlg.ShowDialog(firstTime ? this : null) == DialogResult.OK)
        {
            lblDoctor.Text = "Bác sĩ trực: " + dlg.DutyName;
            lblInfo.Text = "Sẵn sàng";
            mnuManage.Enabled = true;
            mnuReload.Enabled = true;
            if (!Visible) Show();
        }
        else
        {
            Close();
        }
    }

    private void Logout()
    {
        var rs = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (rs != DialogResult.Yes) return;

        timerProgress.Stop();
        progressBar.Value = 0;
        CloseAllChildren();
        lblDoctor.Text = "Bác sĩ trực: (chưa đăng nhập)";
        mnuManage.Enabled = false;
        Hide();
        DoLogin(firstTime: false);
    }
}
