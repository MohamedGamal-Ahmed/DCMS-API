using System.Collections.ObjectModel;
using DCMS.Domain.Enums;
using DCMS.Infrastructure.Data;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media;

namespace DCMS.WPF.ViewModels;

public class ExecutiveAnalysisViewModel : ViewModelBase
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    
    // KPI Properties
    private int _totalActivities;
    private double _onlinePercentage;
    private string _busiestMonth = string.Empty;
    private int _busiestMonthCount;

    public int TotalActivities { get => _totalActivities; set => SetProperty(ref _totalActivities, value); }
    public double OnlinePercentage { get => _onlinePercentage; set => SetProperty(ref _onlinePercentage, value); }
    public string BusiestMonth { get => _busiestMonth; set => SetProperty(ref _busiestMonth, value); }
    public int BusiestMonthCount { get => _busiestMonthCount; set => SetProperty(ref _busiestMonthCount, value); }

    // Chart Series
    public SeriesCollection MonthlySeries { get; set; }
    public string[] MonthlyLabels { get; set; }
    
    public SeriesCollection PeakTimesSeries { get; set; }
    public string[] PeakTimeLabels { get; set; }

    public SeriesCollection TypeSeries { get; set; }
    
    public SeriesCollection MarketSeries { get; set; }
    public string[] MarketLabels { get; set; }
    
    public Func<double, string> NormalFormatter { get; set; }

    public ExecutiveAnalysisViewModel(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        MonthlySeries = new SeriesCollection();
        PeakTimesSeries = new SeriesCollection();
        TypeSeries = new SeriesCollection();
        MarketSeries = new SeriesCollection();

        NormalFormatter = value => value.ToString("N0");

        // Load data initially
        _ = LoadAnalysisData();
    }

    public async Task LoadAnalysisData()
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Filter meetings to current year only
            var currentYear = DateTime.UtcNow.Year;
            var startOfYear = new DateTime(currentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfYear = new DateTime(currentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            
            var allMeetings = await context.Meetings
                .Where(m => m.StartDateTime >= startOfYear && m.StartDateTime <= endOfYear)
                .ToListAsync();

            if (!allMeetings.Any()) 
            {
                 SmartRecommendations = "• لا توجد بيانات كافية لعرض التوصيات.";
                 return;
            }

            // --- KPIS ---
            TotalActivities = allMeetings.Count;
            
            var onlineCount = allMeetings.Count(m => m.IsOnline || m.MeetingType == MeetingType.Online);
            OnlinePercentage = TotalActivities > 0 ? (double)onlineCount / TotalActivities * 100 : 0;

            // --- MONTHLY ANALYSIS ---
            var meetingsByMonth = allMeetings
                .GroupBy(m => m.StartDateTime.ToString("MMM")) // Group by Month Name
                .Select(g => new { Month = g.Key, Count = g.Count(), MonthNum = g.First().StartDateTime.Month })
                .OrderBy(x => x.MonthNum)
                .ToList();

            var busiest = meetingsByMonth.OrderByDescending(x => x.Count).FirstOrDefault();
            if (busiest != null)
            {
                BusiestMonth = busiest.Month;
                BusiestMonthCount = busiest.Count;
            }

            MonthlySeries.Clear();
            MonthlySeries.Add(new ColumnSeries
            {
                Title = "الأنشطة",
                Values = new ChartValues<int>(meetingsByMonth.Select(x => x.Count)),
                Fill = Brushes.DodgerBlue
            });
            MonthlyLabels = meetingsByMonth.Select(x => x.Month).ToArray();
            OnPropertyChanged(nameof(MonthlyLabels));

            // --- DAILY PEAK TIMES ---
            // Morning (6-12), Afternoon (12-18), Evening (18-24), Early (0-6)
            var morning = allMeetings.Count(m => m.StartDateTime.Hour >= 6 && m.StartDateTime.Hour < 12);
            var afternoon = allMeetings.Count(m => m.StartDateTime.Hour >= 12 && m.StartDateTime.Hour < 18);
            var evening = allMeetings.Count(m => m.StartDateTime.Hour >= 18);
            var early = allMeetings.Count(m => m.StartDateTime.Hour < 6);

            PeakTimesSeries.Clear();
            PeakTimesSeries.Add(new ColumnSeries
            {
                Title = "الأنشطة",
                Values = new ChartValues<int> { morning, afternoon, evening, early },
                DataLabels = true
            });
            PeakTimeLabels = new[] { "صباح (6-12)", "ظهر (12-18)", "مساء (18-24)", "فجر (0-6)" };
            OnPropertyChanged(nameof(PeakTimeLabels));

            // --- TYPOLOGY (Pic Chart) ---
            var typeGroups = allMeetings
                .GroupBy(m => m.MeetingType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            TypeSeries.Clear();
            foreach (var group in typeGroups)
            {
                TypeSeries.Add(new PieSeries
                {
                    Title = GetArabicMeetingType(group.Type),
                    Values = new ChartValues<int> { group.Count },
                    DataLabels = true,
                    LabelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation) 
                });
            }

            // --- MARKETS / COUNTRIES ---
            // Normalize and Group
            var countryGroups = allMeetings
                .Where(m => !string.IsNullOrWhiteSpace(m.Country))
                .GroupBy(m => NormalizeCountryName(m.Country))
                .Select(g => new { Country = g.First().Country.Trim(), NormalizedName = g.Key, Count = g.Count() })
                .OrderBy(x => x.Count)
                .ToList();

            // Take top 10 for bar chart
            var topCountries = countryGroups
                .OrderBy(x => x.Count) // Ascending for RowSeries
                .Take(10)
                .ToList();

            MarketSeries.Clear();
            MarketSeries.Add(new RowSeries
            {
                Title = "الزيارات/الاجتماعات",
                Values = new ChartValues<int>(topCountries.Select(x => x.Count)),
                DataLabels = true,
                RowPadding = 10 
            });
            MarketLabels = topCountries.Select(x => x.Country).ToArray();
            OnPropertyChanged(nameof(MarketLabels));

            // --- WORLD HEAT MAP ---
            var mapValues = new Dictionary<string, double>();
            foreach (var group in countryGroups)
            {
                var isoCode = GetCountryIsoCode(group.NormalizedName);
                if (!string.IsNullOrEmpty(isoCode))
                {
                    if (mapValues.ContainsKey(isoCode))
                    {
                        mapValues[isoCode] += group.Count;
                    }
                    else
                    {
                        mapValues.Add(isoCode, group.Count);
                    }
                }
            }
            HeatMapValues = mapValues;
            
            // --- SMART RECOMMENDATIONS ---
            CalculateSmartRecommendations(busiest, morning, afternoon, countryGroups.OrderByDescending(x => x.Count).FirstOrDefault());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exec Analysis Error: {ex.Message}");
        }
    }

    private Dictionary<string, double> _heatMapValues;
    public Dictionary<string, double> HeatMapValues
    {
        get => _heatMapValues;
        set => SetProperty(ref _heatMapValues, value);
    }

    private string NormalizeCountryName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        
        var text = input.Trim();
        // Simple normalization for common Arabic mismatches
        text = text.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا");
        text = text.Replace("ة", "ه");
        text = text.Replace("ى", "ي");
        
        return text.ToLower();
    }

    private string GetCountryIsoCode(string normalizedName)
    {
        // Simple mapping for demonstration - expand as needed
        if (normalizedName.Contains("اردن")) return "JO";
        if (normalizedName.Contains("مصر")) return "EG";
        if (normalizedName.Contains("سعودي")) return "SA";
        if (normalizedName.Contains("امارات")) return "AE";
        if (normalizedName.Contains("كويت")) return "KW";
        if (normalizedName.Contains("قطر")) return "QA";
        if (normalizedName.Contains("عمان")) return "OM";
        if (normalizedName.Contains("بحرين")) return "BH";
        if (normalizedName.Contains("سوريا")) return "SY";
        if (normalizedName.Contains("عراق")) return "IQ";
        if (normalizedName.Contains("لبنان")) return "LB";
        if (normalizedName.Contains("فلسطين")) return "PS";
        if (normalizedName.Contains("يمن")) return "YE";
        if (normalizedName.Contains("سودان")) return "SD";
        if (normalizedName.Contains("ليبيا")) return "LY";
        if (normalizedName.Contains("تونس")) return "TN";
        if (normalizedName.Contains("جزائر")) return "DZ";
        if (normalizedName.Contains("مغرب")) return "MA";
        if (normalizedName.Contains("موريتانيا")) return "MR";
        if (normalizedName.Contains("صومال")) return "SO";
        if (normalizedName.Contains("جيبوتي")) return "DJ";
        if (normalizedName.Contains("جزر القمر")) return "KM";
        
        // Africa / Others
        if (normalizedName.Contains("غانا")) return "GH";
        if (normalizedName.Contains("كوت ديفوار") || normalizedName.Contains("ساحل العاج")) return "CI";
        if (normalizedName.Contains("زامبيا")) return "ZM";
        if (normalizedName.Contains("كينيا")) return "KE";
        if (normalizedName.Contains("اثيوبيا")) return "ET";
        if (normalizedName.Contains("نيجيريا")) return "NG";
        if (normalizedName.Contains("جنوب افريقيا")) return "ZA";
        if (normalizedName.Contains("هند")) return "IN";
        if (normalizedName.Contains("صين")) return "CN";
        if (normalizedName.Contains("امريكا") || normalizedName.Contains("الولايات")) return "US";
        if (normalizedName.Contains("بريطانيا") || normalizedName.Contains("انجلترا")) return "GB";
        if (normalizedName.Contains("فرنسا")) return "FR";
        if (normalizedName.Contains("المانيا")) return "DE";
        if (normalizedName.Contains("ايطاليا")) return "IT";
        if (normalizedName.Contains("اسبانيا")) return "ES";
        if (normalizedName.Contains("روسيا")) return "RU";
        if (normalizedName.Contains("تركيا")) return "TR";

        return string.Empty; // Not found
    }

    private string _smartRecommendations;
    public string SmartRecommendations
    {
        get => _smartRecommendations;
        set => SetProperty(ref _smartRecommendations, value);
    }

    private void CalculateSmartRecommendations(dynamic? busiestMonth, int morningCount, int afternoonCount, dynamic? topCountry)
    {
        var recommendations = new System.Text.StringBuilder();

        // 1. Seasonal Pressure
        if (busiestMonth != null)
        {
             recommendations.AppendLine($"• شهر {GetArabicMonth(busiestMonth.Month)} هو الأعلى ضغطاً ({busiestMonth.Count} نشاطاً)، يُنصح بتوزيع الأعمال غير العاجلة على أشهر أخرى.");
        }

        // 2. Daily Schedule
        if (morningCount > afternoonCount * 1.5) // If morning is significantly busier
        {
            recommendations.AppendLine("• الفترة الصباحية (6-12) مزدحمة جداً، حاول استغلال فترة الظهيرة للأعمال المكتبية والتركيز.");
        }
        else if (afternoonCount > morningCount * 1.5)
        {
            recommendations.AppendLine("• فترة الظهيرة (12-18) مزدحمة، يُفضل جدولة الاجتماعات الهامة صباحاً.");
        }
        else
        {
             recommendations.AppendLine("• توزيع الأنشطة اليومي متوازن نسبياً.");
        }

        // 3. International Travel
        if (topCountry != null && !string.IsNullOrWhiteSpace(topCountry.Country))
        {
            recommendations.AppendLine($"• {topCountry.Country} هي الوجهة الأكثر تكراراً، تأكد من تنسيق الرحلات والحجوزات مسبقاً.");
        }

        SmartRecommendations = recommendations.ToString();
    }
    
    private string GetArabicMonth(string engMonth)
    {
        return engMonth switch
        {
             "Jan" => "يناير", "Feb" => "فبراير", "Mar" => "مارس", "Apr" => "أبريل",
             "May" => "مايو", "Jun" => "يونيو", "Jul" => "يوليو", "Aug" => "أغسطس",
             "Sep" => "سبتمبر", "Oct" => "أكتوبر", "Nov" => "نوفمبر", "Dec" => "ديسمبر",
             _ => engMonth
        };
    }

    private string GetArabicMeetingType(MeetingType type)
    {
        return type switch
        {
            MeetingType.Meeting => "اجتماع",
            MeetingType.Online => "أون لاين (قديم)",
            MeetingType.Travel => "سفر",
            MeetingType.PublicHoliday => "إجازة",
            MeetingType.Committee => "لجنة",
            MeetingType.Interview => "مقابلة",
            MeetingType.Training => "دورة تدريبية",
            MeetingType.Exam => "امتحان",
            MeetingType.Workshop => "ورشة عمل",
            _ => type.ToString()
        };
    }
}
