using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using DCMS.WPF.Services;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.ViewModels;

public class AddMeetingDialogViewModel : ViewModelBase
{
    private readonly DCMSDbContext _context;
    private readonly NotificationService _notificationService;
    private Meeting? _existingMeeting;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private DateTime _startDate = DateTime.Today;
    private TimeSpan _startTime = new TimeSpan(9, 0, 0);
    private TimeSpan _endTime = new TimeSpan(10, 0, 0);
    private string _location = string.Empty;
    private string _attendees = string.Empty;
    private RecurrenceType _recurrenceType = RecurrenceType.None;
    private DateTime? _recurrenceEndDate;
    private int? _reminderMinutes = 15;
    private int _meetingTypeIndex = 0;
    public ObservableCollection<string> AvailableTimes { get; private set; }
    
    private string _selectedStartTime = "09:00";
    private string _selectedEndTime = "10:00";

    // New Fields
    private bool _isOnline;
    private string _country = string.Empty;
    private string _relatedProject = string.Empty;
    private string _relatedPartner = string.Empty;
    private string _onlineMeetingLink = string.Empty;

    public AddMeetingDialogViewModel(DCMSDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
        
        GenerateAvailableTimes();
        
        SaveCommand = new RelayCommand(ExecuteSave, CanSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }
    
    private void GenerateAvailableTimes()
    {
        AvailableTimes = new ObservableCollection<string>();
        var start = TimeSpan.FromHours(6); // Start at 06:00
        var end = TimeSpan.FromHours(26); // End at 02:00 next day (26 hours)
        
        for (var time = start; time <= end; time = time.Add(TimeSpan.FromMinutes(15)))
        {
            // Handle wrap around 24 hours for display if needed, but simple string is fine
            // 24:00 -> 00:00, 25:00 -> 01:00
            var normalized = time.TotalHours >= 24 ? time.Subtract(TimeSpan.FromHours(24)) : time;
            AvailableTimes.Add(normalized.ToString(@"hh\:mm"));
        }
    }

    public bool IsEditMode => _existingMeeting != null;

    public string DialogTitle => IsEditMode ? "تعديل اجتماع" : "إضافة اجتماع جديد";

    public int MeetingTypeIndex
    {
        get => _meetingTypeIndex;
        set => SetProperty(ref _meetingTypeIndex, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public TimeSpan EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public string SelectedStartTime
    {
        get => _selectedStartTime;
        set 
        {
            if (SetProperty(ref _selectedStartTime, value))
            {
                // Auto-adjust End Time logic
                if (TimeSpan.TryParse(value, out var newStart))
                {
                    // Default duration: 15 minutes
                    var newEnd = newStart.Add(TimeSpan.FromMinutes(15));
                    
                    // Normalize if valid (simple check)
                    SelectedEndTime = newEnd.ToString(@"hh\:mm");
                }
            }
        }
    }

    public string SelectedEndTime
    {
        get => _selectedEndTime;
        set => SetProperty(ref _selectedEndTime, value);
    }

    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    public string Attendees
    {
        get => _attendees;
        set => SetProperty(ref _attendees, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    public string Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    public string RelatedProject
    {
        get => _relatedProject;
        set => SetProperty(ref _relatedProject, value);
    }

    public string RelatedPartner
    {
        get => _relatedPartner;
        set => SetProperty(ref _relatedPartner, value);
    }

    public string OnlineMeetingLink
    {
        get => _onlineMeetingLink;
        set => SetProperty(ref _onlineMeetingLink, value);
    }

    public RecurrenceType RecurrenceType
    {
        get => _recurrenceType;
        set
        {
            if (SetProperty(ref _recurrenceType, value))
            {
                OnPropertyChanged(nameof(IsRecurring));
            }
        }
    }

    public bool IsRecurring => RecurrenceType != RecurrenceType.None;

    public DateTime? RecurrenceEndDate
    {
        get => _recurrenceEndDate;
        set => SetProperty(ref _recurrenceEndDate, value);
    }

    public int? ReminderMinutes
    {
        get => _reminderMinutes;
        set => SetProperty(ref _reminderMinutes, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public bool DialogResult { get; private set; }

    public void LoadMeeting(Meeting meeting)
    {
        _existingMeeting = meeting;
        Title = meeting.Title;
        Description = meeting.Description ?? string.Empty;
        StartDate = meeting.StartDateTime.ToLocalTime().Date;
        _startTime = meeting.StartDateTime.ToLocalTime().TimeOfDay;
        _endTime = meeting.EndDateTime.ToLocalTime().TimeOfDay;
        Location = meeting.Location ?? string.Empty;
        Attendees = meeting.Attendees ?? string.Empty;
        MeetingTypeIndex = (int)meeting.MeetingType;
        
        // Load New Fields
        IsOnline = meeting.IsOnline;
        Country = meeting.Country ?? string.Empty;
        RelatedProject = meeting.RelatedProject ?? string.Empty;
        RelatedPartner = meeting.RelatedPartner ?? string.Empty;
        OnlineMeetingLink = meeting.OnlineMeetingLink ?? string.Empty;

        _recurrenceType = meeting.RecurrenceType;
        RecurrenceEndDate = meeting.RecurrenceEndDate?.ToLocalTime();
        _reminderMinutes = meeting.ReminderMinutesBefore;

        // Set ComboBox string values for binding
        SelectedStartTime = meeting.StartDateTime.ToLocalTime().ToString("HH:mm");
        SelectedEndTime = meeting.EndDateTime.ToLocalTime().ToString("HH:mm");
        
        OnPropertyChanged(nameof(DialogTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    public void SetDefaultDate(DateTime selectedDate)
    {
        StartDate = selectedDate;
    }

    private bool CanSave(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Title);
    }

    private async void ExecuteSave(object? parameter)
    {
        try
        {
            if (parameter is not Window window) return;

            // Parse UI inputs (Bound properties)
            TimeSpan.TryParse(SelectedStartTime, out var startTime);
            TimeSpan.TryParse(SelectedEndTime, out var endTime);
            
            // Combine Date and Time as Local first, then convert to Utc
            var localStart = StartDate.Date.Add(startTime);
            var localEnd = StartDate.Date.Add(endTime);
            var startDateTime = localStart.ToUniversalTime();
            var endDateTime = localEnd.ToUniversalTime();

            // Get recurrence
            var recurrenceIndex = ((window.FindName("cmbRecurrence") as System.Windows.Controls.ComboBox)?.SelectedIndex ?? 0);
            var recurrenceType = recurrenceIndex switch
            {
                1 => RecurrenceType.Daily,
                2 => RecurrenceType.Weekly,
                3 => RecurrenceType.Monthly,
                4 => RecurrenceType.Yearly,
                _ => RecurrenceType.None
            };

            // Get recurrence count
            var recurrenceCountIndex = ((window.FindName("cmbRecurrenceCount") as System.Windows.Controls.ComboBox)?.SelectedIndex ?? 0);
            int? recurrenceCount = recurrenceCountIndex switch
            {
                0 => 2,
                1 => 3,
                2 => 4,
                3 => 5,
                4 => 10,
                5 => 20,
                _ => 2
            };

            // Get reminder
            var reminderIndex = ((window.FindName("cmbReminder") as System.Windows.Controls.ComboBox)?.SelectedIndex ?? 0);
            int? reminderMinutes = reminderIndex switch
            {
                1 => 0,
                2 => 5,
                3 => 10,
                4 => 15,
                5 => 30,
                6 => 60,
                7 => 1440, // 1 day
                _ => null
            };

            // Get meeting type (updated enum mapping)
            var meetingTypeIndex = ((window.FindName("cmbMeetingType") as System.Windows.Controls.ComboBox)?.SelectedIndex ?? 0);
            var meetingType = (MeetingType)meetingTypeIndex; 

            string notificationMessage;

            if (IsEditMode && _existingMeeting != null)
            {
                // Fetch the tracked entity from the database to avoid duplicates
                var trackedMeeting = await _context.Meetings.FindAsync(_existingMeeting.Id);
                
                if (trackedMeeting != null)
                {
                    // Update existing meeting
                    trackedMeeting.Title = Title;
                    trackedMeeting.Description = Description;
                    trackedMeeting.StartDateTime = startDateTime;
                    trackedMeeting.EndDateTime = endDateTime;
                    trackedMeeting.Location = Location;
                    trackedMeeting.Attendees = Attendees;
                    trackedMeeting.MeetingType = meetingType;
                    
                    // Update new fields
                    trackedMeeting.IsOnline = IsOnline;
                    trackedMeeting.Country = Country;
                    trackedMeeting.RelatedProject = RelatedProject;
                    trackedMeeting.RelatedPartner = RelatedPartner;
                    trackedMeeting.OnlineMeetingLink = OnlineMeetingLink;

                    trackedMeeting.IsRecurring = recurrenceType != RecurrenceType.None;
                    trackedMeeting.RecurrenceType = recurrenceType;
                    trackedMeeting.RecurrenceCount = recurrenceCount;
                    trackedMeeting.RecurrenceEndDate = RecurrenceEndDate.HasValue 
                        ? DateTime.SpecifyKind(RecurrenceEndDate.Value, DateTimeKind.Utc) 
                        : null;
                    trackedMeeting.ReminderMinutesBefore = reminderMinutes;
                    trackedMeeting.IsNotificationSent = false;
                    
                    notificationMessage = $"تم تحديث الاجتماع: {Title}";
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على الاجتماع في قاعدة البيانات.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                // Add new meeting
                var meeting = new Meeting
                {
                    Title = Title,
                    Description = Description,
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    Location = Location,
                    Attendees = Attendees,
                    MeetingType = meetingType,
                    
                    // New fields
                    IsOnline = IsOnline,
                    Country = Country,
                    RelatedProject = RelatedProject,
                    RelatedPartner = RelatedPartner,
                    OnlineMeetingLink = OnlineMeetingLink,

                    IsRecurring = recurrenceType != RecurrenceType.None,
                    RecurrenceType = recurrenceType,
                    RecurrenceCount = recurrenceCount,
                    RecurrenceEndDate = RecurrenceEndDate.HasValue 
                        ? DateTime.SpecifyKind(RecurrenceEndDate.Value, DateTimeKind.Utc) 
                        : null,
                    ReminderMinutesBefore = reminderMinutes,
                    IsNotificationSent = false
                };

                _context.Meetings.Add(meeting);
                notificationMessage = $"تم إضافة اجتماع جديد: {Title} ({startDateTime.ToLocalTime():dd/MM HH:mm})";
            }

            await _context.SaveChangesAsync();
            
            // Send notification
            await _notificationService.AddNotification(notificationMessage, null, NotificationType.Success);

            DialogResult = true;
            window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الاجتماع: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteCancel(object? parameter)
    {
        DialogResult = false;
        CloseWindow(parameter);
    }

    private void CloseWindow(object? parameter)
    {
        if (parameter is Window window)
        {
            window.Close();
        }
    }
}
