# Getting started

[Back to the project](../README.md)

## 1. Frontend showcase: no backend required

Requirements: Node.js 22.12+ and npm. Node 22 is used for local verification and CI.

```bash
cd src/ArchiveSystem.Frontend
npm ci
npm run showcase
```

Open the URL printed by Vite. A synthetic administrator session is created automatically. The showcase's Axios adapter rejects API requests locally, allowing the existing mock/fallback handlers to supply demo data without contacting a backend. Select a classification in the right panel to list sample cases.

- This is the actual application UI, with an in-app demo label.
- No SQL Server, customer data, scanner, SMS account or OCR model is required.
- Some routes require a backend and do not have complete offline behavior.
- Demo edits live in memory. The synthetic session is stored locally in the browser.
- Normal builds do not automatically enter demo mode; avoid mixing showcase and real application sessions in the same browser origin.

For a normal frontend build:

```bash
npm run build
npm run preview
```

For a regular development frontend connected to an API, copy `.env.example` to `.env.local`, set the API proxy target, and run `npm run dev`. Leave `VITE_ENABLE_OFFLINE_FALLBACK=false` for real backend testing.

## 2. Build and test the .NET code

Requirements: .NET 8 SDK and internet access for the first NuGet restore. The solution includes .NET projects only; Vite handles the frontend separately.

```bash
dotnet restore src/ArchiveSystem.sln
dotnet build src/ArchiveSystem.sln -c Release --no-restore
dotnet test src/ArchiveSystem.sln -c Release --no-build
```

The portable solution replaces the original workstation's Visual Studio website and container-project entries. `NuGet.Config` uses the public feed and the normal per-user cache, without a Windows-specific cache path.

See [verification results](VALIDATION.md) for the outcomes on this source edition and any remaining warnings.

## 3. Development infrastructure with Docker Compose

The root Compose file is a **local development example**, with localhost-bound ports and unencrypted internal service traffic. It is not a hardened production deployment. Install Docker with Compose and enough memory for SQL Server and OpenSearch.

```bash
cp .env.example .env
# Edit .env: replace every CHANGE_ME value with a new local credential.
docker compose up --build -d
```

Use distinct, strong local values. SQL Server requires a password that meets its complexity rules. JWT signing material must be a new random value of at least 32 characters. `.env` is ignored by Git.

| Service | Local address |
| --- | --- |
| Web application | http://localhost:8080 |
| API | http://localhost:5194 |
| Swagger in Development | http://localhost:5194/swagger |
| API health | http://localhost:5194/health/live and `/health/ready` |
| MinIO API / console | http://localhost:9000 / http://localhost:9001 |
| RabbitMQ management | http://localhost:15672 |
| OpenSearch | http://localhost:9200 |

The local API example creates its database schema through configured migrations and provisions the administrator credentials supplied in `.env`. The source's shipped `appsettings.json` files contain empty credentials; environment variables supply actual local settings.

Optional preview/indexing workers:

```bash
docker compose --profile processing up --build -d
```

OCR and folder-ingest services are intentionally not enabled by this compact example. They need model/native-runtime provisioning or explicitly configured watched folders. The Compose stack must be integration-tested in your environment; a build alone does not establish runtime correctness.

## 4. OCR assets: provision separately

The OCR project expects model paths configured in [`RapidOcrEngineOptions`](../src/ArchiveSystem.Worker.Ocr/Options/RapidOcrEngineOptions.cs), including detection/classification models, a Persian recognition ONNX model and its dictionary. Model binaries and scanned identity-document fixtures are excluded from this repository.

Obtain compatible models from their original publishers, verify their licenses and checksums, and configure `RapidOcr` paths locally. Match managed/native PDF and OCR library versions for the target OS. Do not assume that an arbitrary ONNX recognizer and dictionary are interchangeable.

The worker validates initialization before processing. Without the assets, it leaves jobs pending rather than producing an accepted OCR result. Full OCR quality, PDF behavior and identity-field accuracy need representative evaluation data and explicit acceptance criteria.

## 5. Desktop scanning

The Scanner Bridge builds separately as a .NET project. Actual WIA/TWAIN scanning requires Windows, NAPS2 console tooling and a configured scanner. Configure allowed frontend origins and NAPS2's local executable path through local settings. The bridge listens on localhost and restricts allowed origins.

No hardware or production scanner validation is implied by the frontend screenshots or .NET build.
