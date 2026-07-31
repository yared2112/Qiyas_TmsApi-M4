using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;
using TmsApi.Data;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id); // primary key
        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(100); // example length
        builder.Property(s => s.GPA)
               .HasColumnType("decimal(3,2)");
    }
}
