using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class SubTask
    {
        [Key]
        public int SubTaskId { get; set; }

        public int TaskId { get; set; }
        
        [ForeignKey("TaskId")]
        public TodoTask? TodoTask { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
