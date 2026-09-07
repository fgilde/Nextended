using Xunit;

namespace Nextended.Aspire.Hosting.Supabase.Tests;

/// <summary>
/// All model tests run serially. Two reasons: <c>AddSupabase</c> materializes config files
/// (infra/supabase/config/kong.yml …) under the test output directory, which parallel classes
/// would write at the same time; and <see cref="Builders.SupabaseBuilderExtensions.PublishTarget"/>
/// is a static switch, so concurrent tests would race over it.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SupabaseModelCollection
{
    public const string Name = "supabase-model";
}
