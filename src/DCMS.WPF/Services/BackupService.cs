using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DCMS.WPF.Services;

public class BackupService
{
    private readonly IConfiguration _configuration;
    private readonly string _backupDirectory;
    private const int DaysToKeepBackups = 10; // Keep backups for 10 days

    public BackupService(IConfiguration configuration)
    {
        _configuration = configuration;
        _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        
        // Create backup directory if it doesn't exist
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    private string GetPostgresBinaryPath(string binaryName)
    {
        // 1. Check if user configured a specific path in appsettings.json
        var configuredPath = _configuration["PostgreSQL:BinPath"];
        if (!string.IsNullOrEmpty(configuredPath))
        {
            var fullPath = Path.Combine(configuredPath, binaryName + ".exe");
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var foundPaths = new List<string>();
        
        // 2. Search in common PostgreSQL installation paths
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var searchRoots = new[]
        {
            Path.Combine(programFiles, "PostgreSQL"),
            Path.Combine(programFilesX86, "PostgreSQL"),
            Path.Combine(programFiles, "pgAdmin 4"),
            Path.Combine(programFilesX86, "pgAdmin 4"),
            Path.Combine(localAppData, "Programs", "pgAdmin 4")
        };

        foreach (var root in searchRoots)
        {
            if (!Directory.Exists(root)) continue;

            try 
            {
                var files = Directory.GetFiles(root, binaryName + ".exe", SearchOption.AllDirectories);
                foundPaths.AddRange(files);
            }
            catch { /* Ignore access errors */ }
        }

        // Sort by version number descending
        var bestMatch = foundPaths
            .Select(p => new { Path = p, Version = GetVersionFromPath(p) })
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        if (bestMatch != null && bestMatch.Version >= 16.0)
        {
            return bestMatch.Path;
        }

        // 3. Fallback: Check if binary is in PATH
        if (IsBinaryInPath(binaryName))
        {
            return binaryName;
        }

        // Build helpful error message
        var message = $"لم يتم العثور على {binaryName} الإصدار 16 أو 17.\n\n";
        
        if (bestMatch != null)
        {
            message += $"تم العثور على الإصدار {bestMatch.Version:F1} فقط في:\n{bestMatch.Path}\n\n";
        }
        
        message += "الحلول الممكنة:\n";
        message += "1. تثبيت PostgreSQL 16 أو 17 من: https://www.postgresql.org/download/\n";
        message += "2. أو تحديد مسار أدوات PostgreSQL يدوياً في ملف appsettings.json:\n";
        message += "   \"PostgreSQL\": { \"BinPath\": \"C:\\\\Program Files\\\\PostgreSQL\\\\16\\\\bin\" }";
        
        throw new Exception(message);
    }

    private double GetVersionFromPath(string path)
    {
        try
        {
            // Try file version info first (most accurate)
            var versionInfo = FileVersionInfo.GetVersionInfo(path);
            if (versionInfo.FileMajorPart > 0)
            {
                return versionInfo.FileMajorPart + (versionInfo.FileMinorPart * 0.1);
            }

            // Fallback: Try to find version in path (e.g., "17", "v17", "16")
            var parts = path.Split(Path.DirectorySeparatorChar);
            foreach (var part in parts)
            {
                if (double.TryParse(part, out double v)) return v;
                if (part.StartsWith("v") && double.TryParse(part.Substring(1), out double v2)) return v2;
            }
        }
        catch { }
        return 0;
    }

    private bool IsBinaryInPath(string binaryName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return false;

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, binaryName + ".exe");
            if (File.Exists(fullPath))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<string> CreateBackupAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("لم يتم العثور على سلسلة الاتصال");
            }

            // Parse connection string
            var parts = connectionString.Split(';');
            var host = GetValue(parts, "Host");
            var port = GetValue(parts, "Port") ?? "5432";
            var database = GetValue(parts, "Database");
            var username = GetValue(parts, "Username");
            var password = GetValue(parts, "Password");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"DCMS_Backup_{timestamp}.sql";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            // Set password environment variable
            Environment.SetEnvironmentVariable("PGPASSWORD", password);

            var pgDumpPath = GetPostgresBinaryPath("pg_dump");

            // Create pg_dump process
            var startInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} -F p -f \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("فشل في بدء عملية النسخ الاحتياطي");
            }

            await process.WaitForExitAsync();

            // Clear password environment variable
            Environment.SetEnvironmentVariable("PGPASSWORD", null);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"فشل النسخ الاحتياطي: {error}");
            }

            return backupPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}", ex);
        }
    }

    public async Task RestoreBackupAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود", backupPath);
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("لم يتم العثور على سلسلة الاتصال");
            }

            // Parse connection string
            var parts = connectionString.Split(';');
            var host = GetValue(parts, "Host");
            var port = GetValue(parts, "Port") ?? "5432";
            var database = GetValue(parts, "Database");
            var username = GetValue(parts, "Username");
            var password = GetValue(parts, "Password");

            // Set password environment variable
            Environment.SetEnvironmentVariable("PGPASSWORD", password);

            var psqlPath = GetPostgresBinaryPath("psql");

            // Create psql process
            var startInfo = new ProcessStartInfo
            {
                FileName = psqlPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} -f \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("فشل في بدء عملية الاستعادة");
            }

            await process.WaitForExitAsync();

            // Clear password environment variable
            Environment.SetEnvironmentVariable("PGPASSWORD", null);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"فشل الاستعادة: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"خطأ في استعادة النسخة الاحتياطية: {ex.Message}", ex);
        }
    }

    public List<BackupInfo> ListBackups()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(_backupDirectory))
        {
            return backups;
        }

        var files = Directory.GetFiles(_backupDirectory, "*.*")
            .Where(f => f.EndsWith(".sql") || f.EndsWith(".json"));
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            backups.Add(new BackupInfo
            {
                FileName = fileInfo.Name,
                FilePath = fileInfo.FullName,
                FileSize = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime
            });
        }

        return backups.OrderByDescending(b => b.CreatedDate).ToList();
    }

    public void DeleteBackup(string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
    }

    private string? GetValue(string[] parts, string key)
    {
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                var value = part.Split('=')[1].Trim();
                return value;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a backup was already created today
    /// </summary>
    public bool ShouldCreateDailyBackup()
    {
        if (!Directory.Exists(_backupDirectory))
            return true;

        var today = DateTime.Today;
        var todayPattern = $"DCMS_Backup_{today:yyyyMMdd}_*.sql";
        var todayBackups = Directory.GetFiles(_backupDirectory, todayPattern);
        
        return todayBackups.Length == 0;
    }

    /// <summary>
    /// Creates an automatic daily backup (silently, no UI feedback)
    /// </summary>
    public async Task<string?> CreateAutoBackupAsync()
    {
        try
        {
            if (!ShouldCreateDailyBackup())
            {
                Debug.WriteLine("Auto-backup skipped: backup already exists for today");
                return null;
            }

            var backupPath = await CreateBackupAsync();
            Debug.WriteLine($"Auto-backup created: {backupPath}");
            
            // Cleanup old backups after successful backup
            CleanupOldBackups();
            
            return backupPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Auto-backup failed: {ex.Message}");
            return null;
        }
    }

    private const int MaxBackupsToKeep = 10; // Keep last 10 backups

    // ... (constructor remains same)

    // ... (other methods remain same)

    /// <summary>
    /// Removes backups exceeding MaxBackupsToKeep (keeps only the newest 10)
    /// </summary>
    public void CleanupOldBackups()
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
                return;

            var files = Directory.GetFiles(_backupDirectory, "*.sql")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (files.Count > MaxBackupsToKeep)
            {
                var filesToDelete = files.Skip(MaxBackupsToKeep).ToList();
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        Debug.WriteLine($"Deleted old backup: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to delete {file.Name}: {ex.Message}");
                    }
                }
                
               Debug.WriteLine($"Cleanup complete: deleted {filesToDelete.Count} old backups");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Backup cleanup failed: {ex.Message}");
        }
    }
}

public class BackupInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public string FileSizeFormatted => FormatFileSize(FileSize);

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
