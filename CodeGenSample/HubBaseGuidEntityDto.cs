namespace MyGenerated.Code.Test;

public partial class HubBaseGuidEntityDto : IEntityDto<Guid>
{
    public Guid Id { get; set; }
}

public interface IEntityDto<TKey> 
{
    TKey Id { get; set; }
}
