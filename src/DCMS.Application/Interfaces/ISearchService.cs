using DCMS.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface ISearchService
{
    Task<(List<object> Items, int TotalCount)> SearchAsync(SearchCriteria criteria, int page, int pageSize);
    Task ExportToPdfAsync(SearchCriteria criteria, string filePath, string reportTitle);
}
