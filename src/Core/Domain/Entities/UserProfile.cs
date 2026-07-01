using System.ComponentModel.DataAnnotations;

namespace JobSearchAi.Core.Domain.Entities
{
    public class UserProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? BrandStatement { get; set; }
        public string? Summary { get; set; }
        public string? MasterResumePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
