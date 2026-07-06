# AI Stack AppHost — LocalAI + Ollama + n8n

A demo/reference Aspire AppHost that starts three services and wires them together:

| Resource  | What it is                                             | Used from n8n as |
|-----------|--------------------------------------------------------|------------------|
| `ollama`  | Native Ollama LLM/embedding server                     | Ollama node      |
| `localai` | Self-hosted **OpenAI-compatible** multimodal API       | OpenAI node      |
| `n8n`     | Low-code automation (auto-provisions its own Postgres) | —                |

```bash
dotnet run   # Docker required
```

## How the wiring works

The AppHost (`Program.cs`) does two things so the AI backends are usable inside n8n:

1. **Ordering** — `n8n.WaitFor(ollama).WaitFor(localai)` so n8n starts only once both are up.
2. **Discovery** — the backend URLs are injected into the n8n container as environment
   variables (resolved by Aspire to the in-network addresses):

   | Env var                              | Value (inside the n8n container)     |
   |--------------------------------------|--------------------------------------|
   | `OPENAI_API_BASE_URL` / `OPENAI_BASE_URL` / `LOCALAI_BASE_URL` | `http://localai:8080/v1` |
   | `OPENAI_API_KEY`                     | `sk-local`                           |
   | `OLLAMA_BASE_URL` / `OLLAMA_HOST`    | `http://ollama:11434`                |

   `N8N_BLOCK_ENV_ACCESS_IN_NODE=false` is set so workflow expressions and Code nodes
   can read those vars, e.g. `{{ $env.OLLAMA_BASE_URL }}`.

## What you still do once, in the n8n UI

n8n stores node **credentials** in its own database — there is no env var that
auto-creates them. So the one-time step is to add two credentials pointing at the
injected URLs above:

- **OpenAI** credential → Base URL `http://localai:8080/v1`, API key `sk-local`.
  Now n8n's *OpenAI* / *AI Agent* / *Embeddings OpenAI* nodes talk to LocalAI.
- **Ollama** credential → Base URL `http://ollama:11434`.
  Now n8n's *Ollama* nodes talk to Ollama.

(If a URL ever differs, read the exact value from the n8n container's environment —
that's what the injected vars above are for.)

## Seeded smoke test

`workflows/ai-stack-smoke-test.json` is imported on first start. It's a Manual
Trigger → HTTP Request to `{{ $env.OLLAMA_BASE_URL }}/api/tags` — no credentials
needed — so you can immediately execute it and confirm n8n can reach Ollama.
Delete the `workflows/` folder (and the `.WithWorkflowsFromDirectory(...)` call)
to skip seeding.

## Notes

- **GPU** is enabled by default (NVIDIA). For CPU-only hosts: remove
  `.WithGPUSupport(OllamaGpuVendor.Nvidia)` on Ollama and set `o.Gpu = LocalAiGpu.None`
  on LocalAI.
- **First start** downloads several GB of models per backend; `WithDataVolume()`
  keeps them across restarts.
