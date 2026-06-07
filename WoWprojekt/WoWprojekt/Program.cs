using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Authorization;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<WoWprojekt.Authorization.RequestMethodAuthorizationFilter>();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddRazorPages();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("hr-HR"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var app = builder.Build();

var localizationOptions = app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>()
    .Value;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    SeedData.Initialize(context);
}

using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<WoWprojekt.Models.ApplicationUser>>();

        async Task EnsureRoleAsync(string role)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
            }
        }

        await EnsureRoleAsync("admin");
        await EnsureRoleAsync("obicanuser");

        var adminEmail = builder.Configuration["AdminUser:Email"] ?? "admin@gmail.com";
        var adminPassword = builder.Configuration["AdminUser:Password"] ?? "Adminpass123";
        var userEmail = builder.Configuration["RegularUser:Email"] ?? "user@gmail.com";
        var userPassword = builder.Configuration["RegularUser:Password"] ?? "Userpass123";

        async Task EnsureUserAsync(string email, string password, string displayName, string role)
        {
            var existingUser = await userManager.FindByEmailAsync(email) ?? await userManager.FindByNameAsync(email);

            if (existingUser is null)
            {
                existingUser = new WoWprojekt.Models.ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayName,
                    RegisteredAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(existingUser, password);
                if (!createResult.Succeeded)
                {
                    return;
                }
            }
            else
            {
                existingUser.UserName = email;
                existingUser.Email = email;
                existingUser.DisplayName = displayName;
                await userManager.UpdateAsync(existingUser);

                if (await userManager.HasPasswordAsync(existingUser))
                {
                    await userManager.RemovePasswordAsync(existingUser);
                }

                await userManager.AddPasswordAsync(existingUser, password);
            }

            if (!await userManager.IsInRoleAsync(existingUser, role))
            {
                await userManager.AddToRoleAsync(existingUser, role);
            }
        }

        await EnsureUserAsync(adminEmail, adminPassword, "Administrator", "admin");
        await EnsureUserAsync(userEmail, userPassword, "Regular Player", "obicanuser");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Role seeding failed (likely missing Identity migrations). Continuing startup without seeding.");
    }
}

app.Run();
