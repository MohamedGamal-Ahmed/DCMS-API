using System.Windows;
using System.Windows.Input;
using DCMS.WPF.Helpers;
using DCMS.WPF.Services;
using Microsoft.Win32;

namespace DCMS.WPF.ViewModels;

public class ImportViewModel : ViewModelBase
{
    private readonly Services.ExcelImportService _importService;
    private string _selectedFilePath = string.Empty;
    private string _statusMessage = string.Empty;
    private string _progressLog = string.Empty;
    private bool _isImporting;
    private int _progressValue;

    public ImportViewModel(Services.ExcelImportService importService)
    {
        _importService = importService;
        BrowseCommand = new RelayCommand(ExecuteBrowse);
        ImportCommand = new RelayCommand(ExecuteImport, CanExecuteImport);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            SetProperty(ref _selectedFilePath, value);
            StatusMessage = string.IsNullOrEmpty(value) ? "" : "جاهز للاستيراد";
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ProgressLog
    {
        get => _progressLog;
        set => SetProperty(ref _progressLog, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        set => SetProperty(ref _isImporting, value);
    }

    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public ICommand BrowseCommand { get; }
    public ICommand ImportCommand { get; }

    private void ExecuteBrowse(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Title = "اختر ملف Excel للاستيراد"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            ProgressLog = string.Empty;
        }
    }

    private bool CanExecuteImport(object? parameter)
    {
        return !string.IsNullOrEmpty(SelectedFilePath) && 
               System.IO.File.Exists(SelectedFilePath) && 
               !IsImporting;
    }

    private async void ExecuteImport(object? parameter)
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;

        // Confirm before import
        var confirm = MessageBox.Show(
            "هل أنت متأكد من استيراد البيانات من هذا الملف؟\n\nملاحظة: لن يتم استيراد السجلات المكررة.",
            "تأكيد الاستيراد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsImporting = true;
        ProgressLog = string.Empty;
        ProgressValue = 0;

        var progress = new Progress<string>(message =>
        {
            ProgressLog += message + "\n";
            ProgressValue = Math.Min(ProgressValue + 20, 90);
        });

        try
        {
            StatusMessage = "جاري الاستيراد...";
            
            ImportResult result;

            if (SelectedFilePath.Contains("اجتماعات") || SelectedFilePath.Contains("Meetings"))
            {
                // Import Meetings Calendar
                var count = await Task.Run(() => _importService.ImportMeetingsCalendarAsync(SelectedFilePath, progress));
                result = new ImportResult { Success = true, MeetingCount = count, Message = $"تم استيراد {count} اجتماع بنجاح" };
            }
            else
            {
                // Import Correspondence
                result = await Task.Run(() => _importService.ImportFromExcelAsync(SelectedFilePath, progress));
            }

            ProgressValue = 100;

            if (result.Success)
            {
                StatusMessage = result.Message;
                MessageBox.Show(
                    $"تم الاستيراد بنجاح!\n\n{result.Message}",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "فشل الاستيراد";
                MessageBox.Show(
                    $"فشل الاستيراد:\n{result.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "حدث خطأ";
            MessageBox.Show(
                $"حدث خطأ أثناء الاستيراد:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsImporting = false;
        }
    }
}
