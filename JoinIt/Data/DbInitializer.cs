using JoinIt.Models;
using Microsoft.AspNetCore.Identity;

namespace JoinIt.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ---------- ROLES ----------

            string[] roles =
            {
                "Admin",
                "User"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---------- ADMIN ----------

            string adminEmail = "admin@joinit.pt";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Nome = "Administrador",
                    CriadoEm = DateTime.Now
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // ---------- CATEGORIAS ----------

            if (!context.Categorias.Any())
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