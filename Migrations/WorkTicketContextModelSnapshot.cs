using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkTicketApp.Data;

#nullable disable

namespace WorkTicketApp.Migrations
{
    [DbContext(typeof(WorkTicketContext))]
    partial class WorkTicketContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("WorkTicketApp.Models.User", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Password")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<string>("Username")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.HasKey("Id");

                b.HasIndex("Username")
                    .IsUnique();

                b.ToTable("Users");
            });

            modelBuilder.Entity("WorkTicketApp.Models.WorkTicket", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<string>("Activity")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("CostCentre")
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("CreatedBy")
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<DateTime>("EndDateTime")
                    .HasColumnType("datetime2");

                b.Property<int>("EndCounter")
                    .HasColumnType("int");

                b.Property<string>("MaterialUsed")
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<int>("NumOperators")
                    .HasColumnType("int");

                b.Property<string>("OperatorName")
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<int>("QuantityIn")
                    .HasColumnType("int");

                b.Property<int>("QuantityOut")
                    .HasColumnType("int");

                b.Property<DateTime>("StartDateTime")
                    .HasColumnType("datetime2");

                b.Property<int>("StartCounter")
                    .HasColumnType("int");

                b.Property<string>("TicketNumber")
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.ToTable("WorkTickets");
            });
#pragma warning restore 612, 618
        }
    }
}
