using APBD_Test1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_Test1.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController:ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppointmentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    [HttpGet]
    [Route("api/appointments/{id}")]
    public async Task<IActionResult> GetAppointmentInformation(string id,CancellationToken token)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);
        //init objects here latr
        Doctor doctor = new Doctor();
        Patient patient = new Patient();
        Appointment appointment = new Appointment();
        List<Service> serviceList = new List<Service>();
        int patientId ;

        try
        {
            await using var cmd = new SqlCommand("select * from Appointment where id = @id", con);
            cmd.Parameters.AddWithValue("@id", id);
            
            await using var rdr = await cmd.ExecuteReaderAsync(token);
            if(!await rdr.ReadAsync(token))
                return NotFound("no such appointment exists");
            doctor.id=(int)rdr["doctor_id"];
            patientId=(int)rdr["patient_id"];
            appointment.date=(DateTime)rdr["date"];
            await rdr.CloseAsync();
            
            
            await using var cmdPatient =new SqlCommand(@"select * from patient where patient_id = @id", con);
            cmdPatient.Parameters.AddWithValue("@id", patientId);
            
            await using var rdrPatient = await cmdPatient.ExecuteReaderAsync(token);
            if (!await rdrPatient.ReadAsync(token))
                return NotFound("no such patient exists");
            patient.firstName = (string) rdrPatient["first_name"];
            patient.lastName = (string) rdrPatient["last_name"];
            patient.dateOfBirth = (DateTime) rdrPatient["date_of_birth"];
            await rdrPatient.CloseAsync();
            
            await using var cmdDoctor =new SqlCommand(@"select PWZ from Doctor where doctor_id = @id", con);
            cmdDoctor.Parameters.AddWithValue("@id", doctor.id);
            
            await using var rdrDoctor = await cmdDoctor.ExecuteReaderAsync(token);
            if (!await rdrDoctor.ReadAsync(token))
                return NotFound("no such doctor exists");
            doctor.pwz = (string) rdrDoctor["pwz"];
            await rdrDoctor.CloseAsync();
            
            await using var cmdService=new SqlCommand(@"
select ass.service_fee , s.name from Appointment_Service ass 
                                join Service s 
                                on ass.service_id = s.service_id
                                where appoitment_id = @id", con);
            cmdService.Parameters.AddWithValue("@id", id);
            
            await using var rdrService = await cmdService.ExecuteReaderAsync(token);
            if (!await rdrService.ReadAsync(token))
                return NotFound("Service not found");
            while (await rdrService.ReadAsync(token))
            {
                var serv = new Service
                {
                    name = (string)rdrService["name"],
                    price = (double)rdrService["service_fee"]
                };
                
                appointment.services.Add(serv);
            }
            await rdrService.CloseAsync();
            appointment.patient = patient;
            appointment.doctor = doctor;
            
            return Ok(appointment);


        }catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
        
        
        
        
        return Ok();
    }



    //TODO : the second end point to implemant later
    [HttpPost]
    public async Task<IActionResult> AddAppointment(Appointment appointment)
    {
        return Ok();
    }
    
}