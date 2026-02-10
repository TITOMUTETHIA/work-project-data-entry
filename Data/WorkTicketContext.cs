using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Models;

namespace WorkTicketApp.Data;

public sealed class WorkTicketContext(DbContextOptions<WorkTicketContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<WorkTicket> WorkTickets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(500);
            entity.HasIndex(e => e.Username)
                .IsUnique();
        });

        // Configure WorkTicket entity
        modelBuilder.Entity<WorkTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TicketNumber)
                .HasMaxLength(50);
            entity.Property(e => e.CostCentre)
                .HasMaxLength(100);
            entity.Property(e => e.Activity)
                .HasMaxLength(200);
            entity.Property(e => e.OperatorName)
                .HasMaxLength(100);
            entity.Property(e => e.MaterialUsed)
                .HasMaxLength(500);
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);
        });
    }
}
