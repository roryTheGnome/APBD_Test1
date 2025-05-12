namespace APBD_Test1.Models;

//check if neccessery later
public class Appointment
{
    public Patient patient { get; set; }
    public Doctor doctor { get; set; }
    public List<Service> services { get; set; }
}