using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TagName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Color { get; set; }

        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public AppUserModel? User { get; set; }

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}
