namespace UtilityBillSplitterAPI.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        public string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}

