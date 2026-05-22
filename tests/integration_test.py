import urllib.request
import json
import time
import random

BASE_URL = "http://localhost:5108"

# Dynamic usernames to prevent collision in hosted DB
suffix = str(random.randint(1000, 9999))
ALICE = f"alice_{suffix}"
BOB = f"bob_{suffix}"

print(f"Dynamic test users: {ALICE} and {BOB}")

def make_request(path, method="GET", payload=None, token=None):
    url = f"{BASE_URL}{path}"
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
        
    data = None
    if payload:
        data = json.dumps(payload).encode("utf-8")
        
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req) as response:
            res_data = response.read().decode("utf-8")
            return response.status, json.loads(res_data)
    except urllib.error.HTTPError as e:
        res_data = e.read().decode("utf-8")
        try:
            return e.code, json.loads(res_data)
        except Exception:
            return e.code, res_data

def run_tests():
    print("--- Running E2EE & Core Messaging API Integration Tests ---")
    
    # 1. Register Alice
    print(f"\n1. Registering Alice ({ALICE})...")
    status, res = make_request("/api/auth/register", "POST", {
        "username": ALICE,
        "password": "Password123!",
        "displayName": "Alice Wonderland"
    })
    print(f"Status: {status}")
    assert status == 200, "Registration failed"
    assert res["isSuccess"] == True
    alice_token = res["data"]["token"]
    recovery_phrase = res["data"]["recoveryMnemonic"]
    print(f"Alice registered successfully. Recovery Phrase: {recovery_phrase}")
    
    # 2. Register Bob
    print(f"\n2. Registering Bob ({BOB})...")
    status, res = make_request("/api/auth/register", "POST", {
        "username": BOB,
        "password": "Password456!",
        "displayName": "Builder Bob"
    })
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    bob_token = res["data"]["token"]
    
    # 3. Login Alice
    print(f"\n3. Logging in Alice...")
    status, res = make_request("/api/auth/login", "POST", {
        "username": ALICE,
        "password": "Password123!"
    })
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    alice_token = res["data"]["token"]
    
    # 4. Update Alice profile
    print("\n4. Updating Alice profile...")
    status, res = make_request("/api/profile/update", "PUT", {
        "displayName": "Alice In Wonderland",
        "bio": "Curiouser and curiouser!",
        "isPrivate": False,
        "metadata": '{"theme": "dark"}'
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    
    # 5. Update Bob profile to private
    print("\n5. Updating Bob profile to Private...")
    status, res = make_request("/api/profile/update", "PUT", {
        "displayName": "Bob Private",
        "bio": "Secrets!",
        "isPrivate": True,
        "metadata": None
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    
    # 6. Upload prekeys for Alice
    print("\n6. Uploading E2EE prekeys for Alice...")
    status, res = make_request("/api/keys/upload", "POST", {
        "identityKey": "alice_identity_key_base64",
        "signedPrekey": "alice_signed_prekey_base64",
        "signature": "alice_signature_base64",
        "signedPrekeyId": 100,
        "oneTimePrekeys": [
            { "keyId": 1001, "keyData": "alice_otp_1" },
            { "keyId": 1002, "keyData": "alice_otp_2" }
        ]
    }, token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True

    # 7. Upload prekeys for Bob
    print("\n7. Uploading E2EE prekeys for Bob...")
    status, res = make_request("/api/keys/upload", "POST", {
        "identityKey": "bob_identity_key_base64",
        "signedPrekey": "bob_signed_prekey_base64",
        "signature": "bob_signature_base64",
        "signedPrekeyId": 200,
        "oneTimePrekeys": [
            { "keyId": 2001, "keyData": "bob_otp_1" },
            { "keyId": 2002, "keyData": "bob_otp_2" }
        ]
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 8. Test privacy block: Alice tries to fetch Bob's prekey bundle (Bob is private, hasn't messaged Alice)
    print(f"\n8. Alice tries to fetch Bob's prekey bundle (Should fail due to privacy rules)...")
    status, res = make_request(f"/api/keys/bundle/{BOB}", "GET", token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("cannot view prekeys for this private user" in error for error in res["errors"])

    # 9. Bob fetches Alice's prekey bundle (Alice is public, should succeed and vend OTP 1001)
    print(f"\n9. Bob fetches Alice's prekey bundle (OTP 1)...")
    status, res = make_request(f"/api/keys/bundle/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    assert res["data"]["identityKey"] == "alice_identity_key_base64"
    assert res["data"]["oneTimePrekey"]["keyId"] == 1001
    assert res["data"]["oneTimePrekey"]["keyData"] == "alice_otp_1"

    # 10. Bob fetches Alice's prekey bundle again (should vend OTP 1002)
    print(f"\n10. Bob fetches Alice's prekey bundle again (OTP 2)...")
    status, res = make_request(f"/api/keys/bundle/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["data"]["oneTimePrekey"]["keyId"] == 1002
    assert res["data"]["oneTimePrekey"]["keyData"] == "alice_otp_2"

    # 11. Bob fetches Alice's prekey bundle a third time (should be null as OTPs are exhausted)
    print(f"\n11. Bob fetches Alice's prekey bundle a third time (OTPs exhausted)...")
    status, res = make_request(f"/api/keys/bundle/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["data"]["oneTimePrekey"] is None

    # 12. Test message privacy block: Alice sends E2EE message to Bob (Bob is private, hasn't messaged Alice)
    print("\n12. Testing E2EE message privacy block...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": BOB,
        "ciphertext": "encrypted_alice_to_bob_msg",
        "ephemeralKey": "alice_ephemeral_key",
        "signedPrekeyIdUsed": 200,
        "oneTimePrekeyIdUsed": 2001
    }, token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("cannot send messages to this private user" in error for error in res["errors"])

    # 13. Bob sends E2EE message to Alice (initiates chat)
    print("\n13. Bob sends E2EE message to Alice (initiating chat)...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "encrypted_bob_to_alice_msg",
        "ephemeralKey": "bob_ephemeral_key",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": 1001
    }, token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True

    # 14. Now Alice can fetch Bob's prekey bundle (Bob initiated the chat!)
    print(f"\n14. Alice fetches Bob's prekey bundle after Bob initiated chat (Should succeed)...")
    status, res = make_request(f"/api/keys/bundle/{BOB}", "GET", token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    assert res["data"]["identityKey"] == "bob_identity_key_base64"
    assert res["data"]["oneTimePrekey"]["keyId"] == 2001

    # 15. Alice replies to Bob with an E2EE message
    print("\n15. Alice replies to Bob with E2EE message...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": BOB,
        "ciphertext": "encrypted_alice_reply_msg",
        "ephemeralKey": "alice_reply_ephemeral_key",
        "signedPrekeyIdUsed": 200,
        "oneTimePrekeyIdUsed": 2001
    }, token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True

    # 16. Retrieve chat history and verify E2EE headers
    print("\n16. Retrieving chat history and verifying cryptographic headers...")
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    print(f"Status: {status}")
    print(f"Response count: {len(res['data'])}")
    assert len(res['data']) == 2
    
    msg1 = res["data"][0]
    print(f"Message 1 headers: {msg1}")
    assert msg1["senderUsername"] == BOB
    assert msg1["receiverUsername"] == ALICE
    assert msg1["ciphertext"] == "encrypted_bob_to_alice_msg"
    assert msg1["ephemeralKey"] == "bob_ephemeral_key"
    assert msg1["signedPrekeyIdUsed"] == 100
    assert msg1["oneTimePrekeyIdUsed"] == 1001

    msg2 = res["data"][1]
    print(f"Message 2 headers: {msg2}")
    assert msg2["senderUsername"] == ALICE
    assert msg2["receiverUsername"] == BOB
    assert msg2["ciphertext"] == "encrypted_alice_reply_msg"
    assert msg2["ephemeralKey"] == "alice_reply_ephemeral_key"
    assert msg2["signedPrekeyIdUsed"] == 200
    assert msg2["oneTimePrekeyIdUsed"] == 2001
    
    # 17. Query Key Status for Alice (Verifying key depletion count is tracked perfectly)
    print("\n17. Querying key status for Alice...")
    status, res = make_request("/api/keys/status", "GET", token=alice_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    assert res["data"]["isUploaded"] == True
    # Alice started with 2 OTPs, Bob vended both (1001 and 1002). Count should now be exactly 0.
    assert res["data"]["remainingOneTimePrekeysCount"] == 0
    assert res["data"]["signedPrekeyId"] == 100
    
    # 18. Encrypted Media & File Attachment Relay (Blind Storage)
    print("\n18. Testing Encrypted Media & File Attachment Relay (Blind Storage)...")
    simulated_encrypted_data = b"SimulatedAES-GCMEncryptedAttachmentDataPayload"
    
    # helper for multipart upload
    import uuid
    boundary = f"----WebKitFormBoundary{uuid.uuid4().hex}"
    upload_url = f"{BASE_URL}/api/attachment/upload"
    headers = {
        "Content-Type": f"multipart/form-data; boundary={boundary}",
        "Authorization": f"Bearer {alice_token}"
    }
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="encrypted_payload.bin"\r\n'
        f"Content-Type: application/octet-stream\r\n\r\n"
    ).encode("utf-8") + simulated_encrypted_data + f"\r\n--{boundary}--\r\n".encode("utf-8")
    
    req = urllib.request.Request(upload_url, data=body, headers=headers, method="POST")
    upload_status = 0
    upload_res = None
    try:
        with urllib.request.urlopen(req) as response:
            upload_status = response.status
            upload_res = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        upload_status = e.code
        upload_res = e.read().decode("utf-8")
        
    print(f"Upload Status: {upload_status}")
    print(f"Upload Response: {upload_res}")
    assert upload_status == 200
    assert upload_res["isSuccess"] == True
    attachment_url = upload_res["data"]
    assert attachment_url.startswith("/attachments/")
    
    # Try downloading the uploaded blind storage file
    print(f"Downloading uploaded attachment from: {BASE_URL}{attachment_url}")
    download_req = urllib.request.Request(f"{BASE_URL}{attachment_url}", method="GET")
    with urllib.request.urlopen(download_req) as response:
        download_status = response.status
        download_data = response.read()
        
    print(f"Download Status: {download_status}")
    assert download_status == 200
    assert download_data == simulated_encrypted_data
    print("Attachment retrieval payload match verified!")
    
    print("\n--- ALL E2EE, PRIVACY, AND ATTACHMENT TESTS PASSED SUCCESSFULLY! ---")

if __name__ == "__main__":
    run_tests()
