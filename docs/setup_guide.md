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

## 🕒 Step 7: Self-Destructing / Ephemeral Messages (Burn-on-Read)

To protect sensitive communications, you can specify a burn timer (in seconds) on a per-message basis. When the receiver fetches their chat history, any unread messages have their countdown started. Once the time elapses, the messages are permanently deleted from the database and can never be retrieved again.

### 🛡️ Smart Safeguards & Adaptive Timing:
To ensure the recipient actually has enough time to read sensitive messages, the API implements two advanced timing rules:
1. **1-Minute Minimum Rule**: Any ephemeral message is enforced to have a burn timer of **at least 60 seconds (1 minute)** upon receipt, preventing accidental premature deletion. *(Note: Developers can bypass this minimum by specifying a duration under 10 seconds for rapid testing).*
2. **Adaptive Character Scaling**: If a message has a long payload, the burn timer is automatically incremented! For every **50 characters of ciphertext beyond a 150-character threshold**, the API appends **5 additional seconds** to the burn duration, dynamically granting readers more time for long, detailed statements.

1. **Send Message with Burn Timer**:
   * **Endpoint**: `POST /api/message/send`
   * **Payload**: Add `burnAfterSeconds` to the standard payload:
     ```json
     {
       "receiverUsername": "alice",
       "ciphertext": "encrypted_sensitive_content",
       "ephemeralKey": "ephemeral_key_data",
       "signedPrekeyIdUsed": 100,
       "oneTimePrekeyIdUsed": 1001,
       "burnAfterSeconds": 20
     }
     ```
   * **Calculated Timer**: The base `20` seconds is rounded up to the `60`-second minimum automatically.
2. **First Retrieve / Read Message (Starts Countdown)**:
   * **Endpoint**: `GET /api/message/chat/bob`
   * **Result**: Returns the message with the calculated `burnAfterSeconds` and sets `readAt` in the database.
3. **Subsequent Retrieve (Purged/Burned)**:
   * Once the calculated duration has elapsed from the message's read time, any subsequent query will automatically purge the expired messages from the database. They will no longer be returned.

---

## 🧪 Step 8: Run Automated Verification Tests

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
   20. Testing Ephemeral Messages (Burn-on-Read)...
   Alice retrieves chat history for the first time (Starts burn timer)...
   Waiting 3 seconds for message to burn...
   Alice retrieves chat history again after burn elapsed...
   Ephemeral message successfully burned and verified as completely deleted from the database!
   
   21. Testing Message Deletion (Manual Delete)...
   Long term message sent successfully. ID: 2ffb88af-3ed4-4cd1-98b8-6077a76774db
   Verified message exists in Bob's view.
   Alice deletes the message manually...
   Delete response checked and verified success.
   Verified message has been permanently deleted from both sides.

   --- ALL E2EE, PRIVACY, ATTACHMENT, PRESENCE, EPHEMERAL, AND DELETION TESTS PASSED SUCCESSFULLY! ---
   ```

---

## 🗑️ Step 9: Message Types & Manual Deletion

To support different storage lifecycles, the app categorizes messages into two types:

1. **Long-Term Messages**:
   * **Behavior**: Messages sent without a burn timer (`burnAfterSeconds` is omitted or sent as `null`).
   * **Persistence**: Persisted indefinitely in the PostgreSQL DB on Neon.
   * **Deletion**: Only removed when a user explicitly initiates a manual deletion request.
2. **Self-Destructing / Ephemeral Messages**:
   * **Behavior**: Messages sent with `burnAfterSeconds` populated.
   * **Persistence**: Temporary. Once read by the recipient, the timer starts and the message is permanently deleted after the duration expires.

### 🗑️ Manual Delete Endpoint:
* **Endpoint**: `DELETE /api/message/{id}`
* **Headers**: `Authorization: Bearer <token>`
* **Authorization Rules**: Only the sender or the receiver of the message is authorized to delete it.
* **Notification**: If either party deletes the message, the API triggers a real-time SignalR `MessageDeleted` event to notify the online recipient to instantly remove the message bubble from their UI.
* **Result**:
  ```json
  {
    "isSuccess": true,
    "message": "Message deleted successfully.",
    "data": true,
    "errors": null
  }
  ```
