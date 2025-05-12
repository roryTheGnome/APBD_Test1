namespace APBD_Test1.Models;

public class Appointment
{
    public DateTime date { get; set; }
    public Patient patient { get; set; }
    public Doctor doctor { get; set; }
    public List<Service> services { get; set; }
}