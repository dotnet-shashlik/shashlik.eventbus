using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Shashlik.EventBus.PostgreSQL.Tests.Efcore.Migrations
{
    [DbContext(typeof(DemoDbContext))]
    partial class DemoDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("NodeCommon.Users", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer")
                    .HasAnnotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("character varying(8)")
                    .HasMaxLength(8);

                b.HasKey("Id");

                b.ToTable("Users");
            });
#pragma warning restore 612, 618
        }
    }
}