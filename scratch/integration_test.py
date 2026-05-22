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
    assert msg2["deliveredAt"] is None, "Message 2 should not be delivered yet"
    assert msg2["readAt"] is None

    # 17. Bob marks Alice's reply as delivered
    print("\n17. Bob marks Alice's reply as delivered...")
    status, res_del = make_request("/api/message/deliver", "POST", {
        "messageIds": [msg2["id"]]
    }, token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res_del}")
    assert status == 200
    assert res_del["isSuccess"] == True

    # 18. Retrieve chat history again and verify deliveredAt is set but readAt is still null
    print("\n18. Retrieving chat history again to verify delivery receipt...")
    status, res_chat = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    
    updated_msg2 = res_chat["data"][1]
    print(f"Updated Message 2 headers: {updated_msg2}")
    assert updated_msg2["deliveredAt"] is not None, "Message 2 should now have deliveredAt set"
    assert updated_msg2["readAt"] is None, "Message 2 should still have readAt as null"
    
    # 19. Alice registers a second device: desktop-1
    print("\n19. Uploading E2EE prekeys for Alice's secondary device (desktop-1)...")
    status, res = make_request("/api/keys/upload", "POST", {
        "deviceId": "desktop-1",
        "deviceName": "Alice's MacBook Pro",
        "identityKey": "alice_desktop_identity_key_base64",
        "signedPrekey": "alice_desktop_signed_prekey_base64",
        "signature": "alice_desktop_signature_base64",
        "signedPrekeyId": 150,
        "oneTimePrekeys": [
            { "keyId": 5001, "keyData": "alice_desktop_otp_1" }
        ]
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 20. Bob fetches Alice's bundle again and verifies that BOTH devices are present
    print("\n20. Bob fetches Alice's prekey bundle (should see 'primary' and 'desktop-1')...")
    status, res = make_request(f"/api/keys/bundle/{ALICE}", "GET", token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    devices_in_res = res["data"]["devices"]
    print(f"Returned devices: {[d['deviceId'] for d in devices_in_res]}")
    assert len(devices_in_res) >= 2
    
    primary_dev = next(d for d in devices_in_res if d["deviceId"] == "primary")
    desktop_dev = next(d for d in devices_in_res if d["deviceId"] == "desktop-1")
    
    assert primary_dev["identityKey"] == "alice_identity_key_base64"
    assert desktop_dev["identityKey"] == "alice_desktop_identity_key_base64"
    assert desktop_dev["oneTimePrekey"]["keyId"] == 5001
    assert desktop_dev["oneTimePrekey"]["keyData"] == "alice_desktop_otp_1"

    # 21. Bob sends an E2EE message targeted specifically to Alice's second device desktop-1
    print("\n21. Bob sends E2EE message targeted specifically to Alice's desktop-1 device...")
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "recipientDeviceId": "desktop-1",
        "ciphertext": "encrypted_bob_to_alice_desktop_msg",
        "ephemeralKey": "bob_ephemeral_key_to_desktop",
        "signedPrekeyIdUsed": 150,
        "oneTimePrekeyIdUsed": 5001
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True
    desktop_msg_id = res["data"]["id"]

    # 22. Alice retrieves chat history on desktop-1 and receives the message
    print("\n22. Alice retrieves chat history on desktop-1 (should see the desktop message)...")
    status, res_chat = make_request(f"/api/message/chat/{BOB}?deviceId=desktop-1", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    desktop_chat = res_chat["data"]
    print(f"Chat messages on desktop-1: {[m['ciphertext'] for m in desktop_chat]}")
    assert any(m["id"] == desktop_msg_id for m in desktop_chat)

    # 23. Alice retrieves chat history on primary device and should NOT see the desktop message
    print("\n23. Alice retrieves chat history on primary device (should NOT see the desktop message)...")
    status, res_chat_primary = make_request(f"/api/message/chat/{BOB}?deviceId=primary", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    primary_chat = res_chat_primary["data"]
    print(f"Chat messages on primary: {[m['ciphertext'] for m in primary_chat]}")
    assert not any(m["id"] == desktop_msg_id for m in primary_chat)

    # 24. WebRTC call signaling: Alice initiates call to Bob
    print("\n24. WebRTC call signaling: Alice initiates E2EE call to Bob...")
    status, res = make_request("/api/call/initiate", "POST", {
        "receiverUsername": BOB,
        "sdpOffer": "alice_sdp_offer_payload"
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 25. WebRTC call signaling: Bob accepts call from Alice
    print("\n25. WebRTC call signaling: Bob accepts E2EE call from Alice...")
    status, res = make_request("/api/call/accept", "POST", {
        "callerUsername": ALICE,
        "sdpAnswer": "bob_sdp_answer_payload"
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 26. WebRTC call signaling: Alice sends ICE candidate to Bob
    print("\n26. WebRTC call signaling: Alice sends ICE candidate to Bob...")
    status, res = make_request("/api/call/candidate", "POST", {
        "receiverUsername": BOB,
        "candidate": "alice_ice_candidate_data"
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 27. WebRTC call signaling: Bob declines call from Alice
    print("\n27. WebRTC call signaling: Bob declines call from Alice...")
    status, res = make_request("/api/call/decline", "POST", {
        "callerUsername": ALICE
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 28. WebRTC call signaling: Alice hangs up call with Bob
    print("\n28. WebRTC call signaling: Alice hangs up call with Bob...")
    status, res = make_request("/api/call/hangup", "POST", {
        "receiverUsername": BOB
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 29. WebRTC call signaling blocklist validation: Alice blocks Bob, Bob tries to call Alice
    print(f"\n29. Alice blocks Bob ({BOB})...")
    status, res = make_request(f"/api/block/{BOB}", "POST", token=alice_token)
    print(f"Status: {status}")
    assert status == 200

    print("\n30. Bob tries to call Alice (should fail due to blocklist validation)...")
    status, res = make_request("/api/call/initiate", "POST", {
        "receiverUsername": ALICE,
        "sdpOffer": "bob_blocked_sdp_offer"
    }, token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("You cannot call this user because one of you has blocked the other" in error for error in res["errors"])

    # Cleanup: Unblock Bob
    print(f"\n31. Alice unblocks Bob ({BOB})...")
    status, res = make_request(f"/api/block/{BOB}", "DELETE", token=alice_token)
    print(f"Status: {status}")
    assert status == 200

    # 32. Direct chat: Bob sends a threaded/quoted reply to Alice
    print("\n32. Direct chat: Bob sends a threaded/quoted reply to Alice...")
    # First Bob needs a message to reply to. Alice sends a message to Bob.
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": BOB,
        "ciphertext": "original_alice_message_ciphertext",
        "ephemeralKey": "alice_eph_key",
        "signedPrekeyIdUsed": 200,
        "oneTimePrekeyIdUsed": 2002
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    original_msg_id = res["data"]["id"]

    # Bob replies to that message
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "bob_threaded_reply_ciphertext",
        "ephemeralKey": "bob_eph_key",
        "signedPrekeyIdUsed": 100,
        "parentMessageId": original_msg_id
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    reply_msg_id = res["data"]["id"]
    assert res["data"]["parentMessageId"] == original_msg_id

    # 33. Direct chat: Alice retrieves chat history and verifies parentMessageId is populated
    print("\n33. Direct chat: Alice retrieves chat history and verifies reply relationship...")
    status, res = make_request(f"/api/message/chat/{BOB}?deviceId=primary", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    chat = res["data"]
    reply_msg = next(m for m in chat if m["id"] == reply_msg_id)
    assert reply_msg["parentMessageId"] == original_msg_id

    # 34. Reactions: Bob reacts to Alice's original message
    print("\n34. Reactions: Bob reacts to Alice's original message...")
    status, res = make_request("/api/message/react", "POST", {
        "messageId": original_msg_id,
        "reactionCiphertext": "encrypted_heart_emoji"
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 35. Reactions: Alice retrieves chat history and verifies reaction exists
    print("\n35. Reactions: Alice retrieves chat history and verifies Bob's reaction...")
    status, res = make_request(f"/api/message/chat/{BOB}?deviceId=primary", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    chat = res["data"]
    orig_msg = next(m for m in chat if m["id"] == original_msg_id)
    assert len(orig_msg["reactions"]) == 1
    rx = orig_msg["reactions"][0]
    assert rx["messageId"] == original_msg_id
    assert rx["username"] == BOB
    assert rx["reactionCiphertext"] == "encrypted_heart_emoji"

    # 36. Reactions toggle/remove: Bob removes his reaction (by sending empty reaction ciphertext)
    print("\n36. Reactions: Bob removes his reaction...")
    status, res = make_request("/api/message/react", "POST", {
        "messageId": original_msg_id,
        "reactionCiphertext": ""
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # Verify reaction is gone
    status, res = make_request(f"/api/message/chat/{BOB}?deviceId=primary", "GET", token=alice_token)
    assert status == 200
    orig_msg = next(m for m in res["data"] if m["id"] == original_msg_id)
    assert len(orig_msg["reactions"]) == 0

    # 37. Blocklist reaction validation: Alice blocks Bob, Bob tries to react to original message
    print(f"\n37. Reactions: Alice blocks Bob ({BOB}) again...")
    status, res = make_request(f"/api/block/{BOB}", "POST", token=alice_token)
    print(f"Status: {status}")
    assert status == 200

    print("\n38. Reactions: Bob tries to react to Alice's message while blocked...")
    status, res = make_request("/api/message/react", "POST", {
        "messageId": original_msg_id,
        "reactionCiphertext": "blocked_reaction"
    }, token=bob_token)
    print(f"Status: {status}")
    print(f"Response: {res}")
    assert status == 400
    assert res["isSuccess"] == False
    assert any("You cannot react to this message due to blocklist rules" in error for error in res["errors"])

    # Cleanup: Unblock Bob
    print(f"\n39. Alice unblocks Bob ({BOB}) again...")
    status, res = make_request(f"/api/block/{BOB}", "DELETE", token=alice_token)
    print(f"Status: {status}")
    assert status == 200

    # 40. Register Charlie (CHARLIE)
    CHARLIE = f"charlie_{suffix}"
    print(f"\n40. Registering Charlie ({CHARLIE})...")
    status, res = make_request("/api/auth/register", "POST", {
        "username": CHARLIE,
        "password": "Password789!",
        "displayName": "Charlie Angels"
    })
    print(f"Status: {status}")
    assert status == 200
    charlie_token = res["data"]["token"]

    # 41. Group Roles: Alice creates a new group
    print("\n41. Group Roles: Alice creates a group with Bob...")
    status, res = make_request("/api/group", "POST", {
        "name": "Security Circle",
        "memberUsernames": [BOB]
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    group_id = res["data"]["id"]
    members = res["data"]["members"]
    alice_member = next(m for m in members if m["username"] == ALICE)
    bob_member = next(m for m in members if m["username"] == BOB)
    assert alice_member["role"] == "Owner"
    assert bob_member["role"] == "Member"

    # 42. Group Roles: Bob (Member) tries to rename the group (should fail)
    print("\n42. Group Roles: Bob (Member) tries to rename the group...")
    status, res = make_request(f"/api/group/{group_id}/metadata", "PUT", {
        "newName": "Bob's Hack Shack"
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 400
    assert res["isSuccess"] == False

    # 43. Group Roles: Alice (Owner) promotes Bob to Admin
    print("\n43. Group Roles: Alice promotes Bob to Admin...")
    status, res = make_request(f"/api/group/{group_id}/role", "PUT", {
        "username": BOB,
        "newRole": "Admin"
    }, token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 44. Group Roles: Bob (Admin) renames the group (should succeed)
    print("\n44. Group Roles: Bob (Admin) renames the group...")
    status, res = make_request(f"/api/group/{group_id}/metadata", "PUT", {
        "newName": "High Security Circle"
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 45. Group Roles: Bob adds Charlie to the group
    print("\n45. Group Roles: Bob (Admin) adds Charlie to the group...")
    status, res = make_request(f"/api/group/{group_id}/member", "POST", {
        "username": CHARLIE
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200

    # 46. Pinned Messages: Bob sends a group message
    print("\n46. Pinned Messages: Bob sends a message to the group...")
    status, res = make_request("/api/message/group", "POST", {
        "groupId": group_id,
        "ciphertext": "secret_circle_message_ciphertext"
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    group_msg_id = res["data"]["id"]

    # 47. Pinned Messages: Charlie (Member) tries to pin Bob's message (should fail)
    print("\n47. Pinned Messages: Charlie (Member) tries to pin group message...")
    status, res = make_request(f"/api/message/{group_msg_id}/pin", "POST", token=charlie_token)
    print(f"Status: {status}")
    assert status == 400
    assert res["isSuccess"] == False

    # 48. Pinned Messages: Bob (Admin) pins the message (should succeed)
    print("\n48. Pinned Messages: Bob (Admin) pins the group message...")
    status, res = make_request(f"/api/message/{group_msg_id}/pin", "POST", token=bob_token)
    print(f"Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # 49. Pinned Messages: Verify IsPinned is true in chat history
    print("\n49. Pinned Messages: Retrieving group history to check IsPinned status...")
    status, res = make_request(f"/api/message/group/{group_id}", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    msg = next(m for m in res["data"] if m["id"] == group_msg_id)
    assert msg["isPinned"] == True

    # 50. Pinned Messages: Fetch pinned messages list for group
    print("\n50. Pinned Messages: Fetching pinned messages for the group...")
    status, res = make_request(f"/api/message/pinned?groupId={group_id}", "GET", token=alice_token)
    print(f"Status: {status}")
    assert status == 200
    assert len(res["data"]) == 1
    assert res["data"][0]["id"] == group_msg_id

    # 51. Pinned Messages: Charlie (Member) tries to unpin message (should fail)
    print("\n51. Pinned Messages: Charlie (Member) tries to unpin message...")
    status, res = make_request(f"/api/message/{group_msg_id}/pin", "DELETE", token=charlie_token)
    print(f"Status: {status}")
    assert status == 400

    # 52. Pinned Messages: Bob (Admin) unpins the message (should succeed)
    print("\n52. Pinned Messages: Bob (Admin) unpins the message...")
    status, res = make_request(f"/api/message/{group_msg_id}/pin", "DELETE", token=bob_token)
    print(f"Status: {status}")
    assert status == 200

    # 53. Group Roles: Charlie (Member) tries to remove Bob from the group (should fail)
    print("\n53. Group Roles: Charlie (Member) tries to remove Bob from group...")
    status, res = make_request(f"/api/group/{group_id}/member", "DELETE", {
        "username": BOB
    }, token=charlie_token)
    print(f"Status: {status}")
    assert status == 400

    # 54. Group Roles: Bob (Admin) removes Charlie from the group (should succeed)
    print("\n54. Group Roles: Bob (Admin) removes Charlie from group...")
    status, res = make_request(f"/api/group/{group_id}/member", "DELETE", {
        "username": CHARLIE
    }, token=bob_token)
    print(f"Status: {status}")
    assert status == 200

    # 55. Conversation Muting: Direct Chat Muting
    print("\n55. Conversation Muting: Direct Chat Muting...")
    # Alice mutes Bob
    status, res = make_request("/api/conversation/mute", "POST", {
        "mutedUsername": BOB,
        "durationMinutes": 10
    }, token=alice_token)
    print(f"Mute Bob Status: {status}")
    assert status == 200
    assert res["isSuccess"] == True

    # Bob sends a message to Alice
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "muted_msg_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_mute",
        "signedPrekeyIdUsed": 100
    }, token=bob_token)
    print(f"Send Muted Message Status: {status}")
    assert status == 200

    # Alice fetches chat history and checks if IsAlertSilenced is True
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    print(f"Get Chat History Status: {status}")
    assert status == 200
    muted_msg = next(m for m in res["data"] if m["ciphertext"] == "muted_msg_ciphertext")
    assert muted_msg["isAlertSilenced"] == True

    # Alice unmutes Bob
    status, res = make_request("/api/conversation/unmute", "POST", {
        "mutedUsername": BOB
    }, token=alice_token)
    print(f"Unmute Bob Status: {status}")
    assert status == 200

    # Bob sends another message
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "unmuted_msg_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_unmute",
        "signedPrekeyIdUsed": 100
    }, token=bob_token)
    print(f"Send Unmuted Message Status: {status}")
    assert status == 200

    # Alice fetches history and verifies IsAlertSilenced is False
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    unmuted_msg = next(m for m in res["data"] if m["ciphertext"] == "unmuted_msg_ciphertext")
    assert unmuted_msg["isAlertSilenced"] == False

    # 56. Conversation Muting: Group Chat Muting
    print("\n56. Conversation Muting: Group Chat Muting...")
    # Alice mutes the group
    status, res = make_request("/api/conversation/mute", "POST", {
        "groupId": group_id,
        "durationMinutes": 10
    }, token=alice_token)
    print(f"Mute Group Status: {status}")
    assert status == 200

    # Bob sends group message
    status, res = make_request("/api/message/group", "POST", {
        "groupId": group_id,
        "ciphertext": "muted_group_msg"
    }, token=bob_token)
    print(f"Send Group Message Status: {status}")
    assert status == 200

    # Alice retrieves group history and checks if IsAlertSilenced is True
    status, res = make_request(f"/api/message/group/{group_id}", "GET", token=alice_token)
    print(f"Get Group History Status: {status}")
    assert status == 200
    group_muted_msg = next(m for m in res["data"] if m["ciphertext"] == "muted_group_msg")
    assert group_muted_msg["isAlertSilenced"] == True

    # Alice unmutes the group
    status, res = make_request("/api/conversation/unmute", "POST", {
        "groupId": group_id
    }, token=alice_token)
    print(f"Unmute Group Status: {status}")
    assert status == 200

    # Alice retrieves group history again and checks if IsAlertSilenced is False
    status, res = make_request(f"/api/message/group/{group_id}", "GET", token=alice_token)
    group_unmuted_msg = next(m for m in res["data"] if m["ciphertext"] == "muted_group_msg")
    assert group_unmuted_msg["isAlertSilenced"] == False

    # 57. Message Retraction: Delete message for everyone
    print("\n57. Message Retraction: Delete message for everyone...")
    # Bob sends a message to Alice
    status, res = make_request("/api/message/send", "POST", {
        "receiverUsername": ALICE,
        "ciphertext": "retract_this_ciphertext",
        "ephemeralKey": "bob_ephemeral_key_retract",
        "signedPrekeyIdUsed": 100
    }, token=bob_token)
    print(f"Send Retract Message Status: {status}")
    assert status == 200
    retract_msg_id = res["data"]["id"]

    # Bob deletes the message for everyone
    status, res = make_request(f"/api/message/{retract_msg_id}", "DELETE", token=bob_token)
    print(f"Delete Message Status: {status}")
    assert status == 200

    # Alice retrieves history; message should NOT be present
    status, res = make_request(f"/api/message/chat/{BOB}", "GET", token=alice_token)
    assert not any(m["id"] == retract_msg_id for m in res["data"])
    print("Retracted message successfully confirmed deleted for recipient.")

    # 58. Prekey Replenishment Alerts
    print("\n58. Prekey Replenishment Alerts...")
    # Get Alice's key status (should have 0 OTPs remaining after Bob vended them)
    status, res = make_request("/api/keys/status", "GET", token=alice_token)
    print(f"Key Status: {status}")
    assert status == 200
    assert res["data"]["remainingOneTimePrekeysCount"] == 0

    # Replenish Alice's prekeys
    status, res = make_request("/api/keys/upload", "POST", {
        "identityKey": "alice_identity_key_base64",
        "signedPrekey": "alice_signed_prekey_base64",
        "signature": "alice_signature_base64",
        "signedPrekeyId": 100,
        "oneTimePrekeys": [
            { "keyId": 1101, "keyData": "alice_otp_1101" },
            { "keyId": 1102, "keyData": "alice_otp_1102" },
            { "keyId": 1103, "keyData": "alice_otp_1103" }
        ]
    }, token=alice_token)
    print(f"Replenish Keys Status: {status}")
    assert status == 200

    # Verify replenished key status count
    status, res = make_request("/api/keys/status", "GET", token=alice_token)
    print(f"Updated Key Status: {status}")
    assert status == 200
    assert res["data"]["remainingOneTimePrekeysCount"] == 3

    # 59. Lockout Protection: Mnemonic Lockout
    print("\n59. Lockout Protection: Mnemonic Lockout...")
    import time
    lockout_user = f"lock_user_{int(time.time())}"
    
    # Register lockout_user
    status, res = make_request("/api/auth/register", "POST", {
        "username": lockout_user,
        "password": "Password123!",
        "displayName": "Lockout User"
    })
    print(f"Register Lockout User Status: {status}")
    assert status == 200
    
    # 4 invalid recovery attempts
    for i in range(1, 5):
        status, res = make_request("/api/auth/recover-password", "POST", {
            "username": lockout_user,
            "mnemonic": "invalid mnemonic phrase that does not match at all",
            "newPassword": "NewPassword123!"
        })
        print(f"Attempt {i} Status: {status}")
        assert status == 400
        assert any(f"{5 - i} attempts remaining" in error for error in res["errors"])

    # 5th attempt triggers 5-minute lockout
    status, res = make_request("/api/auth/recover-password", "POST", {
        "username": lockout_user,
        "mnemonic": "invalid mnemonic phrase that does not match at all",
        "newPassword": "NewPassword123!"
    })
    print(f"Attempt 5 Status: {status}")
    assert status == 400
    assert any("locked out for 5 minutes" in error for error in res["errors"])

    # 6th attempt should fail immediately indicating lockout
    status, res = make_request("/api/auth/recover-password", "POST", {
        "username": lockout_user,
        "mnemonic": "invalid mnemonic phrase that does not match at all",
        "newPassword": "NewPassword123!"
    })
    print(f"Attempt 6 (Locked Out) Status: {status}")
    assert status == 400
    assert any("locked out due to too many failed attempts" in error for error in res["errors"])

    # 60. Inactivity Self-Destruct
    print("\n60. Inactivity Self-Destruct...")
    destruct_user = f"destruct_{int(time.time())}"
    
    # Register destruct_user
    status, res = make_request("/api/auth/register", "POST", {
        "username": destruct_user,
        "password": "Password123!",
        "displayName": "Destruct User"
    })
    print(f"Register Destruct User Status: {status}")
    assert status == 200
    destruct_token = res["data"]["token"]

    # Override LastSeenAt to 200 seconds ago (so it exceeds 180 seconds inactivity limit)
    import datetime
    two_hundred_seconds_ago = (datetime.datetime.utcnow() - datetime.timedelta(seconds=200)).isoformat() + "Z"
    status, res = make_request("/api/profile/update", "PUT", {
        "lastSeenAt": two_hundred_seconds_ago
    }, token=destruct_token)
    print(f"Override LastSeenAt Status: {status}")
    assert status == 200

    # Wait for the background worker to execute self-destruct
    print("Waiting for self-destruct worker to delete user (polling)...")
    deleted = False
    for _ in range(10):
        time.sleep(1.5)
        status, res = make_request("/api/auth/login", "POST", {
            "username": destruct_user,
            "password": "Password123!"
        })
        if status == 400:
            deleted = True
            print(f"Login Destructed User Status: {status} (User successfully deleted)")
            assert res["isSuccess"] == False
            break
        print("User still exists, retrying...")

    assert deleted, "User was not deleted by the background worker within timeout."
    print("Inactivity self-destruct verified successfully.")

    print("\n--- ALL E2EE, PRIVACY, REAL-TIME DELIVERY RECEIPTS, MULTI-DEVICE SYNC, WEBRTC CALL SIGNALING, PHASE 5-10 TESTS PASSED SUCCESSFULLY! ---")

if __name__ == "__main__":
    run_tests()
