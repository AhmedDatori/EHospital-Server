using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EHospital.Models;
using System.Threading.Tasks;
using EHospital.Data;
using EHospital.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using EHospital.Entities;
using System.Security.Claims;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly UsersDbContext _context;
        private readonly IAuthService _authService;
        private readonly HybridCache _hybridCache;

        public PatientsController(UsersDbContext context, IAuthService authService, HybridCache hybridCache)
        {
            _context = context;
            _authService = authService;
            _hybridCache = hybridCache;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetPatients()
        {

            var currentUserRole = HttpContext.User.FindFirstValue("role");


            if (currentUserRole != "admin") return Forbid($"You are not admin {currentUserRole}");

            var cachKey = "Patients";
            var cachedPatient = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
            {
                var patients = await _context.Patients.ToListAsync();
                return patients;

            });


            return Ok(cachedPatient);
        }

        [Authorize]
        [HttpGet]
        [Route("/api/Patient/userId/{userID}")]
        public async Task<ActionResult> GetPatientByUserID(int userID)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            if (currentUserID != userID.ToString() && currentUserRole != "admin") return Forbid();

            var cachKey = $"Patient_UserID_{userID}";

            var cachedPatient = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
            {
                var patient = await _context.Patients
                .Where(p => p.UserID == userID)
                .FirstOrDefaultAsync();
                if (patient == null) return null;
                return patient;
        });
            return Ok(cachedPatient);
        }


        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetPatient(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");


            var cachKey = $"Patient_{id}";
            var cachedPatient = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
            {
                var patient = await _context.Patients.FindAsync(id);
            if(currentUserID != patient.UserID.ToString() && currentUserRole != "admin" && currentUserRole != "doctor") return null;
                return patient;

            });


            return cachedPatient is null ? NotFound() : Ok(cachedPatient);
        }

        [HttpPost]
        public async Task<ActionResult> AddPatient(Patients patient)
        {
            Users newUser = new Users
            {
                Email = patient.Email,
                Password = patient.Password,
                Role = "patient"
            };

            var registeredUser = await _authService.RegisterAsync(newUser);
            if (registeredUser == null)
                return BadRequest("Email already exists");

            patient.UserID = registeredUser.Id;
            // Temporarily set IDENTITY_INSERT to ON
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Patients ON");

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Set IDENTITY_INSERT back to OFF
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Patients OFF");

            return CreatedAtAction(nameof(GetPatient), new { id = patient.ID }, patient);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, Patients updatedPatient)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            if (currentUserID != updatedPatient.UserID.ToString() && currentUserRole != "admin") return Forbid();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ID == id);
            if (patient == null) return NotFound();
            if(currentUserID != patient.UserID.ToString() && currentUserRole != "admin") return Forbid();

            var user = await _context.UserH.FindAsync(patient.UserID);
            var checkEmail = await _context.Patients.AnyAsync(Patients => Patients.Email == updatedPatient.Email && patient.ID!=updatedPatient.ID);
            if (user == null || checkEmail==null)
            {
                return BadRequest("Problem updating user"); 
            }
            else
            {
                user.Email = updatedPatient.Email;
                var hashedPassword = new PasswordHasher<UserH>().HashPassword(user, updatedPatient.Password);
                user.PasswordHash = hashedPassword;
                await _context.SaveChangesAsync();
            }
            
            

            patient.FirstName = updatedPatient.FirstName;
            patient.LastName = updatedPatient.LastName;
            patient.Email = updatedPatient.Email;
            patient.Password = updatedPatient.Password;
            patient.Birthdate = updatedPatient.Birthdate;

            await _context.SaveChangesAsync();
            return Ok(patient);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            // Find the patient
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            // check if the user is the patient or an admin
            if (currentUserID != patient.UserID.ToString() && currentUserRole != "admin") return Forbid();


            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            // Delete all the apointments that has the patientID == id
            var appointments = await _context.Appointments.Where(a => a.PatientsID == id).ToListAsync();
            _context.Appointments.RemoveRange(appointments);
            await _context.SaveChangesAsync();





            // delete the user from the UserH table
            var user = await _context.UserH.FindAsync(patient.UserID);
            await _authService.DeleteUserAsync(user);


            return NoContent();
        }
    }
}
