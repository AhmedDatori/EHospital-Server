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
using EHospital.Services.Caching;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly UsersDbContext _context;
        private readonly IAuthService _authService;
        private readonly HybridCache _hybridCache;
        private readonly IRedisCacheService _cache;

        public PatientsController(UsersDbContext context, IAuthService authService, HybridCache hybridCache, IRedisCacheService cache)
        {
            _context = context;
            _authService = authService;
            _hybridCache = hybridCache;
            _cache = cache;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetPatients()
        {

            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };

            var cacheKey = "Patients";

            // Redis cache
            var cachedPatient = _cache.GetData<List<Patients>>(cacheKey);
            if (cachedPatient != null) {
                return Ok(cachedPatient);
            }

            cachedPatient = await _context.Patients.ToListAsync();
            _cache.SetData(cacheKey, cachedPatient);


            // Hybrid cache
            //var cachedPatient = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var patients = await _context.Patients.ToListAsync();
            //    return patients;

            //});


            return Ok(cachedPatient);
        }

        [Authorize]
        [HttpGet]
        [Route("/api/Patient/userId/{userID}")]
        public async Task<ActionResult> GetPatientByUserID(int userID)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue( ClaimTypes.Role);


            if (currentUserRole != "admin" && currentUserRole != "doctor")
            {
                
                    if (currentUserID != userID.ToString()) return Forbid("You are not authorized to view this content");

            };



            var cacheKey = $"Patient_UserID_{userID}";
            // Redis cache
            var cachedPatient = _cache.GetData<Patients>(cacheKey);
            if (cachedPatient != null)
            {
                return Ok(cachedPatient);
            }
            var patient = await _context.Patients
                .Where(p => p.UserID == userID)
                .FirstOrDefaultAsync();
            if (patient == null) return null;
            _cache.SetData(cacheKey, patient);


            // Hybrid cache
            //var cachedPatient = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var patient = await _context.Patients
            //    .Where(p => p.UserID == userID)
            //    .FirstOrDefaultAsync();
            //    if (patient == null) return null;
            //    return patient;
            //});
            return Ok(patient);
        }


        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetPatient(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue( ClaimTypes.Role);


            var cacheKey = $"Patient_{id}";

            // Redis cache
            var cachedPatient = _cache.GetData<Patients>(cacheKey);
            if (cachedPatient != null)
            {
                return Ok(cachedPatient);
            }
            cachedPatient = await _context.Patients.FindAsync(id);
            if (cachedPatient == null) return null;
            _cache.SetData(cacheKey, cachedPatient);





            // Hybrid cache
            //var cachedPatient = await _hybridCache.GetOrCreateAsync(cacheKey, async t =>
            //{
            //    var patient = await _context.Patients.FindAsync(id);

            //    if (currentUserRole != "admin" && currentUserRole != "doctor")
            //    {
            //        currentUserRole = HttpContext.User.FindFirstValue("role");
            //        if (currentUserRole != "admin" && currentUserRole != "doctor")
            //            if (currentUserID != patient.UserID.ToString()) return null;
            //    };

            //    return patient;

            //});


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

            // Redis cache
            _cache.DeleteData("Patients");

            return CreatedAtAction(nameof(GetPatient), new { id = patient.ID }, patient);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, Patients updatedPatient)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);




            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            if (currentUserRole != "admin" && currentUserRole != "doctor")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin" && currentUserRole != "doctor")
                    if (currentUserID != patient.UserID.ToString()) return Forbid("You are not authorized to view this content");

            };


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

            // Redis cache
            _cache.DeleteData($"Patient_{id}");
            _cache.DeleteData("Patients");


            return Ok(patient);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var currentUserID = HttpContext.User.FindFirstValue("userID");
            var currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);




            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            if (currentUserRole != "admin" && currentUserRole != "doctor")
            {
                currentUserRole = HttpContext.User.FindFirstValue("role");
                if (currentUserRole != "admin" && currentUserRole != "doctor")
                    if (currentUserID != patient.UserID.ToString()) return Forbid("You are not authorized to view this content");

            };

                _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            // Delete all the apointments that has the patientID == id
            var appointments = await _context.Appointments.Where(a => a.PatientsID == id).ToListAsync();
            _context.Appointments.RemoveRange(appointments);
            await _context.SaveChangesAsync();





            // delete the user from the UserH table
            var user = await _context.UserH.FindAsync(patient.UserID);
            await _authService.DeleteUserAsync(user);


            // Redis cache
            _cache.DeleteData($"Patient_{id}");
            _cache.DeleteData("Patients");

            return NoContent();
        }
    }
}
