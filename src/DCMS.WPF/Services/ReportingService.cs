using DCMS.Application.Interfaces;
using DCMS.Application.Models;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using DCMS.WPF.Services.Reports;
using System;
using System.Collections.Generic;

namespace DCMS.WPF.Services;

public class ReportingService : IReportingService
{
    public ReportingService()
    {
        // Set License to Community (Free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateEngineerPerformanceReport(string filePath, string title, IEnumerable<EngineerPerformanceItem> items, DateTime from, DateTime to)
    {
        var document = new EngineerPerformanceReportDocument(title, items, from, to);
        document.GeneratePdf(filePath);
    }

    public void GenerateSlaReport(string filePath, string title, IEnumerable<SlaItem> items, DateTime from, DateTime to)
    {
        var document = new SlaReportDocument(title, items, from, to);
        document.GeneratePdf(filePath);
    }

    public void GenerateTransmittalReport(string filePath, string title, IEnumerable<TransmittalItem> items, string recipientName)
    {
        var document = new TransmittalReportDocument(title, items, recipientName);
        document.GeneratePdf(filePath);
    }

    public void GenerateInventoryReport(string filePath, string title, IEnumerable<InventoryItem> items, DateTime from, DateTime to)
    {
        var document = new InventoryReportDocument(title, items, from, to);
        document.GeneratePdf(filePath);
    }

    public void GenerateSearchReport(string filePath, string title, IEnumerable<SearchItem> items, string engineerName)
    {
        var document = new SearchReportDocument(title, items);
        document.GeneratePdf(filePath);
    }

    public void GenerateMeetingAgendaReport(string filePath, string title, IEnumerable<DCMS.Domain.Entities.Meeting> items, DateTime from, DateTime to)
    {
        var document = new MeetingAgendaReportDocument(title, items, from, to);
        document.GeneratePdf(filePath);
    }

    public void GenerateMonthlyCalendarReport(string filePath, string title, IEnumerable<DCMS.Domain.Entities.Meeting> items, DateTime monthDate)
    {
        var document = new MonthlyCalendarReportDocument(title, items, monthDate);
        document.GeneratePdf(filePath);
    }
}
