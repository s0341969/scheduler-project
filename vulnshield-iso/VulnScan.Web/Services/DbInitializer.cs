using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        LocalAuthOptions localAuthOptions,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (localAuthOptions.BootstrapUsers.Count > 0)
        {
            foreach (var bootstrapUser in localAuthOptions.BootstrapUsers)
            {
                if (string.IsNullOrWhiteSpace(bootstrapUser.Account) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.UserName) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.RoleName) ||
                    string.IsNullOrWhiteSpace(bootstrapUser.Password))
                {
                    continue;
                }

                var existingUser = await dbContext.Users.FirstOrDefaultAsync(
                    item => item.Account == bootstrapUser.Account,
                    cancellationToken);

                if (existingUser is null)
                {
                    var user = new User
                    {
                        Account = bootstrapUser.Account.Trim(),
                        UserName = bootstrapUser.UserName.Trim(),
                        Email = string.IsNullOrWhiteSpace(bootstrapUser.Email) ? null : bootstrapUser.Email.Trim(),
                        RoleName = bootstrapUser.RoleName.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        PasswordChangedAt = DateTime.UtcNow,
                    };
                    user.PasswordHash = passwordHasher.HashPassword(user, bootstrapUser.Password);
                    dbContext.Users.Add(user);
                }
                else if (string.IsNullOrWhiteSpace(existingUser.PasswordHash))
                {
                    existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, bootstrapUser.Password);
                    existingUser.PasswordChangedAt = DateTime.UtcNow;
                    existingUser.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        if (!await dbContext.ScanAllowedRanges.AnyAsync(cancellationToken))
        {
            dbContext.ScanAllowedRanges.Add(new ScanAllowedRange
            {
                RangeName = "內網預設",
                Cidr = "10.0.0.0/8",
                Description = "預設允許 10.x.x.x 內網掃描範圍",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
