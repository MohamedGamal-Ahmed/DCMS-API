using System.ComponentModel.DataAnnotations;

namespace DCMS.Domain.Entities;

public class InboundCode
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EngineerName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
