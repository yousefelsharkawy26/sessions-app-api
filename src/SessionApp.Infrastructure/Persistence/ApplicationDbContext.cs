using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Domain.Entities;
using System.Reflection;

namespace SessionApp.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PrekeyBundle> PrekeyBundles => Set<PrekeyBundle>();
    public DbSet<OneTimePrekey> OneTimePrekeys => Set<OneTimePrekey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
