using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SessionApp.Domain.Entities;
using SessionApp.Infrastructure.Persistence;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionApp.Infrastructure.Services;

public class AccountSelfDestructWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccountSelfDestructWorker> _logger;
    private readonly IConfiguration _configuration;

    public AccountSelfDestructWorker(
        IServiceProvider serviceProvider,
        ILogger<AccountSelfDestructWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Account Self-Destruct Worker started.");

        // Read configured interval (defaulting to 1 hour, or 5 seconds in testing/custom configuration)
        var intervalSeconds = _configuration.GetValue<int>("PrivacySettings:WorkerIntervalSeconds", 3600);
        if (intervalSeconds <= 0) intervalSeconds = 3600;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInactiveAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during account self-destruct processing.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Account Self-Destruct Worker stopped.");
    }

    private async Task ProcessInactiveAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Read inactivity period
        // For testing/mocking, we can define inactivity in seconds/minutes if configured
        var inactivityMonths = _configuration.GetValue<int>("PrivacySettings:AccountInactivityMonths", 6);
        var inactivitySeconds = _configuration.GetValue<int>("PrivacySettings:AccountInactivitySeconds", 0);

        DateTime threshold;
        if (inactivitySeconds > 0)
        {
            threshold = DateTime.UtcNow.AddSeconds(-inactivitySeconds);
        }
        else
        {
            threshold = DateTime.UtcNow.AddMonths(-inactivityMonths);
        }

        _logger.LogInformation("Checking for inactive users with LastSeenAt older than {Threshold}", threshold);

        // Fetch users whose LastSeenAt is older than threshold
        // Note: For newly registered users, if LastSeenAt is null, we can treat their registration/creation time as last seen
        // but for safety, we only prune users who have a LastSeenAt value set and it is expired
        var inactiveUsers = await dbContext.Users
            .Where(u => u.LastSeenAt != null && u.LastSeenAt < threshold)
            .ToListAsync(cancellationToken);

        if (!inactiveUsers.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} inactive users to self-destruct.", inactiveUsers.Count);

        foreach (var user in inactiveUsers)
        {
            try
            {
                _logger.LogInformation("Initiating self-destruct for user {Username} (ID: {UserId}) due to inactivity since {LastSeenAt}", 
                    user.UserName, user.Id, user.LastSeenAt);

                // 1. Manually delete all Messages sent or received by this user to bypass DeleteBehavior.Restrict
                var userMessages = await dbContext.Messages
                    .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                    .ToListAsync(cancellationToken);

                if (userMessages.Any())
                {
                    dbContext.Messages.RemoveRange(userMessages);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Deleted {Count} messages associated with user {Username}", userMessages.Count, user.UserName);
                }

                // 2. Delete Profile Picture from disk if it exists locally
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var rootDir = Directory.GetCurrentDirectory();
                        var relativePath = user.ProfilePictureUrl.TrimStart('/');
                        var fullPath = Path.Combine(rootDir, "wwwroot", relativePath);

                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            _logger.LogInformation("Deleted profile picture file from disk: {Path}", fullPath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "Failed to delete profile picture file for user {Username}", user.UserName);
                    }
                }

                // 3. Delete user via UserManager (this cascade-deletes PrekeyBundles, OneTimePrekeys, UserDevices, GroupMembers, reactions, pinned messages)
                var deleteResult = await userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to delete user {Username} from identity store: {Errors}", user.UserName, errors);
                }
                else
                {
                    _logger.LogInformation("Successfully deleted user {Username} (self-destruct completed).", user.UserName);
                }
            }
            catch (Exception userEx)
            {
                _logger.LogError(userEx, "Error during self-destruct execution for user {Username}", user.UserName);
            }
        }
    }
}
