using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire;
using Nextended.Aspire.Hosting.LocalAI;
using Nextended.Aspire.Hosting.N8n.Builders;
using Nextended.Aspire.Hosting.WebDataStudio;


var builder = DistributedApplication.CreateBuilder(args);

if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");


var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
 
    .WithGPUSupport(OllamaGpuVendor.Nvidia);

ollama.AddModel("llama3.2");         
ollama.AddModel("nomic-embed-text");

var localai = builder.AddLocalAI("localai", o =>
    {
        o.Gpu = LocalAiGpu.Nvidia; 
    })
    .WithDataVolume()
    .AddModel(KnownTextModel.Qwen3_8b)            
    .AddModel(KnownEmbeddingModel.BertEmbeddings)  
    .AddModel(KnownHuggingFaceImageModel.NsfwGenV2) 
    .AddModel(KnownHuggingFaceImageModel.NsfwV1)  
    .AddModel(KnownSoundModel.AceStepTurbo)
    .WithAceStepUi()
    .WithSdNextUi()
    .WithOpenWebUI()
    ;

var localAiOpenAiBase = ReferenceExpression.Create($"{localai.Resource.HttpEndpoint}/v1");

var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    .WaitFor(ollama)
    .WaitFor(localai)
    .WithEnvironment("OPENAI_API_BASE_URL", localAiOpenAiBase)
    .WithEnvironment("OPENAI_BASE_URL", localAiOpenAiBase) 
    .WithEnvironment("LOCALAI_BASE_URL", localAiOpenAiBase)
    .WithEnvironment("OPENAI_API_KEY", "sk-local")      
    .WithEnvironment("OLLAMA_BASE_URL", ollama.Resource.PrimaryEndpoint)
    .WithEnvironment("OLLAMA_HOST", ollama.Resource.PrimaryEndpoint)
    .WithEnvironmentVariable("N8N_BLOCK_ENV_ACCESS_IN_NODE", "false")
    .WithWorkflowsFromDirectory(Path.Combine(builder.AppHostDirectory, "workflows"))
    .WithOwner("admin@localhost", "Test1234!", "Admin");

var pg = builder.AddPostgres("pg");
var db = pg.AddDatabase("havewadb", "havewa");

var dbUrl = ReferenceExpression.Create(
    $"postgresql://postgres:{pg.Resource.PasswordParameter}@{pg.Resource.PrimaryEndpoint.Property(EndpointProperty.Host)}:{pg.Resource.PrimaryEndpoint.Property(EndpointProperty.Port)}/havewa");

// The database studio, with its assistance pointed at the Ollama in this stack: the statement or
// the question goes to a container next door, so nothing about it leaves the machine. Swap
// WithOllamaAssistant for WithLocalAiAssistant(localai, "qwen3-8b") to use LocalAI instead.
builder.AddWebDataStudio("studio")
    .WithReference(db)
    .WithOllamaAssistant(ollama, "llama3.2")
    .WithLogin("hans", "hans")
    .WithUser("grace", "grace", StudioRoles.Viewer);

builder.AddGithubRepository("havewa", "https://github.com/fgilde/hausverwaltung")
    .WithHttpEndpoint(targetPort: 3000)
    .WithEnvironment("DATABASE_URL", dbUrl)
    .WaitFor(db);


builder.Build().EnsureDockerRunningIfLocalDebug().Run();
