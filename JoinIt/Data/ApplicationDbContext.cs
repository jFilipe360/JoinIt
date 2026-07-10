using JoinIt.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace JoinIt.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Evento> Eventos { get; set; }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Participante> Participantes { get; set; }

        public DbSet<Mensagem> Mensagens { get; set; }

        public DbSet<Amizade> Amizades { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuração da relação 
            builder.Entity<Evento>()
                .HasOne(e => e.Criador)
                .WithMany(u => u.EventosCriados)
                .HasForeignKey(e => e.CriadorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Evento>()
                .HasOne(e => e.Categoria)
                .WithMany(c => c.Eventos)
                .HasForeignKey(e => e.CategoriaId);

            builder.Entity<Participante>()
                .HasOne(p => p.User)
                .WithMany(u => u.EventosParticipados)
                .HasForeignKey(p => p.UserId);

            builder.Entity<Participante>()
                .HasOne(p => p.Evento)
                .WithMany(e => e.Participantes)
                .HasForeignKey(p => p.EventoId);

            builder.Entity<Mensagem>()
                .HasOne(m => m.User)
                .WithMany(u => u.Mensagens)
                .HasForeignKey(m => m.UserId);

            builder.Entity<Mensagem>()
                .HasOne(m => m.Evento)
                .WithMany(e => e.Mensagens)
                .HasForeignKey(m => m.EventoId);

            builder.Entity<Amizade>()
                .HasOne(a => a.PedidoPor)
                .WithMany(u => u.PedidosEnviados)
                .HasForeignKey(a => a.PedidoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Amizade>()
                .HasOne(a => a.PedidoA)
                .WithMany(u => u.PedidosRecebidos)
                .HasForeignKey(a => a.PedidoAId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
