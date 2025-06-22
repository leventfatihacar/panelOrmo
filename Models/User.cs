using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace panelOrmo.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        public string PasswordSalt { get; set; }
        public string Email { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }

    public class UserCreateViewModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Email { get; set; }
        public bool IsSuperAdmin { get; set; }
    }
}