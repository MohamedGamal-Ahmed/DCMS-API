using DCMS.Application.Models;
using System;
using System.Collections.Generic;

namespace DCMS.Application.Interfaces;

public interface IReportingService
{
    void GenerateEngineerPerformanceReport(string filePath, string title, IEnumerable<EngineerPerformanceItem> items, DateTime from, DateTime to);
    void GenerateSlaReport(string filePath, string title, IEnumerable<SlaItem> items, DateTime from, DateTime to);
    void GenerateTransmittalReport(string filePath, string title, IEnumerable<TransmittalItem> items, string recipientName);
    void GenerateInventoryReport(string filePath, string title, IEnumerable<InventoryItem> items, DateTime from, DateTime to);
    void GenerateSearchReport(string filePath, string title, IEnumerable<SearchItem> items, string engineerName);
    void GenerateMeetingAgendaReport(string filePath, string title, IEnumerable<DCMS.Domain.Entities.Meeting> items, DateTime from, DateTime to);
    void GenerateMonthlyCalendarReport(string filePath, string title, IEnumerable<DCMS.Domain.Entities.Meeting> items, DateTime monthDate);
}
