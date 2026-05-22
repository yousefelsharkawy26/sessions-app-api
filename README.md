# Session-Style Messaging App API

A clean architecture, real-time messaging API built with **.NET 10**, **EF Core**, and **SignalR**, backed by **PostgreSQL**. The project is designed with robust privacy constraints inspired by secure decentralized messengers like the Session app.

---

## 🏗️ Architecture Overview

The project is structured according to **Clean Architecture** and **CQRS (Command Query Responsibility Segregation)** principles:

```
src/
├── SessionApp.Domain/         # Entities, Core Models, Exceptions
├── SessionApp.Application/    # Commands, Queries, DTOs, Handlers, Interfaces, Behaviors (MediatR)
├── SessionApp.Infrastructure/ # Database (EF Core), Auth (JWT), Storage, Notifications, SignalR
└── SessionApp.API/            # Controllers, Middleware, Startup Configurations
```

### Clean Architecture Layers:
- **Domain Layer**: Contains enterprise entities (`ApplicationUser`, `Message`) and business logic helpers (e.g., `MnemonicHelper` for generating BIP39 recovery words).
- **Application Layer**: Contains business use cases implemented as MediatR commands and queries. Validations and DTO mappings reside here.
- **Infrastructure Layer**: Implements database persistence using Entity Framework Core, JWT token generation, local image storage services, and the SignalR `ChatHub`.
- **API Layer**: Exposes secure REST endpoints and maps the SignalR hub routing.

---

## 🔒 Key Features

### 1. Username-Only Identity System (No Email/Phone)
- Register using only a `Username`, `Password`, and `DisplayName`.
- Registration generates a cryptographically secure **12-word recovery mnemonic phrase**.
- Users can recover accounts and reset passwords by inputting their username and mnemonic phrase.

### 2. Rich Profiles with Privacy Settings
- Profiles store `DisplayName`, `Bio`, `ProfilePictureUrl`, `IsPrivate` (boolean), and `Metadata` (JSON string).
- Avatars can be uploaded via a base64 encoded string.
- If a user sets `IsPrivate = true`:
  - They are excluded from the global user search results.
  - Direct profile queries by other users return limited information (`Bio = "[Private Profile]"` and `ProfilePictureUrl = null`).

### 3. Real-time Messaging with SignalR
- One-on-one instant messaging utilizing a SignalR hub mapped at `/hubs/chat`.
- Messages are persisted to PostgreSQL.
- **Privacy Rule**: A user cannot send a message to a private user unless the private user has initiated the conversation by messaging them first.

---

## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK**
- **PostgreSQL** database (e.g., Neon hosted PostgreSQL instance)

### Configuration
Update the database connection string in `src/SessionApp.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=[HOST]; Database=[DB]; Username=[USER]; Password=[PASSWORD]; SSL Mode=VerifyFull;"
  },
  "JwtSettings": {
    "Secret": "[ENCRYPTION_KEY]",
    "Issuer": "[ISSUER]",
    "Audience": "[AUDIENCE]",
    "ExpiryMinutes": "1440"
  }
}
```

### Running the Application

1. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

2. **Restore local tools and apply migrations**:
   ```bash
   dotnet tool restore
   dotnet ef database update --project src/SessionApp.Infrastructure --startup-project src/SessionApp.API
   ```

3. **Start the API server**:
   ```bash
   dotnet run --project src/SessionApp.API
   ```
   The API will listen at `http://localhost:5108` and the Swagger UI will be available at `http://localhost:5108/swagger`.

---

## 📡 API Endpoints

### Authentication (`/api/auth`)
- **POST `/api/auth/register`**: Registers a new user. Returns a JWT token and a 12-word mnemonic phrase.
- **POST `/api/auth/login`**: Authenticates a user. Returns a JWT token.
- **POST `/api/auth/recover-password`**: Resets a user's password using the 12-word mnemonic phrase.

### Profiles (`/api/profile`)
- **GET `/api/profile/me`**: Retrieves the authenticated user's profile.
- **GET `/api/profile/{username}`**: Retrieves a user's profile (privacy filters applied).
- **PUT `/api/profile/update`**: Updates the current user's profile details.
- **GET `/api/profile/search?searchTerm=<term>`**: Searches public profiles by username or display name.

### Messages (`/api/message`)
- **POST `/api/message/send`**: Sends a message to a user (privacy blocking rules applied).
- **GET `/api/message/chat/{username}`**: Retrieves chat history between the current user and target user.

### SignalR Chat Hub (`/hubs/chat`)
Clients can connect to `/hubs/chat` using WebSockets, sending their JWT token in the `access_token` query parameter. Once connected, clients can send and receive real-time message events.
