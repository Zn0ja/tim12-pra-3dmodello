using System.ComponentModel.DataAnnotations;

namespace ModelExchange.Models
{
    public class Model3D
    {
        public int Id { get; set; }

        [Required]
        public string OwnerUserId { get; set; } = default!;

        [Required, MaxLength(120)]
        public string Name { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(400)]
        public string? Tags { get; set; }

        [MaxLength(80)]
        public string? Category { get; set; }

        [Required]
        public string Visibility { get; set; } = "public";

        [Required]
        public string FilePath { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
