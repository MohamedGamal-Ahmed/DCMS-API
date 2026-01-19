using System;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IMeetingImportService
{
    Task<int> ImportMeetingsCalendarAsync(string filePath, IProgress<string>? progress = null);
}
