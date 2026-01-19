using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using DCMS.Domain.Models; // Changed from DCMS.Domain.Entities;

namespace DCMS.WPF.Services;

public class AiRoiReportService
{
    static AiRoiReportService()
    {
        // Set QuestPDF license to Community
        QuestPDF.Settings.License = LicenseType.Community;
        // Basic Arabic font support if system font is available, otherwise defaults might be square boxes
        // Ideally we embed a font, but for now we rely on Arial or similar common fonts
    }

    public void GenerateReport(string filePath, RoiReportData data)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                
                // Use a font that likely supports Arabic on Windows
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial")); 
                page.ContentFromRightToLeft(); // IMPORTANT: Enable RTL layout for the whole page

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("نظام إدارة المراسلات والوثائق (DCMS)").FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text("تقرير كفاءة وعائد الاستثمار (ROI) - المساعد الذكي").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                    });

                    // Logo removed to avoid broken placeholder
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                {
                    x.Spacing(20);

                    // 1. Executive Summary Cards (AI Metrics)
                    x.Item().Row(row =>
                    {
                        row.Spacing(10);
                        
                        row.RelativeItem().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten4).Padding(10).Column(col =>
                        {
                            col.Item().Text("الساعات الموفّرة").FontSize(10).FontColor(Colors.Blue.Medium);
                            col.Item().Text($"{data.TotalHoursSaved:F1} ساعة").FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                        });

                        row.RelativeItem().Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Lighten4).Padding(10).Column(col =>
                        {
                            col.Item().Text("ما يعادل جهد بشري (FTE)").FontSize(10).FontColor(Colors.Green.Medium);
                            col.Item().Text($"{data.FteImpact:F2} موظف").FontSize(16).SemiBold().FontColor(Colors.Green.Medium);
                        });

                        row.RelativeItem().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten4).Padding(10).Column(col =>
                        {
                            col.Item().Text("دقة وأداء المساعد").FontSize(10).FontColor(Colors.Orange.Medium);
                            col.Item().Text($"{data.SuccessRate:F0}%").FontSize(16).SemiBold().FontColor(Colors.Orange.Medium);
                        });
                    });

                    // 2. System Usage Stats
                    x.Item().Column(col => 
                    {
                        col.Item().Text("إحصائيات النظام العام (سنة 2026)").FontSize(14).SemiBold();
                        col.Item().PaddingTop(10).Row(row => 
                        {
                            row.Spacing(10);
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c => {
                                c.Item().Text("تم استلام").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{data.TotalReceived} مراسلة").FontSize(14).SemiBold();
                            });
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c => {
                                c.Item().Text("تم عرض/دراسة").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{data.TotalPresented} موضوع").FontSize(14).SemiBold();
                            });
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c => {
                                c.Item().Text("تم تحويل").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{data.TotalTransferred} معاملة").FontSize(14).SemiBold();
                            });
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c => {
                                c.Item().Text("قيد الدراسة/جديد").FontSize(10).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{data.TotalPending} موضوع").FontSize(14).SemiBold();
                            });
                        });
                    });

                    // 3. Narrative Section
                    x.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(col =>
                    {
                        col.Item().Text("ملخص الأداء الشهري").FontSize(14).SemiBold();
                        col.Item().PaddingTop(5).Text(data.Narrative).FontSize(11).LineHeight(1.5f);
                    });

                    // 4. Detailed Usage Table
                    x.Item().Column(col =>
                    {
                        col.Item().Text("تفاصيل استخدام الأدوات").FontSize(14).SemiBold();
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40); // #
                                columns.RelativeColumn();  // Tool
                                columns.ConstantColumn(80); // Count
                                columns.ConstantColumn(100); // Saved Time
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("الأداة الذكية");
                                header.Cell().Element(CellStyle).Text("مرات الاستخدام");
                                header.Cell().Element(CellStyle).Text("الوقت الموفّر");

                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int i = 1;
                            foreach (var item in data.ToolUsage)
                            {
                                table.Cell().Element(CellStyle).Text(i++.ToString());
                                table.Cell().Element(CellStyle).Text(item.ToolNameAR);
                                table.Cell().Element(CellStyle).Text(item.Count.ToString());
                                table.Cell().Element(CellStyle).Text($"{item.HoursSaved:F1} ساعة");

                                static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                            }
                        });
                    });

                    // 5. Engineer Workload Table with Visualization
                    if (data.EngineerWorkload?.Any() == true)
                    {
                        x.Item().Column(col =>
                        {
                            col.Item().Text("أعباء العمل على المهندسين").FontSize(14).SemiBold();
                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(2); // Name
                                    columns.RelativeColumn(1); // Open
                                    columns.RelativeColumn(1); // Closed
                                    columns.RelativeColumn(1); // Total
                                    columns.ConstantColumn(20); // Status Indicator
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("#");
                                    header.Cell().Element(CellStyle).Text("المهندس");
                                    header.Cell().Element(CellStyle).Text("جاري");
                                    header.Cell().Element(CellStyle).Text("منجز");
                                    header.Cell().Element(CellStyle).Text("الإجمالي");
                                    header.Cell().Element(CellStyle).Text(""); // Status

                                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                });

                                int i = 1;
                                var maxLoad = data.EngineerWorkload.Max(e => e.OpenTasks + e.ClosedTasks);
                                if (maxLoad == 0) maxLoad = 1;


                                foreach (var eng in data.EngineerWorkload)
                                {
                                    var total = eng.OpenTasks + eng.ClosedTasks;
                                    var loadRatio = (double)eng.OpenTasks / maxLoad; 
                                    
                                    // Status color based on open tasks ratio
                                    var statusColor = Colors.Green.Medium;
                                    if (eng.OpenTasks > 10) statusColor = Colors.Orange.Medium;
                                    if (eng.OpenTasks > 20) statusColor = Colors.Red.Medium;

                                    table.Cell().Element(CellStyle).Text(i++.ToString());
                                    table.Cell().Element(CellStyle).Text(eng.Name);
                                    table.Cell().Element(CellStyle).Text(eng.OpenTasks.ToString());
                                    table.Cell().Element(CellStyle).Text(eng.ClosedTasks.ToString());
                                    table.Cell().Element(CellStyle).Text(total.ToString());
                                    table.Cell().Element(CellStyle).MinHeight(15).Width(15).Height(15).Background(statusColor).CornerRadius(7.5f);

                                    static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                                }
                            });
                        });
                    }

                    // 6. Staff Performance Table (Mowazafeen)
                    if (data.StaffPerformance?.Any() == true)
                    {
                        x.Item().Column(col =>
                        {
                            col.Item().Text("أداء موظفي المتابعة والإداريين").FontSize(14).SemiBold();
                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1); // Total
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("#");
                                    header.Cell().Element(CellStyle).Text("الموظف");
                                    header.Cell().Element(CellStyle).Text("تسجيل");
                                    header.Cell().Element(CellStyle).Text("إغلاق");
                                    header.Cell().Element(CellStyle).Text("الإجمالي");

                                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                });

                                int i = 1;
                                foreach (var emp in data.StaffPerformance)
                                {
                                    table.Cell().Element(CellStyle).Text(i++.ToString());
                                    table.Cell().Element(CellStyle).Text(emp.UserName);
                                    table.Cell().Element(CellStyle).Text(emp.Registrations.ToString());
                                    table.Cell().Element(CellStyle).Text(emp.Closures.ToString());
                                    table.Cell().Element(CellStyle).Text(emp.TotalActivity.ToString());

                                    static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                                }
                            });
                        });
                    }

                    // 7. External Distribution Table
                    if (data.ExternalDistribution?.Any() == true)
                    {
                        x.Item().Column(col =>
                        {
                            col.Item().Text("التوزيع الخارجي (الجهات الخارجية)").FontSize(14).SemiBold();
                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("#");
                                    header.Cell().Element(CellStyle).Text("الجهة/الشخص");
                                    header.Cell().Element(CellStyle).Text("عدد الموضوعات");

                                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                });

                                int i = 1;
                                foreach (var ext in data.ExternalDistribution.Take(15))
                                {
                                    table.Cell().Element(CellStyle).Text(i++.ToString());
                                    table.Cell().Element(CellStyle).Text(ext.Name);
                                    table.Cell().Element(CellStyle).Text(ext.Count.ToString());

                                    static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                                }
                            });
                        });
                    }

                    // 8. Executive Recommendations (Management Insights)
                    if (!string.IsNullOrEmpty(data.ManagementInsights))
                    {
                        x.Item().Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(15).Column(col =>
                        {
                            col.Item().Text("✨ التوصيات التنفيذية (Executive Recommendations)").FontSize(14).SemiBold().FontColor(Colors.Purple.Medium);
                            col.Item().PaddingTop(10).Text(data.ManagementInsights).FontSize(11).LineHeight(1.5f);
                        });
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("تاريخ الطباعة: ").FontColor(Colors.Grey.Darken1);
                            x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm tt")).SemiBold();
                        });

                        row.RelativeItem().AlignLeft().Text(x =>
                        {
                            x.Span("Developed by: ").FontColor(Colors.Grey.Darken1);
                            x.Span("Mohamed Gamal").SemiBold().FontColor(Colors.Blue.Darken2);
                        });
                    });
                });
            });
        }).GeneratePdf(filePath);
    }
}

public class RoiReportData
{
    public double TotalHoursSaved { get; set; }
    public double FteImpact { get; set; }
    public double SuccessRate { get; set; }
    public int TotalReceived { get; set; }
    public int TotalPresented { get; set; }
    public int TotalPending { get; set; }
    public int TotalTransferred { get; set; }
    public string Narrative { get; set; } = string.Empty;
    public string ManagementInsights { get; set; } = string.Empty;
    public List<ToolUsageStats> ToolUsage { get; set; } = new();
    public List<EngineerWorkloadStats> EngineerWorkload { get; set; } = new();
    public List<UserPerformanceItem> StaffPerformance { get; set; } = new();
    public List<ExternalDistributionStats> ExternalDistribution { get; set; } = new();
}

public class ToolUsageStats
{
    public string ToolNameAR { get; set; } = string.Empty;
    public int Count { get; set; }
    public double HoursSaved { get; set; }
}
