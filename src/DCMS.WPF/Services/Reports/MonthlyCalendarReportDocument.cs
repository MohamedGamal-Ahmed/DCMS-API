using DCMS.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DCMS.WPF.Services.Reports;

public class MonthlyCalendarReportDocument : BaseReportDocument
{
    private readonly IEnumerable<Meeting> _items;
    private readonly DateTime _monthDate;

    public MonthlyCalendarReportDocument(string title, IEnumerable<Meeting> items, DateTime monthDate)
        : base(title, monthDate, monthDate.AddMonths(1).AddSeconds(-1))
    {
        _items = items;
        _monthDate = new DateTime(monthDate.Year, monthDate.Month, 1);
    }

    public override DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A3.Landscape());
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.ContentFromRightToLeft();

            page.Header().Element(ComposeCalendarHeader);
            page.Content().Element(ComposeGridContent);
            page.Footer().Element(ComposeCalendarFooter);
        });
    }

    private void ComposeCalendarHeader(IContainer container)
    {
        container.AlignCenter().Column(col =>
        {
            col.Item().Text($"{_monthDate:MMMM yyyy}").FontSize(24).Bold().FontColor(Colors.Black);
            col.Item().PaddingBottom(10).AlignCenter().Text("شركة المقاولون العرب - نظام إدارة المراسلات والاجتماعات").FontSize(11);
        });
    }

    private void ComposeGridContent(IContainer container)
    {
        container.PaddingVertical(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (int i = 0; i < 7; i++) columns.RelativeColumn();
            });

            // Days of week header (RTL: Sunday to Saturday)
            table.Header(header =>
            {
                var days = new[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
                foreach (var day in days)
                {
                    header.Cell().Background("#FFF3E0").Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(day).FontSize(11).Bold().FontColor(Colors.Black);
                }
            });

            // Calendar Cells
            var firstDayOfMonth = _monthDate;
            var daysInMonth = DateTime.DaysInMonth(_monthDate.Year, _monthDate.Month);
            var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek; 

            // Empty cells for leading days
            for (int i = 0; i < startDayOfWeek; i++)
            {
                table.Cell().Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Height(70);
            }

            // Days of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDay = _monthDate.AddDays(day - 1);
                var dayMeetings = _items.Where(m => m.StartDateTime.Date == currentDay.Date).OrderBy(m => m.StartDateTime).ToList();

                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).MinHeight(70).Padding(3).Column(col =>
                {
                    col.Item().Row(r => 
                    {
                        r.RelativeItem().PaddingHorizontal(2).Text(day.ToString())
                            .FontSize(11).Bold()
                            .FontColor(currentDay.DayOfWeek == DayOfWeek.Friday ? Colors.Red.Medium : Colors.Blue.Darken2);
                    });

                    foreach (var meeting in dayMeetings)
                    {
                        col.Item().PaddingTop(2).Background(Colors.Grey.Lighten5).Padding(2).BorderBottom(0.2f).BorderColor(Colors.Grey.Lighten3).Column(mc =>
                        {
                            mc.Item().Text(meeting.Title).FontSize(8).Bold().LineHeight(1.1f);
                            mc.Item().Text(x =>
                            {
                                x.Span($"{meeting.StartDateTime:HH:mm}").FontSize(7).FontColor(Colors.Red.Medium).SemiBold();
                                if (!string.IsNullOrEmpty(meeting.Location))
                                    x.Span($" @ {meeting.Location}").FontSize(7).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    }
                });
            }

            // Fill remaining cells
            int totalCells = startDayOfWeek + daysInMonth;
            int remainingCells = (7 - (totalCells % 7)) % 7;
            for (int i = 0; i < remainingCells; i++)
            {
                table.Cell().Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Height(70);
            }
        });
    }

    private void ComposeCalendarFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("تم إعداد التقرير بواسطة نظام DCMS").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignCenter().Text(x =>
            {
                x.Span("صفحة ");
                x.CurrentPageNumber();
                x.Span(" من ");
                x.TotalPages();
            });
            row.RelativeItem().AlignLeft().Text($"Generated On: {DateTime.Now:yyyy/MM/dd HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }
}
