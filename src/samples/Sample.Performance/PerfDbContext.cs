using Microsoft.EntityFrameworkCore;

namespace Sample.Performance
{
    public class PerfDbContext : DbContext
    {
        public PerfDbContext(DbContextOptions<PerfDbContext> options) : base(options)
        {
        }
    }
}
