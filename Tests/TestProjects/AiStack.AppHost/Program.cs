using Aspire.Hosting.ApplicationModel;
using Nextended.Aspire;
using Nextended.Aspire.Hosting.LocalAI;
using Nextended.Aspire.Hosting.N8n.Builders;

// =============================================================================
//  AI-Stack AppHost — LocalAI + Ollama + n8n, wired together.
//
//  Demonstrates how the Nextended n8n hosting extension can consume the two
//  self-hosted AI backends in this repo:
//    • LocalAI  — OpenAI-compatible multimodal API (chat, embeddings, images, …)
//    • Ollama   — native LLM/embedding server
//    • n8n      — low-code automation; its OpenAI/Ollama/AI-Agent nodes call both.
//
//  What the wiring below does — and what it deliberately does NOT do:
//    Aspire injects the backend URLs into n8n as environment variables and adds
//    WaitFor() ordering, so from inside n8n the backends are reachable and
//    discoverable. n8n itself, however, stores node credentials in its own DB
//    (created via the editor UI), so there is no env var that auto-provisions a
//    ready-made "OpenAI"/"Ollama" credential. You create those once in the n8n UI
//    pointing at the injected URLs (see README.md). The injected env vars are also
//    readable from workflow expressions / Code nodes (N8N_BLOCK_ENV_ACCESS_IN_NODE
//    is relaxed below), which the seeded smoke-test workflow uses.
//
//  Run with `dotnet run` (Docker required). GPU is enabled by default (NVIDIA);
//  see the comments on each backend for CPU-only fallbacks.
// =============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// Azure Container Apps environment for `azd up` deployment (publish mode only).
if (builder.ExecutionContext.IsPublishMode)
    builder.AddAzureContainerAppEnvironment("env");

// -----------------------------------------------------------------------------
//  Ollama — native LLM/embedding server (OpenAI-ish + its own /api).
//  First start pulls the models below (several GB); WithDataVolume keeps them.
// -----------------------------------------------------------------------------
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    // Remove this line on machines without an NVIDIA GPU (Ollama then runs on CPU).
    .WithGPUSupport(OllamaGpuVendor.Nvidia);

ollama.AddModel("llama3.2");         // chat / tool-use LLM
ollama.AddModel("nomic-embed-text"); // embeddings for RAG / vector nodes

// -----------------------------------------------------------------------------
//  LocalAI — self-hosted, OpenAI-compatible multimodal API. Everything n8n's
//  "OpenAI" node can do (chat, embeddings, images, TTS/STT) is served here under
//  one OpenAI-compatible base URL ({endpoint}/v1) with the dev key "sk-local".
// -----------------------------------------------------------------------------
var localai = builder.AddLocalAI("localai", o =>
    {
        o.Gpu = LocalAiGpu.Nvidia; // switch to LocalAiGpu.None for CPU-only hosts
    })
    .WithDataVolume()
    .AddModel(KnownTextModel.Qwen3_8b)             // /v1/chat/completions
    .AddModel(KnownEmbeddingModel.BertEmbeddings)  // /v1/embeddings
    // Optional extra modalities — uncomment to expose them in n8n via the same base:
    // .AddModel(KnownImageModel.StableDiffusion15)  // /v1/images/generations
    // .AddModel(KnownTextToSpeechModel.Kokoro)      // /v1/audio/speech
    // .AddModel(KnownSpeechToTextModel.WhisperBase) // /v1/audio/transcriptions
    ;

// LocalAI speaks the OpenAI API under /v1 — build that base URL once (resolved at runtime).
var localAiOpenAiBase = ReferenceExpression.Create($"{localai.Resource.HttpEndpoint}/v1");

// -----------------------------------------------------------------------------
//  n8n — auto-provisions its own PostgreSQL backend. We wait for both AI
//  backends, then inject their URLs so they're reachable & discoverable inside n8n.
// -----------------------------------------------------------------------------
var n8n = builder.AddN8n("n8n")
    .WithTimezone("Europe/Berlin")
    // Start ordering: bring n8n up only once both AI backends are running.
    .WaitFor(ollama)
    .WaitFor(localai)
    // LocalAI (OpenAI-compatible): base URL + key. Point n8n's "OpenAI" credential here.
    .WithEnvironment("OPENAI_API_BASE_URL", localAiOpenAiBase)
    .WithEnvironment("OPENAI_BASE_URL", localAiOpenAiBase) // some nodes read this alias
    .WithEnvironment("LOCALAI_BASE_URL", localAiOpenAiBase)
    .WithEnvironment("OPENAI_API_KEY", "sk-local")         // LocalAI's default dev key
    // Ollama (native API): base URL. Point n8n's "Ollama" credential here.
    .WithEnvironment("OLLAMA_BASE_URL", ollama.Resource.PrimaryEndpoint)
    .WithEnvironment("OLLAMA_HOST", ollama.Resource.PrimaryEndpoint)
    // Let workflow expressions / Code nodes read the env vars above via {{ $env.NAME }}.
    // Dev convenience — relaxes n8n's node sandbox; drop it for stricter setups.
    .WithEnvironmentVariable("N8N_BLOCK_ENV_ACCESS_IN_NODE", "false")
    // Seed a tiny smoke-test workflow that hits Ollama through the injected URL
    // (no credentials needed). Optional — delete the workflows folder to skip it.
    .WithWorkflowsFromDirectory(Path.Combine(builder.AppHostDirectory, "workflows"));

builder.Build().EnsureDockerRunningIfLocalDebug().Run();
