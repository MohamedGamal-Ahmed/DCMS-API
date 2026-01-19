using DCMS.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace DCMS.WPF.Services;

public class ExcelImportService
{
    private readonly ICorrespondenceImportService _correspondenceImportService;
    private readonly IMeetingImportService _meetingImportService;

    public ExcelImportService(
        ICorrespondenceImportService correspondenceImportService,
        IMeetingImportService meetingImportService)
    {
        _correspondenceImportService = correspondenceImportService;
        _meetingImportService = meetingImportService;
    }

    public async Task<ImportResult> ImportFromExcelAsync(string filePath, IProgress<string>? progress = null)
    {
        var resultDto = await _correspondenceImportService.ImportFromExcelAsync(filePath, progress);
        return new ImportResult
        {
            Success = resultDto.Success,
            Message = resultDto.Message,
            InboundCount = resultDto.InboundCount,
            OutboundCount = resultDto.OutboundCount
        };
    }

    public async Task<int> ImportMeetingsCalendarAsync(string filePath, IProgress<string>? progress = null)
    {
        return await _meetingImportService.ImportMeetingsCalendarAsync(filePath, progress);
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InboundCount { get; set; }
    public int OutboundCount { get; set; }
    public int MeetingCount { get; set; }
}
