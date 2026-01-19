using DCMS.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace DCMS.WPF.Services.Reports;

public class InventoryReportDocument : BaseReportDocument
{
    private readonly IEnumerable<InventoryItem> _items;

    public InventoryReportDocument(string title, IEnumerable<InventoryItem> items, DateTime from, DateTime to)
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
                columns.ConstantColumn(70);   // Inbound Date
                columns.ConstantColumn(70);   // Subject Number
                columns.RelativeColumn(2);    // Subject
                columns.RelativeColumn(1);    // From Entity
                columns.RelativeColumn(1);    // Responsible Engineer
                columns.ConstantColumn(60);   // Status
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("م");
                header.Cell().Element(CellStyle).Text("التاريخ");
                header.Cell().Element(CellStyle).Text("رقم الوارد");
                header.Cell().Element(CellStyle).Text("الموضوع");
                header.Cell().Element(CellStyle).Text("الجهة");
                header.Cell().Element(CellStyle).Text("المسئول");
                header.Cell().Element(CellStyle).Text("الحالة");

                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold().FontSize(10)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Background(Colors.Grey.Lighten4).AlignCenter();
            });

            int index = 1;
            foreach (var item in _items)
            {
                table.Cell().Element(CellStyle).Text(index++.ToString());
                table.Cell().Element(CellStyle).Text(item.Date.ToString("yyyy/MM/dd"));
                table.Cell().Element(CellStyle).Text(item.SubjectNumber ?? "-");
                table.Cell().Element(CellStyle).Text(item.Subject);
                table.Cell().Element(CellStyle).Text(item.FromEntity);
                table.Cell().Element(CellStyle).Text(item.ResponsibleEngineer ?? "-");
                table.Cell().Element(CellStyle).Text(item.Status);

                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(2);
            }
        });
    }
}
