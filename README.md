# Attachment Management Service

A secure and configurable .NET 8 microservice using Clean Architecture for managing file attachments via a RESTful API with API Key authentication and temporary signed URL support.

<!-- Optional: Add Badges (replace placeholders) -->
<!-- [![Build Status](PLACEHOLDER_BUILD_BADGE_URL)](PLACEHOLDER_BUILD_URL) -->
<!-- [![Code Coverage](PLACEHOLDER_COVERAGE_BADGE_URL)](PLACEHOLDER_COVERAGE_URL) -->
<!-- [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) -->

## Key Features

*   **Upload Attachments:** Store files associated with authenticated applications.
*   **Download Attachments:** Retrieve files securely using API Key authentication.
*   **Generate Signed URLs:** Create time-limited, publicly accessible links for file downloads without needing API keys.
*   **Metadata Management:** Store and retrieve file metadata (name, type, size, upload time, associated user ID).
*   **Application Authentication:** Secure API access using unique, hashed API Keys per application.
*   **Application Configuration:** Optional limits per application (e.g., max file size).
*   **Clean Architecture:** Maintainable and testable codebase following Domain-Driven Design principles.
*   **Database Backend:** Uses PostgreSQL to store metadata.
*   **Flexible File Storage:** Abstracted file storage (default: local file system, easily extensible for cloud storage like Azure Blob or AWS S3).
*   **Docker Support:** Includes `Dockerfile` and `docker-compose.yml` for easy containerization and deployment.

## Technology Stack

*   **.NET 8**
*   **ASP.NET Core 8:** For the Web API framework.
*   **Entity Framework Core 8:** For data access with PostgreSQL.
*   **PostgreSQL:** Relational database for metadata storage.
*   **MediatR:** (Implied by CQRS command/query structure) For implementing CQRS pattern.
*   **BCrypt.Net:** For securely hashing API Keys.
*   **Docker & Docker Compose:** For containerization.

## Architecture Overview

This service implements the **Clean Architecture** pattern:

1.  **Domain:** Contains core business entities (`Attachment`, `AllowedApplication`) and interfaces for repositories (`IAttachmentRepository`, `IAllowedApplicationRepository`). Has no external dependencies.
2.  **Application:** Contains application logic (use cases) implemented as Commands and Queries (CQRS pattern via MediatR). Defines interfaces for infrastructure concerns (`IFileStorageService`, `ISignedUrlService`, `ICurrentRequestService`). Depends only on the Domain layer.
3.  **Infrastructure:** Provides implementations for interfaces defined in the Application layer. Handles external concerns like database access (EF Core, Repositories), file storage (Local File System, Signed URL generation), authentication details (`CurrentRequestService`), etc. Depends on the Application layer.
4.  **API (Presentation):** ASP.NET Core Web API project. Contains controllers, middleware (`ApiKeyAuthMiddleware`), request/response DTOs, and dependency injection setup (`Program.cs`). Depends on the Application and Infrastructure layers.

## Getting Started

### Prerequisites

*   **.NET 8 SDK**
*   **Docker & Docker Compose**
*   **PostgreSQL Client:** (Optional) For inspecting the database directly (e.g., `psql`, DBeaver, pgAdmin).
*   **Git**

### Configuration

The application uses `appsettings.json` for base configuration, which can be overridden by environment-specific files (`appsettings.Development.json`, `appsettings.Production.json`) and environment variables.

**Key Configuration Values (Override via Environment Variables in Docker):**

*   **`ConnectionStrings__DefaultConnection`**: The connection string for the PostgreSQL database.
    *   *Docker Default:* `Server=db;Port=5432;Database=ManagedFileService;User Id=digital;Password=digital;Trust Server Certificate=True;` (points to the `db` service in `docker-compose.yml`).
    *   *Local Default:* `Host=localhost;Port=5432;...` (configure for your local Postgres instance).
