using DCMS.Application.Interfaces;
using DCMS.Domain.Entities;
using DCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCMS.Infrastructure.Services;

public class EngineerService : IEngineerService
{
    private readonly IDbContextFactory<DCMSDbContext> _contextFactory;

    public EngineerService(IDbContextFactory<DCMSDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Engineer>> GetActiveEngineersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Engineers
            .Where(e => e.IsActive)
            .OrderBy(e => e.FullName)
            .ToListAsync();
    }

    public async Task<List<Engineer>> GetResponsibleEngineersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Engineers
            .Where(e => e.IsActive && e.IsResponsibleEngineer)
            .OrderBy(e => e.FullName)
            .ToListAsync();
    }
}
