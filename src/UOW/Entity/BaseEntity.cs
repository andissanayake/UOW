using Dapper.Contrib.Extensions;

namespace UOW
{
    public class BaseEntity<TID> : AuditableEntity
    {
        [Key]
        public TID Id { get; set; } = default!;
    }
    public class AuditableEntity
    {
        public DateTimeOffset Created { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTimeOffset? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