*   **`FileStorage__BasePath`**: The directory where uploaded files are stored.
    *   *Docker Default:* `/app/uploads` (maps to the `upload_storage` volume).
    *   *Local Default:* Configure a path accessible by the application user (e.g., `/Users/youruser/ManagedFileDir/` or `C:\\FileStorage`).
*   **`SignedUrlSettings__SecretKey`**: **CRITICAL SECRET!** A strong, unique key used for signing temporary download URLs.
    *   **DO NOT COMMIT REAL SECRETS TO `appsettings.json`!**
    *   *Local Development:* Use .NET User Secrets: `dotnet user-secrets set SignedUrlSettings:SecretKey "YOUR_STRONG_SECRET_KEY"`
    *   *Docker Compose:* Set via an environment variable (e.g., from a `.env` file loaded by Docker Compose): `SIGNED_URL_SECRET_KEY=YOUR_STRONG_SECRET_KEY`
*   **`SignedUrlSettings__MaxExpiryMinutes`**: Optional maximum validity duration (in minutes) for signed URLs.

### Bootstrapping the First Application

To use the attachment service, you first need at least one `AllowedApplication` registered in the database with its hashed API Key.

1.  **Run the Application** (locally or via Docker).
2.  **Use the `CreateAllowedApplication` Endpoint:** Send a `POST` request to `/api/allowedapplications`. This endpoint is marked `[AllowAnonymous]` specifically for this bootstrapping purpose **(Secure this endpoint before production!)**.

    **Example Request Body:**
    ```json
    {
      "name": "My First App",
      "apiKey": "my-super-secret-api-key-123", // Choose a strong key!
      "maxFileSizeMegaBytes": 100 // Optional limit
    }
    ```
3.  **Store the API Key:** Securely store the *original* plain-text API key (`my-super-secret-api-key-123` in this example). You will need it for the `X-Api-Key` header when calling protected endpoints. The service only stores the secure hash.

    *(Alternatively, implement database seeding for development/testing).*

### Running Locally (Without Docker)

1.  Clone the repository.
2.  Ensure you have a local PostgreSQL instance running.
3.  Configure `appsettings.Development.json` or User Secrets:
    *   Set `ConnectionStrings:DefaultConnection` to your local Postgres instance.
    *   Set `FileStorage:BasePath` to a valid local directory.
    *   Set `SignedUrlSettings:SecretKey` using User Secrets.
4.  Apply EF Core Migrations:
    ```bash
    cd path/to/ManagedFileService/API # Navigate to the API project directory
    dotnet ef database update
    ```
5.  Run the API project:
    ```bash
    dotnet run
    ```
6.  Bootstrap the first `AllowedApplication` as described above.

### Running with Docker Compose

1.  Clone the repository.
2.  **(Recommended)** Create a `.env` file in the project root directory (where `docker-compose.yml` is located) to store secrets:
    ```dotenv
    # .env file
    SIGNED_URL_SECRET_KEY=YourActualVeryStrongSecretKeyGeneratedSecurely
    ```
3.  Build and start the services:
    ```bash
    docker compose up --build -d
    ```
    This will:
    *   Build the `managedfileservice` Docker image using `ManagedFileService/Dockerfile`.
    *   Start the `managedfileservice` container.
    *   Start the `db` (PostgreSQL) container.
    *   Create named volumes (`postgres_data`, `upload_storage`) for persistent data.
    *   Apply configuration overrides from environment variables defined in `docker-compose.yml`.
    *   The application will apply EF Core migrations on startup.
4.  The service will be available on `http://localhost:7100` (or the host port you configured).
5.  Bootstrap the first `AllowedApplication` as described above by sending a POST to `http://localhost:7100/api/allowedapplications`.

## API Endpoints

**Authentication:** Protected endpoints require an `X-Api-Key` header containing the plain-text API key associated with a registered `AllowedApplication`.

*   **`POST /api/allowedapplications`** `[AllowAnonymous]`
    *   Creates a new application allowed to use the service.
    *   **Request Body:** `{ "name": "string", "apiKey": "string", "maxFileSizeMegaBytes": long? }`
    *   **Response:** `201 Created` with the new application `Guid`.
    *   **Note:** Secure this endpoint in production!

