using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class TaskAssignment
    {
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public TodoTask? Task { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public AppUserModel? User { get; set; }

        public string Role { get; set; } = "Assignee";
    }
}
