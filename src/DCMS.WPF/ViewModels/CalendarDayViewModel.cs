using System.Collections.ObjectModel;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.WPF.Helpers;

namespace DCMS.WPF.ViewModels;

public class CalendarDayViewModel : ViewModelBase
{
    private DateTime _date;
    private bool _isCurrentMonth;
    private bool _isToday;
    private ObservableCollection<Meeting> _meetings;

    public ICommand SelectCommand { get; }

    public CalendarDayViewModel(DateTime date, bool isCurrentMonth, bool isToday, ICommand selectCommand)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
        SelectCommand = selectCommand;
        Meetings = new ObservableCollection<Meeting>();
    }

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public int DayNumber => _date.Day;

    public bool IsCurrentMonth
    {
        get => _isCurrentMonth;
        set => SetProperty(ref _isCurrentMonth, value);
    }

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public ObservableCollection<Meeting> Meetings
    {
        get => _meetings;
        set => SetProperty(ref _meetings, value);
    }

    public string DayDisplay => DayNumber.ToString();
}
