using DCMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCMS.Application.Interfaces;

public interface IEngineerService
{
    Task<List<Engineer>> GetActiveEngineersAsync();
    Task<List<Engineer>> GetResponsibleEngineersAsync();
}
