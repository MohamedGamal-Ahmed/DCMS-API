namespace DCMS.Domain.Enums;

public enum MeetingType
{
    Meeting = 0,      // اجتماع
    Online = 1,       // أون لاين (Deprecating in favor of IsOnline flag, but keeping for legacy)
    Travel = 2,       // سفر
    PublicHoliday = 3,// إجازة قومية
    Committee = 4,    // لجنة
    Interview = 5,    // مقابلة
    Training = 6,     // دورة تدريبية
    Exam = 7,         // امتحان
    Workshop = 8      // ورشة عمل
}
