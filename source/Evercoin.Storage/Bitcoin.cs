using System.Data.Entity;

namespace Evercoin.Storage
{
    public class Bitcoin : DbContext
    {
        public DbSet<Block> Blocks { get; set; }

        public DbSet<Transaction> Transactions { get; set; }
    }
}