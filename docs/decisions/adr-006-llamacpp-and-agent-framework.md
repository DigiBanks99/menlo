# ADR-006: llama.cpp and Microsoft Agent Framework for Menlo.AI

## Status

**Accepted** - 2026-11-11

Supersedes the Ollama part of [ADR-001](adr-001-hosting-strategy.md) and [ADR-004](adr-004-linux-deployment-host.md) (local AI served by Ollama with Phi models). The rest of both ADRs stays.

## Context

Menlo.AI ran on two AI stacks:

- **Production** already served a local model through llama.cpp's `llama-server` on the VM, shared with the knowledge base.
- **Menlo.AI** itself used Microsoft Semantic Kernel with an Ollama container, in development and in the deploy pipeline.

That meant two runtimes, two sets of models and configuration, and a deploy that started an Ollama container next to a llama.cpp server that was already running. Semantic Kernel's successor, Microsoft Agent Framework, is the framework Microsoft is building forward, and Proactive Budget Adjustment is planned on it. The AI surface was small enough (chat, vision, and an embeddings service nothing called) to migrate cheaply.

The decision came from [#500](https://github.com/DigiBanks99/menlo/issues/500), was worked through on wayfinder map [#563](https://github.com/DigiBanks99/menlo/issues/563), and is delivered by Requirement [#568](https://github.com/DigiBanks99/menlo/issues/568) and its Specs [#575](https://github.com/DigiBanks99/menlo/issues/575), [#576](https://github.com/DigiBanks99/menlo/issues/576), [#577](https://github.com/DigiBanks99/menlo/issues/577) and [#578](https://github.com/DigiBanks99/menlo/issues/578).

## Decision

- **Agent Framework replaces Semantic Kernel.** `Menlo.AI` uses `Microsoft.Agents.AI` and `Microsoft.Extensions.AI` against an OpenAI-compatible endpoint. Semantic Kernel is no longer a dependency.
- **llama.cpp is the only local model runtime.** Ollama is removed from development, tests and the deploy pipeline. No second runtime is kept alongside it.
- **One model for chat and vision.** Both use `qwen3.5-4b` (`unsloth/Qwen3.5-4B-MTP-GGUF`, `UD-Q4_K_XL`), vision through the model's image projector. The chat and vision model names are separate settings (`Menlo:AI:ChatModel`, `Menlo:AI:VisionModel`) so vision can move to another model later.
- **Models are served by a shared router-mode `llama-server`.** It serves the models named in its preset file and loads one on first request. Development runs the pinned `ggml-org/llama.cpp` `server-b12242` image from the Aspire AppHost with `src/api/Menlo.AppHost/llama/models.ini`, mirroring the VM's preset. Production uses the VM's existing server, and the deploy starts no second instance.
- **Production reaches it over host networking.** The API container uses host networking, listens on `127.0.0.1:8080`, and calls `llama-server` on the VM loopback at `http://127.0.0.1:9931/v1` with an API key (`MENLO_AI_API_KEY`). Forwarded headers are trusted from `127.0.0.0/8`.
- **Embeddings are dropped** until a feature needs them. Nothing called the old embeddings service.
- **Versions are pinned.** The llama.cpp image tag is pinned in the AppHost, and the model is named in the preset. Upgrades are deliberate and recorded.

## Options Considered

### 1. Keep Semantic Kernel and Ollama

- **Pros**: No work
- **Cons**: Two runtimes and two model sets; an Ollama container in the deploy; Proactive Budget Adjustment built on a framework being replaced
- **Assessment**: The cost of migrating only grows as the AI surface grows

### 2. Move to Agent Framework, keep Ollama

- **Pros**: Framework migration only
- **Cons**: Still two runtimes
- **Assessment**: Leaves the main duplication in place

### 3. Agent Framework on llama.cpp (Selected)

- **Pros**: One runtime and one model everywhere; no Ollama container to deploy; reuses the VM's server; the forward-looking framework
- **Cons**: The model download from Hugging Face is untested from CI and dev machines (accepted by the owner); dev inference runs on CPU only

## Consequences

- The deploy has one fewer service, and `--remove-orphans` clears the old `menlo-ollama` container.
- If the VM's `llama-server` is down, the assistants degrade as before and budgeting and planning keep working.
- Automated tests run without a model.
- Provenance (model, version, timestamp, user) is built with the first feature that produces AI suggestions.
- Ollama and Semantic Kernel mentions in the ADR-001 and ADR-004 text and in `docs/requirements/` are historical.
