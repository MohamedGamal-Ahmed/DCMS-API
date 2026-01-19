using DCMS.Domain.Enums;

namespace DCMS.Domain.Models;

public class SearchCriteria
{
    public SearchRecordType? RecordType { get; set; }
    public CorrespondenceStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Code { get; set; }
    public string? Subject { get; set; }
    public string? SubjectNumber { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? ResponsibleEngineer { get; set; }
    public string? TransferredTo { get; set; }
    public int SelectedYear { get; set; }
    public string? SearchQuery { get; set; }
    public string? ContractType { get; set; }
}
