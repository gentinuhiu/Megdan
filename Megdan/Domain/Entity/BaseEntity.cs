using System.ComponentModel.DataAnnotations;

namespace Megdan.Domain.Entity
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
