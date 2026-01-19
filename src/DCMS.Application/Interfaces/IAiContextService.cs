using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IAiContextService
{
    Task<string> GetSystemPromptAsync();
    Task<string> GetCriticalAlertsAsync();
}
