namespace Nextended.Aspire.Hosting.Supabase.Config;

public interface ISupabaseReferenceInfo
{
    public string ProjectRefId { get; }
    public string ServiceKey { get; }
    public string GetApiUrl() => $"https://{ProjectRefId}.supabase.co";
}

public interface ISupabaseFullSyncInfo : ISupabaseReferenceInfo
{
    public string DbPassword { get; }
    public string ManagementToken { get; }
}