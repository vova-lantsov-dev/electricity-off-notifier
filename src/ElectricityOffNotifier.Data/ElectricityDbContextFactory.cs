using ElectricityOffNotifier.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace ElectricityOffNotifier.Data;

public sealed class ElectricityDbContextFactory : IDesignTimeDbContextFactory<ElectricityDbContext>
{
    public ElectricityDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<ElectricityDbContext>()
            .UseNpgsql("Host=localhost;")
            .Options;

        return new ElectricityDbContext(opts,
            new OptionsWrapper<DatabaseEncryptionOptions>(
                new DatabaseEncryptionOptions
                {
                    // Fake values to prevent BASE64 parsing error
                    EncryptionKey = "aYgVfeWSkxXusqjNzzdiNw==",
                    EncryptionIV = "hz1Yy4d1kVEGtN6S/P9LJA=="
                }));
    }
}