using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MapDataServer.Models
{
    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options)
            : base(options)
        { }

        public DbSet<TestModel1> Model1 { get; set; }
        public DbSet<TestModel2> Model2 { get; set; }
    }

    public class TestModel1
    {
        [Key]
        public string Name { get; set; }

        public ICollection<TestModel2> DetailPacks { get; set; }

        public TestModel1 Next { get; set; }
    }
    public class TestModel2
    {
        [Key]
        public int Detail1 { get; set; }

        public double Detail2 { get; set; }

        public bool ThatsTrue { get; set; }
    }
}
