using System.ComponentModel.DataAnnotations;

namespace OmniscanAdmin.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class TwoFactorModel
    {
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "2FA code is required")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Code must be 4 digits")]
        public string Code { get; set; } = string.Empty;
    }

    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
    }

    public class Chip
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "health";
        public string LastCommand { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class DisableChipModel
    {
        [Required(ErrorMessage = "Chip ID is required")]
        public int ChipId { get; set; }

        [Required(ErrorMessage = "Master disable code is required")]
        [StringLength(50, MinimumLength = 10, ErrorMessage = "Master disable code must be between 10-50 characters")]
        public string DisableCode { get; set; } = string.Empty;
    }

    public class UpdateChipStatusModel
    {
        [Required(ErrorMessage = "Chip ID is required")]
        public int ChipId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;

        public string? Command { get; set; }
    }

    public class BatchUpdateChipStatusModel
    {
        public List<UpdateChipStatusModel> Updates { get; set; } = new();
    }
}