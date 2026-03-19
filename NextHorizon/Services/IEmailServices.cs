using System.Threading.Tasks;

namespace NextHorizon.Services
{
    public interface IEmailService
    {
        Task<bool> SendOTPEmailAsync(string email, string name, string otpCode);
        Task<bool> SendPasswordResetConfirmationAsync(string email, string name);
    }
}