using Microsoft.EntityFrameworkCore;

namespace Shashlik.Dashboard.Demo
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }
    }
}
