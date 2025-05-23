using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
//using MySql.Data.Entity;
using System.Data.Common;

namespace TunnelVision.DataModel.Fileserver
{

#if Travel
    public class FileContext //: DbContext
    {
        public FileContext()
            //: base("name=TunnelFile6")
        {
        }

#else
   // [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class FileContext : DbContext
    {
            public FileContext()
            : base("name=FileServer")
        {
        }

#endif
        public int Id { get; set; }
        public virtual DbSet<FileDataTrace> FileDataTraces { get; set; }
        public virtual DbSet<FileTrace> FileTraces { get; set; }
        public virtual DbSet<FileHistogram> FileHistograms { get; set; }
        public virtual DbSet<FileQueue> FileQueues { get; set; }


       

        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    //modelBuilder.Entity<FileDataTrace>().MapToStoredProcedures();
        //    //modelBuilder.Entity<FileTrace>().MapToStoredProcedures();
        //    //modelBuilder.Entity<FileHistogram>().MapToStoredProcedures();
        //    //modelBuilder.Entity<FileQueue>().MapToStoredProcedures();
        //}


        public void FakeData()
        {
          

        }
    }
}
