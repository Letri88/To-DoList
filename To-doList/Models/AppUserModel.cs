// Models/AppUserModel.cs
using Microsoft.AspNetCore.Identity;

namespace To_doList.Models
{
    public class AppUserModel : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}