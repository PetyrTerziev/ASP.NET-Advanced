using Microsoft.EntityFrameworkCore;

using GuardPost.Data.Models;

namespace GuardPost.Services.Tests.Infrastructure;

internal static class QueryExtensions
{
    public static async Task<Site> FirstAsyncById(this DbSet<Site> sites, int id)
        => (await sites.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id))!;

    public static async Task<Guard> FirstAsyncById(this DbSet<Guard> guards, int id)
        => (await guards.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == id))!;
}
