using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GymManager;

/// <summary>
/// Model hội viên. Implement INotifyPropertyChanged để BindingList&lt;T&gt;
/// tự động báo cho DataGridView cập nhật khi một thuộc tính thay đổi.
/// </summary>
public class MemberModel : INotifyPropertyChanged
{
    private string _memberId = "";
    private string _fullName = "";
    private string _phone = "";
    private string _package = "Theo tháng";
    private DateTime _startDate = DateTime.Today;
    private int _fee;

    public string MemberId { get => _memberId; set => Set(ref _memberId, value); }
    public string FullName { get => _fullName; set => Set(ref _fullName, value); }
    public string Phone { get => _phone; set => Set(ref _phone, value); }
    public string Package { get => _package; set => Set(ref _package, value); }
    public DateTime StartDate { get => _startDate; set => Set(ref _startDate, value); }
    public int Fee { get => _fee; set => Set(ref _fee, value); }

    public event PropertyChangedEventHandler PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
