using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.WPF.Services
{
    public class CodeEntry
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Engineer { get; set; } = string.Empty;
        
        public string DisplayName => $"{Code} - {Entity}";
    }

    public class CodeLookupService
    {
        private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
        public ObservableCollection<CodeEntry> AvailableCodes { get; } = new();

        public CodeLookupService(IDbContextFactory<DCMSDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Load codes directly
                var codesFromDb = await context.InboundCodes
                    .OrderBy(c => c.Code)
                    .ToListAsync();

                // If empty, seed once and reload
                if (!codesFromDb.Any())
                {
                    await SeedInitialDataAsync(context);
                    codesFromDb = await context.InboundCodes.OrderBy(c => c.Code).ToListAsync();
                }
                else
                {
                    // Temporary Fix: Clean up prefixes if they exist (only if found)
                    if (codesFromDb.Any(c => c.EntityName.StartsWith("رواد من - ") || c.EntityName.StartsWith("مكتب وارد من ")))
                    {
                        await FixExistingData(context);
                        codesFromDb = await context.InboundCodes.OrderBy(c => c.Code).ToListAsync();
                    }
                }

                var codes = codesFromDb.Select(c => new CodeEntry
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Entity = c.EntityName,
                        Engineer = c.EngineerName
                    })
                    .ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableCodes.Clear();
                    foreach (var code in codes)
                    {
                        AvailableCodes.Add(code);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CodeLookupService: {ex.Message}");
            }
        }

        private async Task SeedInitialDataAsync(DCMSDbContext context)
        {
            var initialCodes = new List<InboundCode>
            {
                new() { Code = "CHR", EntityName = "رئيس مجلس الادارة", EngineerName = "م/ احمد العصار" },
                new() { Code = "HBA", EntityName = "نائب رئيس مجلس الادارة", EngineerName = "م. هبة أبو العلا" },
                new() { Code = "SWZ", EntityName = "نائب رئيس مجلس الادارة", EngineerName = "م. سيد الوزير" },
                new() { Code = "DAT", EntityName = "عضو مجلس الادارة", EngineerName = "م. دينا عادل" },
                new() { Code = "ELW", EntityName = "عضو مجلس الادارة", EngineerName = "م. محمد علوي هاشم" },
                new() { Code = "HMS", EntityName = "النائب الاول لرئيس مجلس الادارة", EngineerName = "م. حسن مصطفي" },
                new() { Code = "HIB", EntityName = "عضو مجلس الادارة", EngineerName = "م. حسن ابراهيم" },
                new() { Code = "ANS", EntityName = "نائب رئيس مجلس الادارة", EngineerName = "د. انس البشوتي" },
                new() { Code = "MOS", EntityName = "عضو المجلس التنفيذي", EngineerName = "أ. موسي علي موسي" },
                new() { Code = "CMT", EntityName = "لجان", EngineerName = "لجنة" },
                new() { Code = "GNL", EntityName = "العام", EngineerName = "متنوع" },
                new() { Code = "DEV", EntityName = "تنمية الاعمال", EngineerName = "م/ لبني شتلة" },
                new() { Code = "INS", EntityName = "المعهد التكنولوجي", EngineerName = "م/ شريف حمدي" },
                new() { Code = "ASK", EntityName = "طلبات", EngineerName = "سفر / شكاوي" },
                new() { Code = "Int", EntityName = "مراجعة داخلية", EngineerName = "أ/ طارق صلاح" },
                new() { Code = "EXC", EntityName = "إدارة التعاقدت الدولية", EngineerName = "م/ شريف محسن" },
                new() { Code = "CON", EntityName = "إدارة التعاقدات", EngineerName = "م/ علياء عبد العظيم" },
                new() { Code = "FIN", EntityName = "إدارة المشروعات المنتهية", EngineerName = "ا/رمضان عبد العزيز " },
                new() { Code = "TCA", EntityName = "شئون فنية الشركة", EngineerName = "م/ مني عثمان" },
                new() { Code = "TND", EntityName = "أدارة العطاءات", EngineerName = "م/ احمد صبحي" },
                new() { Code = "QLF", EntityName = "إدارة التاهيل", EngineerName = "م/ غادة صلاح" },
                new() { Code = "CLM", EntityName = "المطالبات والتحكيم", EngineerName = "م/ مني كرارة" },
                new() { Code = "BUY", EntityName = "إدارة المشتريات المركزية", EngineerName = "م/ هبة عبد العال" },
                new() { Code = "PLN", EntityName = "إدارة التخطيط ومتابعة الاحتياجات", EngineerName = "م/ سلوي سامي" },
                new() { Code = "RVW", EntityName = "إدارة المراجعة الفنية", EngineerName = "م/ محمد السيد" },
                new() { Code = "TCS", EntityName = "إدارة الدعم الفني", EngineerName = "م/ احمد الديب" },
                new() { Code = "DBA", EntityName = "مشروع الضبعة", EngineerName = "م/ احمد بركات" },
                new() { Code = "AKP", EntityName = "مشروع أبو قير", EngineerName = "م/ سمير عبد السلام" },
                new() { Code = "KJV", EntityName = "ابوقير (مدكور)", EngineerName = "م/ عبد الحق الصاوي" },
                new() { Code = "JRD", EntityName = "المملكة الاردنية الهاشمية", EngineerName = "" },
                new() { Code = "PRJ", EntityName = "مشروعات (عامة)", EngineerName = "" },
                new() { Code = "CAM", EntityName = "الكاميرون", EngineerName = "م/ اشرف علي حسن " },
                new() { Code = "TNZ", EntityName = "تنزانيا", EngineerName = "م/ محمد محمود سماحه " },
                new() { Code = "AGL", EntityName = "أنغولا", EngineerName = "" },
                new() { Code = "CMR", EntityName = "جزر القمر", EngineerName = "م/ حسن حسون" },
                new() { Code = "NGR", EntityName = "نيجيريا", EngineerName = "" },
                new() { Code = "UGD", EntityName = "أوغندا", EngineerName = "" },
                new() { Code = "CNG", EntityName = "الكونغو", EngineerName = "م/ حسن محمد السيد حسون " },
                new() { Code = "CVR", EntityName = "كوت ديفوار", EngineerName = "" },
                new() { Code = "CHD", EntityName = "تشاد", EngineerName = "م : محمد القيعي" },
                new() { Code = "ZAM", EntityName = "زامبيا", EngineerName = "م/ أحمد محمود السيد " },
                new() { Code = "GIN", EntityName = "غينيا الاستوائية", EngineerName = "" },
                new() { Code = "TOG", EntityName = "توجو", EngineerName = "" },
                new() { Code = "BSW", EntityName = "بتسوانا", EngineerName = "" },
                new() { Code = "KEN", EntityName = "كينيا", EngineerName = "" },
                new() { Code = "GAN", EntityName = "غانا", EngineerName = "م/ احمد منصور" },
                new() { Code = "ETH", EntityName = "اثيوبيا", EngineerName = "" },
                new() { Code = "MEA", EntityName = "السودان / الجزائر / المغرب / تونس / ليبيا / موريتانيا", EngineerName = "" },
                new() { Code = "KSA", EntityName = "السعودية", EngineerName = "م/ عصام عبد الخالق" },
                new() { Code = "OMW", EntityName = "عمان", EngineerName = "م/ محمد امين زهيري" },
                new() { Code = "KWT", EntityName = "الكويت", EngineerName = "م/ عونى توفيق العجماوى " },
                new() { Code = "IRQ", EntityName = "العراق", EngineerName = "م/ محمد بكر " },
                new() { Code = "ASI", EntityName = "الباقي", EngineerName = "متنوع" },
            };

            context.InboundCodes.AddRange(initialCodes);
            await context.SaveChangesAsync();
        }

        private async Task FixExistingData(DCMSDbContext context)
        {
            var codesToFix = await context.InboundCodes
                .Where(c => c.EntityName.StartsWith("رواد من - ") || c.EntityName.StartsWith("مكتب وارد من "))
                .ToListAsync();

            if (codesToFix.Any())
            {
                foreach (var item in codesToFix)
                {
                    item.EntityName = item.EntityName
                        .Replace("رواد من - ", "")
                        .Replace("مكتب وارد من ", "");
                    item.UpdatedAt = DateTime.UtcNow;
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task ReloadCodesAsync()
        {
            await InitializeAsync();
        }

        public async Task AddCodeAsync(string code, string entity, string engineer)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var newCode = new InboundCode
            {
                Code = code,
                EntityName = entity,
                EngineerName = engineer
            };
            context.InboundCodes.Add(newCode);
            await context.SaveChangesAsync();
            await ReloadCodesAsync();
        }

        public async Task UpdateCodeAsync(int id, string code, string entity, string engineer)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.InboundCodes.FindAsync(id);
            if (existing != null)
            {
                existing.Code = code;
                existing.EntityName = entity;
                existing.EngineerName = engineer;
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await ReloadCodesAsync();
            }
        }

        public async Task DeleteCodeAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.InboundCodes.FindAsync(id);
            if (existing != null)
            {
                context.InboundCodes.Remove(existing);
                await context.SaveChangesAsync();
                await ReloadCodesAsync();
            }
        }

        public CodeEntry? GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return AvailableCodes.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }
    }
}
