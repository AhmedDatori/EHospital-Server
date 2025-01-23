using EHospital.Data;
using EHospital.Models;
using EHospital.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
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
            var cachKey = "Doctors";
            var cachedDoctor = await _hybridCache.GetOrCreateAsync(cachKey, async t =>
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
            var cachedDoctor = await _hybridCache.GetOrCreateAsync($"DoctorUser_{userID}", async t =>
            {
                var doctor = await _context.Doctors
                    .Where(d => d.UserID == userID)
                    .FirstOrDefaultAsync();
                return doctor;
            });
            
            if (cachedDoctor == null) return NotFound();
            return Ok(cachedDoctor);
        }


        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddDoctor(Doctors newDoctor)
        {
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, Doctors updatedDoctor)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(x => x.ID == id);
            if (doctor == null) return NotFound();

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
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync(); // Complete all changes related to doctor before proceeding

            var user = await _context.UserH.FindAsync(doctor.UserID);
            if (user != null)
            {
                await _authService.DeleteUserAsync(user); // This method needs to await all its internal operations using _context
                 // Only called after DeleteUserAsync completes
            }

            return NoContent();
        }


    }
}
