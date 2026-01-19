using DCMS.Application.Interfaces;
using DCMS.Application.Models;
using DCMS.Domain.Entities;
using DCMS.Domain.Models;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCMS.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly SearchQueryService _searchQueryService;
    private readonly IReportingService _reportingService;

    public SearchService(
        IDbContextFactory<DCMSDbContext> contextFactory,
        SearchQueryService searchQueryService,
        IReportingService reportingService)
    {
        _contextFactory = contextFactory;
        _searchQueryService = searchQueryService;
        _reportingService = reportingService;
    }

    public async Task<(List<object> Items, int TotalCount)> SearchAsync(SearchCriteria criteria, int page, int pageSize)
    {
        // EMERGENCY: Limit increased to 100 (from 5) to allow seeing more data while still saving bandwidth
        pageSize = Math.Min(pageSize, 100);
        
        using var context = await _contextFactory.CreateDbContextAsync();
        
        if (criteria.RecordType == SearchRecordType.Outbound)
        {
            var query = _searchQueryService.BuildOutboundQuery(criteria, context);
            var totalCount = await query.CountAsync();
            var results = await query
                .OrderByDescending(o => o.OutboundDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new Outbound 
                { 
                    Id = o.Id, 
                    Subject = o.Subject, 
                    SubjectNumber = o.SubjectNumber, 
                    OutboundDate = o.OutboundDate, 
                    ToEntity = o.ToEntity, 
                    ResponsibleEngineer = o.ResponsibleEngineer 
                })
                .ToListAsync();

            return (results.Cast<object>().ToList(), totalCount);
        }
        else
        {
            var query = _searchQueryService.BuildInboundQuery(criteria, context);
            var totalCount = await query.CountAsync();
            var results = await query
                .OrderByDescending(i => i.InboundDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new Inbound 
                { 
                    Id = i.Id, 
                    Subject = i.Subject, 
                    SubjectNumber = i.SubjectNumber, 
                    InboundDate = i.InboundDate, 
                    Code = i.Code,
                    Status = i.Status, 
                    FromEntity = i.FromEntity,
                    ResponsibleEngineer = i.ResponsibleEngineer ?? i.ResponsibleEngineers.OrderByDescending(re => re.InboundId).Select(re => re.Engineer.FullName).FirstOrDefault(),
                    TransferredTo = i.TransferredTo ?? i.Transfers.OrderByDescending(t => t.TransferDate).Select(t => t.Engineer.FullName).FirstOrDefault(),
                    TransferDate = i.TransferDate ?? i.Transfers.OrderByDescending(t => t.TransferDate).Select(t => (DateTime?)t.TransferDate).FirstOrDefault(),
                    Reply = i.Reply
                })
                .ToListAsync();

            return (results.Cast<object>().ToList(), (int)totalCount);
        }
    }

    public async Task ExportToPdfAsync(SearchCriteria criteria, string filePath, string reportTitle)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        List<SearchItem> allSearchItems = new();

        if (criteria.RecordType == SearchRecordType.Outbound)
        {
            var query = _searchQueryService.BuildOutboundQuery(criteria, context);
            // EMERGENCY: Limit 5 for export too
            var allOutbounds = await query
                .OrderByDescending(o => o.OutboundDate)
                .Select(o => new { o.SubjectNumber, o.Code, o.Subject, o.OutboundDate, o.ResponsibleEngineer, o.ToEntity })
                .ToListAsync();

            allSearchItems = allOutbounds.Select(o => new SearchItem
            {
                SubjectNumber = o.SubjectNumber,
                Code = o.Code ?? "",
                Subject = o.Subject,
                Date = o.OutboundDate,
                FromEntity = o.ResponsibleEngineer ?? "",
                ResponsibleEngineer = o.ResponsibleEngineer ?? "",
                TransferredTo = o.ToEntity ?? "",
                Reply = "",
                Status = "صادر"
            }).ToList();
        }
        else
        {
            var query = _searchQueryService.BuildInboundQuery(criteria, context);
            // EMERGENCY: Limit 5 for export too
            var allInbounds = await query
                .OrderByDescending(i => i.InboundDate)
                .Select(i => new 
                { 
                    i.SubjectNumber, 
                    Code = i.Code ?? "", 
                    i.Subject, 
                    i.InboundDate, 
                    FromEntity = i.FromEntity ?? "", 
                    ResponsibleEngineer = i.ResponsibleEngineer ?? i.ResponsibleEngineers.OrderByDescending(re => re.InboundId).Select(re => re.Engineer.FullName).FirstOrDefault() ?? "",
                    TransferredTo = i.TransferredTo ?? i.Transfers.OrderByDescending(t => t.TransferDate).Select(t => t.Engineer.FullName).FirstOrDefault() ?? "",
                    Reply = i.Reply ?? "", 
                    Status = i.Status.ToString()
                })
                .ToListAsync();

            allSearchItems = allInbounds.Select(i => new SearchItem
            {
                SubjectNumber = i.SubjectNumber,
                Code = i.Code,
                Subject = i.Subject,
                Date = i.InboundDate,
                FromEntity = i.FromEntity,
                ResponsibleEngineer = i.ResponsibleEngineer,
                TransferredTo = i.TransferredTo,
                Reply = i.Reply,
                Status = i.Status
            }).ToList();
        }

        _reportingService.GenerateSearchReport(filePath, reportTitle, allSearchItems, criteria.TransferredTo ?? "الكل");
    }
}
