using System.ComponentModel.DataAnnotations;

namespace ModelExchange.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        public int Model3DId { get; set; }
        public Model3D Model3D { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
