# DocuMind

Upload a PDF, ask questions about it, get answers that cite the page they came from.

DocuMind is a retrieval-augmented generation (RAG) application: a .NET 10 API, a Next.js 16 web app,
and PostgreSQL with pgvector doing the searching. It is built so that an answer can always be
checked — every claim carries a citation to a passage you can open and read, and a question the
documents cannot answer gets a refusal rather than an invention.

---

## What it does

- **Upload PDFs** — validated on their contents, not their file extension, and stored either on disk
  or sent straight from the browser to Cloudinary with a signed ticket.
- **Index them in the background** — text extracted page by page, cleaned, split into overlapping
  passages, and turned into vectors. The upload returns immediately; the UI polls for progress.
- **Search by meaning** — passages are found by vector similarity in Postgres, never by keyword.
- **Ask questions** — answers are written only from retrieved passages and cite them by number.
  Answers stream in as they are written, and Stop actually cancels the request to the AI provider.
- **Keep conversations** — threads, turns and the sources of each answer survive a reload, and
  follow-up questions understand what "it" refers to.
- **See what you have used** — token counts per account, recorded from what the provider reported.

## What it deliberately does not do

Listed up front, because a feature that half-exists is worse than one that is absent. See
[What is missing](#what-is-missing) for the full list — the headline is that **password reset does
not work**: the screens exist but there is no endpoint and no email provider behind them.

---

## How it works

```
  Browser ──upload──► API ──► storage (disk or Cloudinary)
                       │
                       └──► queue ──► background worker
                                          │
                     PdfPig: text, page by page
                                          │
                     clean, split into ~1000-character
                     passages that overlap by ~150
                                          │
                     embed each passage (1536 numbers)
                                          │
                                    PostgreSQL
                              chunks + vector(1536) + HNSW index

  Question ──► embed ──► nearest passages for THIS user ──► prompt ──► model ──► answer + citations
```

Two properties are worth knowing because everything else follows from them:

1. **The user filter lives inside the SQL.** Ownership is a predicate in the query that ranks
   passages, not a check performed afterwards. Another account's passage cannot reach the ranking
   even if its text is identical.
2. **Citations are built in code, never parsed out of the model's prose.** The model cites `[1]`;
   the file name and page number attached to that marker come from the passage the server sent. A
   marker for a passage that was not sent is dropped. The model cannot invent a page number because
   it is never asked for one.

---

## Tech stack

| Layer | Choice |
|---|---|
| API | .NET 10, ASP.NET Core, Clean Architecture (Domain / Application / Infrastructure / Api) |
| Database | PostgreSQL 17 with pgvector 0.8, EF Core 10 + Npgsql |
| Vector search | `vector(1536)` column, HNSW index, cosine distance in raw SQL |
| PDF | PdfPig |
| AI | Any OpenAI-protocol provider. Configured for Google Gemini's free tier |
| Web | Next.js 16, React 19, TanStack Query, Tailwind CSS v4 |
| API client | Generated from OpenAPI with orval — the frontend never hand-writes a request type |
| Auth | JWT access token in memory + rotating refresh token in an httpOnly cookie |
| Logging | Serilog, one line per request |
| Tests | xUnit — unit tests plus integration tests against a real Postgres |

---

## Running it locally

### 1. Prerequisites

| Need | Version | Notes |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Node.js | 20+ | 24 is what this was built on |
| PostgreSQL | 16 or 17 | must have the **pgvector** extension available |
| An AI key | — | free from [Google AI Studio](https://aistudio.google.com) |

Check pgvector is available on your server before going further:

```sql
SELECT * FROM pg_available_extensions WHERE name = 'vector';
```

If that returns nothing, install pgvector for your Postgres. The app creates the extension itself
during migration, but it can only do that if the server has it to offer.

Install the EF Core CLI once:

```bash
dotnet tool install --global dotnet-ef
```

### 2. Get an AI key (free, no card)

1. Go to [aistudio.google.com](https://aistudio.google.com) and sign in.
2. **Get API key** → **Create API key** → pick or create a project.
3. Copy it. It starts with `AIza`.

The free tier's limits are per model and modest (about 15 requests a minute for the configured chat
model). The app's own rate limiter sits just under that, so you meet a friendly message rather than
the provider's error.

### 3. Configure secrets

Nothing secret goes in `appsettings.json` — that file is committed. Secrets live in .NET user
secrets, outside the repository:

```bash
cd backend/DocuMind.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=documind;Username=postgres;Password=YOUR_PASSWORD"

dotnet user-secrets set "Jwt:Key" "any-string-of-at-least-32-characters-you-invent"

dotnet user-secrets set "AI:ApiKey" "AIza...your key..."
```

Optional — direct-to-cloud uploads. Without these, files are stored under
`backend/DocuMind.Api/uploads/` and everything still works:

```bash
dotnet user-secrets set "Storage:Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Storage:Cloudinary:ApiKey"   "123456789012345"
dotnet user-secrets set "Storage:Cloudinary:ApiSecret" "your-api-secret"
```

> `dotnet user-secrets list` prints the **values**, so don't paste its output anywhere.

### 4. Create the database

```bash
createdb documind                      # or CREATE DATABASE documind; in psql

cd backend
dotnet ef database update --project DocuMind.Infrastructure --startup-project DocuMind.Api
```

Migrations are **not** applied automatically at startup, so this step is deliberate rather than
something that happens behind your back on a production box.

### 5. Run it

Two terminals:

```bash
# terminal 1 — the API on http://localhost:5100
cd backend
dotnet run --project DocuMind.Api --launch-profile http

# terminal 2 — the web app on http://localhost:3000
cd frontend
npm install
npm run dev
```

Then open <http://localhost:3000>, register an account, and upload a PDF. Watch the status move from
**Uploaded** to **Processing** to **Completed**, then open the document and choose *Chat with
document*.

Swagger UI is at <http://localhost:5100/swagger> while the API runs in Development.

### If something does not work

| Symptom | Cause |
|---|---|
| `[AI] AI:ApiKey is not set` at startup | Step 3 was skipped or run from the wrong folder |
| Documents reach **Failed** with *"rejected this server's credentials"* | The AI key is wrong |
| **Failed** with *"busy or over quota"* | The free tier's per-minute limit; wait a moment |
| Answers say *"I could not find anything"* for questions the document answers | The document was indexed with a different embedding model — reprocess it |
| `CREATE EXTENSION vector` fails on migration | pgvector is not installed on the Postgres server |
| Uploads go to disk although Cloudinary is configured | All three Cloudinary values must be set |

---

## Configuration

Everything below lives in `backend/DocuMind.Api/appsettings.json` and can be overridden by
environment variables using `__` as the separator (`AI__ChatModel`).

| Key | Default | What it does |
|---|---|---|
| `AI:Provider` | `OpenAI` | The **wire protocol**, not the vendor. Gemini speaks it too |
| `AI:Endpoint` | Gemini's OpenAI-compatible URL | Empty means OpenAI itself. This is what picks the vendor |
| `AI:ChatModel` | `gemini-3.5-flash-lite` | Writes the answers |
| `AI:EmbeddingModel` | `gemini-embedding-001` | Turns passages into vectors |
| `AI:EmbeddingDimensions` | `1536` | Must match the database column; changing it needs a migration |
| `Rag:ChunkSize` / `ChunkOverlap` | `1000` / `150` | Characters per passage, and how much neighbours repeat |
| `Rag:TopK` | `5` | Passages a search returns |
| `Rag:SimilarityThreshold` | `0.5` | How close a passage must be to be used. **Measured against the embedding model** — change the model and this has to be measured again |
| `Rag:MaxContextChunks` | `5` | Passages allowed into a prompt |
| `Rag:MaxHistoryMessages` | `8` | Earlier turns replayed for a follow-up |
| `Storage:Provider` | `Cloudinary` | Falls back to local disk when Cloudinary is unconfigured |
| `Storage:MaxFileSizeBytes` | 20 MB | |
| `Jwt:AccessTokenMinutes` / `RefreshTokenDays` | `15` / `7` | |

Rate limits (in `HardeningExtensions.cs`): **20 uploads** and **12 AI calls** per user per minute.

---

## Project structure

```
backend/
  DocuMind.Domain/          entities and rules — references nothing
  DocuMind.Application/     use cases, DTOs, validators, and the interfaces below
  DocuMind.Infrastructure/  EF Core, pgvector SQL, PdfPig, storage, AI providers
  DocuMind.Api/             controllers, auth, error handling, rate limiting
  DocuMind.Tests/           unit and integration tests

frontend/
  src/app/                  routes: auth screens, dashboard, documents, chat, settings
  src/components/           UI built from design tokens — no hardcoded colours
  src/lib/api/              generated client + the hand-written streaming reader
  src/lib/auth/             session handling
```

The dependency rule holds: `Domain` knows nothing about the others, `Infrastructure` implements
interfaces declared in `Application`, and only `Api` knows about HTTP.

---

## API

All routes need `Authorization: Bearer <token>` except register, login and refresh.

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register`, `/login`, `/refresh`, `/logout` | Sessions |
| GET / PUT | `/api/auth/me` | Read or rename the signed-in user |
| POST | `/api/auth/change-password` | Changes it and revokes every session |
| POST | `/api/documents/upload` | Multipart upload |
| POST | `/api/documents/upload-ticket`, `/confirm` | Signed direct-to-Cloudinary upload |
| GET | `/api/documents`, `/{id}`, `/{id}/status`, `/{id}/text`, `/{id}/chunks`, `/{id}/download` | Read |
| POST | `/api/documents/{id}/reprocess` | Index it again |
| DELETE | `/api/documents/{id}` | Removes the file, its text and its passages. Conversations about it survive |
| POST | `/api/search/semantic` | Passages nearest a question |
| POST | `/api/documents/{id}/chat` | One grounded answer, no thread kept |
| GET/POST/DELETE | `/api/conversations{/id}` | Saved threads |
| GET/POST | `/api/conversations/{id}/messages` | Read a thread, or ask in it |
| POST | `/api/conversations/{id}/messages/stream` | The same, as Server-Sent Events |
| GET | `/api/usage` | This month's token counts |
| GET | `/health` | Liveness |

**Errors** are RFC-9457 ProblemDetails with two additions: `errorCode` (a stable string to branch on)
and `traceId` (which appears in the server log for the same request). Another user's resource is
always **404**, never 403 — a 403 would confirm that the id exists.

---

## Tests

```bash
cd backend
dotnet test
```

61 tests. They need the Postgres server from your connection string; they create their own databases
and drop them afterwards, so your development data is never touched. No AI credentials are needed —
the provider is replaced by a fake.

- **Unit tests** cover what belongs to this code rather than a provider: chunk sizes and page
  attribution, refusing without calling the model, citing only passages that were really sent, what
  a stopped answer leaves behind, and that the prompt carries the rules a grounded answer needs.
- **Integration tests** start the real application in-process and drive it over HTTP against a real
  database: ownership on every route, the answer contract, the error shape, rate limiting, and a
  real PDF's journey from upload to cited answer.

---

## What is missing

Known gaps, so nobody has to discover them:

**Password reset does not work.** The *forgot password* and *reset password* screens exist in the
repository but are not linked from anywhere, and there is no endpoint behind them. Implementing it
needs an email provider (Resend, SendGrid, SMTP) — a fourth external service — so it was left out
deliberately rather than half-built.

Also absent:

- **Deployment.** This runs locally only. There is no Dockerfile, no CI pipeline and no hosting
  configuration.
- **Profile extras.** Avatar upload, theme and language preferences, response-style settings, data
  export, and account deletion are not implemented. The controls for them were removed from the
  settings screen rather than left as buttons that do nothing.
- **Session management.** Refresh tokens are per session and revocable, but there is no screen
  listing active sessions or revoking one.
- **Frontend tests.** The web app is checked by TypeScript, ESLint and a production build — there
  are no component or end-to-end tests.
- **Collections.** A folders-for-documents screen was designed and dropped; it was never in the
  specification's API surface. The history is in git if it is wanted.

Provider limitations worth knowing:

- **Gemini does not report token usage for embeddings**, so the usage screen shows embedding calls
  with no token count. Chat usage is reported correctly. The app records what the provider says and
  never estimates.
- **Embeddings are capped at 30,000 tokens a minute** on the free tier. A very large PDF will lean
  on retries while indexing.
- **A stopped answer records no cost.** The provider does not report usage for a call that was
  abandoned, so nothing is claimed for it.

---

## Design decisions worth knowing

A few choices that are easy to misread as accidents:

- **`AI:Provider` says `OpenAI` while the app talks to Gemini.** It names the protocol, not the
  company. Switching providers is an endpoint and two model names.
- **A question nothing matches never reaches the model.** If no passage clears the similarity floor,
  the refusal is written locally. It cannot hallucinate what it was never asked.
- **A citation is a copy, not a reference.** File name, page range and passage text are snapshotted
  onto the answer, and message sources have no foreign key to the document. Delete a document and
  its conversations stay readable; new questions in them simply find nothing.
- **A stopped answer is stored as stopped.** The text really is incomplete, so it is shown that way
  and replayed to the model that way rather than passed off as finished.
- **The browser streams with `fetch`, not `EventSource`.** `EventSource` cannot send a bearer token
  or a request body.
- **Chunk page numbers come from a chunk's own words**, never from the overlap it borrowed from its
  neighbour — otherwise every citation after the first would point one page early.
