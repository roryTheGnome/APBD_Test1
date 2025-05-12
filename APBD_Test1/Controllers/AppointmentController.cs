using APBD_Test1.Models;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<List<Appointment>> GetAppointmentInformation(string id)
    {
        return Ok();
    }



    //TODO : the second end point to implemant later
    [HttpPost]
    public async Task<List<Appointment>> AddAppointment(Appointment appointment)
    {
        return Ok();
    }
    
}