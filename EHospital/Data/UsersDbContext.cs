using EHospital.Entities;
using EHospital.Models;
using Microsoft.EntityFrameworkCore;

namespace EHospital.Data
{
    public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
    {
        public DbSet<Doctors> Doctors => Set<Doctors>();
        public DbSet<Patients> Patients => Set<Patients>();
        public DbSet<Admins> Admins => Set<Admins>();
        public DbSet<UserH> UserH => Set<UserH>();
        public DbSet<Appointments> Appointments => Set<Appointments>();
        public DbSet<Specializations> Specializations => Set<Specializations>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            
            

        }
    }

}
