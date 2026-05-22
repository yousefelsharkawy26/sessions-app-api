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
    
    # 19. Testing Batch Presence Querying API
    print("\n19. Testing Batch Presence Querying API (POST /api/profile/presence)...")
    presence_status, presence_res = make_request("/api/profile/presence", "POST", [ALICE, BOB, "non_existent_user"], token=alice_token)
    print(f"Presence Status: {presence_status}")
    print(f"Presence Response: {presence_res}")
    assert presence_status == 200
    assert presence_res["isSuccess"] == True
    presence_data = presence_res["data"]
    assert len(presence_data) == 3
    
    alice_presence = next(p for p in presence_data if p["username"] == ALICE)
    bob_presence = next(p for p in presence_data if p["username"] == BOB)
    non_existent_presence = next(p for p in presence_data if p["username"] == "non_existent_user")
    
    # Since these are pure HTTP calls, they are not connected to the SignalR socket in this test execution
    assert alice_presence["isOnline"] == False
    assert bob_presence["isOnline"] == False
    assert non_existent_presence["isOnline"] == False
    print("Batch presence values retrieved successfully and verified!")
    
    # 20. Testing Ephemeral Messages (Burn-on-Read)
    print("\n20. Testing Ephemeral Messages (Burn-on-Read)...")
    
    # 20a. Verify 1-minute minimum rule for normal requested burn durations (>= 10 seconds)
    print("Testing 1-minute minimum rule for normal burn durations...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "normal_ephemeral_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_normal",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": 1002,
        "burnAfterSeconds": 20  # Should be converted to 60 seconds
    }, token=bob_token)
    assert status == 200
    assert res["data"]["burnAfterSeconds"] == 60
    print("Normal burn duration minimum of 60 seconds verified successfully!")

    # 20b. Verify adaptive increment for long messages
    print("Testing adaptive increment for long messages...")
    long_ciphertext = "a" * 255  # 255 characters (105 characters above the 150 threshold) -> adds 10 extra seconds (105 / 50 * 5)
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": long_ciphertext,
        "ephemeralKey": "bob_ephemeral_key_long",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": 1002,
        "burnAfterSeconds": 20  # Base 20 -> minimum 60 -> plus 10 extra seconds = 70 seconds total
    }, token=bob_token)
    assert status == 200
    assert res["data"]["burnAfterSeconds"] == 70
    print(f"Adaptive increment verified! Long message received burnAfterSeconds of {res['data']['burnAfterSeconds']} instead of 60.")

    # 20c. Bob sends an ephemeral message with an ultra-short 2-second burn timer (bypassing minimum for testing speed)
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "ephemeral_bob_to_alice_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_2",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": 1002,
        "burnAfterSeconds": 2
    }, token=bob_token)
    print(f"Send Ephemeral Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    assert res["data"]["burnAfterSeconds"] == 2
    ephemeral_msg_id = res["data"]["id"]
    
    # Alice retrieves chat history - this marks the message as read and starts the burn timer!
    print("Alice retrieves chat history for the first time (Starts burn timer)...")
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    print(f"Chat count: {len(res['data'])}")
    # Should include our ephemeral message
    ephemeral_msg = next((m for m in res["data"] if m["id"] == ephemeral_msg_id), null := None)
    assert ephemeral_msg is not None
    assert ephemeral_msg["burnAfterSeconds"] == 2
    assert ephemeral_msg["readAt"] is not None
    print(f"Ephemeral message retrieved and read state initialized at {ephemeral_msg['readAt']}")
    
    # Wait for the burn timer to elapse (3 seconds)
    import time
    print("Waiting 3 seconds for message to burn...")
    time.sleep(3)
    
    # Alice retrieves chat history again - the expired message should now be purged
    print("Alice retrieves chat history again after burn elapsed...")
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    print(f"Chat count: {len(res['data'])}")
    ephemeral_msg_after = next((m for m in res["data"] if m["id"] == ephemeral_msg_id), null := None)
    assert ephemeral_msg_after is None
    print("Ephemeral message successfully burned and verified as completely deleted from the database!")
    
    # 21. Testing Message Deletion (Manual Delete)
    print("\n21. Testing Message Deletion (Manual Delete)...")
    # Alice sends a long term message (burnAfterSeconds is null) to Bob
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": BOB,
        "ciphertext": "long_term_message_ciphertext",
        "ephemeralKey": "alice_long_term_ephemeral_key",
        "signedPrekeyIdUsed": 200,
        "oneTimePrekeyIdUsed": None,
        "burnAfterSeconds": None
    }, token=alice_token)
    assert status == 200
    msg_id = res["data"]["id"]
    print(f"Long term message sent successfully. ID: {msg_id}")

    # Bob fetches chat history to confirm it is present
    status, res = make_request(f"/api/message/chat/{ALICE}", "GET", token=bob_token)
    matching_msg = next((m for m in res["data"] if m["id"] == msg_id), None)
    assert matching_msg is not None
    print("Verified message exists in Bob's view.")

    # Alice deletes the message
    print("Alice deletes the message manually...")
    status, res = make_request(f"/api/message/{msg_id}", "DELETE", token=alice_token)
    assert status == 200
    assert res["isSuccess"] == True
    print("Delete response checked and verified success.")

    # Bob fetches chat history to verify it is no longer returned
    status, res = make_request(f"/api/message/chat/{ALICE}", "GET", token=bob_token)
    matching_msg_after = next((m for m in res["data"] if m["id"] == msg_id), None)
    assert matching_msg_after is None
    print("Verified message has been permanently deleted from both sides.")
    
    # 22. Testing Message Editing (E2EE Message Editing API)
    print("\n22. Testing Message Editing (E2EE Message Editing API)...")
    # Bob sends a long term message to Alice
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "original_bob_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_edit_test",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": None,
        "burnAfterSeconds": None
    }, token=bob_token)
    assert status == 200
    msg_id = res["data"]["id"]
    print(f"Original message sent successfully. ID: {msg_id}")

    # Bob edits his message
    print("Bob edits his message...")
    status, res = make_request(f"/api/message/{msg_id}", "PUT", {
        "newCiphertext": "edited_bob_ciphertext",
        "newEphemeralKey": "new_bob_ephemeral_key_edit_test"
    }, token=bob_token)
    assert status == 200
    assert res["isSuccess"] == True
    assert res["data"]["isEdited"] == True
    assert res["data"]["editedAt"] is not None
    assert res["data"]["ciphertext"] == "edited_bob_ciphertext"
    assert res["data"]["ephemeralKey"] == "new_bob_ephemeral_key_edit_test"
    print("Bob's edit response verified successfully.")

    # Alice fetches history and verifies she gets the updated message
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    edited_msg = next((m for m in res["data"] if m["id"] == msg_id), None)
    assert edited_msg is not None
    assert edited_msg["isEdited"] == True
    assert edited_msg["ciphertext"] == "edited_bob_ciphertext"
    print("Alice verified the edited message correctly.")

    # Alice tries to edit Bob's message (Should fail)
    print("Alice tries to edit Bob's message (expected failure)...")
    status, res = make_request(f"/api/message/{msg_id}", "PUT", {
        "newCiphertext": "alice_hijack_ciphertext",
        "newEphemeralKey": "alice_hijack_key"
    }, token=alice_token)
    assert status == 400
    assert res["isSuccess"] == False
    assert "permission" in res["errors"][0]
    print("Unauthorized edit attempt blocked and verified.")

    # Bob sends an ephemeral message with a 2-second burn timer
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "ephemeral_to_be_read_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_burn_edit_test",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": None,
        "burnAfterSeconds": 2
    }, token=bob_token)
    assert status == 200
    ephemeral_msg_id = res["data"]["id"]

    # Alice retrieves history to mark the message as read (starts burning)
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    
    # Bob tries to edit the burning message (Should fail)
    print("Bob tries to edit the burning ephemeral message (expected failure)...")
    status, res = make_request(f"/api/message/{ephemeral_msg_id}", "PUT", {
        "newCiphertext": "edited_burning_ciphertext",
        "newEphemeralKey": "edited_burning_key"
    }, token=bob_token)
    assert status == 400
    assert res["isSuccess"] == False
    assert "burning" in res["errors"][0]
    print("Edit attempt on burning message blocked and verified successfully.")
    
    # 23. Testing E2EE Group Chats
    print("\n23. Testing E2EE Group Chats...")
    CHARLIE = f"charlie_{suffix}"
    DAVID = f"david_{suffix}"
    
    # Register Charlie
    print(f"Registering Charlie ({CHARLIE})...")
    status, res = make_request("/api/auth/register", "POST", {
        "username": CHARLIE,
        "password": "Password123!",
        "displayName": "Charlie Chaplin"
    })
    assert status == 200
    
    # Log in Charlie
    print("Logging in Charlie...")
    status, res = make_request("/api/auth/login", "POST", {
        "username": CHARLIE,
        "password": "Password123!"
    })
    assert status == 200
    charlie_token = res["data"]["token"]

    # Charlie uploads prekeys so we can fetch them batched
    print("Charlie uploads E2EE prekeys...")
    status, res = make_request("/api/keys/upload", "POST", {
        "identityKey": "charlie_identity_key_base64",
        "signedPrekey": "charlie_signed_prekey_base64",
        "signature": "charlie_signature_base64",
        "signedPrekeyId": 300,
        "oneTimePrekeys": [
            {"keyId": 3001, "keyData": "charlie_otp_1"},
            {"keyId": 3002, "keyData": "charlie_otp_2"}
        ]
    }, token=charlie_token)
    print(f"Charlie Upload Prekeys Status: {status}, Response: {res}")
    assert status == 200

    # Register David
    print(f"Registering David ({DAVID})...")
    status, res = make_request("/api/auth/register", "POST", {
        "username": DAVID,
        "password": "Password123!",
        "displayName": "David Copperfield"
    })
    assert status == 200
    
    # Log in David
    print("Logging in David...")
    status, res = make_request("/api/auth/login", "POST", {
        "username": DAVID,
        "password": "Password123!"
    })
    assert status == 200
    david_token = res["data"]["token"]

    # Alice creates a group with Bob and Charlie
    print("Alice creates E2EE group...")
    status, res = make_request("/api/group", "POST", {
        "name": "Secret Society",
        "memberUsernames": [BOB, CHARLIE]
    }, token=alice_token)
    assert status == 200
    group_id = res["data"]["id"]
    print(f"Group created successfully. ID: {group_id}")
    members = [m["username"] for m in res["data"]["members"]]
    assert ALICE in members
    assert BOB in members
    assert CHARLIE in members

    # Alice fetches other members' prekey bundles (Batched E2EE setup)
    print("Alice fetches batched member prekeys for pairwise key sharing...")
    status, res = make_request(f"/api/group/{group_id}/prekeys", "GET", token=alice_token)
    assert status == 200
    prekey_list = res["data"]
    usernames_with_keys = [p["username"] for p in prekey_list]
    assert BOB in usernames_with_keys
    assert CHARLIE in usernames_with_keys
    print("Alice successfully retrieved prekey bundles for Bob and Charlie.")

    # Alice sends an E2EE message to the group (encrypted via group Sender Key)
    print("Alice sends E2EE group message...")
    status, res = make_request("/api/message/group", "POST", {
        "groupId": group_id,
        "ciphertext": "group_ciphertext_from_alice",
        "ephemeralKey": "alice_group_ephemeral_key"
    }, token=alice_token)
    assert status == 200
    msg_id = res["data"]["id"]
    print(f"Group message sent. ID: {msg_id}")

    # Bob retrieves group history and confirms he sees the ciphertext
    print("Bob retrieves group chat history...")
    status, res = make_request(f"/api/message/group/{group_id}", "GET", token=bob_token)
    assert status == 200
    group_msgs = res["data"]
    assert len(group_msgs) == 1
    assert group_msgs[0]["ciphertext"] == "group_ciphertext_from_alice"
    assert group_msgs[0]["senderUsername"] == ALICE
    print("Bob successfully retrieved and verified the E2EE group message.")

    # Charlie adds David to the group
    print("Charlie adds David to the group...")
    status, res = make_request(f"/api/group/{group_id}/member", "POST", {
        "username": DAVID
    }, token=charlie_token)
    assert status == 200

    # Charlie removes Bob from the group
    print("Charlie removes Bob from the group...")
    status, res = make_request(f"/api/group/{group_id}/member", "DELETE", {
        "username": BOB
    }, token=charlie_token)
    assert status == 200

    # Bob tries to fetch history (Should fail)
    print("Bob tries to fetch group history after being removed (expected failure)...")
    status, res = make_request(f"/api/message/group/{group_id}", "GET", token=bob_token)
    assert status == 400
    assert res["isSuccess"] == False
    print("Bob's access to group history was successfully revoked.")

    # Bob tries to send a message to the group (Should fail)
    print("Bob tries to send group message after being removed (expected failure)...")
    status, res = make_request("/api/message/group", "POST", {
        "groupId": group_id,
        "ciphertext": "unauthorized_group_ciphertext",
        "ephemeralKey": "bob_group_ephemeral_key"
    }, token=bob_token)
    assert status == 400
    assert res["isSuccess"] == False
    print("Bob's ability to post messages to the group was successfully revoked.")
    
    # 24. Testing Blocklist / Privacy Shield (Phase 1)
    print("\n24. Testing Blocklist / Privacy Shield (User Blocklist System)...")
    
    # Alice blocks Bob
    print(f"Alice blocks Bob...")
    status, res = make_request(f"/api/block/{BOB}", "POST", token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    
    # Alice retrieves blocked users list (should contain Bob)
    print("Alice gets her blocked users list...")
    status, res = make_request("/api/block", "GET", token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    blocked_usernames = [u["username"] for u in res["data"]]
    assert BOB in blocked_usernames
    print("Bob found in Alice's blocked list.")
    
    # Bob tries to view Alice's profile (Should fail, return 400 User not found for privacy)
    print("Bob tries to get Alice's profile (expected failure)...")
    status, res = make_request(f"/api/profile/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("not found" in err.lower() for err in res["errors"])
    
    # Bob tries to fetch Alice's prekey bundle (Should fail)
    print("Bob tries to fetch Alice's prekey bundle (expected failure)...")
    status, res = make_request(f"/api/keys/bundle/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("blocked" in err.lower() for err in res["errors"])
    
    # Bob tries to send Alice a message (Should fail)
    print("Bob tries to send Alice a message (expected failure)...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "blocked_bob_ciphertext",
        "ephemeralKey": "bob_blocked_key",
        "signedPrekeyIdUsed": 100,
        "oneTimePrekeyIdUsed": None
    }, token=bob_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("blocked" in err.lower() for err in res["errors"])
    
    # Alice tries to send Bob a message (Should fail since block is active in either direction)
    print("Alice tries to send Bob a message while blocked (expected failure)...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": BOB,
        "ciphertext": "blocked_alice_ciphertext",
        "ephemeralKey": "alice_blocked_key",
        "signedPrekeyIdUsed": 200,
        "oneTimePrekeyIdUsed": None
    }, token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("blocked" in err.lower() for err in res["errors"])

    # Alice searches for Bob (Should not find him)
    print("Alice searches for Bob (expected empty result)...")
    status, res = make_request(f"/api/profile/search?searchTerm={BOB}", "GET", token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert len(res["data"]) == 0
    
    # Bob searches for Alice (Should not find her)
    print("Bob searches for Alice (expected empty result)...")
    status, res = make_request(f"/api/profile/search?searchTerm={ALICE}", "GET", token=bob_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert len(res["data"]) == 0
    
    # Alice tries to add Bob to a new group (Should exclude or fail)
    print("Alice creates a group trying to include Bob...")
    status, res = make_request("/api/group", "POST", {
        "name": "Should Exclude Bob",
        "memberUsernames": [BOB, CHARLIE]
    }, token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    group_members = [m["username"] for m in res["data"]["members"]]
    assert BOB not in group_members
    assert CHARLIE in group_members
    print("Verified Bob was excluded from the created group.")

    # Alice unblocks Bob
    print("Alice unblocks Bob...")
    status, res = make_request(f"/api/block/{BOB}", "DELETE", token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert res["isSuccess"] == True
    
    # Alice gets her blocked list (should be empty now)
    print("Alice gets her blocked list again...")
    status, res = make_request("/api/block", "GET", token=alice_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    assert len(res["data"]) == 0
    
    # Bob searches for Alice again (Should find her now)
    print("Bob searches for Alice after being unblocked...")
    status, res = make_request(f"/api/profile/search?searchTerm={ALICE}", "GET", token=bob_token)
    print(f"Status: {status}, Response: {res}")
    assert status == 200
    found_usernames = [u["username"] for u in res["data"]]
    assert ALICE in found_usernames
    print("Unblocked successfully and verified all constraints reset!")
    
    print("\n--- ALL E2EE, PRIVACY, ATTACHMENT, PRESENCE, EPHEMERAL, DELETION, EDITING, GROUP, AND BLOCKLIST TESTS PASSED SUCCESSFULLY! ---")

if __name__ == "__main__":
    run_tests()
