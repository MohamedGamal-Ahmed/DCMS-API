using System;
using System.Linq;
using System.Threading.Tasks;
using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DCMS.Infrastructure.Services;

public class AiHistoryService : IAiHistoryService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public AiHistoryService(
        IDbContextFactory<DCMSDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<int> LogAiRequestAsync(string prompt, string response, string? action = null, int promptTokens = 0, int completionTokens = 0, int secondsSaved = 0, bool isSuccess = true)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == _currentUserService.CurrentUserName);

            var log = new AiRequestLog
            {
                UserPrompt = prompt,
                AiResponse = response,
                ActionExecuted = action,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                SecondsSaved = secondsSaved,
                IsSuccess = isSuccess,
                UserId = user?.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.AiRequestLogs.Add(log);
            await context.SaveChangesAsync();
            return log.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AI HISTORY ERROR] {ex.Message}");
            return 0;
        }
    }
}
