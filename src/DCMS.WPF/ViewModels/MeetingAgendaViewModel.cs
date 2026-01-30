using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DCMS.Application.Interfaces;
using DCMS.WPF.ViewModels.Dialogs;

namespace DCMS.WPF.ViewModels;

public class MeetingAgendaViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IReportingService _reportingService;
    private DateTime _currentDate;
    private ObservableCollection<Meeting> _meetings;
    private ObservableCollection<CalendarDayViewModel> _dayViewModels;
    private Meeting? _selectedMeeting;
    private bool _isBusy;
    private DateTime _selectedDate = DateTime.Today;
    private List<Meeting> _currentMonthMeetings = new();

    public MeetingAgendaViewModel(IDbContextFactory<DCMSDbContext> contextFactory, 
                                 IServiceProvider serviceProvider,
                                 IReportingService reportingService)
    {
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _reportingService = reportingService;
        _currentDate = DateTime.Today;
        _meetings = new ObservableCollection<Meeting>();
        _dayViewModels = new ObservableCollection<CalendarDayViewModel>();
        
        NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
        PreviousMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
        AddMeetingCommand = new RelayCommand(ExecuteAddMeeting);
        EditMeetingCommand = new RelayCommand(ExecuteEditMeeting);
        DeleteMeetingCommand = new RelayCommand(ExecuteDeleteMeeting);
        SelectDayCommand = new RelayCommand(ExecuteSelectDay);
        ExportReportCommand = new RelayCommand(ExecuteExportReport);
        JoinMeetingCommand = new RelayCommand(ExecuteJoinMeeting, CanJoinMeeting);

        GenerateCalendar();
        _ = Task.Run(async () => await LoadMeetingsAsync());
    }

    public DateTime CurrentDate
    {
        get => _currentDate;
        set
        {
            if (SetProperty(ref _currentDate, value))
            {
                GenerateCalendar();
                _ = LoadMeetingsAsync();
            }
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                UpdateDisplayedMeetings();
            }
        }
    }

    public string CurrentMonthYear => CurrentDate.ToString("MMMM yyyy");

    public ObservableCollection<Meeting> Meetings
    {
        get => _meetings;
        set => SetProperty(ref _meetings, value);
    }

    public ObservableCollection<CalendarDayViewModel> DayViewModels
    {
        get => _dayViewModels;
        set => SetProperty(ref _dayViewModels, value);
    }

    public Meeting? SelectedMeeting
    {
        get => _selectedMeeting;
        set => SetProperty(ref _selectedMeeting, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ICommand NextMonthCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand AddMeetingCommand { get; }
    public ICommand EditMeetingCommand { get; }
    public ICommand DeleteMeetingCommand { get; }
    public ICommand SelectDayCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand JoinMeetingCommand { get; }

    private void ChangeMonth(int months)
    {
        CurrentDate = CurrentDate.AddMonths(months);
        OnPropertyChanged(nameof(CurrentMonthYear));
    }

    private void ExecuteSelectDay(object? parameter)
    {
        if (parameter is DateTime date)
        {
            SelectedDate = date;
        }
    }

    private void UpdateDisplayedMeetings()
    {
        var selectedDateStart = DateTime.SpecifyKind(SelectedDate.Date, DateTimeKind.Utc);
        var selectedDateEnd = DateTime.SpecifyKind(SelectedDate.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var filteredMeetings = _currentMonthMeetings
            .Where(m => m.StartDateTime >= selectedDateStart && m.StartDateTime <= selectedDateEnd)
            .OrderBy(m => m.StartDateTime)
            .ToList();

        Meetings = new ObservableCollection<Meeting>(filteredMeetings);
    }

    private void GenerateCalendar()
    {
        DayViewModels.Clear();
        
        var firstDayOfMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
        
        var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        
        var previousMonth = firstDayOfMonth.AddMonths(-1);
        var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
        
        for (int i = 0; i < startDayOfWeek; i++)
        {
            var date = new DateTime(previousMonth.Year, previousMonth.Month, daysInPreviousMonth - startDayOfWeek + 1 + i);
            DayViewModels.Add(new CalendarDayViewModel(date, false, date.Date == DateTime.Today, SelectDayCommand));
        }
        
        for (int i = 1; i <= daysInMonth; i++)
        {
            var date = new DateTime(CurrentDate.Year, CurrentDate.Month, i);
            DayViewModels.Add(new CalendarDayViewModel(date, true, date.Date == DateTime.Today, SelectDayCommand));
        }
        
        var remainingCells = 42 - DayViewModels.Count;
        var nextMonth = firstDayOfMonth.AddMonths(1);
        
        for (int i = 1; i <= remainingCells; i++)
        {
            var date = new DateTime(nextMonth.Year, nextMonth.Month, i);
            DayViewModels.Add(new CalendarDayViewModel(date, false, date.Date == DateTime.Today, SelectDayCommand));
        }
    }

    private async Task LoadMeetingsAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var startOfMonth = DateTime.SpecifyKind(
                new DateTime(CurrentDate.Year, CurrentDate.Month, 1), 
                DateTimeKind.Utc).AddDays(-1); // Buffer for timezones
            var endOfMonth = DateTime.SpecifyKind(
                new DateTime(CurrentDate.Year, CurrentDate.Month, 1).AddMonths(1), 
                DateTimeKind.Utc).AddDays(1); // Buffer for timezones

            var meetings = await context.Meetings
                .AsNoTracking()
                .Where(m => (m.StartDateTime >= startOfMonth && m.StartDateTime <= endOfMonth) ||
                            (m.IsRecurring && (m.RecurrenceEndDate == null || m.RecurrenceEndDate >= startOfMonth)))
                .OrderBy(m => m.StartDateTime)
                .ToListAsync();

            _currentMonthMeetings.Clear();
            foreach (var meeting in meetings)
            {
                if (meeting.IsRecurring && meeting.RecurrenceCount.HasValue)
                {
                    for (int i = 0; i < meeting.RecurrenceCount.Value; i++)
                    {
                        var instanceStart = meeting.RecurrenceType switch
                        {
                            RecurrenceType.Daily => meeting.StartDateTime.AddDays(i),
                            RecurrenceType.Weekly => meeting.StartDateTime.AddDays(i * 7),
                            RecurrenceType.Monthly => meeting.StartDateTime.AddMonths(i),
                            RecurrenceType.Yearly => meeting.StartDateTime.AddYears(i),
                            _ => meeting.StartDateTime
                        };

                        if (instanceStart >= startOfMonth && instanceStart <= endOfMonth)
                        {
                            var instance = new Meeting
                            {
                                Id = meeting.Id,
                                Title = meeting.Title,
                                Description = meeting.Description,
                                StartDateTime = instanceStart,
                                EndDateTime = instanceStart.Add(meeting.EndDateTime - meeting.StartDateTime),
                                Location = meeting.Location,
                                Attendees = meeting.Attendees,
                                MeetingType = meeting.MeetingType,
                                IsRecurring = meeting.IsRecurring,
                                RecurrenceType = meeting.RecurrenceType,
                                RecurrenceCount = meeting.RecurrenceCount,
                                ReminderMinutesBefore = meeting.ReminderMinutesBefore
                            };
                            _currentMonthMeetings.Add(instance);
                        }
                    }
                }
                else
                {
                    _currentMonthMeetings.Add(meeting);
                }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateDisplayedMeetings();

                foreach (var dayVM in DayViewModels)
                {
                    dayVM.Meetings.Clear();
                    var dayMeetings = _currentMonthMeetings.Where(m => m.StartDateTime.Date == dayVM.Date.Date).ToList();
                    foreach (var meeting in dayMeetings)
                    {
                        dayVM.Meetings.Add(meeting);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الاجتماعات: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteAddMeeting(object? parameter)
    {
        try
        {
            var dialog = _serviceProvider.GetRequiredService<Views.AddMeetingDialog>();
            var viewModel = dialog.DataContext as ViewModels.AddMeetingDialogViewModel;
            
            // Set the default date to the currently selected date or today
            if (viewModel != null)
            {
                viewModel.SetDefaultDate(SelectedDate);
            }
            
            if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow != dialog)
            {
                dialog.Owner = System.Windows.Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadMeetingsAsync();

                if (viewModel != null)
                {
                    SelectedDate = viewModel.StartDate.Date;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح نافذة إضافة اجتماع: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteEditMeeting(object? parameter)
    {
        if (parameter is not Meeting meeting) return;

        try
        {
            var dialog = _serviceProvider.GetRequiredService<Views.AddMeetingDialog>();
            var viewModel = dialog.DataContext as ViewModels.AddMeetingDialogViewModel;
            
            if (viewModel != null)
            {
                viewModel.LoadMeeting(meeting);
            }
            
            if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow != dialog)
            {
                dialog.Owner = System.Windows.Application.Current.MainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await LoadMeetingsAsync();
                
                // Refresh sidebar for the edited meeting's date
                SelectedDate = meeting.StartDateTime.Date;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح نافذة تعديل الاجتماع: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecuteDeleteMeeting(object? parameter)
    {
        Meeting? meetingToDelete = parameter as Meeting ?? SelectedMeeting;
        
        if (meetingToDelete == null) return;

        if (MessageBox.Show($"هل تريد حذف الاجتماع '{meetingToDelete.Title}'؟", "تأكيد", 
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                // Find the tracked entity by ID
                var trackedMeeting = await context.Meetings.FindAsync(meetingToDelete.Id);
                
                if (trackedMeeting != null)
                {
                    context.Meetings.Remove(trackedMeeting);
                    await context.SaveChangesAsync();
                    
                    MessageBox.Show("تم حذف الاجتماع بنجاح!", "نجح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    await LoadMeetingsAsync();
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على الاجتماع في قاعدة البيانات.", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    await LoadMeetingsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الاجتماع: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public async Task<bool> CheckConflict(Meeting newMeeting)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Meetings.AnyAsync(m => 
            m.Id != newMeeting.Id &&
            m.StartDateTime < newMeeting.EndDateTime && 
            newMeeting.StartDateTime < m.EndDateTime);
    }

    private void ExecuteExportReport(object? parameter)
    {
        try
        {
            var exportVM = _serviceProvider.GetRequiredService<MeetingExportOptionsViewModel>();
            var dialog = new Views.Dialogs.MeetingExportOptionsView(exportVM);

            if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow != dialog)
            {
                dialog.Owner = System.Windows.Application.Current.MainWindow;
            }

            dialog.ShowDialog();

            if (exportVM.IsCancelled) return;

            DateTime start = exportVM.StartDate;
            DateTime end = exportVM.EndDate;
            string title = "أجندة الاجتماعات";

            // Filter meetings (Local Cache)
            // Note: Cache contains UTC dates, comparison should be UTC
            var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            var reportMeetings = _currentMonthMeetings
                .Where(m => m.StartDateTime >= startUtc && m.StartDateTime <= endUtc)
                .OrderBy(m => m.StartDateTime)
                .ToList();

            if (!reportMeetings.Any())
            {
                MessageBox.Show("لا توجد اجتماعات في الفترة المختارة للتصدير.", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Agenda_{start:yyyyMMdd}-{end:yyyyMMdd}.pdf",
                Title = "حفظ أجندة الاجتماعات"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Use month or day/week logic based on date range
                bool isMonthExport = (end - start).TotalDays > 20;
                if (isMonthExport)
                {
                    _reportingService.GenerateMonthlyCalendarReport(saveFileDialog.FileName, title, reportMeetings, start);
                }
                else
                {
                    _reportingService.GenerateMeetingAgendaReport(saveFileDialog.FileName, title, reportMeetings, start, end);
                }
                
                MessageBox.Show("تم تصدير الأجندة بنجاح!", "تم بنجاح", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء تصدير التقرير: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanJoinMeeting(object? parameter)
    {
        return parameter is Meeting meeting && !string.IsNullOrWhiteSpace(meeting.OnlineMeetingLink);
    }

    private void ExecuteJoinMeeting(object? parameter)
    {
        if (parameter is Meeting meeting && !string.IsNullOrWhiteSpace(meeting.OnlineMeetingLink))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = meeting.OnlineMeetingLink,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح الرابط: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
