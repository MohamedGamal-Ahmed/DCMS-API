using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Domain.Models;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class SearchQueryService
{
    public IQueryable<Inbound> BuildInboundQuery(SearchCriteria criteria, DCMSDbContext context)
    {
        var query = context.Inbounds
            .AsNoTracking()
            .Include(i => i.Transfers)
            .ThenInclude(t => t.Engineer)
            .Include(i => i.ResponsibleEngineers)
            .ThenInclude(r => r.Engineer)
            .AsQueryable();

        if (criteria.RecordType.HasValue)
        {
            var inboundCategory = ConvertToInboundCategory(criteria.RecordType.Value);
            query = query.Where(i => i.Category == inboundCategory);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Code)) query = query.Where(i => (i.Code ?? "").Contains(criteria.Code));
        if (!string.IsNullOrWhiteSpace(criteria.SubjectNumber)) query = query.Where(i => (i.SubjectNumber ?? "").Contains(criteria.SubjectNumber));
        if (!string.IsNullOrWhiteSpace(criteria.Subject)) query = query.Where(i => i.Subject.Contains(criteria.Subject));
        if (!string.IsNullOrWhiteSpace(criteria.From)) query = query.Where(i => (i.FromEntity ?? "").Contains(criteria.From) || (i.FromEngineer ?? "").Contains(criteria.From));
        
        if (criteria.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(criteria.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(i => i.InboundDate >= start);
        }
        if (criteria.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(criteria.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(i => i.InboundDate <= end);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ResponsibleEngineer) && criteria.ResponsibleEngineer != "الكل")
        {
            var variations = GetEngineerNameVariations(criteria.ResponsibleEngineer);
            query = query.Where(i => i.ResponsibleEngineers.Any(re => 
                variations.Contains(re.Engineer.FullName) || 
                re.Engineer.FullName.Contains(criteria.ResponsibleEngineer)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TransferredTo) && criteria.TransferredTo != "الكل")
        {
            query = query.Where(i => 
                i.Status != CorrespondenceStatus.Closed && 
                (
                    (i.TransferredTo != null && i.TransferredTo.Contains(criteria.TransferredTo)) ||
                    i.Transfers.Any(t => t.Engineer.FullName.Contains(criteria.TransferredTo))
                ));
        }

        if (criteria.Status.HasValue) query = query.Where(i => i.Status == criteria.Status.Value);
        if (criteria.SelectedYear > 0) query = query.Where(i => i.InboundDate.Year == criteria.SelectedYear);

        if (!string.IsNullOrWhiteSpace(criteria.SearchQuery))
        {
            query = query.Where(i => 
                i.Subject.Contains(criteria.SearchQuery) || 
                (i.Code ?? "").Contains(criteria.SearchQuery) ||
                (i.FromEntity ?? "").Contains(criteria.SearchQuery) ||
                i.SubjectNumber.Contains(criteria.SearchQuery));
        }

        return query;
    }

    public IQueryable<Outbound> BuildOutboundQuery(SearchCriteria criteria, DCMSDbContext context)
    {
        var query = context.Outbounds.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Code)) query = query.Where(o => (o.Code ?? "").Contains(criteria.Code));
        if (!string.IsNullOrWhiteSpace(criteria.SubjectNumber)) query = query.Where(o => (o.SubjectNumber ?? "").Contains(criteria.SubjectNumber));
        if (!string.IsNullOrWhiteSpace(criteria.Subject)) query = query.Where(o => o.Subject.Contains(criteria.Subject));
        if (!string.IsNullOrWhiteSpace(criteria.From)) query = query.Where(o => (o.ResponsibleEngineer ?? "").Contains(criteria.From));
        if (!string.IsNullOrWhiteSpace(criteria.To)) query = query.Where(o => (o.ToEntity ?? "").Contains(criteria.To) || (o.ToEngineer ?? "").Contains(criteria.To));

        if (criteria.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(criteria.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(o => o.OutboundDate >= start);
        }
        if (criteria.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(criteria.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(o => o.OutboundDate <= end);
        }

        if (criteria.SelectedYear > 0) query = query.Where(o => o.OutboundDate.Year == criteria.SelectedYear);

        if (!string.IsNullOrWhiteSpace(criteria.SearchQuery))
        {
            query = query.Where(o => 
                o.Subject.Contains(criteria.SearchQuery) || 
                (o.Code ?? "").Contains(criteria.SearchQuery) ||
                (o.ToEntity ?? "").Contains(criteria.SearchQuery) ||
                (o.ResponsibleEngineer ?? "").Contains(criteria.SearchQuery));
        }

        return query;
    }

    private InboundCategory ConvertToInboundCategory(SearchRecordType recordType)
    {
        return recordType switch
        {
            SearchRecordType.Posta => InboundCategory.Posta,
            SearchRecordType.Email => InboundCategory.Email,
            SearchRecordType.Contract => InboundCategory.Contract,
            SearchRecordType.Delegation => InboundCategory.Delegation,
            SearchRecordType.Custody => InboundCategory.Custody,
            SearchRecordType.Mission => InboundCategory.Mission,
            SearchRecordType.Request => InboundCategory.Request,
            SearchRecordType.Complaint => InboundCategory.Complaint,
            _ => InboundCategory.Posta
        };
    }

    private List<string> GetEngineerNameVariations(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new List<string> { name };
        
        var normalized = name.ToLower().Trim();
        if (normalized.Contains("azza") || normalized.Contains("عزة")) return new List<string> { "م/ عزة الدسوقي", "عزة", "azza" };
        if (normalized.Contains("hadeer") || normalized.Contains("هدير")) return new List<string> { "م/ هدير عمرو", "هدير", "hadeer" };
        if (normalized.Contains("engy") || normalized.Contains("انجي")) return new List<string> { "م/ انجي محمد", "انجي", "engy" };
        if (normalized.Contains("karam") || normalized.Contains("كرم")) return new List<string> { "م/ احمد كرم", "كرم", "karam" };
        if (normalized.Contains("nada") || normalized.Contains("ندي") || normalized.Contains("ندى")) return new List<string> { "م/ ندي القصير", "م/ ندا عاطف", "ندى", "ندي", "nada" };
        
        return new List<string> { name };
    }
}
