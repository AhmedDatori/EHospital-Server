using EHospital.Data;
using EHospital.Entities;
using EHospital.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EHospital.Services
{
    public class AuthService(UsersDbContext context, IConfiguration configuration) : IAuthService
    {
        public async Task<ActionResult> DeleteUserAsync(UserH user)
        {
            if (user == null) return null;
            context.UserH.Remove(user);
            context.SaveChangesAsync();
            return null;
        }

        public async Task<TokenResponse> LoginAsync(Users request)
        {
            var user = await context.UserH.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return null;

            if (new PasswordHasher<UserH>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return await CreateTokenResponse(user);
        }

        private async Task<TokenResponse> CreateTokenResponse(UserH? user)
        {
            return new TokenResponse
            {
                AccessToken = GenerateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        public async Task<UserH?> RegisterAsync(Users request)
        {
            if(await context.UserH.AnyAsync(u => u.Email == request.Email))
            {
                return null;
            }
            var user = new UserH();
            var hashPassword = new PasswordHasher<UserH>()
                .HashPassword(user, request.Password);
            user.Email = request.Email;
            user.PasswordHash = hashPassword;
            user.Role = request.Role;

            context.UserH.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<UserH> UpdateUserAsync(string newEmail,string email, string password)
        {
            var userh = await context.UserH.FirstOrDefaultAsync(u => u.Email == email);
            if (userh == null)
            {
                return null;
            }
            var userE = await context.UserH.FirstOrDefaultAsync(u => u.Email == newEmail);

            if (userE == null || userE.Id==userh.Id)
            {
                userh.Email = email;
            }
            else
            {
                
                return null;
            }

            var hashedPassword = new PasswordHasher<UserH>().HashPassword(userh, password);
            userh.PasswordHash = hashedPassword;

            context.UserH.Update(userh);
            await context.SaveChangesAsync();
            return userh;

        }

        public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
                return null;

            return await CreateTokenResponse(user);

        }

        private async Task<UserH?> ValidateRefreshTokenAsync(int userId,string refreshToken)
        {
            var user = await context.UserH.FindAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return null;
            }
            return user;
        }


        private string GenerateRefreshToken()
        {
            var randNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randNumber);
            return Convert.ToBase64String(randNumber);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(UserH user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await context.SaveChangesAsync();
            return refreshToken;

        }

        private string GenerateToken(UserH user)
        {
            var claims = new List<Claim>
            {
                new Claim("email",user.Email),
                new Claim("userID", user.Id.ToString()),
                new Claim("role", user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public async Task<UserH> GetUserByIdAsync(int userId)
        {

            
            var user = await context.UserH.FindAsync(userId);

            return user;
        }
    }
}
