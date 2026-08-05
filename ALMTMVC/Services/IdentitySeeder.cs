using Microsoft.AspNetCore.Identity;

namespace ALMTMVC.Services;

public static class IdentitySeeder
{
    private const string AdminRole = "Admin";

    public static async Task SeedAdminAsync(
        IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<IdentityUser>>();

        var configuration =
            services.GetRequiredService<IConfiguration>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            IdentityResult roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(AdminRole));

            if (!roleResult.Succeeded)
            {
                string errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(e =>
                        e.Description));

                throw new InvalidOperationException(
                    $"Admin role creation failed: {errors}");
            }
        }

        string? adminEmail =
            configuration["AdminUser:Email"];

        string? adminPassword =
            configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            // Credentials have not been configured yet.
            return;
        }

        IdentityUser? admin =
            await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            IdentityResult createResult =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            if (!createResult.Succeeded)
            {
                string errors = string.Join(
                    "; ",
                    createResult.Errors.Select(e =>
                        e.Description));

                throw new InvalidOperationException(
                    $"Admin creation failed: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                AdminRole))
        {
            IdentityResult roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    AdminRole);

            if (!roleResult.Succeeded)
            {
                string errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(e =>
                        e.Description));

                throw new InvalidOperationException(
                    $"Adding admin role failed: {errors}");
            }
        }
    }
}