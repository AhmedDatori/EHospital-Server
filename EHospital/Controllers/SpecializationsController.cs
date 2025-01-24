using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EHospital.Models;
using System.Threading.Tasks;
using System.IO;
using EHospital.Data;
using Microsoft.AspNetCore.Authorization;
using System.Numerics;
using System.Security.Claims;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpecializationsController(UsersDbContext context) : ControllerBase
    {
        private readonly UsersDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<List<Specializations>>> GetSpecializations()
        {
            var specializations = await _context.Specializations.ToListAsync();

            return Ok(specializations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Specializations>> GetSpecialization(int id)
        {
            var specialization = await _context.Specializations.Where(s => s.ID == id).FirstOrDefaultAsync();

            if (specialization == null) return NotFound();
            return Ok(specialization);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddSpecialization(Specializations specialization)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };

            _context.Specializations.Add(specialization);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSpecialization), new { id = specialization.ID }, specialization);
        }

        [Authorize] 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSpecialization(int id,Specializations updatedSpecialization)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };


            var specialization = await _context.Specializations.FirstOrDefaultAsync(s => s.ID == id);

            if (specialization == null) return NotFound();

            specialization.Specialization = updatedSpecialization.Specialization;

            await _context.SaveChangesAsync();
            return Ok(specialization);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSpecialization(int id)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");

            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };


            var specialization = await _context.Specializations.FirstOrDefaultAsync(s => s.ID == id);
            var doctors = await _context.Doctors.Where(d => d.SpecializationID == id).ToListAsync();
            if (doctors.Count > 0) return BadRequest("Cannot delete specialization with doctors assigned to it");

            if (specialization == null) return NotFound();
            _context.Specializations.Remove(specialization);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
