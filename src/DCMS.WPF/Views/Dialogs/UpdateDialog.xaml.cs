using System.Windows;
using DCMS.WPF.Services;
using System.Net.Http;
using System.IO;
using System.Diagnostics;

namespace DCMS.WPF.Views.Dialogs;

public partial class UpdateDialog : Window
{
    private readonly UpdateInfo _updateInfo;
    private bool _isDownloading = false;

    public UpdateDialog(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;

        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        txtCurrentVersion.Text = currentVersion?.ToString(3) ?? "Unknown";
        txtNewVersion.Text = updateInfo.LatestVersion;
        txtReleaseNotes.Text = string.IsNullOrEmpty(updateInfo.ReleaseNotes) 
            ? "تحسينات وإصلاحات في هذا الإصدار" 
            : updateInfo.ReleaseNotes;
    }

    private void Window_ContentRendered(object? sender, EventArgs e)
    {
        // Automatically start the update if a download URL is available
        if (!string.IsNullOrEmpty(_updateInfo.DownloadUrl))
        {
            BtnUpdate_Click(this, new RoutedEventArgs());
        }
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;

        if (string.IsNullOrEmpty(_updateInfo.DownloadUrl))
        {
            MessageBox.Show("رابط التحميل غير متاح. يرجى التحميل يدوياً من GitHub.", 
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // If it's not a direct EXE link, just open it in browser as fallback
        if (!_updateInfo.DownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = _updateInfo.DownloadUrl, UseShellExecute = true });
                MessageBox.Show("سيتم فتح صفحة التحميل في المتصفح. يرجى تحميل الإصدار الجديد وتثبيته يدوياً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

        await StartDownloadAndInstall();
    }

    private async Task StartDownloadAndInstall()
    {
        _isDownloading = true;
        btnUpdate.IsEnabled = false;
        pnlProgress.Visibility = Visibility.Visible;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "DCMS_Update.exe");
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DCMS-Updater");
                
                using (var response = await client.GetAsync(_updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var downloadStream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[81920];
                        var bytesRead = 0;
                        var totalRead = 0L;

                        while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes != -1)
                            {
                                var progress = (int)((double)totalRead / totalBytes * 100);
                                progressBar.Value = progress;
                                txtProgress.Text = $"جاري التحميل... {progress}%";
                            }
                        }
                    }
                }
            }

            // Successfully downloaded, now prepare replacement
            var currentExe = Process.GetCurrentProcess().MainModule.FileName;
            var currentDir = Path.GetDirectoryName(currentExe);
            var batchPath = Path.Combine(Path.GetTempPath(), "dcms_update.bat");

            string batchContent = $@"
@echo off
title DCMS Update System
echo ========================================
echo        DCMS Update In Progress
echo ========================================
echo.
echo [1/3] Stopping application...
taskkill /F /IM ""{Path.GetFileName(currentExe)}"" /T > nul 2>&1
timeout /t 3 /nobreak > nul

:rename_retry
echo [2/3] Backing up old version...
if exist ""{currentExe}.old"" del ""{currentExe}.old""
move /y ""{currentExe}"" ""{currentExe}.old"" > nul
if errorlevel 1 (
    echo [RETRY] File is locked. Waiting...
    timeout /t 1 /nobreak > nul
    taskkill /F /IM ""{Path.GetFileName(currentExe)}"" /T > nul 2>&1
    goto rename_retry
)

:copy_retry
echo [3/3] Installing update...
copy /v /y ""{tempPath}"" ""{currentExe}"" > nul
if errorlevel 1 (
    echo [RETRY] Copy failed. Waiting...
    timeout /t 1 /nobreak > nul
    goto copy_retry
)

echo.
echo [SUCCESS] Update Installed Successfully!
echo Restarting...
start """" ""{currentExe}""
del ""{tempPath}""
del ""{currentExe}.old""
(goto) 2>nul & del ""%~f0""
";
            File.WriteAllText(batchPath, batchContent);

            MessageBox.Show("اكتمل التحميل. سيتم إغلاق التطبيق وتثبيت التحديث الآن.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batchPath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء التحديث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            _isDownloading = false;
            btnUpdate.IsEnabled = true;
            pnlProgress.Visibility = Visibility.Hidden;
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isDownloading)
        {
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            e.Cancel = true;
        }
    }
}
