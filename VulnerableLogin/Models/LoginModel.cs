using System.ComponentModel.DataAnnotations;

namespace VulnerableLogin.Models
{
    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }
    
    public class TwoFactorModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
    
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}