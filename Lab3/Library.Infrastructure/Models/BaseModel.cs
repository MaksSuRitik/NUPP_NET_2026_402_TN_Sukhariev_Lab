namespace Library.Infrastructure.Models
{
    public abstract class BaseModel
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; } = Guid.NewGuid();
    }
}
