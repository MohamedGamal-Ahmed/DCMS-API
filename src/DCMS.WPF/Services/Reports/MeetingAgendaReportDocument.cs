using DCMS.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DCMS.WPF.Services.Reports;

public class MeetingAgendaReportDocument : BaseReportDocument
{
    private readonly IEnumerable<Meeting> _items;

    public MeetingAgendaReportDocument(string title, IEnumerable<Meeting> items, DateTime from, DateTime to)
        : base(title, from, to)
    {
        _items = items;
    }

    public override DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));
            page.ContentFromRightToLeft();

            page.Header().Element(ComposeMeetingHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeMeetingFooter);
        });
    }

    private void ComposeMeetingHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("شركة المقاولون العرب - عثمان أحمد عثمان وشركاه").FontSize(11).SemiBold();
                column.Item().Text("نظام إدارة المراسلات والاجتماعات - DCMS").FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().PaddingTop(5).Text(Title).FontSize(13).SemiBold();
                
                if (From.HasValue && To.HasValue)
                {
                    column.Item().Text($"الفترة من: {From:yyyy/MM/dd}  إلى: {To:yyyy/MM/dd}").FontSize(10).FontColor(Colors.Grey.Darken1);
                }
                column.Item().Text($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            // Try to find logo in secondary path if primary fails
            var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "report_logo.png");
            if (System.IO.File.Exists(logoPath))
            {
                row.ConstantItem(80).AlignLeft().Image(logoPath);
            }
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // AI Snapshot Section
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("AI Snapshot / ملخص الأجندة").FontSize(10).SemiBold().FontColor(Colors.Blue.Medium);
                    c.Item().Text($"إجمالي عدد الاجتماعات في هذه الفترة: {_items.Count()}").FontSize(12).Bold();
                });
            });

            column.Item().PaddingVertical(10);

            // Meetings List
            foreach (var meeting in _items.OrderBy(m => m.StartDateTime))
            {
                column.Item().PaddingBottom(15).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(10).Column(c =>
                {
                    // Title Line
                    c.Item().Row(row =>
                    {
                        row.RelativeItem().Text(meeting.Title).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(100).AlignLeft().Text(meeting.StartDateTime.ToLocalTime().ToString("yyyy/MM/dd")).FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    // Time and Location
                    c.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("⏱ الوقت: ").SemiBold();
                            x.Span($"{meeting.StartDateTime.ToLocalTime():HH:mm} - {meeting.EndDateTime.ToLocalTime():HH:mm}");
                            x.Span("    📍 المكان: ").SemiBold();
                            x.Span(meeting.Location ?? "غير محدد");
                        });
                    });

                    // Description / Agenda
                    if (!string.IsNullOrWhiteSpace(meeting.Description))
                    {
                        c.Item().PaddingTop(8).Column(descCol =>
                        {
                            descCol.Item().Text("الأجندة / الموضوعات:").FontSize(10).SemiBold().Underline();
                            descCol.Item().PaddingTop(2).Text(meeting.Description).FontSize(11).LineHeight(1.4f);
                        });
                    }

                    // Participants
                    if (!string.IsNullOrWhiteSpace(meeting.Attendees))
                    {
                        c.Item().PaddingTop(8).Text(x =>
                        {
                            x.Span("👥 المشاركون: ").SemiBold();
                            x.Span(meeting.Attendees);
                        });
                    }
                });
            }
        });
    }

    private void ComposeMeetingFooter(IContainer container)
    {
        container.Column(col => 
        {
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("تم إعداد التقرير بواسطة نظام DCMS").FontSize(9).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignLeft().Text(x =>
                {
                    x.Span("Generated On: ").FontSize(8);
                    x.Span(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")).FontSize(8);
                });
            });

            col.Item().AlignCenter().Text(x =>
            {
                x.Span("صفحة ");
                x.CurrentPageNumber();
                x.Span(" من ");
                x.TotalPages();
            });
        });
    }
}
