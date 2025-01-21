using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EHospital.Models;
using System.Threading.Tasks;
using EHospital.Data;
using EHospital.Services;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminsController : ControllerBase
    {
        private readonly UsersDbContext _context;
        private readonly IAuthService _authService;

        public AdminsController(UsersDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }
        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetAdmins()
        {
            var admins = await _context.Admins.ToListAsync();
            return Ok(admins);
        }
        //[Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetAdmin(int id)
        {
            var admin = await _context.Admins
                .Where(a => a.ID == id).FirstOrDefaultAsync();
            if (admin == null) return NotFound();
            return Ok(admin);
        }

        //get admin by admin.userID
        [HttpGet("userId/{userID}")]
        public async Task<ActionResult> GetAdminByUserID(int userID)
        {
            var admin = await _context.Admins
                .Where(a => a.UserID == userID)
                .FirstOrDefaultAsync();
            if (admin == null) return NotFound();
            return Ok(admin);
        }



        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> AddAdmin(Admins admin)
        {
            Users newUser = new Users
            {
                Email = admin.Email,
                Password = admin.Password,
                Role = "admin"
            };

            var registeredUser = await _authService.RegisterAsync(newUser);
            if (registeredUser == null)
                return BadRequest("Email already exists");

            admin.UserID = registeredUser.Id;

            // Temporarily set IDENTITY_INSERT to ON
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Admins ON");
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            // Set IDENTITY_INSERT back to OFF
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Admins OFF");

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAdmin), new { id = admin.ID }, admin);
        }
        //[Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin(int id, Admins updatedAdmin)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.ID == id);
            if (admin == null) return NotFound();

            var userUpdate = await _authService.UpdateUserAsync(updatedAdmin.Email, admin.Email, updatedAdmin.Password);
            if (userUpdate == null)
                return BadRequest("Problem updating user");

            admin.FirstName = updatedAdmin.FirstName;
            admin.LastName = updatedAdmin.LastName;
            admin.Email = updatedAdmin.Email;
            admin.Password = updatedAdmin.Password;
            admin.Birthdate = updatedAdmin.Birthdate;

            await _context.SaveChangesAsync();
            return Ok(admin);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.ID == id);
            if (admin == null) return NotFound();

            // Remove the admin entry first
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync(); // Ensure this save is completed before proceeding

            // Assuming you need to delete a corresponding user
            var user = await _context.UserH.FirstOrDefaultAsync(u => u.Email == admin.Email);
            if (user != null)
            {
                await _authService.DeleteUserAsync(user); // Assuming this method does not use _context internally
            }

            return NoContent();
        }
    }
}
