using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace DCMS.WPF.Services.Reports;

public abstract class BaseReportDocument : IDocument
{
    protected string Title { get; }
    protected DateTime? From { get; }
    protected DateTime? To { get; }

    protected BaseReportDocument(string title, DateTime? from = null, DateTime? to = null)
    {
        Title = title;
        From = from;
        To = to;
    }

    public abstract DocumentMetadata GetMetadata();
    public abstract void Compose(IDocumentContainer container);

    protected void ComposeHeader(IContainer container)
    {
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "report_logo.png");
        if (!File.Exists(logoPath))
        {
            logoPath = @"d:\DCMS\src\DCMS.WPF\Assets\report_logo.png";
        }

        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("نظام إدارة المراسلات - DCMS").FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text(Title).FontSize(14).SemiBold();
                if (From.HasValue && To.HasValue)
                {
                    column.Item().Text($"الفترة من: {From:yyyy/MM/dd}  إلى: {To:yyyy/MM/dd}").FontSize(10).FontColor(Colors.Grey.Darken1);
                }
                column.Item().Text($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                column.Item().DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken2)).Text(text => 
                {
                    text.Span("Developed by: ");
                    text.Span("Mohamed Gamal").SemiBold();
                });
            });

            if (File.Exists(logoPath))
            {
                row.ConstantItem(100).AlignLeft().Image(logoPath);
            }
        });
    }

    protected void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("صفحة ");
            x.CurrentPageNumber();
            x.Span(" من ");
            x.TotalPages();
        });
    }
}
