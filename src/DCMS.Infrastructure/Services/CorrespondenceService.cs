using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCMS.Application.DTOs;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class CorrespondenceService : ICorrespondenceService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly NumberingService _numberingService;

    public CorrespondenceService(IDbContextFactory<DCMSDbContext> contextFactory, ICurrentUserService currentUserService, NumberingService numberingService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _numberingService = numberingService;
    }

    public async Task<List<AiSearchResultDto>> SearchAsync(
        string? query = null,
        string? recordType = null,
        CorrespondenceStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? subject = null,
        string? code = null,
        string? fromEntity = null,
        string? toEntity = null,
        string? transferredTo = null,
        int take = 10)
    {
        var results = new List<AiSearchResultDto>();
        using var context = await _contextFactory.CreateDbContextAsync();

        // 1. Search Inbounds
        if (recordType == null || recordType.Equals("Inbound", StringComparison.OrdinalIgnoreCase))
        {
            var inQuery = context.Inbounds.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                if (query.Contains("-"))
                {
                    inQuery = inQuery.Where(i => i.SubjectNumber.Contains(query) || (i.Code != null && i.Code.Contains(query)));
                }
                else
                {
                    inQuery = inQuery.Where(i => i.Subject.Contains(query) || (i.Code != null && i.Code.Contains(query)) || i.SubjectNumber.Contains(query));
                }
            }
            if (status.HasValue)
                inQuery = inQuery.Where(i => i.Status == status.Value);
            if (startDate.HasValue)
                inQuery = inQuery.Where(i => i.InboundDate >= startDate.Value.ToUniversalTime());
            if (endDate.HasValue)
                inQuery = inQuery.Where(i => i.InboundDate <= endDate.Value.ToUniversalTime());
            if (!string.IsNullOrEmpty(subject))
                inQuery = inQuery.Where(i => i.Subject.Contains(subject));
            if (!string.IsNullOrEmpty(code))
                inQuery = inQuery.Where(i => i.Code != null && i.Code.Contains(code));
            if (!string.IsNullOrEmpty(fromEntity))
                inQuery = inQuery.Where(i => i.FromEntity != null && i.FromEntity.Contains(fromEntity));
            if (!string.IsNullOrEmpty(transferredTo))
                inQuery = inQuery.Where(i => i.TransferredTo != null && i.TransferredTo.Contains(transferredTo));

            // EMERGENCY: Limit results and minimal SELECT
            var inbounds = await inQuery
                .OrderByDescending(i => i.InboundDate)
                .Select(i => new { i.Id, i.Subject, i.Status, i.InboundDate, i.SubjectNumber, i.FromEntity, i.ResponsibleEngineer, i.ReplyAttachmentUrl, i.OriginalAttachmentUrl })
                .Take(take)
                .ToListAsync();

            results.AddRange(inbounds.Select(i => new AiSearchResultDto
            {
                Id = i.Id.ToString(),
                Type = "Inbound",
                SubjectNumber = i.SubjectNumber,
                Subject = i.Subject,
                Date = i.InboundDate.ToString("yyyy-MM-dd"),
                FromOrTo = i.FromEntity,
                Status = i.Status.ToString(),
                Summary = $"وارد من {i.FromEntity} بتاريخ {i.InboundDate:yyyy-MM-dd}",
                ResponsibleEngineer = i.ResponsibleEngineer,
                AttachmentUrl = i.ReplyAttachmentUrl ?? i.OriginalAttachmentUrl
            }));
        }

        // 2. Search Outbounds
        if (recordType == null || recordType.Equals("Outbound", StringComparison.OrdinalIgnoreCase))
        {
            var outQuery = context.Outbounds.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                if (query.Contains("-"))
                {
                    outQuery = outQuery.Where(o => o.SubjectNumber.Contains(query) || (o.Code != null && o.Code.Contains(query)));
                }
                else
                {
                    outQuery = outQuery.Where(o => o.Subject.Contains(query) || (o.Code != null && o.Code.Contains(query)) || o.SubjectNumber.Contains(query));
                }
            }
            if (startDate.HasValue)
                outQuery = outQuery.Where(o => o.OutboundDate >= startDate.Value.ToUniversalTime());
            if (endDate.HasValue)
                outQuery = outQuery.Where(o => o.OutboundDate <= endDate.Value.ToUniversalTime());
            if (!string.IsNullOrEmpty(subject))
                outQuery = outQuery.Where(o => o.Subject.Contains(subject));
            if (!string.IsNullOrEmpty(toEntity))
                outQuery = outQuery.Where(o => o.ToEntity != null && o.ToEntity.Contains(toEntity));

            // EMERGENCY: Limit results and minimal SELECT
            var outbounds = await outQuery
                .OrderByDescending(o => o.OutboundDate)
                .Select(o => new { o.Id, o.Subject, o.OutboundDate, o.SubjectNumber, o.ToEntity, o.ReplyAttachmentUrl, o.OriginalAttachmentUrl })
                .Take(take)
                .ToListAsync();

            results.AddRange(outbounds.Select(o => new AiSearchResultDto
            {
                Id = o.Id.ToString(),
                Type = "Outbound",
                SubjectNumber = o.SubjectNumber,
                Subject = o.Subject,
                Date = o.OutboundDate.ToString("yyyy-MM-dd"),
                FromOrTo = o.ToEntity,
                Status = "N/A",
                Summary = $"صادر إلى {o.ToEntity} بتاريخ {o.OutboundDate:yyyy-MM-dd}",
                AttachmentUrl = o.ReplyAttachmentUrl ?? o.OriginalAttachmentUrl
            }));
        }

        return results.OrderByDescending(r => r.Date).ToList();
    }

    public async Task<AiSearchResultDto?> GetByIdAsync(int id, string type)
    {
        return await GetByIdAsync(id.ToString(), type);
    }

    public async Task<AiSearchResultDto?> GetByIdAsync(string idOrNumber, string type)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        if (type.Equals("Inbound", StringComparison.OrdinalIgnoreCase))
        {
            // Try numeric ID first
            Inbound? i = null;
            if (int.TryParse(idOrNumber, out var numericId))
            {
                i = await context.Inbounds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == numericId);
            }
            
            // Fallback: search by SubjectNumber
            if (i == null)
            {
                i = await context.Inbounds.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubjectNumber.Contains(idOrNumber) || (x.Code != null && x.Code.Contains(idOrNumber)));
            }

            if (i == null) return null;
            return new AiSearchResultDto
            {
                Id = i.Id.ToString(),
                Type = "Inbound",
                SubjectNumber = i.SubjectNumber,
                Subject = i.Subject,
                Date = i.InboundDate.ToString("yyyy-MM-dd"),
                FromOrTo = i.FromEntity,
                Status = i.Status.ToString(),
                Summary = i.Reply
            };
        }
        else
        {
            Outbound? o = null;
            if (int.TryParse(idOrNumber, out var numericId))
            {
                o = await context.Outbounds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == numericId);
            }
            
            // Fallback: search by SubjectNumber
            if (o == null)
            {
                o = await context.Outbounds.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SubjectNumber.Contains(idOrNumber) || (x.Code != null && x.Code.Contains(idOrNumber)));
            }

            if (o == null) return null;
            return new AiSearchResultDto
            {
                Id = o.Id.ToString(),
                Type = "Outbound",
                SubjectNumber = o.SubjectNumber,
                Subject = o.Subject,
                Date = o.OutboundDate.ToString("yyyy-MM-dd"),
                FromOrTo = o.ToEntity,
                Status = "N/A"
            };
        }
    }

    public async Task<int> CreateInboundAsync(string subject, string code, string fromEntity, string assignedEngineer)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var generatedNumber = await _numberingService.GenerateNextInboundNumberAsync();

        var inbound = new Inbound
        {
            SubjectNumber = generatedNumber,
            Subject = subject,
            Code = code,
            FromEntity = fromEntity,
            FromEngineer = assignedEngineer,
            Category = InboundCategory.Posta,
            InboundDate = DateTime.UtcNow,
            Status = CorrespondenceStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.CurrentUserId
        };

        context.Inbounds.Add(inbound);
        await context.SaveChangesAsync();
        return inbound.Id;
    }

    public async Task<int> CreateOutboundAsync(string subject, string code, string toEntity)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var generatedNumber = await _numberingService.GenerateNextOutboundNumberAsync();

        var outbound = new Outbound
        {
            SubjectNumber = generatedNumber,
            Subject = subject,
            Code = code,
            ToEntity = toEntity,
            OutboundDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUserService.CurrentUserId
        };

        context.Outbounds.Add(outbound);
        await context.SaveChangesAsync();
        return outbound.Id;
    }
    
    public async Task<List<AiSearchResultDto>> GetSimilarInboundsAsync(string subject, int take = 3)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length < 3) return new List<AiSearchResultDto>();
        
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // 1. Exact match (fastest) - using Contains for partial matches
        var exactMatches = await context.Inbounds
            .AsNoTracking()
            .Where(i => i.Subject.Contains(subject))
            .OrderByDescending(i => i.InboundDate)
            .Take(take)
            .ToListAsync();
            
        if (exactMatches.Count >= take)
        {
            return exactMatches.Select(i => MapToDto(i)).ToList();
        }
        
        // 2. Word-based similarity (if few exact matches)
        // Common Arabic stop words to ignore
        var stopWords = new HashSet<string> { "بشأن", "طلب", "إلى", "من", "في", "على", "عن", "مع", "هذا", "هذه", "تم", "حول", "بخصوص", "بناء", "على" };
        
        var words = subject.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToList();
            
        if (words.Count < 2) 
        {
            // If we only have one significant word, just return what we found in exact matches
            return exactMatches.Select(i => MapToDto(i)).ToList();
        }
        
        // Find inbounds that contain at least TWO of the major words
        // We'll search for items that contain at least one word first, then filter in-memory for precision
        var query = context.Inbounds.AsNoTracking().AsQueryable();
        
        // EMERGENCY: Limit 5 and minimal SELECT
        var candidateItems = await context.Inbounds
            .AsNoTracking()
            .Where(i => words.Take(5).Any(w => i.Subject.Contains(w))) 
            .OrderByDescending(i => i.InboundDate)
            .Select(i => new { i.Id, i.Subject, i.Status, i.InboundDate, i.SubjectNumber, i.FromEntity, i.Reply })
            .Take(5)
            .ToListAsync();
            
        // Map back to Inbound for rank logic (or update rank logic to use anonymous)
        // To save time, we'll map to minimal Inbound objects
        var ranked = candidateItems
            .Select(i => 
            {
                var targetWords = i.Subject.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2 && !stopWords.Contains(w))
                    .ToHashSet();
                
                int matchCount = words.Count(w => targetWords.Contains(w));
                double score = (double)matchCount / words.Count;
                
                return new { Item = i, Score = score, MatchCount = matchCount };
            })
            .Where(s => s.Score >= 0.6 || (words.Count == 1 && s.MatchCount == 1))
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.Item.InboundDate)
            .Take(take)
            .Select(s => new AiSearchResultDto
            {
                Id = s.Item.Id.ToString(),
                Type = "Inbound",
                SubjectNumber = s.Item.SubjectNumber,
                Subject = s.Item.Subject,
                Date = s.Item.InboundDate.ToString("yyyy-MM-dd"),
                FromOrTo = s.Item.FromEntity,
                Status = s.Item.Status.ToString(),
                Summary = s.Item.Reply
            })
            .ToList();
            
        return ranked;
    }

    private AiSearchResultDto MapToDto(Inbound i)
    {
        return new AiSearchResultDto
        {
            Id = i.Id.ToString(),
            Type = "Inbound",
            SubjectNumber = i.SubjectNumber,
            Subject = i.Subject,
            Date = i.InboundDate.ToString("yyyy-MM-dd"),
            FromOrTo = i.FromEntity,
            Status = i.Status.ToString(),
            Summary = i.Reply
        };
    }
}
