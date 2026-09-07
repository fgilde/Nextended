using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// Everything an AspireUI instance can be handed on first start: accounts with permissions, deploy
/// targets, api tokens for automation, store sources, apps from the catalog and stacks from a folder
/// or a repository — plus the settings that used to need a trip through the UI.
/// <para>
/// All of it is idempotent by name on the AspireUI side: restarting with the same AppHost changes
/// nothing, adding one entry adds exactly that one, and anything changed in the UI stays changed.
/// </para>
/// </summary>
public static class AspireUISeedExtensions
{
    private const string SeedMount = "/seed";

    // --- Accounts -------------------------------------------------------------------------------

    /// <summary>
    /// Creates an account on first start. <paramref name="permissions"/> is a preset from
    /// <see cref="AspireUIPermissions"/> or a comma-separated list of permission ids; left out, the
    /// account gets the default set (builder, install, configure).
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithUser(
        this IResourceBuilder<AspireUIResource> builder, string username, string password,
        string? permissions = null, string[]? viewModes = null, bool mustChangePassword = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        builder.Resource.Seed.Users.Add(new AspireUISeed.UserSpec(username.Trim(),
            SeedValue.From(password), permissions, false, viewModes, mustChangePassword));
        return builder;
    }

    /// <summary>
    /// Creates an account whose password comes from an Aspire parameter, so it stays out of the
    /// AppHost source and the manifest.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithUser(
        this IResourceBuilder<AspireUIResource> builder, string username,
        IResourceBuilder<ParameterResource> password, string? permissions = null,
        string[]? viewModes = null, bool mustChangePassword = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        builder.Resource.Seed.Users.Add(new AspireUISeed.UserSpec(username.Trim(),
            SeedValue.From(password.Resource), permissions, false, viewModes, mustChangePassword));
        return builder;
    }

    /// <summary>Creates several accounts at once.</summary>
    public static IResourceBuilder<AspireUIResource> WithUsers(
        this IResourceBuilder<AspireUIResource> builder, params AspireUIUser[] users)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(users);
        foreach (var u in users)
        {
            if (string.IsNullOrWhiteSpace(u.Username) || string.IsNullOrWhiteSpace(u.Password)) continue;
            builder.Resource.Seed.Users.Add(new AspireUISeed.UserSpec(u.Username.Trim(),
                SeedValue.From(u.Password), u.Permissions, u.Admin, u.ViewModes, u.MustChangePassword));
        }
        return builder;
    }

    /// <summary>Creates an account that may install, configure and browse files, but not build stacks.</summary>
    public static IResourceBuilder<AspireUIResource> WithAppUser(
        this IResourceBuilder<AspireUIResource> builder, string username, string password) =>
        builder.WithUser(username, password, AspireUIPermissions.AppUser, ["simple"]);

    /// <summary>Creates an account that may look at the apps, their logs and their files, and change nothing.</summary>
    public static IResourceBuilder<AspireUIResource> WithViewer(
        this IResourceBuilder<AspireUIResource> builder, string username, string password) =>
        builder.WithUser(username, password, AspireUIPermissions.Viewer, ["simple"]);

    /// <summary>Creates a bearer token for automation. The value is yours to pick — it has to be known to be used.</summary>
    public static IResourceBuilder<AspireUIResource> WithApiToken(
        this IResourceBuilder<AspireUIResource> builder, string name, string username, string token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        builder.Resource.Seed.Tokens.Add(new AspireUISeed.TokenSpec(name.Trim(), username.Trim(), SeedValue.From(token)));
        return builder;
    }

    /// <summary>Creates a bearer token whose value comes from an Aspire parameter.</summary>
    public static IResourceBuilder<AspireUIResource> WithApiToken(
        this IResourceBuilder<AspireUIResource> builder, string name, string username,
        IResourceBuilder<ParameterResource> token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        builder.Resource.Seed.Tokens.Add(new AspireUISeed.TokenSpec(name.Trim(), username.Trim(), SeedValue.From(token.Resource)));
        return builder;
    }

    // --- Deploy targets -------------------------------------------------------------------------

    /// <summary>
    /// Adds a deploy target: another machine's docker daemon over SSH. <paramref name="keyFile"/> is a
    /// path on the AppHost machine — the file is mounted into the container read-only and never ends
    /// up in an environment variable. Key auth only: docker's ssh transport has nowhere to type a
    /// password.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSshTarget(
        this IResourceBuilder<AspireUIResource> builder, string name, string host, string user = "root",
        int port = 22, string? keyFile = null, string? key = null, string? passphrase = null,
        string? publicHost = null, bool isDefault = false, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        builder.Resource.Seed.Targets.Add(new AspireUISeed.TargetSpec(name.Trim(), "ssh")
        {
            Host = host.Trim(),
            Port = port,
            User = user,
            Key = key ?? Mount(builder, keyFile, "keys"),
            Passphrase = passphrase,
            PublicHost = publicHost,
            Default = isDefault,
            Notes = notes,
        });
        return builder;
    }

    /// <summary>
    /// Adds a deploy target: a docker daemon over TCP with mTLS. The three certificate files are
    /// mounted into the container read-only.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithDockerTcpTarget(
        this IResourceBuilder<AspireUIResource> builder, string name, string host, int port = 2376,
        string? caFile = null, string? certFile = null, string? keyFile = null,
        string? publicHost = null, bool isDefault = false, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        builder.Resource.Seed.Targets.Add(new AspireUISeed.TargetSpec(name.Trim(), "dockerTcp")
        {
            DockerHost = $"tcp://{host.Trim()}:{port}",
            Ca = Mount(builder, caFile, "certs"),
            Cert = Mount(builder, certFile, "certs"),
            TlsKey = Mount(builder, keyFile, "certs"),
            PublicHost = publicHost,
            Default = isDefault,
            Notes = notes,
        });
        return builder;
    }

    /// <summary>
    /// Adds a deploy target: a Kubernetes cluster, deployed with Helm. <paramref name="expose"/> is
    /// <c>clusterip</c>, <c>nodeport</c>, <c>loadbalancer</c> or <c>ingress</c>;
    /// <paramref name="ingressHost"/> may use <c>{app}</c> and <c>{service}</c>.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithKubernetesTarget(
        this IResourceBuilder<AspireUIResource> builder, string name, string? context = null,
        string? kubeconfigFile = null, string? @namespace = null, string? expose = null,
        string? ingressHost = null, string? storageClass = null, bool isDefault = false, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        builder.Resource.Seed.Targets.Add(new AspireUISeed.TargetSpec(name.Trim(), "k8s")
        {
            Context = context,
            Kubeconfig = Mount(builder, kubeconfigFile, "kube"),
            Namespace = @namespace,
            Expose = expose,
            IngressHost = ingressHost,
            StorageClass = storageClass,
            Default = isDefault,
            Notes = notes,
        });
        return builder;
    }

    // --- Apps and stacks ------------------------------------------------------------------------

    /// <summary>
    /// Installs apps from AspireUI's own catalog by id (<c>vaultwarden</c>, <c>gitea</c>, …) — the
    /// same apps the store lists, built the way the install dialog would build them.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithApps(
        this IResourceBuilder<AspireUIResource> builder, params string[] catalogIds)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(catalogIds);
        foreach (var id in catalogIds)
            if (!string.IsNullOrWhiteSpace(id))
                builder.Resource.Seed.Apps.Add(new AspireUISeed.AppSpec(id.Trim(), null, null));
        return builder;
    }

    /// <summary>Installs one app from the catalog, under a name of your choosing.</summary>
    public static IResourceBuilder<AspireUIResource> WithApp(
        this IResourceBuilder<AspireUIResource> builder, string catalogId, string? name = null, bool? deploy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogId);
        builder.Resource.Seed.Apps.Add(new AspireUISeed.AppSpec(catalogId.Trim(), name, deploy));
        return builder;
    }

    /// <summary>Registers an app manifest url as a store source. Nothing is fetched until the store is opened.</summary>
    public static IResourceBuilder<AspireUIResource> WithAppSource(
        this IResourceBuilder<AspireUIResource> builder, string name, string url)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        builder.Resource.Seed.AppSources.Add(new AspireUISeed.SourceSpec(name.Trim(), url.Trim()));
        return builder;
    }

    /// <summary>
    /// Imports a folder from the AppHost machine as a stack: an app manifest, a docker-compose file or
    /// an Aspire AppHost, whichever it holds. The folder is mounted read-only and imported as a copy,
    /// so AspireUI never writes to your sources.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSeedFromDirectory(
        this IResourceBuilder<AspireUIResource> builder, string hostPath, string? name = null,
        string? mode = null, bool? deploy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        var inside = Mount(builder, hostPath, "stacks", isDirectory: true)!;
        builder.Resource.Seed.Stacks.Add(new AspireUISeed.StackSpec
        {
            Name = name, Path = inside, Mode = mode, Deploy = deploy,
        });
        return builder;
    }

    /// <summary>Imports a single docker-compose file from the AppHost machine as a stack.</summary>
    public static IResourceBuilder<AspireUIResource> WithSeedFromCompose(
        this IResourceBuilder<AspireUIResource> builder, string hostPath, string? name = null, bool? deploy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        builder.Resource.Seed.Stacks.Add(new AspireUISeed.StackSpec
        {
            Name = name, Compose = Mount(builder, hostPath, "compose"), Deploy = deploy,
        });
        return builder;
    }

    /// <summary>
    /// Clones a repository inside the container and imports it as a stack. Nothing is mounted — the
    /// container needs to reach the repository itself.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSeedFromGit(
        this IResourceBuilder<AspireUIResource> builder, string url, string? branch = null,
        string? subdir = null, string? name = null, string? mode = null, bool? deploy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        builder.Resource.Seed.Stacks.Add(new AspireUISeed.StackSpec
        {
            Name = name, Git = url.Trim(), Branch = branch, Subdir = subdir, Mode = mode, Deploy = deploy,
        });
        return builder;
    }

    /// <summary>
    /// Seeds a stack with one <c>AddProject</c> node per project in this AppHost, and mounts each
    /// project's folder into the container at the same path so the stack can also be built and run
    /// there. Pass <paramref name="mountSources"/> false if you only want the stack on the canvas.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithProjectStack(
        this IResourceBuilder<AspireUIResource> builder, string stackName,
        params IResourceBuilder<ProjectResource>[] projects) =>
        builder.WithProjectStack(stackName, true, projects);

    /// <inheritdoc cref="WithProjectStack(IResourceBuilder{AspireUIResource}, string, IResourceBuilder{ProjectResource}[])"/>
    public static IResourceBuilder<AspireUIResource> WithProjectStack(
        this IResourceBuilder<AspireUIResource> builder, string stackName, bool mountSources,
        params IResourceBuilder<ProjectResource>[] projects)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stackName);
        ArgumentNullException.ThrowIfNull(projects);

        var paths = new List<string>();
        foreach (var project in projects)
        {
            if (project.Resource.Annotations.OfType<IProjectMetadata>().FirstOrDefault() is not { } meta) continue;
            var path = meta.ProjectPath;
            paths.Add(path);
            if (mountSources && Path.GetDirectoryName(path) is { Length: > 0 } dir && Directory.Exists(dir))
                builder.WithBindMount(dir, dir);
        }
        if (paths.Count == 0) return builder;

        builder.Resource.Seed.Stacks.Add(new AspireUISeed.StackSpec
        {
            Name = stackName, Projects = [.. paths],
        });
        return builder;
    }

    /// <summary>
    /// Mounts a seed file (or a folder holding <c>aspireui.seed.json</c>) and points AspireUI at it.
    /// Anything the <c>With…</c> methods add is applied on top of it.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSeedFile(
        this IResourceBuilder<AspireUIResource> builder, string hostPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        var inside = Mount(builder, hostPath, "file", isDirectory: Directory.Exists(hostPath))!;
        return builder.WithEnvironment("ASPIREUI_SEED_FILE", inside);
    }

    /// <summary>
    /// Deploys the seeded stacks and apps as soon as hosting is up, instead of leaving them on the
    /// canvas. Anything that fails to deploy stays as a stack you can look at and start by hand.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithAutoDeploy(
        this IResourceBuilder<AspireUIResource> builder, bool deploy = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.Seed.Deploy = deploy;
        return builder;
    }

    // --- Settings -------------------------------------------------------------------------------

    /// <summary>
    /// The host name app urls are built from. Without it AspireUI uses the host the browser asked
    /// for, which is wrong as soon as anything but your own browser has to reach the apps.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithPublicHost(
        this IResourceBuilder<AspireUIResource> builder, string host)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        return builder.WithEnvironment("ASPIREUI_SET_PublicHost", host.Trim());
    }

    /// <summary>
    /// Points AspireUI at an Nginx Proxy Manager instance, so a hosted app can be given a domain and
    /// a certificate from the app's own menu.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithNginxProxyManager(
        this IResourceBuilder<AspireUIResource> builder, string baseUrl, string email, string password,
        string? forwardHost = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        builder.WithEnvironment("ASPIREUI_SET_NpmEnabled", "true")
               .WithEnvironment("ASPIREUI_SET_NpmBaseUrl", baseUrl.Trim())
               .WithEnvironment("ASPIREUI_SET_NpmEmail", email)
               .WithEnvironment("ASPIREUI_SET_NpmPassword", password);
        if (!string.IsNullOrWhiteSpace(forwardHost))
            builder.WithEnvironment("ASPIREUI_SET_NpmForwardHost", forwardHost.Trim());
        return builder;
    }

    /// <summary>
    /// Points AspireUI at an Nginx Proxy Manager running in the same stack; the base url is taken
    /// from that resource's endpoint. Add <c>WaitFor(npm)</c> yourself if the proxy has to be up
    /// before AspireUI starts.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithNginxProxyManager<T>(
        this IResourceBuilder<AspireUIResource> builder, IResourceBuilder<T> npm, string email,
        IResourceBuilder<ParameterResource> password, string endpointName = "http", string? forwardHost = null)
        where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(npm);
        ArgumentNullException.ThrowIfNull(password);
        builder.WithEnvironment("ASPIREUI_SET_NpmEnabled", "true")
               .WithEnvironment("ASPIREUI_SET_NpmBaseUrl", npm.GetEndpoint(endpointName))
               .WithEnvironment("ASPIREUI_SET_NpmEmail", email)
               .WithEnvironment("ASPIREUI_SET_NpmPassword", password);
        if (!string.IsNullOrWhiteSpace(forwardHost))
            builder.WithEnvironment("ASPIREUI_SET_NpmForwardHost", forwardHost.Trim());
        return builder;
    }

    /// <summary>
    /// Where AspireUI reports a deployment that came up, went down or started failing: a webhook
    /// (Slack, Discord, Teams, ntfy, …) and/or a Telegram chat.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithNotifications(
        this IResourceBuilder<AspireUIResource> builder, string? webhookUrl = null,
        string? telegramToken = null, string? telegramChat = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (!string.IsNullOrWhiteSpace(webhookUrl)) builder.WithEnvironment("ASPIREUI_SET_NotifyWebhookUrl", webhookUrl.Trim());
        if (!string.IsNullOrWhiteSpace(telegramToken)) builder.WithEnvironment("ASPIREUI_SET_NotifyTelegramToken", telegramToken.Trim());
        if (!string.IsNullOrWhiteSpace(telegramChat)) builder.WithEnvironment("ASPIREUI_SET_NotifyTelegramChat", telegramChat.Trim());
        return builder;
    }

    /// <summary>
    /// Backs up every hosted app's volumes on a schedule, keeping the last
    /// <paramref name="retain"/> snapshots. Zero hours turns the schedule off.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithBackupSchedule(
        this IResourceBuilder<AspireUIResource> builder, int intervalHours = 24, int retain = 7)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .WithEnvironment("ASPIREUI_SET_BackupIntervalHours", Math.Max(0, intervalHours).ToString())
            .WithEnvironment("ASPIREUI_SET_BackupRetain", (retain <= 0 ? 7 : retain).ToString());
    }

    /// <summary>
    /// Hosts an Aspire dashboard next to every deployed app, with a token AspireUI can turn into a
    /// one-click login link.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithHostedDashboards(
        this IResourceBuilder<AspireUIResource> builder, string? browserToken = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.WithEnvironment("ASPIREUI_SET_HostDashboard", "true");
        if (!string.IsNullOrWhiteSpace(browserToken))
            builder.WithEnvironment("ASPIREUI_SET_DashboardToken", browserToken.Trim());
        return builder;
    }

    /// <summary>Overrides any AspireUI setting by key — the escape hatch for anything without its own method.</summary>
    public static IResourceBuilder<AspireUIResource> WithSetting(
        this IResourceBuilder<AspireUIResource> builder, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return builder.WithEnvironment("ASPIREUI_SET_" + key.Trim(), value);
    }

    /// <summary>
    /// Applies <c>ASPIREUI_SET_*</c> values on every start instead of only filling in what is still
    /// empty. Off by default, because it also overwrites what somebody changed in the UI.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithForcedSettings(
        this IResourceBuilder<AspireUIResource> builder, bool force = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithEnvironment("ASPIREUI_SET_FORCE", force ? "true" : "false");
    }

    /// <summary>
    /// Hands sign-in to an identity provider that speaks OpenID Connect. Only the authority, the
    /// client id and — for a confidential client — the secret are needed; the endpoints come from the
    /// provider's own discovery document. Password sign-in keeps working alongside it.
    /// </summary>
    /// <param name="authority">The issuer url, e.g. <c>https://id.example.com/realms/main</c>.</param>
    /// <param name="clientId">The client registered with the provider.</param>
    /// <param name="clientSecret">The client secret, or null for a public client.</param>
    /// <param name="label">What the login button says: "Sign in with …".</param>
    /// <param name="scopes">Space-separated scopes; the default asks for the usual three.</param>
    /// <param name="usernameClaim">Which claim is the login name. Null tries the usual ones.</param>
    /// <param name="groupsClaim">Which claim holds the groups.</param>
    /// <param name="adminGroup">Members of this group are admins — on every sign-in, in both directions.</param>
    /// <param name="autoCreate">Create an account the first time somebody signs in.</param>
    /// <param name="defaultPermissions">What a created account gets: a preset from <see cref="AspireUIPermissions"/> or a list of ids.</param>
    public static IResourceBuilder<AspireUIResource> WithSingleSignOn(
        this IResourceBuilder<AspireUIResource> builder, string authority, string clientId,
        string? clientSecret = null, string? label = null, string? scopes = null,
        string? usernameClaim = null, string? groupsClaim = null, string? adminGroup = null,
        bool autoCreate = true, string? defaultPermissions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        builder.WithSetting("OidcEnabled", "true")
               .WithSetting("OidcAuthority", authority.Trim().TrimEnd('/'))
               .WithSetting("OidcClientId", clientId.Trim())
               .WithSetting("OidcAutoCreate", autoCreate ? "true" : "false");
        if (!string.IsNullOrWhiteSpace(clientSecret)) builder.WithSetting("OidcClientSecret", clientSecret!);
        if (!string.IsNullOrWhiteSpace(label)) builder.WithSetting("OidcLabel", label!.Trim());
        if (!string.IsNullOrWhiteSpace(scopes)) builder.WithSetting("OidcScopes", scopes!.Trim());
        if (!string.IsNullOrWhiteSpace(usernameClaim)) builder.WithSetting("OidcUsernameClaim", usernameClaim!.Trim());
        if (!string.IsNullOrWhiteSpace(groupsClaim)) builder.WithSetting("OidcGroupsClaim", groupsClaim!.Trim());
        if (!string.IsNullOrWhiteSpace(adminGroup)) builder.WithSetting("OidcAdminGroup", adminGroup!.Trim());
        if (!string.IsNullOrWhiteSpace(defaultPermissions)) builder.WithSetting("OidcDefaultPermissions", defaultPermissions!.Trim());
        return builder;
    }

    /// <inheritdoc cref="WithSingleSignOn(IResourceBuilder{AspireUIResource}, string, string, string?, string?, string?, string?, string?, string?, bool, string?)"/>
    /// <remarks>The secret comes from an Aspire parameter, so it stays out of the source and the manifest.</remarks>
    public static IResourceBuilder<AspireUIResource> WithSingleSignOn(
        this IResourceBuilder<AspireUIResource> builder, string authority, string clientId,
        IResourceBuilder<ParameterResource> clientSecret, string? label = null, string? scopes = null,
        string? usernameClaim = null, string? groupsClaim = null, string? adminGroup = null,
        bool autoCreate = true, string? defaultPermissions = null)
    {
        ArgumentNullException.ThrowIfNull(clientSecret);
        return builder
            .WithSingleSignOn(authority, clientId, (string?)null, label, scopes, usernameClaim,
                groupsClaim, adminGroup, autoCreate, defaultPermissions)
            .WithEnvironment("ASPIREUI_SET_OidcClientSecret", clientSecret);
    }

    /// <summary>
    /// Copies every backup to an S3-compatible bucket as well. A backup on the same disk as the app
    /// is a backup of that disk being fine.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithS3Backups(
        this IResourceBuilder<AspireUIResource> builder, string bucket, string accessKey, string secretKey,
        string? endpoint = null, string? region = null, bool pathStyle = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        builder.WithSetting("BackupRemoteKind", "s3")
               .WithSetting("BackupS3Bucket", bucket.Trim())
               .WithSetting("BackupS3AccessKey", accessKey)
               .WithSetting("BackupS3SecretKey", secretKey)
               .WithSetting("BackupS3PathStyle", pathStyle ? "true" : "false");
        if (!string.IsNullOrWhiteSpace(endpoint)) builder.WithSetting("BackupS3Endpoint", endpoint!.Trim());
        if (!string.IsNullOrWhiteSpace(region)) builder.WithSetting("BackupS3Region", region!.Trim());
        return builder;
    }

    /// <summary>Copies every backup to a WebDAV share as well (Nextcloud, ownCloud, a plain apache).</summary>
    public static IResourceBuilder<AspireUIResource> WithWebDavBackups(
        this IResourceBuilder<AspireUIResource> builder, string baseUrl, string user, string password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return builder
            .WithSetting("BackupRemoteKind", "webdav")
            .WithSetting("BackupWebDavUrl", baseUrl.Trim())
            .WithSetting("BackupWebDavUser", user)
            .WithSetting("BackupWebDavPassword", password);
    }

    /// <summary>
    /// Copies every backup to a directory on another machine over scp. Key auth only — scp has
    /// nowhere to type a password — and the key path is one inside the container.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithSshBackups(
        this IResourceBuilder<AspireUIResource> builder, string host, string user, string path,
        string? keyFile = null, int port = 22)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        builder.WithSetting("BackupRemoteKind", "sftp")
               .WithSetting("BackupSftpHost", host.Trim())
               .WithSetting("BackupSftpUser", user)
               .WithSetting("BackupSftpPath", path)
               .WithSetting("BackupSftpPort", port.ToString());
        // A key on the AppHost machine is mounted read-only, exactly as the ssh target's key is.
        if (!string.IsNullOrWhiteSpace(keyFile))
            builder.WithSetting("BackupSftpKeyFile", Mount(builder, keyFile, "keys")!);
        return builder;
    }

    /// <summary>How long the activity log keeps an entry. Zero keeps everything.</summary>
    public static IResourceBuilder<AspireUIResource> WithAuditRetention(
        this IResourceBuilder<AspireUIResource> builder, int days)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithSetting("AuditRetainDays", Math.Max(0, days).ToString());
    }

    // --- The container itself -------------------------------------------------------------------

    /// <summary>
    /// Keeps AspireUI's data (stacks, users, settings) in a folder on the host instead of a named
    /// volume — handy when you want to look at it, back it up or delete it by hand.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithDataBindMount(
        this IResourceBuilder<AspireUIResource> builder, string hostPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPath);
        builder.Resource.Annotations.Remove(DataMount(builder.Resource));
        return builder.WithBindMount(hostPath, "/data");
    }

    /// <summary>
    /// Runs AspireUI without the host's docker socket. It can still build and publish stacks and
    /// deploy to a remote target, but it cannot host anything on this machine.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithoutDockerSocket(
        this IResourceBuilder<AspireUIResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var mount in builder.Resource.Annotations.OfType<ContainerMountAnnotation>()
                     .Where(m => m.Target.EndsWith("docker.sock", StringComparison.Ordinal)).ToList())
            builder.Resource.Annotations.Remove(mount);
        return builder;
    }

    /// <summary>
    /// Points AspireUI's docker client somewhere other than the mounted socket (<c>DOCKER_HOST</c>) —
    /// a daemon over ssh or tcp, for instance. Implies <see cref="WithoutDockerSocket"/>.
    /// </summary>
    public static IResourceBuilder<AspireUIResource> WithDockerHost(
        this IResourceBuilder<AspireUIResource> builder, string dockerHost)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(dockerHost);
        return builder.WithoutDockerSocket().WithEnvironment("DOCKER_HOST", dockerHost.Trim());
    }

    private static ContainerMountAnnotation? DataMount(AspireUIResource resource) =>
        resource.Annotations.OfType<ContainerMountAnnotation>().FirstOrDefault(m => m.Target == "/data");

    /// <summary>
    /// Bind-mounts a host file or folder read-only under /seed and returns the path inside the
    /// container. Key material belongs in a mount, not in an environment variable: everything with
    /// access to the daemon can read the latter.
    /// </summary>
    private static string? Mount(IResourceBuilder<AspireUIResource> builder, string? hostPath, string kind,
        bool isDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(hostPath)) return null;
        var full = Path.GetFullPath(hostPath);
        var leaf = isDirectory
            ? new DirectoryInfo(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Name
            : Path.GetFileName(full);
        if (string.IsNullOrEmpty(leaf)) leaf = kind;
        var inside = $"{SeedMount}/{kind}/{leaf}";
        builder.WithBindMount(full, inside, isReadOnly: true);
        return inside;
    }
}
