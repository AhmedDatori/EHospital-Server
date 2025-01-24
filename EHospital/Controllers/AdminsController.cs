using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EHospital.Models;
using System.Threading.Tasks;
using EHospital.Data;
using EHospital.Services;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetAdmins()
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };
            var admins = await _context.Admins.ToListAsync();
            return Ok(admins);
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetAdmin(int id)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };

            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();
            return Ok(admin);
        }

        //get admin by admin.userID
        [Authorize]
        [HttpGet("userId/{userID}")]
        public async Task<ActionResult> GetAdminByUserID(int userID)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };
            

            var admin = await _context.Admins
                .Where(a => a.UserID == userID)
                .FirstOrDefaultAsync();
            if (admin == null) return NotFound();
            return Ok(admin);
        }



        //[Authorize]
        [HttpPost]
        public async Task<ActionResult> AddAdmin(Admins admin)
        {
            //var currentUserRole = HttpContext.User.FindFirstValue("role");
            //if (currentUserRole != "admin")
            //    return Forbid("You are not authorized to view this content");


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

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin(int id, Admins updatedAdmin)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };


            var admin = await _context.Admins.FindAsync(id);
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


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var currentUserRole = HttpContext.User.FindFirstValue("role");
            if (currentUserRole != "admin")
            {
                currentUserRole = HttpContext.User.FindFirstValue(ClaimTypes.Role);
                if (currentUserRole != "admin")
                    return Forbid("You are not authorized to view this content");
            };


            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();
            
           
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync(); 


            
            var user = await _context.UserH.FindAsync(admin.UserID);
            if (user != null)
            {
                await _authService.DeleteUserAsync(user); 
            }

            return NoContent();
        }
    }
}
