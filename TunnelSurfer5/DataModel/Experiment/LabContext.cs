using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace TunnelVision.DataModel
{
#if Travel
    public class LabContext : DbContext
    {
#if MSDB
        public LabContext()
           : base("name=TunnelSurfer6")
        {
            Database.Log = s => Writelog(s);

        }
        private void Writelog(string s)
        {
            Debug.WriteLine(s);

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Experiment>().MapToStoredProcedures();
            modelBuilder.Entity<TSUsers>().MapToStoredProcedures();
            base.OnModelCreating(modelBuilder);

        }

#else
        public LabContext()
            : base(new System.Data.SQLite.SQLiteConnection(App.ConnectionString), true)
        {
            Database.Log = s => Writelog(s);

        }
        private void Writelog(string s)
        {
            //Debug.WriteLine(s);

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

           // modelBuilder.Entity<Experiment>().MapToStoredProcedures();
           // modelBuilder.Entity<TSUsers>().MapToStoredProcedures();

            var sqliteConnectionInitializer = new SQLite.CodeFirst.SqliteCreateDatabaseIfNotExists<LabContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
            // base.OnModelCreating(modelBuilder);

        }

        
#endif



#else
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class LabContext : DbContext
    {
        public LabContext()
            : base(App.ConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Experiment>().MapToStoredProcedures();
            modelBuilder.Entity<TSUsers>().MapToStoredProcedures();


        }
#endif


        public int Id { get; set; }
        public virtual DbSet<Experiment> Experiments { get; set; }
        public virtual DbSet<TSUsers> Users { get; set; }
        public virtual DbSet<Batch> Batches { get; set; }
        public virtual DbSet<Chip> Chips { get; set; }
        public virtual DbSet<eNote> Notes { get; set; }

        public virtual DbSet<CalendarGoals> CalendarEvents { get; set; }
        public virtual DbSet<AdditionalFile> AdditionalFiles { get; set; }
        public virtual DbSet<IonicTrace> IonicIV { get; set; }
        public virtual DbSet<DataTrace> Traces { get; set; }
        public virtual DbSet<TunnelingTrace> TunnelIV { get; set; }
        public virtual DbSet<Junction> Junctions { get; set; }


        // Constructor to use on a DbConnection that is already opened
        public LabContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {

        }



        public static byte[] ConvertToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static String ToBinary(Byte[] data)
        {
            return string.Join(" ", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        private List<eNote> makeNotes()
        {
            return new List<eNote>
            {
                 new eNote{  NoteTime=DateTime.Now, ShortNote=DateTime.Now.ToShortDateString() + "Reminder"},
                new eNote{  NoteTime=DateTime.Now, ShortNote=DateTime.Now.AddDays(3).ToShortDateString() + "Issue"},
                new eNote{  NoteTime=DateTime.Now, ShortNote=DateTime.Now.AddDays(5).ToShortDateString() + "Equipment"}
            };
        }

        private List<DataTrace> fakeTraces(List<QualityControlStep> qc, List<TSUsers> users)
        {
            return new List<DataTrace>
            {
                 new DataTrace{ Analyte ="PB" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                      Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="dAMP" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                      Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="dGMP" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                       Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="polyA" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                       Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="polyG" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                        Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="polyA" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                        Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]},
                  new DataTrace{ Analyte ="polyG" , Concentration_mM=1, Buffer ="PB", Conductance = 10, DateAcquired = DateTime.Now, ExpectedConductance =1,
                      Filename = "M1_M2G_TopG_Bttm0mV_P350mV_1mMPB_0000_g.abf",  TraceName ="Control", GeneratingUser=users[0], QCStep=qc[0]}

            };
        }

        private List<IonicTrace> FakeIonic(List<QualityControlStep> qc, List<TSUsers> users)
        {
            return new List<IonicTrace>
            {
                new IonicTrace{ Asymmetry =.5f, Buffer ="Control", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="Control",
                     ExpectedSmeetConductance =1, AccessPoreSize =10, SmeetPoreSize=10},
                      new IonicTrace{ Asymmetry =.5f, Buffer ="PB", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="PBTest",
                     ExpectedSmeetConductance =1, AccessPoreSize =10, SmeetPoreSize=10},
                      new IonicTrace{ Asymmetry =.5f, Buffer ="KCl", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="KClTest",
                     ExpectedSmeetConductance =1, AccessPoreSize =10, SmeetPoreSize=10}
            };

        }

        private List<TunnelingTrace> FakeTunnel(List<QualityControlStep> qc, List<TSUsers> users)
        {
            return new List<TunnelingTrace>
            {
               new TunnelingTrace{ Asymmetry =.5f, Buffer ="AIR", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="M1", Dielectric=3,
                     ExpectedGapSize=2, GapPotential =3, GapSize =2.2f, JunctionArea =1000, Fit = new double[]{2f,2f} },
               new TunnelingTrace{ Asymmetry =.5f, Buffer ="PB", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="M2", Dielectric=3,
                     ExpectedGapSize=2, GapPotential =3, GapSize =2.2f, JunctionArea =1000, Fit = new double[]{2f,2f} },
               new TunnelingTrace{ Asymmetry =.5f, Buffer ="KCl", Capacitance =10, Chi2 =100, Concentration_mM =1, Conductance =10, ControlCapacitance =1,
                     ControlConductance =1, DateAcquired = DateTime.Now , ExpectedConductance =1,
                     Filename = "20160304_NALDB34_Chip03_22cyc_HIM3pC_M1M2TunnelingIV_Air_3_Sweeprate_30mVpers_350mV",
                     GeneratingUser = users[0], IVMethod ="Axon", QCStep = qc[0], TraceName ="M3", Dielectric=3,
                     ExpectedGapSize=2, GapPotential =3, GapSize =2.2f, JunctionArea =1000, Fit = new double[]{2f,2f} }
            };

        }

        private List<Junction> FakeJunctions(List<QualityControlStep> qc, List<TSUsers> users)
        {
            return new List<Junction>
            {
                new Junction{ JunctionName="M1-M4G", Traces=fakeTraces(qc, users),  TunnelIV=FakeTunnel(qc, users) },
                 new Junction{ JunctionName="M2-M4G", Traces=fakeTraces(qc, users),  TunnelIV=FakeTunnel(qc, users) },
                  new Junction{ JunctionName="M3-M4G", Traces=fakeTraces(qc, users),  TunnelIV=FakeTunnel(qc, users) }
            };

        }

        private List<Chip> FakeChips(List<QualityControlStep> qc, List<TSUsers> users)
        {

            return new List<Chip>
            {
                new Chip{ ChipName ="chip01", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip02", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip03", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip04", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip05", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip06", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip07", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip08", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip09", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},
                new Chip{ ChipName ="chip10", DrillMethod ="RIE",
                    Junctions = FakeJunctions(qc, users) , NumberOfJunctions =3, NumberPores =3, PoreSize =20,
                    IonicIV =  FakeIonic(qc,users), Note=makeNotes()[0]},

            };

        }

        private List<Batch> FakeBatches(List<QualityControlStep> qc, List<TSUsers> users)
        {

            return new List<Batch>
            {
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB34", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB35", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB36", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB37", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB38", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB39", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()},
                new Batch{ ALDCycles=22, ALDMaterial ="AL2O3", BatchName ="NALDB40", Chips = FakeChips(qc,users)
                    , DeliveryDate =DateTime.Now , DrillMethod ="vary", JunctionMaterial ="Pd", ManufactorDate =  DateTime.Now ,
                    NumberOfDeliveredChips=100  , Notes= makeNotes()}

            };

        }
        public void FakeData()
        {
            var users = new List<TSUsers>
            {
                new TSUsers{ UserName= "Pei" }
            };



            var experiments = new List<Experiment>
            {
            new Experiment{ExperimentName="DNA", LastNumberOfChips =10,LastNumberOfJunctions=30, ExperimentUsers = new List<TSUsers>(), Batches=new List<Batch>(), CalendarEvents=new List<CalendarGoals>(), Folder="", Notes = new List<eNote>(), OtherInfo=""},
            new Experiment{ExperimentName="RNA", LastNumberOfChips =10,LastNumberOfJunctions=30, ExperimentUsers  = new List<TSUsers>(), Batches=new List<Batch>(), CalendarEvents=new List<CalendarGoals>(), Folder="", Notes = new List<eNote>(), OtherInfo=""},
            new Experiment{ExperimentName="Protein", LastNumberOfChips =10,LastNumberOfJunctions=30, ExperimentUsers  = new List<TSUsers>(), Batches=new List<Batch>(), CalendarEvents=new List<CalendarGoals>(), Folder="", Notes = new List<eNote>(), OtherInfo=""},
            new Experiment{ExperimentName="Lateral", LastNumberOfChips =10,LastNumberOfJunctions=30, ExperimentUsers  = new List<TSUsers>(), Batches=new List<Batch>(), CalendarEvents=new List<CalendarGoals>(), Folder="", Notes = new List<eNote>(), OtherInfo=""}
            };

            for (int i = 0; i < experiments.Count; i++)// (var exp in experiments)
            {
                var exp = experiments[i];
                //if (exp.Batches != null)
                //{
                //    foreach (var batch in exp.Batches)
                //    {
                //        foreach (var chip in batch.Chips)
                //        {
                //            foreach (var junction in chip.Junctions)
                //            {
                //                junction.Experiment = exp;
                //                junction.Chip = chip;
                //            }
                //            chip.Experiment = exp;
                //            chip.Batch = batch;
                //        }
                //        batch.Experiment = exp;
                //    }
                //}
                string dir = "S:\\Research\\TunnelSurfer\\Experiments\\" + exp.ExperimentName;
                try
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                catch
                { }
                exp.Folder = dir;
                Experiments.Add(exp);
                this.SaveChanges();
            }


            //users.ForEach(s => Users.Add(s));
            //this.SaveChanges();
            // experiments.ForEach(s => Experiments.Add(s));
            this.SaveChanges();

        }
    }
}
