# Reading a payment from a phone photo with the local model

Research for [Research: reading a payment from a phone photo with the local model](https://github.com/DigiBanks99/menlo/issues/769), part of [Wayfinder map: Payments](https://github.com/DigiBanks99/menlo/issues/766). It feeds [Decide what a receipt is and how it's kept](https://github.com/DigiBanks99/menlo/issues/770). Researched November 2026. It records facts and trade-offs, not a decision.

The premise: a photo of a till slip or invoice is input only. The local model reads it, the payment form is filled in, and the photo is dropped. Nothing is stored.

## Method and confidence

- Claims link the primary source that owns them: llama.cpp's own docs and source, Microsoft Learn, the `dotnet/extensions` source, Cloudflare docs, the HTML Media Capture spec, WebKit's bug tracker and release posts, Apple Support, and each library's repository or licence file.
- Facts about Menlo come from this repo, as cited by path.
- llama.cpp moves fast. Its facts are from the `master` branch in November 2026. Development pins image `server-b12242`, and the production VM's version isn't recorded in the repo, so check behaviour against the running build.
- Browser support tables come from caniuse.com, which compiles vendor data. It's marked where used.
- **Inference** marks a conclusion drawn from sources rather than stated by one. **Unverified** marks something no primary source confirmed.
- No timings were measured for this research.

## Summary

| Question | Short answer |
| --- | --- |
| Can prod's `llama-server` take images? | Yes, already wired. `qwen3.5-4b` is a vision-language model, the preset turns its projector on (`mmproj-auto`), and `IVisionService` sends images today. Only the till-slip prompt and a JSON schema are missing. |
| Image format and size for the model | JPEG and PNG decode through `stb_image`. Current `master` also decodes WebP. HEIC isn't supported. For Qwen-VL models the server resizes each image to between 8 and 4096 tokens by default, at 32 x 32 pixels per token, so about 4.2 MP at most. Sending more pixels only costs upload time. |
| Structured output | `ChatResponseFormat.ForJsonSchema<T>()` or `AIAgent.RunAsync<T>()` becomes OpenAI `response_format: json_schema`. llama-server accepts that and enforces it with a grammar. |
| No storage | Possible: the bytes can stay in memory. But ASP.NET Core spills any form file over 64 KB to a temp file during the request unless the buffer threshold is raised or the body is read as a stream. |
| How long one photo takes | Not measured. Menlo's own settings say "minutes on CPU". Cloudflare drops a proxied request after 125 s without a response, so the model call can't safely run inside the upload request. |
| Upload limits | Cloudflare: 100 MB (Free and Pro plans). Kestrel: 30,000,000 bytes (about 28.6 MB). Multipart form: 128 MB. A phone photo is a few MB, so none of these bind. |
| htmx | `hx-encoding="multipart/form-data"` on the element, or a `<form enctype="multipart/form-data">`. htmx 2.0.4 checks both. |
| HEIC | Only Safari 17+ displays it; Chrome, Edge, Firefox and Samsung Internet don't. iOS Safari often turns HEIC into JPEG during upload, depending on `accept` and iOS version. The server has to handle both. |
| .NET conversion on linux-arm64 | Magick.NET (Apache-2.0) bundles libheif and libde265, so it decodes HEIC with no system packages. ImageSharp (Six Labors Split License) is fully managed and has no HEIC; its new HEIF codec is AVIF only. NetVips' prebuilt binaries list no HEIC. |
| Camera capture | `capture` is a hint. caniuse lists support in iOS Safari, Chrome for Android and Samsung Internet, and none on desktop. Without it, the input works as a normal file picker. |
| CSP and a preview | `img-src 'self' data:` is the policy. A preview from `URL.createObjectURL` (a `blob:` URL) is blocked unless `blob:` is added. A `data:` URL from `FileReader`, or a preview image the server returns, is allowed. |

## 1. The local model

### What prod runs today

- Production calls the VM's shared `llama-server` at `http://127.0.0.1:9931/v1`. Chat and vision both use `qwen3.5-4b` (`.github/workflows/cd.yml`, `MENLO_AI_VISION_MODEL`). [ADR-006](../decisions/adr-006-llamacpp-and-agent-framework.md) records "one model for chat and vision": `unsloth/Qwen3.5-4B-MTP-GGUF`, `UD-Q4_K_XL`, with vision through the image projector.
- The dev preset `src/api/Menlo.AppHost/llama/models.ini` mirrors the VM's preset. It sets `mmproj-auto = true`, `ctx-size = 16384`, `parallel = 1`, and it runs on CPU only. `deployment/README.md` tells the operator to turn `mmproj-auto` on for the VM preset.
- `parallel = 1` means one request at a time. The VM's server is shared with the knowledge base, so a photo can queue behind other work (**inference**).

### Multimodal in llama-server

- llama.cpp supports image input through `libmtmd`, and llama-server exposes it on the OpenAI-compatible `/chat/completions` endpoint. Pass `--mmproj file.gguf`, or with `-hf` the projector downloads automatically unless `--no-mmproj` is given ([docs/multimodal.md](https://github.com/ggml-org/llama.cpp/blob/master/docs/multimodal.md)). The server README calls multimodal support "an experimental feature" ([server README, Multimodal support](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md#multimodal-support)).
- Content format: a `messages[i].content[j]` part with `type == "image_url"`, where `image_url.url` "can be a remote URL, base64 (raw or URI-encoded via `data:image/...;base64`) or path to local file". It "Accepts formats supported by `stb_image` (jpeg, png, tga, bmp, gif, ...)" ([server README, chat completions](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)). Remote URLs pointing at private addresses are refused unless `--media-allow-private-urls` is set. Base64 inline is the path that fits.
- WebP: `stb_image` doesn't read it. Current `master` sniffs the RIFF/WEBP header and decodes it "with the video decoder as a single frame" ([tools/mtmd/mtmd-helper.cpp](https://github.com/ggml-org/llama.cpp/blob/master/tools/mtmd/mtmd-helper.cpp)). That depends on the build having a video decoder, and on the pinned or VM version including the change (**unverified** for both builds). `VisionService` accepts `image/webp` today.
- HEIC isn't decoded by `stb_image` or the fallback paths, so HEIC has to become JPEG or PNG before it reaches the model (**inference** from the format list).
- EXIF orientation: `stb_image` documents no EXIF handling ([vendor/stb/stb_image.h](https://github.com/ggml-org/llama.cpp/blob/master/vendor/stb/stb_image.h)). A phone JPEG that relies on an orientation tag may reach the model sideways (**inference**). Applying the orientation before sending avoids that.
- Base64 makes the JSON body about a third larger than the image. It travels over loopback.

### Image resolution

- Qwen3.5-4B's preprocessor uses `patch_size` 16 and `merge_size` 2 ([preprocessor_config.json](https://huggingface.co/Qwen/Qwen3.5-4B/blob/main/preprocessor_config.json)), so each image token covers 32 x 32 pixels.
- llama.cpp converts `Qwen3_5ForConditionalGeneration` with the `QWEN3VL` projector ([conversion/qwen3vl.py](https://github.com/ggml-org/llama.cpp/blob/master/conversion/qwen3vl.py)). For that projector it resizes the image to fit 8 to 4096 tokens by default, unless the GGUF carries the model's own pixel limits ([tools/mtmd/mtmd-model.cpp](https://github.com/ggml-org/llama.cpp/blob/master/tools/mtmd/mtmd-model.cpp)). At 4096 tokens that's about 4.2 MP, for example 2048 x 2048 pixels.
- `--image-min-tokens` and `--image-max-tokens` override the range for dynamic-resolution models ([server README](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)). The server logs a warning that Qwen-VL models "require at minimum 1024 image tokens to function correctly on grounding tasks" (`mtmd-model.cpp`). Reading text isn't grounding, but it shows the low end hurts.
- Context: a 4096-token image plus the prompt and the JSON answer fits in the preset's 16384-token context.
- So shrinking a photo in .NET before sending it saves loopback bytes and base64 work, but the model doesn't need it: the server resizes anyway (**inference**). Shrinking below about 1 to 2 MP would cut the tokens the model sees, and so its reading accuracy and its time.

### Which open models read slips

- Qwen3.5-4B is "a Causal Language Model with Vision Encoder", Apache-2.0. Its model card reports OCRBench 85.0, CC-OCR 76.7 and OmniDocBench1.5 86.2 ([Qwen/Qwen3.5-4B](https://huggingface.co/Qwen/Qwen3.5-4B)). These are vendor-reported general OCR and document benchmarks, not till-slip results.
- llama.cpp lists ready vision GGUFs, among them Gemma 3 (4B, 12B), Qwen2.5-VL (3B, 7B), InternVL3 (1B to 14B), SmolVLM and Gemma 4 (E2B, E4B and larger) ([docs/multimodal.md](https://github.com/ggml-org/llama.cpp/blob/master/docs/multimodal.md)).
- It also supports dedicated OCR models (PaddleOCR-VL, GLM-OCR, Deepseek-OCR, Dots.OCR, HunyuanOCR), with the warning that they're "trained with specific prompt and input structure" (same page). They return text or layout, so a second step would map that text to amount, date and merchant (**inference**).
- No primary source benchmarks small models on till slips specifically. How well `qwen3.5-4b` reads amount, date, merchant and line items on real slips is **unverified**. The repo has an opt-in live test, `VisionLiveTests` in `Menlo.AI.Tests`, that a slip sample could extend.

### How long one photo takes

- Not measured here. `AiSettings.VisionTimeout` defaults to 10 minutes, documented as "A photo can take minutes on CPU", and matching the VM `llama-server`'s 600 s `--timeout` (`src/lib/Menlo.AI/Configuration/AiSettings.cs`, [AI Services](../reference/ai-services.md)).
- What drives it (**inference**): the vision encoder pass plus prefill of up to about 4096 image tokens, then a short JSON answer. Prefill speed on the VM's CPU sets the time. Lowering `--image-max-tokens` cuts it roughly in proportion. Speculative MTP decoding speeds up generation, not prefill. A small GPU would offload the projector by default (`--mmproj-offload`).
- To measure: run the live test against the VM with a slip photo and read llama-server's timings, at a couple of `--image-max-tokens` values.

## 2. Menlo.AI: sending the image and getting JSON back

### Sending the image (already built)

- `VisionService` builds one user `ChatMessage` with a `TextContent` prompt and a `DataContent(imageData, mediaType)`, then calls `AIAgent.RunAsync` on a keyed vision agent (`src/lib/Menlo.AI/Services/VisionService.cs`). It accepts `image/jpeg`, `image/png` and `image/webp`.
- The OpenAI adapter in Microsoft.Extensions.AI turns `DataContent` into `ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(...), mediaType, detail)`, an `image_url` part ([OpenAIChatClient.cs](https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIChatClient.cs)).
- The vision agent has its own OpenAI client: no retries, and a network timeout of `VisionTimeout` plus one minute, so a long call isn't cut off at the SDK's 100 s or retried (`src/lib/Menlo.AI/Extensions/ServiceCollectionExtensions.cs`).
- Each request sends `chat_template_kwargs.enable_thinking = false` through `RawRepresentationFactory` unless `EnableThinking` is on (`LlamaChatOptions.cs`). llama-server documents `chat_template_kwargs` ([server README](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)).

### Structured output

- Agent Framework: `AIAgent.RunAsync<T>()` returns `AgentResponse<T>` with a typed `Result`. Alternatively, set `AgentRunOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema<T>()`, or a raw schema with `ForJsonSchema(JsonElement, name, description)`, and deserialize `response.Text` ([Producing Structured Outputs with agents](https://learn.microsoft.com/en-us/agent-framework/agents/structured-outputs)). The page says `ChatClientAgent` "supports structured outputs when used with compatible chat clients", and that the `ResponseFormat` route doesn't take primitives or arrays at the top level, so wrap a line-item list in an object.
- The OpenAI adapter maps `ChatResponseFormatJson` with a schema to `ChatResponseFormat.CreateJsonSchemaFormat(...)`, after a strict-schema transform, and without a schema to `json_object` (`OpenAIChatClient.cs`, `ToOpenAIChatResponseFormat`). It fills `ResponseFormat` only when the raw options left it unset (`result.ResponseFormat ??= ...`), so Menlo's thinking patch and a schema can coexist (**inference** from the code).
- llama-server accepts `{"type": "json_object"}`, `{"type": "json_schema", "schema": ...}` and "the OpenAI wrapper form `{"type": "json_schema", "json_schema": {"name": ..., "strict": ..., "schema": {...}}}`" ([server README](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)). It enforces the schema with grammar-based sampling, which covers a subset of JSON Schema (see its `json_schema` notes and tests).
- A grammar guarantees the JSON's shape, not that the values are right. A misread amount still parses. Nullable fields let the model say "not found" rather than guess (**inference**).

## 3. Keeping the photo in memory only

- ASP.NET Core: "Any single buffered file exceeding 64 KB is moved from memory to a temp file on disk", written to `ASPNETCORE_TEMP` or the user's temp folder. `MemoryBufferThreshold` sets the boundary ([Upload files in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0)). A phone photo will hit disk briefly unless the threshold is raised for that endpoint or the multipart body is read as a stream (same page, "Upload large files with streaming"). Inside the container that temp folder is in the container's writable layer, not a volume (**inference** from `Dockerfile` and `cd.yml`).
- After reading, the image is a `byte[]` that `IVisionService` takes as is. Nothing in `Menlo.AI` logs prompts, images or answers ([AI Services](../reference/ai-services.md)).
- llama-server caches media by a SHA-256 of the bytes (`mtmd-helper.cpp`), and has a prompt cache in RAM (`--cache-ram`, default 8192 MiB) ([server README](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)). That's process memory on the shared server, not a file (**inference**).
- Request versus background:
  - Cloudflare's Proxy Read Timeout is 125 seconds, then HTTP 524. It's configurable only on Enterprise. For Tunnel, the CDN and tunnel timeouts both apply and the shorter wins ([Cloudflare connection limits](https://developers.cloudflare.com/fundamentals/reference/connection-limits/)).
  - A vision call that takes minutes can't finish inside the upload request. Menlo's AI docs already say to "run vision calls as background work, not inside a request" ([AI Services](../reference/ai-services.md)).
  - Holding the bytes in memory for a background job keeps the "no storage" premise. If the API restarts mid-job, the photo is lost and must be retaken (**inference**).
  - The page can follow progress with htmx polling or the vendored SSE extension (`htmx-ext-sse` 2.2.2, `src/ui/Menlo.Ui/wwwroot/vendor/README.md`). Cloudflare Tunnel buffers responses unless the origin sends `Content-Type: text/event-stream` ([Tunnel common errors](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/troubleshoot-tunnels/common-errors/)).

## 4. The upload path

| Hop | Limit | Source |
| --- | --- | --- |
| Cloudflare proxy | Max upload 100 MB on Free and Pro, 200 MB on Business, up to 5 GB on Enterprise | [Cloudflare default cache behavior, upload limits](https://developers.cloudflare.com/cache/concepts/default-cache-behavior/) |
| Cloudflare timeouts | Proxy Write Timeout 30 s, Read Timeout 125 s, Idle 900 s. "Requests that exceed the connection or read timeout can fail mid-upload before reaching the size limit" | [Connection limits](https://developers.cloudflare.com/fundamentals/reference/connection-limits/), [upload limits](https://developers.cloudflare.com/cache/concepts/default-cache-behavior/) |
| Kestrel body size | `MaxRequestBodySize` 30,000,000 bytes (about 28.6 MB). Change per endpoint with `[RequestSizeLimit]` or `IHttpMaxRequestBodySizeFeature` before the body is read | [Kestrel options](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options) |
| Kestrel data rate | `MinRequestBodyDataRate` 240 bytes/s after a 5 s grace period | [Kestrel options](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options) |
| Form parsing | `FormOptions.MultipartBodyLengthLimit` 128 MB. `[RequestFormLimits]` per endpoint | [Upload files](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0) |
| Antiforgery | Minimal API `[FromForm]` and `IFormFile` binding validates antiforgery tokens. Menlo validates its own `X-XSRF-TOKEN` header, which `menlo.js` adds to every htmx request in `htmx:configRequest` | [Parameter binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0), `src/api/Menlo.Api/Antiforgery/MenloAntiforgeryExtensions.cs`, `src/ui/Menlo.Ui/wwwroot/js/menlo.js` |

- A phone photo is a few MB (see section 5), so no size limit binds. The 30 s write timeout and slow mobile uplinks are more likely to matter (**inference**).
- Microsoft's upload guidance applies here too: don't trust the client file name, check the size, and check the file's signature rather than its extension or `Content-Type` ([Upload files, security considerations](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0)).

### htmx

- `hx-encoding="multipart/form-data"` makes htmx submit a `FormData` body, "usually to support file uploads in an ajax request" ([hx-encoding](https://htmx.org/attributes/hx-encoding/)). htmx fires `htmx:xhr:progress` during the upload ([htmx docs](https://htmx.org/docs/)).
- htmx 2.0.4 (the vendored version) uses `FormData` when `hx-encoding` is `multipart/form-data` (looked up through `getClosestAttributeValue`) or when the element is a `<form>` whose own `enctype` is `multipart/form-data` ([htmx 2.0.4 source, `usesFormData`](https://github.com/bigskysoftware/htmx/blob/v2.0.4/src/htmx.js)). With inheritance off in Menlo, put either attribute on the form itself (AGENTS.md).
- htmx's [file upload example](https://htmx.org/examples/file-upload/) draws its progress bar with an inline `<script>` or hyperscript. Menlo's CSP blocks both, so progress handling would live in `menlo.js` (AGENTS.md).

## 5. What phones send

### iPhone

- iPhones capture HEIF/HEVC by default from iPhone 7 and iOS 11. Settings > Camera > Formats > Most Compatible switches new photos to JPEG. Sharing "might automatically" convert to JPEG when the receiver doesn't support HEIF ([Apple: Using HEIF or HEVC media](https://support.apple.com/en-us/116944)).
- Standard HEIF goes up to 24 MP. ProRAW is about 25 MB at 12 MP and about 75 MB at 48 MP, and is "10 to 12 times larger than HEIF or JPEG files" ([Apple: About Apple ProRAW](https://support.apple.com/119916)). That puts a 24 MP to 48 MP HEIF or JPEG at very roughly 2 to 8 MB (**inference** from Apple's ratio).
- What reaches the server from Safari's file input depends on `accept` and the iOS version:
  - WebKit [bug 267277](https://bugs.webkit.org/show_bug.cgi?id=267277) (fixed March 2024): iOS "unconditionally transcoded" HEIC to JPEG. The fix aims to match macOS, which converts only when `accept` restricts types and excludes HEIC.
  - WebKit [bug 292350](https://bugs.webkit.org/show_bug.cgi?id=292350) (fixed February 2026): with `accept="image/jpeg, image/png, image/gif"`, Safari transcodes HEIC to the first listed type.
  - WebKit [bug 310605](https://bugs.webkit.org/show_bug.cgi?id=310605) (fixed March 2026): a change that forced HEIC to JPEG was put "behind a quirk".
  - Developer reports say that `accept="image/*,image/heic"` makes Safari 17+ convert JPEGs to HEIC ([Apple Developer Forums thread 743049](https://developer.apple.com/forums/thread/743049), **unverified**: a forum report).
  - So the server can get HEIC or JPEG from an iPhone, and the behaviour has changed between iOS releases. Sniffing magic bytes is more reliable than trusting the name or `Content-Type` (**inference**).
- What iOS produces for a photo taken through `capture` (HEIC or JPEG, and at what resolution) is **unverified**.

### Android

- Typical Android output (JPEG, Ultra HDR JPEG, optional HEIF on some makers) and sizes weren't confirmed from a primary source (**unverified**).

### Browser display of HEIC

- caniuse lists HEIF/HEIC display in Safari 17+ (macOS and iOS) only. Chrome, Edge, Firefox, Chrome for Android and Samsung Internet don't support it ([caniuse: HEIF](https://caniuse.com/heif)). WebKit: "Safari 17.0 also adds support for HEIC images" ([WebKit Features in Safari 17.0](https://webkit.org/blog/14445/webkit-features-in-safari-17-0/)). This only matters for a preview, since nothing is shown later.

### Converting or shrinking in .NET on linux-arm64

Whatever library is used, the model needs JPEG or PNG (see section 1).

| Library | Licence | linux-arm64 | HEIC decode | Notes |
| --- | --- | --- | --- | --- |
| [Magick.NET](https://github.com/dlemstra/Magick.NET) 15.1.1 | Apache-2.0 | `Magick.NET-Q8-arm64` (linux and linux-musl), or AnyCPU | Yes: Magick.Native links `libheif` with `libde265` and `libaom` statically ([CMakeLists.txt](https://github.com/dlemstra/Magick.Native/blob/main/src/Magick.Native/CMakeLists.txt)) | Native code ships in the package, with libstdc++ and libgcc linked statically. Only the OpenMP variants need `libgomp1` ([README](https://github.com/dlemstra/Magick.NET)). ImageMagick's licence and the HEVC patent position of `libde265` are the owner's to weigh |
| [ImageSharp](https://github.com/SixLabors/ImageSharp) 4.1.4 | Six Labors Split License: Apache-2.0 for open-source projects, transitive dependencies, and for-profits under USD 1M revenue; commercial otherwise ([LICENSE](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE)) | Fully managed, no native dependency | No. The HEIF codec merged in October 2026 is AVIF only: "HEVC (HEIC) is not included, because HEVC is not royalty-free", and HEVC files throw `ImageFormatException` ([PR 2633](https://github.com/SixLabors/ImageSharp/pull/2633)) | Handles JPEG, PNG and WebP resizing and EXIF auto-orient. Menlo is open source, so the Apache-2.0 terms apply |
| [NetVips](https://github.com/kleisauke/net-vips) 3.2.0 | MIT, over libvips | `NetVips.Native.linux-arm64` | Not listed: the bundled binaries support "JPEG, PNG, Ultra HDR, WebP, AVIF, TIFF, GIF and SVG (input)" | libvips' own licence wasn't checked here |
| [SkiaSharp](https://github.com/mono/SkiaSharp) 4.156.2 | MIT | `SkiaSharp.NativeAssets.Linux` | **Unverified**, likely no: the Linux native build sets no HEIF option (`native/linux/build.cake`) | |

- If iOS already sends JPEG, no conversion is needed. Neither shrinking nor re-encoding is required by the model (see "Image resolution"). Applying the EXIF orientation is the one transform that may change what the model reads (**inference**).

## 6. Camera capture and the preview

### `capture`

- The spec: when `capture` is set, the user agent "SHOULD invoke a file picker of the specific capture control type". If `accept` has no matching capture type it "MUST act as if there was no capture attribute", and it falls back to the default camera when the requested facing mode isn't available ([HTML Media Capture, W3C Recommendation 2018-02-01](https://www.w3.org/TR/html-media-capture/)). Values are `user` and `environment`. A back camera for slips is `capture="environment"` ([MDN: capture](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Attributes/capture)).
- MDN marks it "Limited availability", not Baseline. On desktop "you'll likely get a typical file picker" (same page).
- caniuse lists support in iOS Safari 6+, Chrome for Android, Firefox for Android and Samsung Internet, and none in any desktop browser ([caniuse: HTML Media Capture](https://caniuse.com/html-media-capture)).
- Whether the capture UI still lets the user choose an existing photo (for example a screenshot of an emailed invoice) isn't stated by the spec (**unverified** per browser). Without `capture`, `accept="image/*"` gives the normal picker, which on phones usually offers the camera too (**unverified**).

### Content Security Policy

- Menlo's policy is `default-src 'none'`, `img-src 'self' data:`, `script-src 'self'`, `style-src 'self'`, `connect-src 'self'` (`src/api/Menlo.Api/Auth/SecurityHeadersExtensions.cs`, tested in `SecurityHeadersTests`).
- `img-src` "specifies valid sources of images and favicons" and falls back to `default-src` ([MDN: img-src](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/img-src)).
- A client-side preview:
  - `URL.createObjectURL(file)` makes a `blob:` URL, which `'self'` doesn't cover, so it's blocked unless `img-src` gains `blob:` (**inference** from CSP source matching).
  - `FileReader.readAsDataURL` makes a `data:` URL, which is already allowed.
  - Either way the script must live in `menlo.js`, because inline script and `hx-on` are blocked (AGENTS.md).
- A server-rendered preview (the server returns a small image from memory under a same-origin URL, or inline as `data:`) needs no CSP change. It also makes the server read the HEIC, which most browsers can't show (**inference**).
- A HEIC preview fails in every browser but Safari, whichever route is used (see "Browser display of HEIC").
