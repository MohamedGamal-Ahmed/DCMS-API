using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialEngineers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed initial engineers from the old EngineerLists
            migrationBuilder.InsertData(
                schema: "dcms",
                table: "engineers",
                columns: new[] { "full_name", "is_active", "created_at" },
                values: new object[,]
                {
                    { "م/ عزة الدسوقي", true, DateTime.UtcNow },
                    { "م/ ندي القصير", true, DateTime.UtcNow },
                    { "م/ انجي محمد", true, DateTime.UtcNow },
                    { "م/ احمد كرم", true, DateTime.UtcNow },
                    { "م/ هدير عمرو", true, DateTime.UtcNow },
                    { "م/ احمد العصار", true, DateTime.UtcNow },
                    { "م/ حسن مصطفي", true, DateTime.UtcNow },
                    { "م/ هبة ابو العلا", true, DateTime.UtcNow },
                    { "م/ سيد الوزير", true, DateTime.UtcNow },
                    { "د/ محمد انسي الشوتي", true, DateTime.UtcNow },
                    { "م/ دينا عادل فتحي", true, DateTime.UtcNow },
                    { "م/ حسن ابراهيم", true, DateTime.UtcNow },
                    { "م/ محمد علوي", true, DateTime.UtcNow },
                    { "م/ايمان المصري", true, DateTime.UtcNow },
                    { "م/ سمير سعدالله", true, DateTime.UtcNow },
                    { "م/احمد صبحي", true, DateTime.UtcNow },
                    { "م/ علياء عبد العظيم", true, DateTime.UtcNow },
                    { "م/ غادة صلاح", true, DateTime.UtcNow },
                    { "م/شريف عبد الهادي", true, DateTime.UtcNow },
                    { "م/ هبة عبد العال", true, DateTime.UtcNow },
                    { "م/ سلوي سامي", true, DateTime.UtcNow },
                    { "م/ مني كرارة", true, DateTime.UtcNow },
                    { "م/ محمد سعد رفاعي", true, DateTime.UtcNow },
                    { "م/احمد الديب", true, DateTime.UtcNow },
                    { "م/ سامح علي", true, DateTime.UtcNow },
                    { "م/احمد عاطف", true, DateTime.UtcNow },
                    { "م/ امجد علي", true, DateTime.UtcNow },
                    { "م/سامح اللقاني", true, DateTime.UtcNow },
                    { "م/ اماني خضير", true, DateTime.UtcNow },
                    { "م/ سعيد محمود", true, DateTime.UtcNow },
                    { "م/ حمدي هاشم", true, DateTime.UtcNow },
                    { "م/ حمدي حسن", true, DateTime.UtcNow },
                    { "م/احمد مصطفى", true, DateTime.UtcNow },
                    { "م/ محمد شعبان", true, DateTime.UtcNow },
                    { "م/ محمد السيد", true, DateTime.UtcNow },
                    { "م/ احمد عزت", true, DateTime.UtcNow },
                    { "م/ مني عثمان", true, DateTime.UtcNow },
                    { "م/ عبد الرحمن سليمان", true, DateTime.UtcNow },
                    { "م/محمود عبد الفتاح", true, DateTime.UtcNow },
                    { "م/ عبد الحق الصاوي", true, DateTime.UtcNow },
                    { "م/ محمد سلامة", true, DateTime.UtcNow },
                    { "م/ هادي محمد", true, DateTime.UtcNow },
                    { "م/ بسام صفوان", true, DateTime.UtcNow },
                    { "م/ مصطفي عبد المجيد", true, DateTime.UtcNow },
                    { "م/ حسام فايد", true, DateTime.UtcNow },
                    { "م/ طارق صفوت", true, DateTime.UtcNow },
                    { "م /محمد العيروسي", true, DateTime.UtcNow },
                    { "م/ هاني ماهر", true, DateTime.UtcNow },
                    { "م/ هاني خليل", true, DateTime.UtcNow },
                    { "م/ نهي اشرف", true, DateTime.UtcNow },
                    { "م /شريف حمدي", true, DateTime.UtcNow },
                    { "م/ خالد عويس", true, DateTime.UtcNow },
                    { "م/ خالد عيسي", true, DateTime.UtcNow },
                    { "م/ احمد عيسي", true, DateTime.UtcNow },
                    { "م/ اسامة الدسوقي", true, DateTime.UtcNow },
                    { "م/ محمد قاسم", true, DateTime.UtcNow },
                    { "م/ شريف محسن", true, DateTime.UtcNow },
                    { "م/ مرام عشماوي", true, DateTime.UtcNow },
                    { "م/ عوني توفيق", true, DateTime.UtcNow },
                    { "م/رمزي مصطفي", true, DateTime.UtcNow },
                    { "م/ طارق صلاح", true, DateTime.UtcNow },
                    { "م/ ياسر زكريا", true, DateTime.UtcNow },
                    { "م/ علي حمدي", true, DateTime.UtcNow },
                    { "ا/ ايهاب ميرة", true, DateTime.UtcNow },
                    { "ا/عبد العاطي عبيد", true, DateTime.UtcNow },
                    { "ا/ علاء شوقي", true, DateTime.UtcNow },
                    { "ا / موسي علي موسي", true, DateTime.UtcNow },
                    { "ا/ اشرف عطوة", true, DateTime.UtcNow },
                    { "ا/ محمد يحيي", true, DateTime.UtcNow },
                    { "ا/ محمد جمال", true, DateTime.UtcNow },
                    { "ا/ ابراهيم عطا", true, DateTime.UtcNow },
                    { "م/ ابراهيم عباس", true, DateTime.UtcNow },
                    { "م/ محمود مرسي", true, DateTime.UtcNow },
                    { "م/ احمد بركات", true, DateTime.UtcNow },
                    { "م/ محمد حسن رفعت", true, DateTime.UtcNow },
                    { "ا/ محمد غنيم", true, DateTime.UtcNow },
                    { "ا/ عماد عبيد", true, DateTime.UtcNow },
                    { "ا/ حسام عيد", true, DateTime.UtcNow },
                    { "م/ اسامة شوقت", true, DateTime.UtcNow },
                    { "م/ احمد منصور", true, DateTime.UtcNow },
                    { "د / عبد الفتاح المصري", true, DateTime.UtcNow },
                    { "م/ ندا عاطف", true, DateTime.UtcNow },
                    { "م/ محمد شاكر", true, DateTime.UtcNow },
                    { "م/ طارق مكي", true, DateTime.UtcNow },
                    { "م/ ايمن عبدو", true, DateTime.UtcNow },
                    { "م/ يوسف حسين", true, DateTime.UtcNow },
                    { "م/ محمد ابو دشيش", true, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM dcms.engineers");
        }
    }
}
