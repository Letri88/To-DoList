using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public TodoTask? Task { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public AppUserModel? User { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
