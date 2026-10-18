using System.Data;

namespace HospitalMDI;

/// <summary>Form con 3: danh mục khoa/phòng.</summary>
public class frmDepartments : ChildGridForm
{
    public frmDepartments() : base("Danh mục Khoa/Phòng", "Danh mục Khoa / Phòng") { }

    protected override DataTable LoadTable() => SampleData.Departments();
}
