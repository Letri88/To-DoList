using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class Reminder
    {
        [Key]
        public int ReminderId { get; set; }

        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public TodoTask? TodoTask { get; set; }

        public DateTime ReminderTime { get; set; }

        public bool IsSent { get; set; } = false;
    }
}
