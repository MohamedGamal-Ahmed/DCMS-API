using ClosedXML.Excel;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DCMS.WPF.Services;

public class MeetingImportService : IMeetingImportService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public MeetingImportService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> ImportMeetingsCalendarAsync(string filePath, IProgress<string>? progress = null)
    {
        var count = 0;
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var months = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            var newMeetings = new List<Meeting>();

            foreach (var monthName in months)
            {
                var sheet = workbook.Worksheet(monthName);
                if (sheet == null) continue;

                progress?.Report($"جاري قراءة شهر {monthName}...");
                var range = sheet.RangeUsed();
                if (range == null) continue;

                var dateCells = new List<(IXLCell Cell, DateTime Date)>();
                var contentCells = new List<IXLCell>();

                foreach (var row in range.Rows())
                {
                    foreach (var cell in row.Cells())
                    {
                        if (cell.IsEmpty()) continue;
                        if (cell.DataType == XLDataType.DateTime) dateCells.Add((cell, cell.GetDateTime()));
                        else if (cell.DataType == XLDataType.Text)
                        {
                            var text = cell.GetString().Trim();
                            if (string.IsNullOrWhiteSpace(text) || IsHeader(text)) continue;
                            contentCells.Add(cell);
                        }
                    }
                }

                foreach (var contentCell in contentCells)
                {
                    var text = contentCell.GetString();
                    var nearbyDate = dateCells
                        .Where(d => d.Cell.Address.RowNumber <= contentCell.Address.RowNumber && 
                                    Math.Abs(d.Cell.Address.ColumnNumber - contentCell.Address.ColumnNumber) <= 2)
                        .OrderBy(d => GetDistance(d.Cell, contentCell))
                        .FirstOrDefault();

                    if (nearbyDate.Cell != null)
                    {
                        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var meeting = ParseMeetingLine(line, nearbyDate.Date);
                            if (meeting != null) newMeetings.Add(meeting);
                        }
                    }
                }
            }

            if (newMeetings.Any())
            {
                progress?.Report($"جاري حفظ {newMeetings.Count} نشاط...");
                using var context = await _contextFactory.CreateDbContextAsync();
                foreach (var m in newMeetings)
                {
                    var exists = await context.Meetings.AnyAsync(x => x.StartDateTime == m.StartDateTime && x.Title == m.Title);
                    if (!exists)
                    {
                        context.Meetings.Add(m);
                        count++;
                    }
                }
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Calendar Import Error: {ex}");
            throw;
        }
        return count;
    }

    private double GetDistance(IXLCell cell1, IXLCell cell2)
    {
        var dRow = cell2.Address.RowNumber - cell1.Address.RowNumber;
        var dCol = cell2.Address.ColumnNumber - cell1.Address.ColumnNumber;
        return Math.Sqrt(dRow * dRow + dCol * dCol);
    }

    private bool IsHeader(string text)
    {
        var headers = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
                              "الاحد", "الأحد", "الاثنين", "الأثنين", "الثلاثاء", "الاربعاء", "الأربعاء", "الخميس", "الجمعة", "السبت",
                              "January", "February", "March", "Notes", "Unnamed" };
        return headers.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase));
    }

    private Meeting ParseMeetingLine(string text, DateTime date)
    {
        var meeting = new Meeting();
        meeting.Title = text.Trim();
        var timeSpan = new TimeSpan(9, 0, 0);
        var targetYear = 2025;
        var finalDate = new DateTime(targetYear, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        meeting.StartDateTime = finalDate.Add(timeSpan);
        meeting.EndDateTime = meeting.StartDateTime.AddHours(1);
        meeting.MeetingType = InferMeetingType(text);
        if (meeting.MeetingType == MeetingType.Online || text.Contains("اونلاين") || text.Contains("دعم فني")) meeting.IsOnline = true;

        if (text.Contains("الأردن") || text.Contains("عمان") || text.Contains("الاردن") || text.Contains("Jordan", StringComparison.OrdinalIgnoreCase)) meeting.Country = "الأردن";
        if (text.Contains("كينيا") || text.Contains("Kenya", StringComparison.OrdinalIgnoreCase)) meeting.Country = "كينيا";
        if (text.Contains("السنغال") || text.Contains("Senegal", StringComparison.OrdinalIgnoreCase)) meeting.Country = "السنغال";
        if (text.Contains("كوت ديفوار") || text.Contains("كوت ايفوار") || text.Contains("Ivory Coast", StringComparison.OrdinalIgnoreCase) || text.Contains("Cote d'Ivoire", StringComparison.OrdinalIgnoreCase)) meeting.Country = "كوت ديفوار";
        if (text.Contains("غانا") || text.Contains("Ghana", StringComparison.OrdinalIgnoreCase)) meeting.Country = "غانا";
        if (text.Contains("رواندا") || text.Contains("Rwanda", StringComparison.OrdinalIgnoreCase)) meeting.Country = "رواندا";

        return meeting;
    }

    private MeetingType InferMeetingType(string text)
    {
        if (text.Contains("لجنة") || text.Contains("لجنه")) return MeetingType.Committee;
        if (text.Contains("مقابلة") || text.Contains("مقابلة")) return MeetingType.Interview;
        if (text.Contains("دورة") || text.Contains("تدريب")) return MeetingType.Training;
        if (text.Contains("امتحان") || text.Contains("إمتحان")) return MeetingType.Exam;
        if (text.Contains("ورشة")) return MeetingType.Workshop;
        if (text.Contains("أون لاين") || text.Contains("اونلاين") || text.Contains("زوم") || text.Contains("Zoom")) return MeetingType.Online;
        return MeetingType.Meeting;
    }
}
