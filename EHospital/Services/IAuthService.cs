using EHospital.Entities;
using EHospital.Models;
using Microsoft.AspNetCore.Mvc;

namespace EHospital.Services
{
    public interface IAuthService
    {
        Task<UserH?> RegisterAsync(Users request);
        Task<TokenResponse> LoginAsync(Users request);
        Task<UserH> UpdateUserAsync(string newEmail,string email, string password);
        Task<ActionResult> DeleteUserAsync(UserH user);
        Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<UserH> GetUserByIdAsync(int userId);
    }
}
