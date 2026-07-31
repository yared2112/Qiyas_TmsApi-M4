using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;

namespace TmsApi.Data
{
    public class TmsDbContext : DbContext
    {
        public TmsDbContext(DbContextOptions<TmsDbContext> options)
            : base(options) { }

        // DbSets pull entities into the EF Core model
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        // public DbSet<Enrollment> Enrollments { get; set; }        
        public DbSet<Enrollment> Enrollments { get; set; }

        // If you did the stretch exercise, include these too:
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // This line automatically finds and applies all IEntityTypeConfiguration<T>
            // classes in the same assembly (StudentConfiguration, CourseConfiguration, EnrollmentConfiguration, etc.)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);

            base.OnModelCreating(modelBuilder);

            // Enrollment: composite uniqueness (StudentId + CourseId)
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique();

            // Student ↔ Enrollment relationship
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course ↔ Enrollment relationship
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
