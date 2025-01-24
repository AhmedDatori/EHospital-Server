using EHospital.Data;
using EHospital.Models;
using EHospital.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly UsersDbContext _context;
        private readonly IAuthService _authService;
        private readonly HybridCache _hybridCache;

        public DoctorsController(UsersDbContext context, IAuthService authService , HybridCache hybridCache)
        {
            _context = context;
            _authService = authService;
            _hybridCache = hybridCache;
        }

        [HttpGet]
        public async Task<ActionResult> GetDoctors()
        {
            var cacheKey = "Doctors";
            var cachedDoctor = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            {
                var doctors = await _context.Doctors.ToListAsync();
                return doctors;
            });
            return Ok(cachedDoctor);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetDoctorById(int id)
        {
            var cachedDoctor = await _hybridCache.GetOrCreateAsync($"Doctor_{id}", async t =>
            {
                var doctor = await _context.Doctors.FindAsync(id);
                return doctor;
            });
            
            return Ok(cachedDoctor);
        }

        [HttpGet]
        [Route("/api/Doctors/userId/{userID}")]
        public async Task<ActionResult> GetDoctorByUserID(int userID)
        {
            var cachedDoctor = await _hybridCache.GetOrCreateAsync($"DoctorUser_u{userID}", async t =>
            {
                var doctor = await _context.Doctors
                    .Where(d => d.UserID == userID)
                    .FirstOrDefaultAsync();
                return doctor;
            });
            
            if (cachedDoctor == null) return NotFound();
            return Ok(cachedDoctor);
        }

        // get all the patients that has an appointment with the doctor
        [Authorize]
        [HttpGet]
        [Route("/api/Doctors/{id}/Patients")]
        public async Task<ActionResult> GetDoctorPatients(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");


            var doctor = await _context.Doctors.FindAsync( id);


            if (doctor == null) return NotFound();
            if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
                    return Forbid("You are not authorized to view this content");
            };


            var cachedPatients = await _hybridCache.GetOrCreateAsync($"DoctorPatients_{id}", async t =>
            {
                // Fetch appointments for the doctor
                var appointmentPatientIds = await _context.Appointments
                    .Where(a => a.DoctorID == id)
                    .Select(a => a.PatientsID)
                    .Distinct() // Ensure unique patient IDs
                    .ToListAsync();

                // Fetch patients based on the patient IDs
                var patients = await _context.Patients
                    .Where(p => appointmentPatientIds.Contains(p.ID))
                    .ToListAsync();

                return patients;

                
            });

            return Ok(cachedPatients);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddDoctor(Doctors newDoctor)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };

            Users newUser = new Users
            {
                Email = newDoctor.Email,
                Password = newDoctor.Password,
                Role = "doctor"
            };

            var registeredUser = await _authService.RegisterAsync(newUser);
            if (registeredUser == null)
                return BadRequest("Email already exists");

            newDoctor.UserID = registeredUser.Id;

            // Temporarily set IDENTITY_INSERT to ON
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Doctors ON");
            _context.Doctors.Add(newDoctor);
            await _context.SaveChangesAsync();

            // Set IDENTITY_INSERT back to OFF
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Doctors OFF");


            return CreatedAtAction(nameof(GetDoctorById), new { id = newDoctor.ID }, newDoctor);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, Doctors updatedDoctor)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");


            var doctor = await _context.Doctors.FirstOrDefaultAsync(x => x.ID == id);


            if (doctor == null) return NotFound();
            if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
                    return Forbid("You are not authorized to view this content");
            };

            
            var userUpdate = await _authService.UpdateUserAsync(updatedDoctor.Email, doctor.Email, updatedDoctor.Password);
            if (userUpdate == null)
                return BadRequest("Problem updating user");

            doctor.FirstName = updatedDoctor.FirstName;
            doctor.LastName = updatedDoctor.LastName;
            doctor.Email = updatedDoctor.Email;
            doctor.Password = updatedDoctor.Password;
            doctor.Birthdate = updatedDoctor.Birthdate;

            await _context.SaveChangesAsync();
            return Ok(doctor);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");



            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();


            if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin" && currentUserID != doctor.UserID.ToString())
                    return Forbid("You are not authorized to view this content");
            };


            


            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            var appointments = await _context.Appointments.Where(a => a.DoctorID == id).ToListAsync();
            _context.Appointments.RemoveRange(appointments);
            await _context.SaveChangesAsync();

            var user = await _context.UserH.FindAsync(doctor.UserID);
            if (user != null)
            {
                await _authService.DeleteUserAsync(user); 
            }

            return NoContent();
        }


    }
}
