using Microsoft.EntityFrameworkCore;

namespace src
{
    public class FGRContext : DbContext
    {
        public FGRContext(DbContextOptions<FGRContext> options) : base(options)
        {
            
        }

        public DbSet<Game> Game { get; set; }
    }
}