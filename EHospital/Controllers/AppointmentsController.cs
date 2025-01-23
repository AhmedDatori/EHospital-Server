using EHospital.Data;
using EHospital.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Numerics;
using System.Security.Claims;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController(UsersDbContext context, HybridCache hybridCache) : ControllerBase
    {
        private readonly UsersDbContext _context = context;
        private readonly HybridCache _hybridCache = hybridCache;

        //[Authorize]
        [HttpGet]
        public async Task<ActionResult<List<Appointments>>> GetAppointments()
        {
            //var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            //if (currentUserRole != "admin") return Forbid();
            //var cachKey = $"Appointments";
            //var cachedData = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
            //{
                var appointments = await _context.Appointments.ToListAsync();
                return appointments;
            //});
            //return Ok(cachedData);
        }

        [Authorize]
        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<List<Appointments>>> GetAppointmentByID(int id)
        {

            // Get user ID and role from the token
            var currentUserID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();
            var patient = await _context.Patients.FindAsync(appointment.PatientsID);
            var doctor = await _context.Doctors.FindAsync( appointment.DoctorID);

            // Check if the current user is the patient or an admin
            if ((currentUserID != patient.UserID.ToString() && currentUserID != doctor.UserID.ToString()) && currentUserRole != "admin")
            {
                return Forbid(); // Forbids access if not the patient or an admin
            }

            return Ok(appointment);
        }

        //[Authorize]
        [HttpPost]
        public async Task<ActionResult<Appointments>> AddAppointment(Appointments newAppointment)
        {
            if (newAppointment == null) return BadRequest();
            
            //var currentUserID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);


            var patient = await _context.Patients.FindAsync(newAppointment.PatientsID);
            var doctor = await _context.Doctors.FindAsync(newAppointment.DoctorID);
            if (patient == null || doctor == null) return BadRequest();
            //if ((currentUserID != patient.UserID.ToString() && currentUserID != doctor.UserID.ToString()) && currentUserRole != "admin") 
            //{ 
            //    return Forbid();
            //}

            
            // if user has another appointment with the same doctor then return bad request
            var appointment = await _context.Appointments.FirstOrDefaultAsync(x => x.DoctorID == newAppointment.DoctorID && x.PatientsID == newAppointment.PatientsID);

            if (appointment != null) return BadRequest("You already have an appointment with the same doctor");

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAppointmentByID), new { id = newAppointment.ID }, newAppointment);
        }

        //[HttpPut]
        //[Route("{id}")]
        //public async Task<IActionResult> UpdateAppointment(int id, Appointments updatedAppointment)
        //{
        //    var appointment = await _context.Appointments.FirstOrDefaultAsync(x => x.ID == id);
        //    if (appointment == null) return NotFound();
        //    appointment.DoctorID = updatedAppointment.DoctorID;

        //    await _context.SaveChangesAsync();
        //    return Ok(appointment);
        //}

        //[Authorize]
        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> DeleteAppointment(int id)
        {
            //var currentUserID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var patient = await _context.Patients.FindAsync(appointment.PatientsID);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorID);

            //if ((currentUserID != patient.UserID.ToString() && currentUserID != doctor.UserID.ToString()) && currentUserRole != "admin")
            //{
            //    return Forbid();
            //}

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //[Authorize]
        [HttpGet]
        [Route("/api/Appointments/patient/{patientID}")]
        public async Task<ActionResult<List<Appointments>>> GetAppointmentsByPatient(int patientID)
        {
                // Get user ID and role from the token
            //var currentUserID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var patient = await _context.Patients.FindAsync(patientID);
            if (patient == null) return null;

            // Check if the current user is the patient or an admin
            //if (currentUserID != patient.UserID.ToString() && currentUserRole != "admin")
            //{
            //    return Forbid(); // Forbids access if not the patient or an admin
            //}

            //var cachKey = $"Appointments_p{patientID}";
            //var cachedData = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
            //{
                var appointments = await _context.Appointments
                .Where(x => x.PatientsID == patient.ID)
                .ToListAsync();

                if (appointments == null || appointments.Count == 0)
                    return null;

                return appointments;
            //});

            //return Ok(cachedData);
            
        }

        



        ////get appointment by patient id
        //[Authorize]
        //[HttpGet]
        //[Route("/api/Appointments/patients/{patientID}")]
        //public async Task<ActionResult<List<Appointments>>> GetAppointmentsByPatientID(int patientID)
        //{
        //    var currentUserID = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
        //    var patient = await _context.Patients.FindAsync(patientID);
        //    // Check if the current user is the patient or an admin
        //    if (currentUserID != patient.UserID.ToString() && currentUserRole != "admin")
        //    {
        //        return Forbid(); // Forbids access if not the patient or an admin
        //    }
        //    var cachKey = $"Appointments_pp{patientID}";

        //    var cachedData = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
        //    {
        //        var appointments = await _context.Appointments
        //        .Where(x => x.PatientsID == patientID)
        //        .ToListAsync();

        //        if (appointments == null || appointments.Count == 0)
        //            return null;

        //        return appointments;
        //    });

        //    return Ok(cachedData);
        //}


    }
}
