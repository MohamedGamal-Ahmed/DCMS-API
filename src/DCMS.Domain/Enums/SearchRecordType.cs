namespace DCMS.Domain.Enums;

/// <summary>
/// نوع السجل المراد البحث عنه (وارد أو صادر)
/// </summary>
public enum SearchRecordType
{
    // Inbound categories
    Posta,
    Email,
    Contract,
    Delegation,
    Custody,
    Mission,
    Request,
    Complaint,
    
    // Outbound
    Outbound  // المراسلات الصادرة
}