*   **`POST /api/attachments`** `[Requires X-Api-Key]`
    *   Uploads a new file attachment.
    *   **Request Body:** `multipart/form-data` containing the `file` and an optional `userId` (string) form field.
    *   **Response:** `201 Created` with the new attachment `Guid`.

*   **`GET /api/attachments/{id}/metadata`** `[Requires X-Api-Key]`
    *   Retrieves metadata for a specific attachment owned by the authenticated application.
    *   **Response:** `200 OK` with `{ "id": "guid", "originalFileName": "string", "contentType": "string", "sizeBytes": long, "uploadedAtUtc": "datetime", "userId": "string?" }` or `404 Not Found`.

*   **`GET /api/attachments/{id}`** `[Requires X-Api-Key]`
    *   Downloads the content of a specific attachment owned by the authenticated application.
    *   **Response:** `200 OK` with the file stream (`FileStreamResult`) or `404 Not Found`.

*   **`DELETE /api/attachments/{id}`** `[Requires X-Api-Key]`
    *   Deletes an attachment (both metadata and stored file) owned by the authenticated application.
    *   **Response:** `204 No Content` or `404 Not Found`.

*   **`POST /api/attachments/{id}/generatesignedurl`** `[Requires X-Api-Key]`
    *   Generates a temporary signed URL for downloading an attachment.
    *   **Request Body:** `{ "expiresInMinutes": int }` (e.g., `5`).
    *   **Response:** `200 OK` with `{ "signedUrl": "string", "expiresAtUtc": "datetime" }` or `404 Not Found` / `403 Forbidden`.

*   **`GET /api/publicdownloads/download?id=...&expires=...&sig=...`** `[AllowAnonymous]`
    *   Downloads an attachment using a previously generated signed URL. Parameters are provided in the query string.
    *   **Response:** `200 OK` with the file stream, `401 Unauthorized` (invalid/expired link), or `404 Not Found` (attachment deleted).

## Security Considerations

*   **API Keys:** Treat API keys like passwords. Store them securely outside the application code (use the application database hash mechanism). Implement rotation if needed.
*   **Signed URL Secret Key:** This key is critical. Store it securely using environment variables, secret managers (like Azure Key Vault, AWS Secrets Manager), or .NET User Secrets for local development. **Do not commit it to source control.**
*   **HTTPS:** Always use HTTPS in production environments to protect data in transit. Configure Kestrel or use a reverse proxy (like Nginx, YARP) for TLS termination.
*   **Input Validation:** Sanitize filenames (`ReplaceInvalidFileNameChars`) and enforce file size limits (based on application config). Consider content-type validation based on application needs.
*   **Authorization:** Ensure applications can only access/modify attachments they own (`ApplicationId` checks).
*   **Rate Limiting:** Implement rate limiting on API endpoints to prevent abuse.
*   **Endpoint Security:** The `POST /api/allowedapplications` endpoint MUST be secured in production (e.g., require admin authentication).
*   **Docker Permissions:** The provided Docker configuration attempts to handle permissions for the upload volume. Review and adjust if necessary for your environment.

## Future Enhancements / TODO

*   [ ] Implement user-level permissions/quotas within applications.
*   [ ] Add alternative `IFileStorageService` implementations (Azure Blob Storage, AWS S3).
*   [ ] Implement more granular `AllowedApplication` configuration (allowed content types, total storage quota).
*   [ ] Use Transactional Outbox pattern for reliability between file storage and DB commits.
*   [ ] Add comprehensive Audit Logging.
*   [ ] Implement more robust authentication mechanisms (e.g., JWT, OAuth scopes) if needed.
*   [ ] Implement background job for cleaning up potentially orphaned files.
*   [ ] Enhance validation (e.g., file magic number checking).
*   [ ] Secure the `POST /api/allowedapplications` endpoint.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request or open an Issue.

## License

This project is licensed under the [MIT License](LICENSE).
