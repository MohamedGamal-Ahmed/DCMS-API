using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DCMS.WPF.Services;
using Microsoft.Win32;

namespace DCMS.WPF.ViewModels;

public class BackupViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly DatabaseExportService _databaseExportService;
    private ObservableCollection<BackupInfo> _backups;
    private bool _isLoading;

    public BackupViewModel(BackupService backupService, DatabaseExportService databaseExportService)
    {
        _backupService = backupService;
        _databaseExportService = databaseExportService;
        _backups = new ObservableCollection<BackupInfo>();
        
        CreateBackupCommand = new RelayCommand(async _ => await CreateBackup());
        RestoreBackupCommand = new RelayCommand(async p => await RestoreBackup(p));
        DeleteBackupCommand = new RelayCommand(async p => await DeleteBackup(p));
        RefreshCommand = new RelayCommand(_ => LoadBackups());
        ExportJsonCommand = new RelayCommand(async _ => await ExportJson());
        ImportJsonCommand = new RelayCommand(async _ => await ImportJson());

        LoadBackups();
    }

    public ObservableCollection<BackupInfo> Backups
    {
        get => _backups;
        set => SetProperty(ref _backups, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand DeleteBackupCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand ImportJsonCommand { get; }

    private void LoadBackups()
    {
        try
        {
            IsLoading = true;
            var backups = _backupService.ListBackups();
            Backups = new ObservableCollection<BackupInfo>(backups);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل النسخ الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateBackup()
    {
        try
        {
            IsLoading = true;
            await _backupService.CreateBackupAsync();
            MessageBox.Show("تم إنشاء النسخة الاحتياطية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadBackups();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل إنشاء النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RestoreBackup(object? parameter)
    {
        if (parameter is not string filePath) return;

        var result = MessageBox.Show(
            "تحذير: استعادة النسخة الاحتياطية ستقوم بحذف جميع البيانات الحالية واستبدالها بالنسخة المختارة.\nهل أنت متأكد من المتابعة؟",
            "تأكيد الاستعادة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            await _backupService.RestoreBackupAsync(filePath);
            MessageBox.Show("تم استعادة النسخة الاحتياطية بنجاح. سيتم إعادة تشغيل التطبيق.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Restart application
            // Restart application
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
                System.Diagnostics.Process.Start(exePath);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل استعادة النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteBackup(object? parameter)
    {
        if (parameter is not string filePath) return;

        var result = MessageBox.Show(
            "هل أنت متأكد من حذف هذه النسخة الاحتياطية؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _backupService.DeleteBackup(filePath);
            LoadBackups();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل حذف النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportJson()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = $"DCMS_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json",
                Title = "حفظ نسخة احتياطية JSON"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                // Use streaming export to avoid memory issues with large databases
                await _databaseExportService.ExportToJsonFileAsync(saveFileDialog.FileName);

                // Also save a copy to the local Backups directory for history
                try
                {
                   // var backupParams = ... (Removed)
                   var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                   if (Directory.Exists(backupDir))
                   {
                       var copyPath = Path.Combine(backupDir, Path.GetFileName(saveFileDialog.FileName));
                       File.Copy(saveFileDialog.FileName, copyPath, true);
                   }
                }
                catch { /* Ignore copy errors */ }

                MessageBox.Show("تم تصدير النسخة الاحتياطية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadBackups(); // Refresh list
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل تصدير النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ImportJson()
    {
        var result = MessageBox.Show(
            "تحذير: استيراد النسخة الاحتياطية سيقوم بحذف جميع البيانات الحالية (عدا المستخدمين والمهندسين) واستبدالها بالنسخة المختارة.\nهل أنت متأكد من المتابعة؟",
            "تأكيد الاستيراد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "اختر ملف النسخة الاحتياطية JSON"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsLoading = true;
                // Use streaming import to avoid memory issues with large backups
                await _databaseExportService.ImportFromJsonFileAsync(openFileDialog.FileName);
                
                MessageBox.Show("تم استيراد النسخة الاحتياطية بنجاح. سيتم إعادة تشغيل التطبيق.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Restart application
                // Restart application
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    System.Diagnostics.Process.Start(exePath);
                System.Windows.Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل استيراد النسخة الاحتياطية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
