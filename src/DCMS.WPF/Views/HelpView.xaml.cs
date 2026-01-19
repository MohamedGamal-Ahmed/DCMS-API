using System.Windows;
using System.Windows.Controls;

namespace DCMS.WPF.Views;

public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();
    }

    private void LstSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstSections.SelectedItem is ListBoxItem item && item.Tag != null)
        {
            string section = item.Tag.ToString()!;
            LoadSection(section);
        }
    }

    private void LoadSection(string section)
    {
        stkContent.Children.Clear();
        
        var titleBlock = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.DarkSlateGray,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var contentBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.DarkSlateGray,
            LineHeight = 28
        };

        switch (section)
        {
            case "intro":
                titleBlock.Text = "1. مقدمة ومتطلبات التشغيل";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("ما هو نظام DCMS؟") { FontWeight = FontWeights.Bold, FontSize = 16 });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("DCMS (Document Control Management System) هو نظام متكامل لإدارة المراسلات والوثائق داخل المؤسسات.");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("يتيح النظام:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ تسجيل الوارد والصادر");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ تتبع التحويلات بين المهندسين");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ البحث المتقدم والمتابعة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ إدارة الاجتماعات والمواعيد");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ تصدير التقارير إلى Excel");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("✅ نظام إشعارات فوري");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("متطلبات التشغيل:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• نظام التشغيل: Windows 10 أو أحدث");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• الذاكرة: 4 GB RAM");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• المعالج: Intel Core i3 أو ما يعادله");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• الإنترنت: مطلوب (للاتصال بقاعدة البيانات)");
                break;

            case "login":
                titleBlock.Text = "2. تسجيل الدخول";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("خطوات تسجيل الدخول:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. أدخل اسم المستخدم في الحقل الأول");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. أدخل كلمة المرور في الحقل الثاني");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. اضغط زر \"دخول\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("حساب المسؤول الافتراضي:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• اسم المستخدم: Admin");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• كلمة المرور: admin123");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("⚠️ يُنصح بتغيير كلمة المرور الافتراضية");
                break;

            case "dashboard":
                titleBlock.Text = "3. لوحة التحكم";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add("لوحة التحكم هي الشاشة الرئيسية التي تظهر بعد تسجيل الدخول.");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("الإحصائيات الرئيسية:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• عدد الوارد اليوم");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• عدد الصادر اليوم");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• المهام المعلقة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• الإشعارات");
                break;

            case "inbound":
                titleBlock.Text = "4. إدارة الوارد";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("أنواع الوارد (6 أنواع):") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("📮 بوستا - المراسلات البريدية الرسمية");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("📧 إيميل - المراسلات الإلكترونية");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("📄 عقد - العقود والاتفاقيات");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("📝 طلب - طلبات مختلفة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("🚗 مأمورية - تكليفات العمل الخارجي");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("📋 تفويض - تفويضات رسمية");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("لإضافة وارد جديد:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. اضغط \"وارد جديد\" من القائمة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. اختر نوع الوارد");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. املأ البيانات المطلوبة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("4. اضغط \"حفظ\"");
                break;

            case "outbound":
                titleBlock.Text = "5. إدارة الصادر";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("لإضافة صادر جديد:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. اضغط \"صادر جديد\" من القائمة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. املأ البيانات:");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • كود الصادر");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • الموضوع");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • إلى جهة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • تاريخ الصادر");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. اضغط \"حفظ\"");
                break;

            case "search":
                titleBlock.Text = "6. البحث والمتابعة";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("فلاتر البحث المتاحة:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• نوع السجل (الكل، بوستا، إيميل، عقد، إلخ)");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• الكود");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• الموضوع");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• من جهة");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• من تاريخ / إلى تاريخ");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• محول إلى");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("عرض التفاصيل:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("انقر مرتين على أي صف لفتح التفاصيل");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("تصدير Excel:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("اضغط زر \"تصدير Excel\" لتحميل النتائج");
                break;

            case "meetings":
                titleBlock.Text = "7. جدول الاجتماعات";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("لإضافة اجتماع جديد:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. اضغط \"اجتماع جديد\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. املأ البيانات:");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • عنوان الاجتماع");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • التاريخ والوقت");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • المكان");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("   • الحضور");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. اضغط \"حفظ\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("يظهر التقويم الشهري مع الاجتماعات المجدولة");
                break;

            case "users":
                titleBlock.Text = "8. إدارة المستخدمين";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("صلاحيات المستخدمين:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• Admin - جميع الصلاحيات");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• User - إضافة وتعديل المراسلات فقط");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("لإضافة مستخدم جديد (Admin فقط):") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. اذهب إلى \"إدارة المستخدمين\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. اضغط \"إضافة مستخدم\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. املأ البيانات");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("4. اضغط \"حفظ\"");
                break;

            case "audit":
                titleBlock.Text = "9. سجل المراجعة (Audit)";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add("يسجل النظام جميع العمليات التي تتم على البيانات:");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• إنشاء سجل جديد");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• تعديل سجل");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• حذف سجل");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• تحويل لمهندس");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("تفاصيل العملية:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("انقر مرتين على أي صف لرؤية القيم القديمة والجديدة");
                break;

            case "backup":
                titleBlock.Text = "10. النسخ الاحتياطي";
                contentBlock.Inlines.Clear();
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("تصدير البيانات:") { FontWeight = FontWeights.Bold });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("1. اذهب إلى \"النسخ الاحتياطي\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("2. اضغط \"تصدير البيانات\"");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("3. اختر مكان حفظ الملف");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add(new System.Windows.Documents.Run("⚠️ ملاحظات هامة:") { FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.OrangeRed });
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• احتفظ بنسخة احتياطية أسبوعياً على الأقل");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• احفظ النسخة في مكان آمن (USB / Google Drive)");
                contentBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                contentBlock.Inlines.Add("• لا تحذف النسخ القديمة فوراً");
                break;
        }

        stkContent.Children.Add(titleBlock);
        stkContent.Children.Add(contentBlock);
    }
}
