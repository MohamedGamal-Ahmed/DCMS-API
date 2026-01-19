using DCMS.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace DCMS.WPF.Services.Reports;

public class EngineerPerformanceReportDocument : BaseReportDocument
{
    private readonly IEnumerable<EngineerPerformanceItem> _items;

    public EngineerPerformanceReportDocument(string title, IEnumerable<EngineerPerformanceItem> items, DateTime from, DateTime to)
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

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // Sequence
                columns.RelativeColumn();     // Engineer Name
                columns.ConstantColumn(80);   // Open
                columns.ConstantColumn(80);   // Delayed
                columns.ConstantColumn(80);   // Completion %
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("م");
                header.Cell().Element(CellStyle).Text("اسم المهندس");
                header.Cell().Element(CellStyle).Text("جاري (مفتوح)");
                header.Cell().Element(CellStyle).Text("متأخر");
                header.Cell().Element(CellStyle).Text("نسبة الإنجاز");

                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold().FontSize(10)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Background(Colors.Grey.Lighten4).AlignCenter();
            });

            int index = 1;
            foreach (var item in _items)
            {
                table.Cell().Element(CellStyle).Text(index++.ToString());
                table.Cell().Element(CellStyle).Text(item.EngineerName);
                table.Cell().Element(CellStyle).Text(item.OpenCount.ToString());
                table.Cell().Element(CellStyle).Text(item.DelayedCount.ToString()).FontColor(item.DelayedCount > 0 ? Colors.Red.Medium : Colors.Black);
                table.Cell().Element(CellStyle).Text($"{item.CompletionRate:P0}");

                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4).AlignCenter();
            }
        });
    }
}
