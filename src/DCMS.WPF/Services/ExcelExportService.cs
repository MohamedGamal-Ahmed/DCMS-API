using ClosedXML.Excel;
using DCMS.Domain.Entities;
using System.IO;

namespace DCMS.WPF.Services;

public class ExcelExportService
{
    public void ExportInbounds(List<Inbound> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("المراسلات الواردة");

        // Headers
        worksheet.Cell(1, 1).Value = "رقم الموضوع";
        worksheet.Cell(1, 2).Value = "الكود";
        worksheet.Cell(1, 3).Value = "النوع";
        worksheet.Cell(1, 4).Value = "من جهة";
        worksheet.Cell(1, 5).Value = "من مهندس";
        worksheet.Cell(1, 6).Value = "الموضوع";
        worksheet.Cell(1, 7).Value = "المهندس المسئول";
        worksheet.Cell(1, 8).Value = "تاريخ الوارد";
        worksheet.Cell(1, 9).Value = "الحالة";
        worksheet.Cell(1, 10).Value = "الرد";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.SubjectNumber;
            worksheet.Cell(row, 2).Value = item.Code ?? "";
            worksheet.Cell(row, 3).Value = item.Category.ToString();
            worksheet.Cell(row, 4).Value = item.FromEntity ?? "";
            worksheet.Cell(row, 5).Value = item.FromEngineer ?? "";
            worksheet.Cell(row, 6).Value = item.Subject;
            worksheet.Cell(row, 7).Value = item.ResponsibleEngineer ?? "";
            worksheet.Cell(row, 8).Value = item.InboundDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 9).Value = GetStatusArabic(item.Status);
            worksheet.Cell(row, 10).Value = item.Reply ?? "";
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }

    public void ExportOutbounds(List<Outbound> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("المراسلات الصادرة");

        // Headers
        worksheet.Cell(1, 1).Value = "رقم الموضوع";
        worksheet.Cell(1, 2).Value = "الكود";
        worksheet.Cell(1, 3).Value = "إلى جهة";
        worksheet.Cell(1, 4).Value = "الموضوع";
        worksheet.Cell(1, 5).Value = "تاريخ الصادر";
        worksheet.Cell(1, 6).Value = "الحالة";
        worksheet.Cell(1, 7).Value = "الملاحظات";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.SubjectNumber;
            worksheet.Cell(row, 2).Value = item.Code ?? "";
            worksheet.Cell(row, 3).Value = item.ToEntity ?? "";
            worksheet.Cell(row, 4).Value = item.Subject;
            worksheet.Cell(row, 5).Value = item.OutboundDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 6).Value = ""; // Status removed
            worksheet.Cell(row, 7).Value = ""; // Notes removed
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportMeetings(List<Meeting> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الاجتماعات");

        // Headers
        worksheet.Cell(1, 1).Value = "العنوان";
        worksheet.Cell(1, 2).Value = "الوصف";
        worksheet.Cell(1, 3).Value = "تاريخ البداية";
        worksheet.Cell(1, 4).Value = "تاريخ النهاية";
        worksheet.Cell(1, 5).Value = "المكان";
        worksheet.Cell(1, 6).Value = "المنظم";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Title;
            worksheet.Cell(row, 2).Value = item.Description ?? "";
            worksheet.Cell(row, 3).Value = item.StartDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 4).Value = item.EndDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 5).Value = item.Location ?? "";
            worksheet.Cell(row, 6).Value = ""; // Organizer removed
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportAuditLog(List<AuditLog> data, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("سجل العمليات");

        // Headers
        worksheet.Cell(1, 1).Value = "المستخدم";
        worksheet.Cell(1, 2).Value = "العملية";
        worksheet.Cell(1, 3).Value = "نوع الكائن";
        worksheet.Cell(1, 4).Value = "معرف الكائن";
        worksheet.Cell(1, 5).Value = "التاريخ";
        worksheet.Cell(1, 6).Value = "الوصف";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.UserName;
            worksheet.Cell(row, 2).Value = GetActionArabic(item.Action);
            worksheet.Cell(row, 3).Value = item.EntityType;
            worksheet.Cell(row, 4).Value = item.EntityId;
            worksheet.Cell(row, 5).Value = item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(row, 6).Value = item.Description ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    private string GetStatusArabic(Domain.Enums.CorrespondenceStatus status)
    {
        return status switch
        {
            Domain.Enums.CorrespondenceStatus.New => "جديد",
            Domain.Enums.CorrespondenceStatus.InProgress => "جاري",
            Domain.Enums.CorrespondenceStatus.Completed => "مكتمل",
            Domain.Enums.CorrespondenceStatus.Closed => "مغلق",
            _ => status.ToString()
        };
    }

    private string GetActionArabic(Domain.Enums.AuditActionType action)
    {
        return action switch
        {
            Domain.Enums.AuditActionType.Create => "إضافة",
            Domain.Enums.AuditActionType.Update => "تعديل",
            Domain.Enums.AuditActionType.Delete => "حذف",
            Domain.Enums.AuditActionType.Read => "قراءة",
            _ => action.ToString()
        };
    }
}
