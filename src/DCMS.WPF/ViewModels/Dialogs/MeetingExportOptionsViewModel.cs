using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace DCMS.WPF.ViewModels.Dialogs;

public partial class MeetingExportOptionsViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private int _selectedMonth = DateTime.Today.Month;

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    private int _selectedWeek = 1;

    public List<int> Months { get; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
    public List<int> Years { get; } = new();
    public List<int> Weeks { get; } = new() { 1, 2, 3, 4, 5 };

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsCancelled { get; private set; } = true;

    public event Action? RequestClose;

    public MeetingExportOptionsViewModel()
    {
        for (int i = 2024; i <= DateTime.Today.Year + 1; i++)
        {
            Years.Add(i);
        }
    }

    [RelayCommand]
    private void ExportDay()
    {
        StartDate = SelectedDate.Date;
        EndDate = SelectedDate.Date.AddDays(1).AddTicks(-1);
        IsCancelled = false;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void ExportWeek()
    {
        // Logic to calculate week range
        var firstOfMonth = new DateTime(SelectedYear, SelectedMonth, 1);
        var startOfWeek = firstOfMonth.AddDays((SelectedWeek - 1) * 7);
        
        // Adjust to actual start of week if needed (e.g. Saturday for Egypt)
        // For simplicity, we just take 7 days from the calculated start
        StartDate = startOfWeek.Date;
        EndDate = startOfWeek.AddDays(7).AddTicks(-1);
        
        IsCancelled = false;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void ExportMonth()
    {
        StartDate = new DateTime(SelectedYear, SelectedMonth, 1);
        EndDate = StartDate.AddMonths(1).AddTicks(-1);
        IsCancelled = false;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsCancelled = true;
        RequestClose?.Invoke();
    }
}
