// stand-ins for the ABP base DTOs (Volo.Abp.Application.Dtos), so the sample needs no ABP package
namespace Volo.Abp.Application.Dtos;

public class EntityDto
{
}

public class EntityDto<TKey> : EntityDto
{
    public TKey Id { get; set; } = default!;
}

public class CreationAuditedEntityDto<TKey> : EntityDto<TKey>
{
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public long TotalCount { get; set; }
}
