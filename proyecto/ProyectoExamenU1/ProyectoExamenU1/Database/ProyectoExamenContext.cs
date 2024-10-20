﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoExamenU1.Database.Configuration;
using ProyectoExamenU1.Database.Entities;
using ProyectoExamenU1.Helpers;
using ProyectoExamenU1.Services.Interfaces;

namespace ProyectoExamenU1.Database
{
    public class ProyectoExamenContext : IdentityDbContext<IdentityUser>
    {
        private readonly IAuditService _auditService;

        public ProyectoExamenContext(
            DbContextOptions options,
            IAuditService auditService   
  
            ) 
            : base(options)
        {
            this._auditService = auditService;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
            //modelBuilder.Entity<PermitionApplicationEntity>()   // por que usa una entidad
            //.Property(e => e.Type)   // y por que el nombre
            //.UseCollation("SQL_Latin1_General_CP1_CI_AS");

            //Le decimos que nuestras tablas se crearan en esquema de security
            modelBuilder.HasDefaultSchema("security");

            //ASignamos nombres a nuestras tablas para no confundirnos
            modelBuilder.Entity<IdentityUser>().ToTable("users");
            modelBuilder.Entity<IdentityRole>().ToTable("roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("users_roles");

            // Configurar la relación entre ApplicationUser y ApplicationUserRole
            //modelBuilder.Entity<ApplicationUserRole>()
            //    .HasOne(ur => ur.User) // Navegación a User
            //    .WithMany(u => u.UserRoles) // Relación con la colección UserRoles
            //    .HasForeignKey(ur => ur.UserId) // Clave foránea UserId en users_roles
            //    .OnDelete(DeleteBehavior.Cascade); // Eliminar en cascada los roles al eliminar un usuario

            //Estos son los permisos
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("users_claims");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("roles_claims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("users_logins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("users_tokens");

            // modelBuilder.ApplyConfiguration(new CategoryConfiguration());          <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            modelBuilder.ApplyConfiguration(new PermitionApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new PermitionTypeConfiguration());


            //set FKs on Restrict
            var eTypes = modelBuilder.Model.GetEntityTypes();
            foreach (var type in eTypes)
            {
                var foreingkeys = type.GetForeignKeys();
                foreach (var foreingkey in foreingkeys)
                {
                    foreingkey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
        public override async Task<int> SaveChangesAsync(
                 CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified
                ));

            string userId = _auditService.GetUserId();

            // ver si es el seedeer que molesta
            if (userId == null || userId == "Seeder")
            {
              
                var firstUser = await this.Set<IdentityUser>().FirstOrDefaultAsync();

                
                if (firstUser != null)
                {
                    userId = firstUser.Id;
                }
            }

            foreach (var entry in entries)
            {
                var entity = entry.Entity as BaseEntity;
                if (entity != null)
                {
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedBy = userId;
                        entity.CreatedDate = DateTime.Now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entity.UpdatedBy = userId;
                        entity.UpdatedDate = DateTime.Now;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }


        //public DbSet<ENTITY_CLASS> ENTITY_NAME { get; set; }
        public DbSet<PermitionApplicationEntity> ApplicationEntities { get; set; }
        public DbSet<PermitionTypeEntity> PermitionTypes { get; set; }

    }
}
