using EHospital.Data;
using EHospital.Models;
using EHospital.Services.Caching;
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
    public class AppointmentsController(UsersDbContext context, HybridCache hybridCache, IRedisCacheService cache) : ControllerBase
    {
        private readonly UsersDbContext _context = context;
        private readonly HybridCache _hybridCache = hybridCache;
        private readonly IRedisCacheService _cache = cache;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<Appointments>>> GetAppointments()
        {
            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            }

            var cacheKey = $"Appointments";
            // Redis cache
            var cachedData = _cache.GetData<List<Appointments>>(cacheKey);
            if (cachedData != null)
            {
                return Ok(cachedData);
            }
            cachedData = await _context.Appointments.ToListAsync();
            _cache.SetData(cacheKey, cachedData);

            // Hybrid cache
            //var cachedData = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var appointments = await _context.Appointments.ToListAsync();
            //    return appointments;
            //});
            return Ok(cachedData);
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

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Appointments>> AddAppointment(Appointments newAppointment)
        {
            if (newAppointment == null) return BadRequest();

            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue( ClaimTypes.Role);


            var patient = await _context.Patients.FindAsync(newAppointment.PatientsID);
            var doctor = await _context.Doctors.FindAsync(newAppointment.DoctorID);
            if (patient == null || doctor == null) return BadRequest();


            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin")
                    if(currentUserID != patient.UserID.ToString() && currentUserID != doctor.UserID.ToString())
                        return Forbid("You are not authorized to view this content");
            }


            // if user has another appointment with the same doctor then return bad request
            var appointment = await _context.Appointments.FirstOrDefaultAsync(x => x.DoctorID == newAppointment.DoctorID && x.PatientsID == newAppointment.PatientsID);

            if (appointment != null) return BadRequest("You already have an appointment with the same doctor");

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            // Redis cache
            _cache.DeleteData("Appointments");
            _cache.DeleteData($"Appointments_p{patient.ID}");
            _cache.DeleteData($"Appointments_d{doctor.ID}");

            return CreatedAtAction(nameof(GetAppointmentByID), new { id = newAppointment.ID }, newAppointment);
        }

        [Authorize]
        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();


            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role );


            var patient = await _context.Patients.FindAsync(appointment.PatientsID);
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorID);
            if (patient == null || doctor == null) return BadRequest();

            if (currentUserRole != "admin" && currentUserRole != "doctor")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin" && currentUserRole != "doctor")
                    if (currentUserID != patient.UserID.ToString() && currentUserID != doctor.UserID.ToString())
                        return Forbid("You are not authorized to view this content");
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            // Redis cache
            _cache.DeleteData("Appointments");
            _cache.DeleteData($"Appointments_p{patient.ID}");

            return NoContent();
        }

        [Authorize]
        [HttpGet]
        [Route("/api/Appointments/patient/{patientID}")]
        public async Task<ActionResult<List<Appointments>>> GetAppointmentsByPatient(int patientID)
        {
            //Get user ID and role from the token
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue( ClaimTypes.Role);
            var patient = await _context.Patients.FindAsync(patientID);
            if (patient == null) return null;


            //Check if the current user is the patient or an admin
            if (currentUserRole != "admin" && currentUserRole != "doctor")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin" && currentUserRole!="doctor")
                    if (currentUserID != patient.UserID.ToString()) 
                        return Forbid("You are not authorized to view this content");
            }

            var cacheKey = $"Appointments_p{patientID}";
            // Redis cache
            var cachedData = _cache.GetData<List<Appointments>>(cacheKey);
            if (cachedData != null)
            {
                return Ok(cachedData);
            }
            cachedData = await _context.Appointments
                .Where(x => x.PatientsID == patientID)
                .ToListAsync();
            if (cachedData == null || cachedData.Count == 0)
                return NotFound("No appointments found");
            _cache.SetData(cacheKey, cachedData);

            // Hybrid cache
            //var cachedData = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var appointments = await _context.Appointments
            //    .Where(x => x.PatientsID == patient.ID)
            //    .ToListAsync();

            //    if (appointments == null || appointments.Count == 0)
            //        return null;

            //    return appointments;
            //});

            return Ok(cachedData);

        }


        [Authorize]
        [HttpGet]
        [Route("/api/Appointments/doctor/{doctorID}")]
        public async Task<ActionResult<List<Appointments>>> GetAppointmentsByDoctor(int doctorID)
        {
            //Get user ID and role from the token
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue( ClaimTypes.Role);
            var doctor = await _context.Doctors.FindAsync(doctorID);
            if (doctor == null) return NotFound("Doctor not found"); ;


            //Check if the current user is the patient or an admin
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin")
                    if (currentUserID != doctor.UserID.ToString()) return Forbid("You are not authorized to view this content");
            }

            var cacheKey = $"Appointments_d{doctorID}";
            // Redis cache
            var cachedData = _cache.GetData<List<Appointments>>(cacheKey);
            if (cachedData != null)
            {
                return Ok(cachedData);
            }
            cachedData = await _context.Appointments
                .Where(x => x.DoctorID == doctorID)
                .ToListAsync();
            if (cachedData == null || cachedData.Count == 0)
                return NotFound("No appointments found");
            _cache.SetData(cacheKey, cachedData);


            // Hybrid cache
            //var cachedData = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var appointments = await _context.Appointments
            //    .Where(x => x.DoctorID == doctor.ID)
            //    .ToListAsync();

            //    if (appointments == null || appointments.Count == 0)
            //        return null;

            //    return appointments;
            //});

            return Ok(cachedData);

        }


    }
}
