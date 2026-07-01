using System.ComponentModel.DataAnnotations;

namespace JobSearchAi.Core.Domain.Entities
{
    public class AuditEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string EntityType { get; set; } = null!;

        [Required]
        public string EntityId { get; set; } = null!;

        [Required]
        public string Action { get; set; } = null!;

        public string? Actor { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
