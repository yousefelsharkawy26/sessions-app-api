using Microsoft.EntityFrameworkCore;
using SessionApp.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Message> Messages { get; }
    DbSet<PrekeyBundle> PrekeyBundles { get; }
    DbSet<OneTimePrekey> OneTimePrekeys { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMember> GroupMembers { get; }
    DbSet<BlockedUser> BlockedUsers { get; }
    DbSet<UserDevice> UserDevices { get; }
    DbSet<MessageReaction> MessageReactions { get; }
    DbSet<PinnedMessage> PinnedMessages { get; }
    DbSet<DirectChatMute> DirectChatMutes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
