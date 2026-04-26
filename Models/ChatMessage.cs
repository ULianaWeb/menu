using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace menu.Models
{
    public enum ChatMessageType
    {
        Text = 0,
        File = 1
    }

    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        public string? SenderEmail { get; set; }

        // Якщо null => загальне повідомлення
        public string? ReceiverId { get; set; }

        public string? ReceiverEmail { get; set; }

        public ChatMessageType MessageType { get; set; }

        // nullable
        public string? Message { get; set; }

        public string? FileName { get; set; }

        public string? FileUrl { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
