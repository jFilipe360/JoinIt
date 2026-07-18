using JoinIt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Data
{
    // Dados para inicializar a base de dados com roles, admin e categorias
    public static class DbInitializer
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider)
        {
            var configuration =
                serviceProvider.GetRequiredService<IConfiguration>();

            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var context =
                serviceProvider.GetRequiredService<ApplicationDbContext>();

            var adminEmail =
                configuration["SeedAdmin:Email"];

            var adminPassword =
                configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "O email do administrador não foi configurado.");
            }

            // ---------- ROLES ----------

            string[] roles =
            {
                "Admin",
                "User"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // ---------- ADMIN ----------

            var admin =
                await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "A password do administrador não foi configurada.");
                }

                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Nome = "Administrador",
                    CriadoEm = DateTime.Now
                };

                var resultado = await userManager.CreateAsync(
                    admin,
                    adminPassword);

                if (!resultado.Succeeded)
                {
                    var erros = string.Join(
                        "; ",
                        resultado.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Não foi possível criar o administrador: {erros}");
                }
            }

            // Garante que o administrador possui a role Admin,
            // mesmo quando a conta já existia.
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var resultadoRole =
                    await userManager.AddToRoleAsync(admin, "Admin");

                if (!resultadoRole.Succeeded)
                {
                    var erros = string.Join(
                        "; ",
                        resultadoRole.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Não foi possível atribuir a role Admin: {erros}");
                }
            }

            // ---------- CATEGORIAS ----------

            if (!await context.Categorias.AnyAsync())
            {
                context.Categorias.AddRange(
                    new Categoria { Nome = "Música" },
                    new Categoria { Nome = "Desporto" },
                    new Categoria { Nome = "Gaming" },
                    new Categoria { Nome = "Tecnologia" },
                    new Categoria { Nome = "Festa" },
                    new Categoria { Nome = "Gastronomia" },
                    new Categoria { Nome = "Educação" },
                    new Categoria { Nome = "Cultura" }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}