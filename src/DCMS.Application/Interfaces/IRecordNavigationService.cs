using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IRecordNavigationService
{
    Task OpenRecordAsync(string url);
}
