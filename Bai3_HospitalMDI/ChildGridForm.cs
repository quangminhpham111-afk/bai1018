using System.Data;

namespace HospitalMDI;

/// <summary>Form con dùng chung: một tiêu đề + một DataGridView hiển thị DataTable.</summary>
public abstract class ChildGridForm : Form, IReloadable
{
    protected readonly DataGridView Grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window
    };

    private readonly Label _lblTitle = new()
    {
        Dock = DockStyle.Top,
        Height = 34,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(8, 0, 0, 0)
    };

    protected ChildGridForm(string title, string heading)
    {
        Text = title;
        _lblTitle.Text = heading;
        Size = new Size(640, 360);
        Controls.Add(Grid);       // Fill add trước
        Controls.Add(_lblTitle);  // Top add sau
        Load += (s, e) => ReloadData();
    }

    protected abstract DataTable LoadTable();

    public void ReloadData() => Grid.DataSource = LoadTable();
}
