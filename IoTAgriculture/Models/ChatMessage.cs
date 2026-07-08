using System.ComponentModel.DataAnnotations;

namespace IoTAgriculture.Models
{
    public class ChatMessage
    {
        [Key]
        public Guid MessageId { get; set; }

        public Guid UserId { get; set; }

        public string Sender { get; set; } = "";

        public string? MessageText { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public AppUser AppUser { get; set; } = null!;
    }
}