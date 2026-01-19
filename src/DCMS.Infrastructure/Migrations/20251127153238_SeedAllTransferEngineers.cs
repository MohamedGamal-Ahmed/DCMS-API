using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAllTransferEngineers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert all 85+ transfer engineers (non-responsible)
            migrationBuilder.Sql(@"
                INSERT INTO dcms.engineers (full_name, is_active, is_responsible_engineer, created_at) VALUES
                ('م/ احمد العصار', true, false, CURRENT_TIMESTAMP),
                ('م/ حسن مصطفي', true, false, CURRENT_TIMESTAMP),
                ('م/ هبة ابو العلا', true, false, CURRENT_TIMESTAMP),
                ('م/ سيد الوزير', true, false, CURRENT_TIMESTAMP),
                ('د/ محمد انسي الشوتي', true, false, CURRENT_TIMESTAMP),
                ('م/ دينا عادل فتحي', true, false, CURRENT_TIMESTAMP),
                ('م/ حسن ابراهيم', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد علوي', true, false, CURRENT_TIMESTAMP),
                ('م/ايمان المصري', true, false, CURRENT_TIMESTAMP),
                ('م/ سمير سعدالله', true, false, CURRENT_TIMESTAMP),
                ('م/احمد صبحي', true, false, CURRENT_TIMESTAMP),
                ('م/ علياء عبد العظيم', true, false, CURRENT_TIMESTAMP),
                ('م/ غادة صلاح', true, false, CURRENT_TIMESTAMP),
                ('م/شريف عبد الهادي', true, false, CURRENT_TIMESTAMP),
                ('م/ هبة عبد العال', true, false, CURRENT_TIMESTAMP),
                ('م/ سلوي سامي', true, false, CURRENT_TIMESTAMP),
                ('م/ مني كرارة', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد سعد رفاعي', true, false, CURRENT_TIMESTAMP),
                ('م/احمد الديب', true, false, CURRENT_TIMESTAMP),
                ('م/ سامح علي', true, false, CURRENT_TIMESTAMP),
                ('م/احمد عاطف', true, false, CURRENT_TIMESTAMP),
                ('م/ امجد علي', true, false, CURRENT_TIMESTAMP),
                ('م/سامح اللقاني', true, false, CURRENT_TIMESTAMP),
                ('م/ اماني خضير', true, false, CURRENT_TIMESTAMP),
                ('م/ سعيد محمود', true, false, CURRENT_TIMESTAMP),
                ('م/ حمدي هاشم', true, false, CURRENT_TIMESTAMP),
                ('م/ حمدي حسن', true, false, CURRENT_TIMESTAMP),
                ('م/احمد مصطفى', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد شعبان', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد السيد', true, false, CURRENT_TIMESTAMP),
                ('م/ احمد عزت', true, false, CURRENT_TIMESTAMP),
                ('م/ مني عثمان', true, false, CURRENT_TIMESTAMP),
                ('م/ عبد الرحمن سليمان', true, false, CURRENT_TIMESTAMP),
                ('م/محمود عبد الفتاح', true, false, CURRENT_TIMESTAMP),
                ('م/ عبد الحق الصاوي', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد سلامة', true, false, CURRENT_TIMESTAMP),
                ('م/ هادي محمد', true, false, CURRENT_TIMESTAMP),
                ('م/ بسام صفوان', true, false, CURRENT_TIMESTAMP),
                ('م/ مصطفي عبد المجيد', true, false, CURRENT_TIMESTAMP),
                ('م/ حسام فايد', true, false, CURRENT_TIMESTAMP),
                ('م/ طارق صفوت', true, false, CURRENT_TIMESTAMP),
                ('م /محمد العيروسي', true, false, CURRENT_TIMESTAMP),
                ('م/ هاني ماهر', true, false, CURRENT_TIMESTAMP),
                ('م/ هاني خليل', true, false, CURRENT_TIMESTAMP),
                ('م/ نهي اشرف', true, false, CURRENT_TIMESTAMP),
                ('م /شريف حمدي', true, false, CURRENT_TIMESTAMP),
                ('م/ خالد عويس', true, false, CURRENT_TIMESTAMP),
                ('م/ خالد عيسي', true, false, CURRENT_TIMESTAMP),
                ('م/ احمد عيسي', true, false, CURRENT_TIMESTAMP),
                ('م/ اسامة الدسوقي', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد قاسم', true, false, CURRENT_TIMESTAMP),
                ('م/ شريف محسن', true, false, CURRENT_TIMESTAMP),
                ('م/ مرام عشماوي', true, false, CURRENT_TIMESTAMP),
                ('م/ عوني توفيق', true, false, CURRENT_TIMESTAMP),
                ('م/رمزي مصطفي', true, false, CURRENT_TIMESTAMP),
                ('م/ طارق صلاح', true, false, CURRENT_TIMESTAMP),
                ('م/ ياسر زكريا', true, false, CURRENT_TIMESTAMP),
                ('م/ علي حمدي', true, false, CURRENT_TIMESTAMP),
                ('ا/ ايهاب ميرة', true, false, CURRENT_TIMESTAMP),
                ('ا/عبد العاطي عبيد', true, false, CURRENT_TIMESTAMP),
                ('ا/ علاء شوقي', true, false, CURRENT_TIMESTAMP),
                ('ا / موسي علي موسي', true, false, CURRENT_TIMESTAMP),
                ('ا/ اشرف عطوة', true, false, CURRENT_TIMESTAMP),
                ('ا/ محمد يحيي', true, false, CURRENT_TIMESTAMP),
                ('ا/ محمد جمال', true, false, CURRENT_TIMESTAMP),
                ('ا/ ابراهيم عطا', true, false, CURRENT_TIMESTAMP),
                ('م/ ابراهيم عباس', true, false, CURRENT_TIMESTAMP),
                ('م/ محمود مرسي', true, false, CURRENT_TIMESTAMP),
                ('م/ احمد بركات', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد حسن رفعت', true, false, CURRENT_TIMESTAMP),
                ('ا/ محمد غنيم', true, false, CURRENT_TIMESTAMP),
                ('ا/ عماد عبيد', true, false, CURRENT_TIMESTAMP),
                ('ا/ حسام عيد', true, false, CURRENT_TIMESTAMP),
                ('م/ اسامة شوقت', true, false, CURRENT_TIMESTAMP),
                ('م/ احمد منصور', true, false, CURRENT_TIMESTAMP),
                ('د / عبد الفتاح المصري', true, false, CURRENT_TIMESTAMP),
                ('م/ ندا عاطف', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد شاكر', true, false, CURRENT_TIMESTAMP),
                ('م/ طارق مكي', true, false, CURRENT_TIMESTAMP),
                ('م/ ايمن عبدو', true, false, CURRENT_TIMESTAMP),
                ('م/ يوسف حسين', true, false, CURRENT_TIMESTAMP),
                ('م/ محمد ابو دشيش', true, false, CURRENT_TIMESTAMP)
                ON CONFLICT (full_name) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all seeded engineers
            migrationBuilder.Sql(@"
                DELETE FROM dcms.engineers 
                WHERE full_name IN (
                    'م/ احمد العصار', 'م/ حسن مصطفي', 'م/ هبة ابو العلا', 'م/ سيد الوزير',
                    'د/ محمد انسي الشوتي', 'م/ دينا عادل فتحي', 'م/ حسن ابراهيم', 'م/ محمد علوي',
                    'م/ايمان المصري', 'م/ سمير سعدالله', 'م/احمد صبحي', 'م/ علياء عبد العظيم',
                    'م/ غادة صلاح', 'م/شريف عبد الهادي', 'م/ هبة عبد العال', 'م/ سلوي سامي',
                    'م/ مني كرارة', 'م/ محمد سعد رفاعي', 'م/احمد الديب', 'م/ سامح علي',
                    'م/احمد عاطف', 'م/ امجد علي', 'م/سامح اللقاني', 'م/ اماني خضير',
                    'م/ سعيد محمود', 'م/ حمدي هاشم', 'م/ حمدي حسن', 'م/احمد مصطفى',
                    'م/ محمد شعبان', 'م/ محمد السيد', 'م/ احمد عزت', 'م/ مني عثمان',
                    'م/ عبد الرحمن سليمان', 'م/محمود عبد الفتاح', 'م/ عبد الحق الصاوي',
                    'م/ محمد سلامة', 'م/ هادي محمد', 'م/ بسام صفوان', 'م/ مصطفي عبد المجيد',
                    'م/ حسام فايد', 'م/ طارق صفوت', 'م /محمد العيروسي', 'م/ هاني ماهر',
                    'م/ هاني خليل', 'م/ نهي اشرف', 'م /شريف حمدي', 'م/ خالد عويس',
                    'م/ خالد عيسي', 'م/ احمد عيسي', 'م/ اسامة الدسوقي', 'م/ محمد قاسم',
                    'م/ شريف محسن', 'م/ مرام عشماوي', 'م/ عوني توفيق', 'م/رمزي مصطفي',
                    'م/ طارق صلاح', 'م/ ياسر زكريا', 'م/ علي حمدي', 'ا/ ايهاب ميرة',
                    'ا/عبد العاطي عبيد', 'ا/ علاء شوقي', 'ا / موسي علي موسي', 'ا/ اشرف عطوة',
                    'ا/ محمد يحيي', 'ا/ محمد جمال', 'ا/ ابراهيم عطا', 'م/ ابراهيم عباس',
                    'م/ محمود مرسي', 'م/ احمد بركات', 'م/ محمد حسن رفعت', 'ا/ محمد غنيم',
                    'ا/ عماد عبيد', 'ا/ حسام عيد', 'م/ اسامة شوقت', 'م/ احمد منصور',
                    'د / عبد الفتاح المصري', 'م/ ندا عاطف', 'م/ محمد شاكر', 'م/ طارق مكي',
                    'م/ ايمن عبدو', 'م/ يوسف حسين', 'م/ محمد ابو دشيش'
                );
            ");
        }
    }
}
