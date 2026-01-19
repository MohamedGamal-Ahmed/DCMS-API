using DCMS.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace DCMS.WPF.Services.Reports;

public class SlaReportDocument : BaseReportDocument
{
    private readonly IEnumerable<SlaItem> _items;

    public SlaReportDocument(string title, IEnumerable<SlaItem> items, DateTime from, DateTime to)
        : base(title, from, to)
    {
        _items = items;
    }

    public override DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
            page.ContentFromRightToLeft();

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(15).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // Sequence
                columns.ConstantColumn(70);   // Subject Number
                columns.RelativeColumn(2);    // Subject
                columns.ConstantColumn(80);   // Created At
                columns.ConstantColumn(80);   // First Action
                columns.ConstantColumn(80);   // Completion
                columns.RelativeColumn(1);    // Response Time (Lead)
                columns.RelativeColumn(1);    // Cycle Time
                columns.ConstantColumn(60);   // SLA Status
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("م");
                header.Cell().Element(CellStyle).Text("رقم الوارد");
                header.Cell().Element(CellStyle).Text("الموضوع");
                header.Cell().Element(CellStyle).Text("تاريخ التسجيل");
                header.Cell().Element(CellStyle).Text("أول إجراء");
                header.Cell().Element(CellStyle).Text("تاريخ الإنجاز");
                header.Cell().Element(CellStyle).Text("زمن الاستجابة");
                header.Cell().Element(CellStyle).Text("زمن الإنجاز");
                header.Cell().Element(CellStyle).Text("SLA");

                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold().FontSize(9)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Background(Colors.Grey.Lighten4).AlignCenter();
            });

            int index = 1;
            foreach (var item in _items)
            {
                table.Cell().Element(CellStyle).Text(index++.ToString());
                table.Cell().Element(CellStyle).Text(item.SubjectNumber);
                table.Cell().Element(CellStyle).Text(item.Subject);
                table.Cell().Element(CellStyle).Text(item.RegistrationDate.ToString("yyyy/MM/dd HH:mm"));
                table.Cell().Element(CellStyle).Text(item.FirstActionDate?.ToString("yyyy/MM/dd HH:mm") ?? "-");
                table.Cell().Element(CellStyle).Text(item.CompletionDate?.ToString("yyyy/MM/dd HH:mm") ?? "-");
                table.Cell().Element(CellStyle).Text(item.ResponseTimeDisplay);
                table.Cell().Element(CellStyle).Text(item.CycleTimeDisplay);
                
                var statusColor = item.IsDelayed ? Colors.Red.Medium : Colors.Green.Medium;
                table.Cell().Element(CellStyle).Text(item.SlaStatus).FontColor(statusColor).Bold();

                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4).AlignCenter();
            }
        });
    }
}
