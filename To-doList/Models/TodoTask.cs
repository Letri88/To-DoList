using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class TodoTask
    {
        [Key]
        public int TaskId { get; set; }

        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public AppUserModel? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int Priority { get; set; } = 2; // 1: High, 2: Medium, 3: Low

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
    }
}
