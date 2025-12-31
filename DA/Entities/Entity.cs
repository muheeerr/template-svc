using System.ComponentModel.DataAnnotations;

namespace DA.Entities
{
    public class Entity : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; }
    }
    public class BaseAuditableEntity
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTimeOffset? UpdatedAt { get; set; } 
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        //public bool IsServiceGenereated { get; set; } = false;
    }
    public class EntityDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
