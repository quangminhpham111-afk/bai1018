using System.Data;

namespace HospitalMDI;

/// <summary>Form con nào hỗ trợ "Nạp lại dữ liệu" thì implement interface này.</summary>
public interface IReloadable
{
    void ReloadData();
}

/// <summary>Dữ liệu mẫu (DataTable) dùng chung cho các form con.</summary>
public static class SampleData
{
    public static DataTable Patients()
    {
        var t = new DataTable();
        t.Columns.Add("Mã BN");
        t.Columns.Add("Họ tên");
        t.Columns.Add("Năm sinh", typeof(int));
        t.Columns.Add("Giới tính");
        t.Columns.Add("Khoa điều trị");
        t.Columns.Add("Chẩn đoán");
        t.Rows.Add("BN001", "Nguyễn Văn An", 1985, "Nam", "Nội tổng hợp", "Viêm dạ dày");
        t.Rows.Add("BN002", "Trần Thị Bình", 1992, "Nữ", "Sản", "Theo dõi thai kỳ");
        t.Rows.Add("BN003", "Lê Minh Cường", 1970, "Nam", "Tim mạch", "Tăng huyết áp");
        t.Rows.Add("BN004", "Phạm Thu Hà", 2015, "Nữ", "Nhi", "Viêm họng cấp");
        t.Rows.Add("BN005", "Hoàng Quốc Dũng", 1958, "Nam", "Ngoại", "Thoát vị bẹn");
        return t;
    }

    public static DataTable Doctors()
    {
        var t = new DataTable();
        t.Columns.Add("Mã BS");
        t.Columns.Add("Bác sĩ");
        t.Columns.Add("Khoa");
        t.Columns.Add("Ngày trực");
        t.Columns.Add("Ca trực");
        t.Rows.Add("BS01", "BS. Nguyễn Hồng Phúc", "Nội tổng hợp", "Thứ 2", "Sáng (7h-13h)");
        t.Rows.Add("BS02", "BS. Trần Lan Anh", "Sản", "Thứ 3", "Chiều (13h-19h)");
        t.Rows.Add("BS03", "BS. Lê Đức Thành", "Tim mạch", "Thứ 4", "Đêm (19h-7h)");
        t.Rows.Add("BS04", "BS. Vũ Mai Linh", "Nhi", "Thứ 5", "Sáng (7h-13h)");
        t.Rows.Add("BS05", "BS. Đặng Quang Huy", "Ngoại", "Thứ 6", "Chiều (13h-19h)");
        return t;
    }

    public static DataTable Departments()
    {
        var t = new DataTable();
        t.Columns.Add("Mã khoa");
        t.Columns.Add("Tên khoa/phòng");
        t.Columns.Add("Vị trí");
        t.Columns.Add("Số giường", typeof(int));
        t.Rows.Add("K01", "Nội tổng hợp", "Tòa A - Tầng 2", 40);
        t.Rows.Add("K02", "Sản", "Tòa B - Tầng 3", 30);
        t.Rows.Add("K03", "Tim mạch", "Tòa A - Tầng 4", 25);
        t.Rows.Add("K04", "Nhi", "Tòa C - Tầng 1", 35);
        t.Rows.Add("K05", "Ngoại", "Tòa B - Tầng 2", 45);
        return t;
    }
}
