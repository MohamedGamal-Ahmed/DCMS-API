using DCMS.Application.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace DCMS.WPF.Services.Reports;

public class TransmittalReportDocument : BaseReportDocument
{
    private readonly IEnumerable<TransmittalItem> _items;
    private readonly string _recipientName;

    public TransmittalReportDocument(string title, IEnumerable<TransmittalItem> items, string recipientName)
        : base(title)
    {
        _items = items;
        _recipientName = recipientName;
    }

    public override DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public override void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
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
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Text($"السيد المهندس/ {_recipientName}").FontSize(14).SemiBold().Underline();
            column.Item().PaddingBottom(10).Text("تحية طيبة وبعد،،، نرسل لسيادتكم المراسلات التالية للاستلام والعمل بموجبها:");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Sequence
                    columns.RelativeColumn(3);   // Subject
                    columns.RelativeColumn(1);   // Date
                    columns.RelativeColumn(2);   // Sender
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("م");
                    header.Cell().Element(CellStyle).Text("الموضوع");
                    header.Cell().Element(CellStyle).Text("التاريخ");
                    header.Cell().Element(CellStyle).Text("الجهة الواردة من");

                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                });

                int index = 1;
                foreach (var item in _items)
                {
                    table.Cell().Element(CellStyle).Text(index++.ToString());
                    table.Cell().Element(CellStyle).Text(item.Subject);
                    table.Cell().Element(CellStyle).Text(item.Date.ToString("yyyy/MM/dd"));
                    table.Cell().Element(CellStyle).Text(item.Sender);

                    static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                }
            });

            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("توقيع المستلم:");
                    c.Item().PaddingTop(30).Text(".........................");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("يعتمد مدير النظام:");
                    c.Item().PaddingTop(30).Text(".........................");
                });
            });
        });
    }
}
