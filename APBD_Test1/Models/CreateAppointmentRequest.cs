namespace APBD_Test1.Models;

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public string Pwz { get; set; } = null!;
    public List<AppointmentServiceDto> Services { get; set; } = new();
}

