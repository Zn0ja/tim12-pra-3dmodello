namespace ModelExchange.ViewModels
{
    public class ModelCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Category { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FilePath { get; set; } = "";
        public string OwnerUserName { get; set; } = "";
    }
}
