# 🚀 Step-by-Step Configuration & Execution Guide

This guide provides the complete sequence of instructions to configure, run, and verify the Session-Style Messaging App API in your environment.

---

## 🛠️ Step 1: Configuration

The API reads configuration values from the [appsettings.json](file:///home/elsharkawy/Desktop/sessions-app-api/src/SessionApp.API/appsettings.json) file.

### 1. Database Connection (Neon Hosted PostgreSQL)
By default, the connection is configured to connect directly to your hosted Neon database:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=[HOST_NAME]; Database=[DB_NAME]; Username=[DB_USER]; Password=[DB_PASSWORD]; SSL Mode=VerifyFull; Channel Binding=Require;"
}
```
* **Auto-Migrations**: The application automatically executes any pending migrations on startup, meaning you do not need to run `dotnet ef database update` manually.

### 2. JWT Configuration Settings
The JWT configuration coordinates authorization and key lifetimes. You can change these inside `appsettings.json`:
```json
"JwtSettings": {
  "Secret": "[JWT_SECRET]",
  "Issuer": "[JWT_ISSUER]",
  "Audience": "[JWT_AUDIENCE]",
  "ExpiryMinutes": "1440"
}
```

---

## 💻 Step 2: Running the Application Locally

You can run the API directly using the .NET CLI or via the containerized Docker environment.

### Option A: Using the .NET CLI (Recommended)
1. Navigate to the root directory.
2. Run the restore command:
   ```bash
   dotnet restore
   ```
3. Compile the solution:
   ```bash
   dotnet build
   ```
4. Start the application:
   ```bash
   dotnet run --project src/SessionApp.API
   ```
   The API will listen at: `http://localhost:5108`

### Option B: Running with Docker Compose
1. Ensure the Docker daemon is running.
2. Start the containerized API:
   ```bash
   docker-compose up --build -d
   ```
   The container maps internal port `8080` out to: `http://localhost:8080`

---

## 🔍 Step 3: Interactive Swagger API Verification

To interact with and test the endpoints visually, the Swagger UI has been customized with a premium **dark-mode theme**.

1. Start the API locally.
2. Open your browser and navigate to:
   * **Swagger URL**: `http://localhost:5108/swagger`
3. **Register a User**:
   * Expand `/api/auth/register` (POST) -> Click "Try it out".
   * Request Body:
     ```json
     {
       "username": "user1",
       "password": "Password123!",
       "displayName": "User One"
     }
     ```
   * Click "Execute". Save the returned JWT token and the `recoveryMnemonic` phrase.
4. **Authorize your session**:
   * Copy the JWT token from the registration response.
   * Click the **"Authorize"** button at the top-right of the Swagger page.
   * Type: `Bearer <your_token>` and click "Authorize". All subsequent requests will now include your authentication context.

---

## 📡 Step 4: Real-time SignalR WebSocket Setup

To test real-time WebSocket messaging and read receipts directly from the browser's developer console, execute the following script.

1. Navigate to `http://localhost:5108/swagger`.
2. Open the browser's developer tools console (F12).
3. Paste the following JavaScript code to connect to the SignalR chat hub (make sure to replace `<JWT_TOKEN>` with a valid token):

```javascript
// Load the SignalR client library dynamically
const script = document.createElement("script");
script.src = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js";
script.onload = () => {
    console.log("SignalR Library Loaded. Connecting...");
    
    const jwtToken = "<YOUR_JWT_TOKEN>";
    
    // Connect to the Hub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5108/hubs/chat?access_token=" + jwtToken)
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Listen for incoming messages
    connection.on("ReceiveMessage", (message) => {
        console.log("📬 New E2EE Message Received:", message);
    });

    // Listen for read receipt updates
    connection.on("MessagesRead", (receipt) => {
        console.log("👁️ Reader:", receipt.readerUsername, "read messages:", receipt.messageIds);
    });

    // Start the connection
    connection.start()
        .then(() => console.log("🟢 Connected to SignalR Chat Hub!"))
        .catch(err => console.error("🔴 Connection failed: ", err));
};
document.head.appendChild(script);
```

---

## 📎 Step 5: Blind Storage Attachment Uploads

To upload binary media or attachments that the server cannot read (client encrypts beforehand using AES-GCM or another key that is only shared within the E2EE message envelope):

1. **Endpoint**: `POST /api/attachment/upload`
2. **Payload**: `multipart/form-data` with key `file` containing the encrypted file block.
3. **Response**: A clean JSON response containing the public relative URL:
   ```json
   {
     "isSuccess": true,
     "message": "Attachment uploaded successfully.",
     "data": "/attachments/1fec79a8-43eb-4c26-8e9c-9e6bb5827980.bin",
     "errors": null
   }
   ```
4. **Retrieval**: Download directly via standard HTTP request at: `http://localhost:5108/attachments/1fec79a8-43eb-4c26-8e9c-9e6bb5827980.bin`

---

## 🟢 Step 6: Batch Presence Querying API

To check the online status of multiple users at once (e.g., when initializing a list of active chats on app boot):

1. **Endpoint**: `POST /api/profile/presence`
2. **Payload**: A simple list of usernames:
   ```json
   [
     "alice_7209",
     "bob_7209",
     "carol"
   ]
   ```
3. **Response**: A detailed presence object list mapping usernames to their real-time connection status:
   ```json
   {
     "isSuccess": true,
     "message": "Presence status retrieved successfully.",
     "data": [
       {
         "username": "alice_7209",
         "isOnline": true
       },
       {
         "username": "bob_7209",
         "isOnline": false
       },
       {
         "username": "carol",
         "isOnline": false
       }
     ],
     "errors": null
   }
   ```

---

## 🧪 Step 7: Run Automated Verification Tests

To verify registrations, profile restrictions, key vending, read receipts, file uploads, and batch presence checking automatically, run the Python integration suite.

1. Ensure the API is running locally at `http://localhost:5108`.
2. Run the integration test suite:
   ```bash
   python3 tests/integration_test.py
   ```
3. Expected output snippet:
   ```
   --- Running E2EE & Core Messaging API Integration Tests ---
   ...
   18. Testing Encrypted Media & File Attachment Relay (Blind Storage)...
   Upload Status: 200
   Downloading uploaded attachment...
   Download Status: 200
   Attachment retrieval payload match verified!

   --- ALL E2EE, PRIVACY, AND ATTACHMENT TESTS PASSED SUCCESSFULLY! ---
   ```
