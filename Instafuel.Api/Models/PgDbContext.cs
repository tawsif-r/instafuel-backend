using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class PgDbContext : DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options)
        : base(options)
    {
    }
}