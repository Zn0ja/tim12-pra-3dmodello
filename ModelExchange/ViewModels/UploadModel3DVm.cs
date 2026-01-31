using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ModelExchange.ViewModels
{
    public class UploadModel3DVm
    {
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
        public IFormFile File { get; set; } = null!;
    }
}
