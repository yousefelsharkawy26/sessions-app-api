# End-to-End Encryption (E2EE) Specification & Design

This document details the architecture, schemas, and APIs for implementing End-to-End Encryption (E2EE) using X3DH key agreement on the Session-style Messaging App API.

---

## 📋 1. Understanding Summary
- **Goal**: Enable blind server relays for message transport where the server stores and relays ciphertexts and cryptographic headers but cannot read the contents.
- **Scope**: Expose prekey bundle directories and ciphertext relays.
- **Constraints**: Cryptographic operations occur strictly client-side. The database acts as a storage layer for prekey bundles. Privacy boundaries (e.g. `IsPrivate`) must prevent unauthorized users from harvesting prekey bundles.
- **Non-Goals**: No server-side encryption/decryption, key generation, or plaintext message storage.

---

## ⚙️ 2. Assumptions
- **Cryptosystem**: Clients generate Curve25519 identity, signed prekey, and one-time prekeys. They will use standard cryptography libraries (e.g., Libsodium, Noise Protocol) locally.
- **Graceful Degradation**: If a user runs out of One-Time Prekeys (OTPs), senders fall back to the recipient's long-term Signed Prekey.
- **Automatic Client Replenishment**: The client application will replenish its OTP pool when it drops below a threshold.

---

## 🛠️ 3. Decision Log

### Decision 3.1: Cryptographic Protocol
- **What was decided**: Use the Extended Triple Diffie-Hellman (X3DH) prekey bundle protocol.
- **Alternatives considered**: Static DH key exchange (PGP-style).
- **Rationale**: X3DH provides *forward secrecy* and *break-in recovery*, matching the security expectations of a "Session-style" messaging platform.

### Decision 3.2: Key Exhaustion Behavior
- **What was decided**: Fallback to Signed Prekeys without blocking message sending.
- **Alternatives considered**: Fail and block session establishment, or fail-open with a warning.
- **Rationale**: Best balance of usability and security. Blocking communication hurts the user experience.

### Decision 3.3: Database Integration
- **What was decided**: Integrate E2EE tables directly with the main database context (Approach 1).
- **Alternatives considered**: Decoupled cryptographic identity microservice.
- **Rationale**: Less architectural overhead, simpler transaction management, and automatic referential integrity when user records are deleted.

---

## 📐 4. Database Schema

### PrekeyBundle
```csharp
public class PrekeyBundle
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    
    public string IdentityKey { get; set; } = null!;      // Base64 public key
    public string SignedPrekey { get; set; } = null!;      // Base64 public key
    public string Signature { get; set; } = null!;         // Base64 signature
    public int SignedPrekeyId { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
```

### OneTimePrekey
```csharp
public class OneTimePrekey
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    
    public int KeyId { get; set; }
    public string KeyData { get; set; } = null!;          // Base64 public key
}
```

### Message (Updated Columns)
- `Ciphertext` replacing plaintext `Content`.
- `EphemeralKey` (Base64 public key).
- `SignedPrekeyIdUsed` (int).
- `OneTimePrekeyIdUsed` (int, nullable).

---

## 📡 5. API Endpoints

### 1. `POST /api/keys/upload` (Upload/Update Prekeys)
Clients upload identity key, signed prekey, and a pool of one-time prekeys:
- **Body**:
  ```json
  {
    "identityKey": "string",
    "signedPrekey": "string",
    "signature": "string",
    "signedPrekeyId": 100,
    "oneTimePrekeys": [
      { "keyId": 1, "keyData": "string" },
      { "keyId": 2, "keyData": "string" }
    ]
  }
  ```

### 2. `GET /api/keys/bundle/{username}` (Fetch Recipient Key Bundle)
Finds a recipient's keys, vends and deletes one OTP, and returns the bundle.
- **Response**:
  ```json
  {
    "identityKey": "string",
    "signedPrekey": "string",
    "signature": "string",
    "signedPrekeyId": 100,
    "oneTimePrekey": {
      "keyId": 1,
      "keyData": "string"
    } // null if exhausted
  }
  ```

### 3. `POST /api/message/send` (Relay Encrypted Message)
Relays the ciphertext and routing headers.
- **Body**:
  ```json
  {
    "receiverUsername": "bob",
    "ciphertext": "string",
    "ephemeralKey": "string",
    "signedPrekeyIdUsed": 100,
    "oneTimePrekeyIdUsed": 1
  }
  ```
- **Real-time SignalR Event**: Receivers receive a `ReceiveMessage` push event containing these exact parameters.
