using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IAiHistoryService
{
    Task<int> LogAiRequestAsync(string prompt, string response, string? action = null, int promptTokens = 0, int completionTokens = 0, int secondsSaved = 0, bool isSuccess = true);
}
