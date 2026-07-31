using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Student)              // each Enrollment has one Student
                .WithMany(s => s.Enrollments)        // each Student has many Enrollments
       .HasForeignKey(e => e.StudentId)     // FK column in Enrollments
       .OnDelete(DeleteBehavior.Cascade)   // If a student is deleted, their enrollments are deleted too.
       .IsRequired();

        builder.HasOne(e => e.Course)               // each Enrollment has one Course
               .WithMany(c => c.Enrollments)        // each Course has many Enrollments
               .HasForeignKey(e => e.CourseId)      // FK column in Enrollments
               .OnDelete(DeleteBehavior.Restrict)   //// A course with enrollments cannot be deleted until enrollments are removed.
               .IsRequired();

    }
}
