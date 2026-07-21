namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
