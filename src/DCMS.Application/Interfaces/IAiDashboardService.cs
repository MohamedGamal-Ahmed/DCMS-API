using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IAiDashboardService
{
    Task<AiDashboardDataDto> GetAiDashboardDataAsync(int userId, string? userRole, string? fullName, string? userName);
}

public class AiDashboardDataDto
{
    public int TotalReceived { get; set; }
    public int TotalPresented { get; set; }
    public int TotalTransferred { get; set; }
    public int TotalPending { get; set; }
    public int TotalInternalTransactions { get; set; }
    public int CriticalExternalDelays { get; set; }
    public string FastestEngineer { get; set; } = "-";
    public int OverallCompletionRate { get; set; }
    public List<AiPendingItemDto> PendingManagerReview { get; set; } = new();
    public List<AiPendingItemDto> PendingConsultantResponse { get; set; } = new();
    public List<AiPendingItemDto> MissingAttachments { get; set; } = new();
    public string DiagnosticLog { get; set; } = string.Empty;
}

public class AiPendingItemDto
{
    public int Id { get; set; }
    public string SubjectNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TransferredTo { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public int DaysDelayed { get; set; }
    public string DelayType { get; set; } = string.Empty;
}
