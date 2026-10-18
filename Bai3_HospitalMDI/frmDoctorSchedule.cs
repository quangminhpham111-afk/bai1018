using System.Data;

namespace HospitalMDI;

/// <summary>Form con 2: lịch trực của bác sĩ.</summary>
public class frmDoctorSchedule : ChildGridForm
{
    public frmDoctorSchedule() : base("Danh mục Bác sĩ", "Lịch trực của Bác sĩ") { }

    protected override DataTable LoadTable() => SampleData.Doctors();
}
