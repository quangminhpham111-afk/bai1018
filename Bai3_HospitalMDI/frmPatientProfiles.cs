using System.Data;

namespace HospitalMDI;

/// <summary>Form con 1: danh sách hồ sơ bệnh nhân.</summary>
public class frmPatientProfiles : ChildGridForm
{
    public frmPatientProfiles() : base("Hồ sơ Bệnh nhân", "Danh sách hồ sơ bệnh nhân") { }

    protected override DataTable LoadTable() => SampleData.Patients();
}
