using System;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface ICorrespondenceImportService
{
    Task<ImportResultDto> ImportFromExcelAsync(string filePath, IProgress<string>? progress = null);
}

public class ImportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InboundCount { get; set; }
    public int OutboundCount { get; set; }
}
