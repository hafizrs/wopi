# L3-NETCORE-GENERIC-WOPI

A .NET Core WOPI (Web Application Open Platform Interface) server implementation for Collabora Online integration.

## Overview

This project provides a complete WOPI server implementation that allows integration with Collabora Online for document editing. It follows Clean Architecture, CQRS, and DDD patterns.

## Architecture

The project follows the established patterns from the PraxisMonitor project:

- **Clean Architecture**: Separation of concerns with clear layer boundaries
- **CQRS**: Command Query Responsibility Segregation
- **DDD**: Domain-Driven Design with rich domain models
- **Repository Pattern**: Data access abstraction
- **Command/Query Handler Pattern**: Business logic organization
- **Validation Pattern**: FluentValidation for input validation
- **Event-Driven Architecture**: For extensibility

## Project Structure

```
src/
├── WebService/                 # API Layer
│   ├── WopiController.cs      # WOPI endpoints
│   ├── Program.cs             # Application startup
│   └── ServiceCollectionExtensions.cs
├── Domain/                    # Domain Layer
│   └── DomainServices/
│       └── WopiModule/
│           ├── WopiService.cs
│           └── WopiPermissionService.cs
├── Contracts/                 # Contracts Layer
│   ├── Commands/WopiModule/
│   ├── Queries/WopiModule/
│   ├── Models/WopiModule/
│   ├── DomainServices/WopiModule/
│   ├── EntityResponse/
│   └── Constants/
├── CommandHandlers/           # Command Handlers
│   └── WopiModule/
├── QueryHandlers/             # Query Handlers
│   └── WopiModule/
├── Validators/                # Validation
├── ValidationHandlers/        # Validation Handlers
└── Utils/                     # Utilities
```

## Features

### WOPI Protocol Support
- **CheckFileInfo**: File metadata endpoint
- **GetFile**: File content download
- **PutFile**: File content upload
- **Lock/Unlock**: File locking operations

### Session Management
- Create WOPI editing sessions
- Manage session lifecycle
- File download/upload handling
- Permission-based access control

### Security
- JWT Bearer authentication
- Role-based permissions
- Department-level access control
- Session validation

## API Endpoints

### Internal API (Authenticated)
- `POST /api/wopi/create-session` - Create WOPI session
- `POST /api/wopi/delete-session` - Delete WOPI session
- `POST /api/wopi/sessions` - Get sessions for department
- `POST /api/wopi/session` - Get specific session

### WOPI Protocol Endpoints (Anonymous)
- `GET /api/wopi/files/{sessionId}/contents` - Get file content
- `GET /api/wopi/files/{sessionId}` - Get file info
- `POST /api/wopi/files/{sessionId}/contents` - Update file content

## Configuration

The application uses the following configuration:

```json
{
  "CollaboraBaseUrl": "https://colabora.rashed.app",
  "ServiceName": "WopiMonitor",
  "BlocksAuditLogQueueName": "wopi-audit-log"
}
```

## Dependencies

- .NET 6.0
- ASP.NET Core
- SeliseBlocks.Genesis.Framework
- FluentValidation
- Newtonsoft.Json
- Microsoft.Extensions.Http

## Getting Started

1. Clone the repository
2. Restore NuGet packages
3. Configure the application settings
4. Run the application

```bash
dotnet restore
dotnet build
dotnet run --project src/WebService
```

## Development

The project follows the established patterns from the PraxisMonitor project:

- All new features should follow CQRS pattern
- Use FluentValidation for input validation
- Implement proper logging and error handling
- Follow the existing naming conventions
- Add unit tests for new functionality

## License

This project is part of the Selise ECAP platform. 