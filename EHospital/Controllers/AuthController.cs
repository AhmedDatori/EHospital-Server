using EHospital.Entities;
using EHospital.Models;
using EHospital.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EHospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<UserH>> register(Users request)
        {
            var user = await authService.RegisterAsync(request);
            if (user == null)
            {
                return BadRequest("User Already Exists");
            }


            return Ok(user);
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<TokenResponse>> login(Users request)
        {
            var result = await authService.LoginAsync(request);
            if (result == null)
            {
                return BadRequest("Invalid Credentials");
            }
            return Ok(result);


        }

        [Authorize]
        [HttpPost]
        [Route("refresh-token")]
        public async Task<ActionResult<TokenResponse>> RefreshToken(RefreshTokenRequest request)
        {
            var result = await authService.RefreshTokenAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
            {
                return BadRequest("Invalid Refresh Token");
            }
            return Ok(result);
        }



        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok("You are authenticated");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("admin")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("You are Admin");
        }

        
    
}
}
