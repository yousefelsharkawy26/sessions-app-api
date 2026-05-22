using Microsoft.EntityFrameworkCore;
using SessionApp.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Message> Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
