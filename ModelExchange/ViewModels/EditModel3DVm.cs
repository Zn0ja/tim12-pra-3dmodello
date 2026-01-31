using System.ComponentModel.DataAnnotations;

namespace ModelExchange.ViewModels
{
    public class EditModel3DVm
    {
        public int Id { get; set; }

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
    }
}
