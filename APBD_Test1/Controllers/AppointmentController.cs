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
    [Route("{id}")]
    public async Task<IActionResult> GetAppointmentInformation(string id,CancellationToken token)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);
        //init objects here latr
        Doctor doctor = new Doctor();
        Patient patient = new Patient();
        Appointment appointment = new Appointment{
            services = new List<Service>()
        };
        //List<Service> serviceList = new List<Service>();
        int patientId ;

        try
        {
            await using var cmd = new SqlCommand("select * from Appointment where appointment_id = @id", con);
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
                                where appointment_id = @id", con);
            cmdService.Parameters.AddWithValue("@id", id);
            
            // TODO check later
            await using var rdrService = await cmdService.ExecuteReaderAsync(token);
            if (!await rdrService.ReadAsync(token))
                return NotFound("no service found");
            while (await rdrService.ReadAsync(token))
            {
                var serv = new Service
                {
                    name = (string)rdrService["name"],
                    price = (decimal)rdrService["service_fee"]
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
        
    }

    //TODO : the second end point to implemant later
    [HttpPost]
    public async Task<IActionResult> AddAppointment([FromBody] CreateAppointmentRequest request,CancellationToken token)
    {
        await using var con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await con.OpenAsync(token);
        await using var transaction = await con.BeginTransactionAsync(token);


        try
        {
            var checkPatient = new SqlCommand(@"select 1 from patient where patient_id = @id", con,
                (SqlTransaction)transaction);
            checkPatient.Parameters.AddWithValue("@id", request.PatientId);
            if (await checkPatient.ExecuteScalarAsync(token) is null)
                return NotFound("no such patient exists");

            var getDoctor = new SqlCommand("SELECT doctor_id FROM Doctor WHERE PWZ = @pwz", con,
                (SqlTransaction)transaction);
            getDoctor.Parameters.AddWithValue("@pwz", request.Pwz);
            var doctorIdObj = await getDoctor.ExecuteScalarAsync(token);
            if (doctorIdObj is null)
                return NotFound("no such doctor exists");
            var doctorId = (int)doctorIdObj;

            var getMaxId = new SqlCommand("SELECT MAX(appointment_id) FROM Appointment", con,
                (SqlTransaction)transaction);
            var maxAppointmentIdObj = await getMaxId.ExecuteScalarAsync(token);
            var newAppointmentId = (maxAppointmentIdObj is DBNull) ? 1 : (int)maxAppointmentIdObj + 1;

            var insertAppo = new SqlCommand(@"
            INSERT INTO Appointment (appointment_id, patient_id, doctor_id, date)
            VALUES (@appoitmentId, @patientId, @doctorId, @date);
        ", con, (SqlTransaction)transaction);
            insertAppo.Parameters.AddWithValue("@appoitmentId", newAppointmentId);
            insertAppo.Parameters.AddWithValue("@patientId", request.PatientId);
            insertAppo.Parameters.AddWithValue("@doctorId", doctorId);
            insertAppo.Parameters.AddWithValue("@date", DateTime.UtcNow);

            await insertAppo.ExecuteNonQueryAsync(token);

            foreach (var service in request.Services)
            {
                var getServiceId = new SqlCommand("SELECT service_id FROM Service WHERE name = @name", con,
                    (SqlTransaction)transaction);
                getServiceId.Parameters.AddWithValue("@name", service.ServiceName);
                var serviceIdObj = await getServiceId.ExecuteScalarAsync(token);

                if (serviceIdObj is null)
                    return NotFound($"Service '{service.ServiceName}' not found.");

                int serviceId = (int)serviceIdObj;

                var insertService = new SqlCommand(@"
                INSERT INTO Appointment_Service (appointment_id, service_id, service_fee)
                VALUES (@appoitmentId, @serviceId, @fee)
            ", con, (SqlTransaction)transaction);

                insertService.Parameters.AddWithValue("@appoitmentId", newAppointmentId);
                insertService.Parameters.AddWithValue("@serviceId", serviceId);
                insertService.Parameters.AddWithValue("@fee", service.ServiceFee);

                await insertService.ExecuteNonQueryAsync(token);
            }

            await transaction.CommitAsync(token);
            return Ok("success,yey");

        }
        catch(Exception ex)
        {
            await transaction.RollbackAsync(token);
            return StatusCode(500, $"Error here: {ex.Message}");
        }
        
        //return Ok();
    }
    
}