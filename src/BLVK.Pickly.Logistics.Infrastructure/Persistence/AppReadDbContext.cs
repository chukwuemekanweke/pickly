using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class AppReadDbContext(DbContextOptions<AppReadDbContext> options)
    : AppDbContextBase<AppReadDbContext>(options);
