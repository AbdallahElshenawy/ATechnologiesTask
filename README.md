# ATechnologiesTask API

## Overview

The **ATechnologiesTask API** is a .NET Core Web API for managing IP-based country blocking. It supports permanent and temporary country blocks, IP block checks, geolocation lookups, and logs blocked access attempts. The API uses in-memory storage, integrates with ipgeolocation.io, enforces rate limiting (5 requests/minute), and offers Swagger UI for easy testing.

## Features

- **Country Blocking:** Permanently or temporarily block countries using ISO 3166-1 alpha-2 codes.
- **IP Block Check:** Verify if an IP’s country is blocked.
- **Geolocation Lookup:** Retrieve country details for an IP via ipgeolocation.io.
- **Blocked Attempts Logging:** View paginated logs of blocked IP access attempts.
- **Pagination & Search:** List blocked countries with pagination and optional search.
- **Rate Limiting:** Limits to 5 requests per minute per client.
- **In-Memory Storage:** Stores data in memory (non-persistent).
- **Swagger UI:** Interactive API docs at `https://localhost:7014/swagger`.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Git
- ipgeolocation.io API key (free signup)
- IDE (Visual Studio 2022, VS Code, or Rider)
- Terminal / PowerShell

## Setup Instructions

1. Clone the repository:

   ```bash
   git clone https://github.com/your-repo/ATechnologiesTask.git
   cd ATechnologiesTask 
2. Restore dependencies and build:

   ```bash
   dotnet restore
   dotnet build

3.Run the API:

   ```bash
    cd ATechnologiesTask.API
    dotnet run
   ```
## Endpoints
   
```bash
| Method | Endpoint                        | Description                                  |
| ------ | ------------------------------- | -------------------------------------------- |
| POST   | `/api/countries/block`          | Permanently block a country                  |
| DELETE | `/api/countries/block/{code}`   | Unblock a country by ISO code                |
| GET    | `/api/countries/blocked`        | List blocked countries (pagination + search) |
| POST   | `/api/countries/temporal-block` | Temporarily block a country (1–1440 minutes) |
| GET    | `/api/ip/check-block`           | Check if caller's IP country is blocked      |
| GET    | `/api/ip/lookup`                | Get geolocation for an IP (or caller IP)     |
| GET    | `/api/logs/blocked-attempts`    | List blocked IP access logs (paginated)      |
 ```
## Notes

- **In-Memory Storage:** Data is stored in memory and is **not persisted** across application restarts. For production use, consider integrating a database.

- **Temporary Blocks:** Temporary country blocks are automatically removed after their expiration time via a background service (`RemoveExpiredTemporalBlocksAsync`).

- **Extensibility:** The API is designed to be extensible. Future enhancements can include:
  - Persistent storage (e.g., SQL or NoSQL database)
  - Authentication and authorization
