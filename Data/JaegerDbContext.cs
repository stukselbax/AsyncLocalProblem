namespace Data
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class JaegerDbContext : DbContext
    {
        public JaegerDbContext(DbContextOptions<JaegerDbContext> options)
            : base (options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new EntityConfiguration());
        }

        public DbSet<Entity> Dates { get; set; }
    }

    public class Entity
    {
        public Guid Id { get; set; }

        public DateTime Now { get; set; }
    }

    public class EntityConfiguration : IEntityTypeConfiguration<Entity>
    {
        public void Configure(EntityTypeBuilder<Entity> builder)
        {
            builder.ToTable<Entity>("entities");
            builder.HasKey(x => x.Id);

            builder.Property(e => e.Now);
        }
    }
}
