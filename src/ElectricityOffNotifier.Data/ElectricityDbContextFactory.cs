using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ElectricityOffNotifier.Data;

public sealed class ElectricityDbContextFactory : IDesignTimeDbContextFactory<ElectricityDbContext>
{
    public ElectricityDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<ElectricityDbContext>()
            .UseNpgsql("Host=localhost;")
            .Options;

        return new ElectricityDbContext(opts);
    }
}