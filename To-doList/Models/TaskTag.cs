using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class TaskTag
    {
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public TodoTask? TodoTask { get; set; }

        public int TagId { get; set; }

        [ForeignKey("TagId")]
        public Tag? Tag { get; set; }
    }
}
