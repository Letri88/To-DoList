using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace To_doList.Models
{
    public class TaskAttachment
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public TodoTask? TodoTask { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
