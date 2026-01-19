using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCMS.Application.Interfaces;
using DCMS.Domain.Enums;

namespace DCMS.WPF.Services
{
    public class BotService
    {
        private readonly SignalRService _signalRService;
        private readonly IMeetingService _meetingService;
        private readonly ICorrespondenceService _correspondenceService;
        private const string BotName = "DCMS Bot 🤖";

        public BotService(
            SignalRService signalRService,
            IMeetingService meetingService,
            ICorrespondenceService correspondenceService)
        {
            _signalRService = signalRService;
            _meetingService = meetingService;
            _correspondenceService = correspondenceService;
        }

        public async Task StartAsync()
        {
            // Wait for SignalR to connect
            int retries = 0;
            while (!_signalRService.IsConnected && retries < 10)
            {
                await Task.Delay(1000);
                retries++;
            }

            if (_signalRService.IsConnected)
            {
                await SendDailySummaryAsync();
                await SendDelayedRemindersAsync();
            }
        }

        private async Task SendDailySummaryAsync()
        {
            try
            {
                var today = DateTime.Today;
                var meetings = await _meetingService.SearchMeetingsAsync(startDate: today, endDate: today.AddDays(1).AddSeconds(-1));

                if (meetings != null && meetings.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("📅 **ملخص اجتماعات اليوم:**");
                    foreach (var meeting in meetings)
                    {
                        var time = DateTime.TryParse(meeting.Date, out var dt) ? dt.ToString("HH:mm") : "غير محدد";
                        sb.AppendLine($"- {meeting.Subject} (🕒 {time})");
                    }
                    await _signalRService.SendMessageAsync(BotName, sb.ToString());
                }
                else
                {
                    await _signalRService.SendMessageAsync(BotName, "✅ لا توجد اجتماعات مقررة لليوم.");
                }
            }
            catch (Exception)
            {
                // Fallback if service fails
            }
        }

        private async Task SendDelayedRemindersAsync()
        {
            try
            {
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                
                // Get pending inbounds/outbounds older than 7 days (no status filter as Pending doesn't exist)
                var delayedItems = await _correspondenceService.SearchAsync(
                    endDate: sevenDaysAgo);

                var items = delayedItems.Where(i => i.Status != "Completed" && i.Status != "Closed").ToList();

                if (items.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("⚠️ **تنبيه: مراسلات متأخرة (> 7 أيام):**");
                    foreach (var item in items.Take(5)) // Limit to 5 for chat clarity
                    {
                        var code = item.SubjectNumber ?? "???";
                        sb.AppendLine($"- {code}: {item.Subject}");
                    }
                    if (items.Count() > 5)
                        sb.AppendLine($"... و {items.Count() - 5} مراسلات أخرى.");

                    await _signalRService.SendMessageAsync(BotName, sb.ToString());
                }
            }
            catch (Exception)
            {
                // Fallback
            }
        }
    }
}
