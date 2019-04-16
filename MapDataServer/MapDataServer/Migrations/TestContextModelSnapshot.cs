﻿// <auto-generated />
using MapDataServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MapDataServer.Migrations
{
    [DbContext(typeof(TestContext))]
    partial class TestContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.8-servicing-32085")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MapDataServer.Models.TestModel1", b =>
                {
                    b.Property<string>("Name")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("NextName");

                    b.HasKey("Name");

                    b.HasIndex("NextName");

                    b.ToTable("Model1");
                });

            modelBuilder.Entity("MapDataServer.Models.TestModel2", b =>
                {
                    b.Property<int>("Detail1")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("Detail2");

                    b.Property<string>("TestModel1Name");

                    b.Property<bool>("ThatsTrue");

                    b.HasKey("Detail1");

                    b.HasIndex("TestModel1Name");

                    b.ToTable("Model2");
                });

            modelBuilder.Entity("MapDataServer.Models.TestModel1", b =>
                {
                    b.HasOne("MapDataServer.Models.TestModel1", "Next")
                        .WithMany()
                        .HasForeignKey("NextName");
                });

            modelBuilder.Entity("MapDataServer.Models.TestModel2", b =>
                {
                    b.HasOne("MapDataServer.Models.TestModel1")
                        .WithMany("DetailPacks")
                        .HasForeignKey("TestModel1Name");
                });
#pragma warning restore 612, 618
        }
    }
}
