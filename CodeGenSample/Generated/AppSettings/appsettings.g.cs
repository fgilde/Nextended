namespace AppSettings
{
	public partial class ServerConfiguration
	{
		public string ClientUrl { get; set; }
		public CfgConnectionStrings ConnectionStrings { get; set; }
		public CfgPublicSettings PublicSettings { get; set; }
		public string AllowedHosts { get; set; }
		public CfgAppConfiguration AppConfiguration { get; set; }
		public CfgCognitiveServices CognitiveServices { get; set; }
		public CfgApiDocumentation ApiDocumentation { get; set; }
		public CfgMailConfiguration MailConfiguration { get; set; }
		public CfgBackupOptions BackupOptions { get; set; }
	}

	public partial class CfgBackupOptions
	{
		public string BucketName { get; set; }
	}

	public partial class CfgMailConfiguration
	{
		public string SendGridApiKey { get; set; }
		public string From { get; set; }
		public string Host { get; set; }
		public int Port { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string DisplayName { get; set; }
	}

	public partial class CfgApiDocumentation
	{
		public bool RequireLogin { get; set; }
		public bool RequirePermission { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public CfgContact Contact { get; set; }
		public CfgLicense License { get; set; }
	}

	public partial class CfgLicense
	{
		public string Name { get; set; }
		public string SpdxId { get; set; }
		public string Url { get; set; }
	}

	public partial class CfgContact
	{
		public string Name { get; set; }
		public string Email { get; set; }
		public string Url { get; set; }
	}

	public partial class CfgCognitiveServices
	{
		public CfgOpenAi OpenAi { get; set; }
		public CfgTranslation Translation { get; set; }
	}

	public partial class CfgTranslation
	{
		public string Key { get; set; }
		public string TextTranslationEndpoint { get; set; }
		public string DocumentTranslationEndpoint { get; set; }
		public string Region { get; set; }
	}

	public partial class CfgOpenAi
	{
		public string ApiKey { get; set; }
		public string Model { get; set; }
	}

	public partial class CfgAppConfiguration
	{
		public CfgIdHashing IdHashing { get; set; }
		public string Secret { get; set; }
	}

	public partial class CfgIdHashing
	{
		public bool Enabled { get; set; }
		public int MinLength { get; set; }
		public bool AllowAccessWithNotHashedId { get; set; }
		public string Salt { get; set; }
	}

	public partial class CfgPublicSettings
	{
		public bool AssistantAvailable { get; set; }
		public string ContactAddress { get; set; }
		public bool HostClientInServer { get; set; }
		public CfgUserRegistration UserRegistration { get; set; }
		public CfgLoginSettings LoginSettings { get; set; }
	}

	public partial class CfgLoginSettings
	{
		public string LoginMode { get; set; }
		public bool AllowLoginWithUsername { get; set; }
		public List<string> AllowedEmails { get; set; }
	}

	public partial class CfgUserRegistration
	{
		public bool Enabled { get; set; }
		public bool RequireAddress { get; set; }
		public bool RequiresAdministratorActivation { get; set; }
		public bool EmailConfirmationRequired { get; set; }
		public CfgUsernameRules UsernameRules { get; set; }
		public CfgPasswordRules PasswordRules { get; set; }
		public bool RequireDocuments { get; set; }
		public int RegistrationDocumentsMaxFileSize { get; set; }
		public List<string> RegistrationDocumentTypes { get; set; }
		public List<string> AllowedEmails { get; set; }
	}

	public partial class CfgPasswordRules
	{
		public int MinLength { get; set; }
		public bool CapitalLetterRequired { get; set; }
		public bool LowercaseLetterRequired { get; set; }
		public bool NumberRequired { get; set; }
	}

	public partial class CfgUsernameRules
	{
		public int MinLength { get; set; }
		public bool UsernameCanChangedAfterRegistration { get; set; }
		public bool EmailCanChangedAfterRegistration { get; set; }
	}

	public partial class CfgConnectionStrings
	{
		public string DefaultConnection { get; set; }
		public string Ollama { get; set; }
	}

}
