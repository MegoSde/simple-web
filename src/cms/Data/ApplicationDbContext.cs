using Microsoft.EntityFrameworkCore;

namespace cms.Data;


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    // public DbSet<Something> Somethings => Set<Something>();
}