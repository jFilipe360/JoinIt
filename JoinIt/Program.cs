using JoinIt.Data;
using JoinIt.Hubs;
using JoinIt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AcessoNegado";
});

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

var cultura = new CultureInfo("pt-PT");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(cultura);
    options.SupportedCultures = new[] { cultura };
    options.SupportedUICultures = new[] { cultura };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AcessoNegado";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/Home/ErrorHttp",
    "?code={0}"
);

app.UseRequestLocalization();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

var paginasIdentityPermitidas = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    "/Identity/Account/Login",
    "/Identity/Account/Register",
    "/Identity/Account/Logout",
};

app.Use(async (context, next) =>
{
    var caminho = context.Request.Path.Value?
        .TrimEnd('/') ?? string.Empty;

    var pertenceAoIdentity = caminho.StartsWith(
        "/Identity",
        StringComparison.OrdinalIgnoreCase);

    if (pertenceAoIdentity &&
        !paginasIdentityPermitidas.Contains(caminho))
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;

        return;
    }

    await next();
});

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapHub<ChatHub>("/chatHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();

    await DbInitializer.Initialize(services);
}

app.Run();
