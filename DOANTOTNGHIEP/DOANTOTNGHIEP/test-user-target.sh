#!/bin/bash

# ==============================================================================
# Integration Test: User Target Management API
# ==============================================================================
# Tests:
#   1. Login → receive accessToken
#   2. GET /api/user/target (no target set) → 200, hasTarget=false, target=null
#   3. PUT /api/user/target → 200, target="Weight Loss", message
#   4. GET /api/user/target (after update) → 200, hasTarget=true, target persisted
#   5. PUT /api/user/target with empty string → 400
#   6. GET /api/user/target without auth → 401
#   7. PUT /api/user/target without auth → 401
#   8. GET /api/user/target with garbage token → 401
# ==============================================================================

BASE_URL="http://localhost:5239"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color
PASS=0
FAIL=0

pass() {
    PASS=$((PASS + 1))
    echo -e "${GREEN}✓ PASS${NC}: $1"
}

fail() {
    FAIL=$((FAIL + 1))
    echo -e "${RED}✗ FAIL${NC}: $1"
}

cleanup() {
    if [ ! -z "$APP_PID" ] && kill -0 $APP_PID 2>/dev/null; then
        echo -e "${YELLOW}Stopping application (PID: $APP_PID)...${NC}"
        kill $APP_PID 2>/dev/null
        wait $APP_PID 2>/dev/null
    fi
}

# Ensure cleanup on exit
trap cleanup EXIT

echo "========================================================================"
echo "  Integration Test: User Target Management API"
echo "========================================================================"
echo ""

# Configure JWT for testing
export Jwt__Key="test-target-integration-secret-key-123456"
export Jwt__Issuer="DoAnTotNghiep"
export Jwt__Audience="DoAnTotNghiepUsers"
export Jwt__AccessTokenExpirationMinutes=30
export Jwt__RefreshTokenExpirationDays=1

# Start the application in the background
echo -e "${YELLOW}Starting the application...${NC}"
dotnet run > app.log 2>&1 &
APP_PID=$!

# Wait for the application to start
echo "Waiting for application to start..."
for i in $(seq 1 30); do
    if curl -s "$BASE_URL/api/health" > /dev/null 2>&1; then
        echo "Application is ready (after ${i}s)"
        break
    fi
    sleep 1
done

# Verify health endpoint is up
HEALTH_CHECK=$(curl -s "$BASE_URL/api/health")
echo "Health check response: $HEALTH_CHECK"

if echo "$HEALTH_CHECK" | grep -q '"status"'; then
    pass "Health endpoint is accessible"
else
    fail "Health endpoint is not accessible"
    echo "App log (last 20 lines):"
    tail -20 app.log
    exit 1
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 1: Register and get access token"
echo "------------------------------------------------------------------------"

# Use a unique email to avoid conflicts with existing test data
TEST_EMAIL="target-test-$(date +%s)@example.com"

REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$TEST_EMAIL\",
    \"password\": \"password123\",
    \"name\": \"Target Test User\",
    \"confirmPassword\": \"password123\"
  }")

echo "Register response: $REGISTER_RESPONSE"

ACCESS_TOKEN=$(echo "$REGISTER_RESPONSE" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ ! -z "$ACCESS_TOKEN" ]; then
    pass "Registration succeeded, got accessToken"
else
    fail "Registration failed, no accessToken"
    exit 1
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 2: GET /api/user/target — no target set yet"
echo "------------------------------------------------------------------------"

GET_TARGET_RESPONSE=$(curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$BASE_URL/api/user/target")
echo "GET /api/user/target response: $GET_TARGET_RESPONSE"

# Verify contract: response must contain "target" and "hasTarget" fields
if echo "$GET_TARGET_RESPONSE" | grep -q '"target"'; then
    pass "Response contains 'target' field"
else
    fail "Response missing 'target' field: $GET_TARGET_RESPONSE"
fi

if echo "$GET_TARGET_RESPONSE" | grep -q '"hasTarget"'; then
    pass "Response contains 'hasTarget' field"
else
    fail "Response missing 'hasTarget' field: $GET_TARGET_RESPONSE"
fi

# New user should have hasTarget=false
if echo "$GET_TARGET_RESPONSE" | grep -q '"hasTarget":false'; then
    pass "New user has hasTarget=false"
else
    fail "Expected hasTarget=false for new user: $GET_TARGET_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 3: PUT /api/user/target — set target to 'Weight Loss'"
echo "------------------------------------------------------------------------"

PUT_TARGET_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/user/target" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target": "Weight Loss"}')

echo "PUT /api/user/target response: $PUT_TARGET_RESPONSE"

# Verify contract: response must contain "target" and "message"
if echo "$PUT_TARGET_RESPONSE" | grep -q '"target":"Weight Loss"'; then
    pass "Response contains target='Weight Loss'"
else
    fail "Expected target='Weight Loss' in response: $PUT_TARGET_RESPONSE"
fi

if echo "$PUT_TARGET_RESPONSE" | grep -q '"message":"Target updated successfully"'; then
    pass "Response contains success message per API contract"
else
    fail "Expected success message in response: $PUT_TARGET_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 4: GET /api/user/target — verify target persisted"
echo "------------------------------------------------------------------------"

GET_TARGET_AFTER_RESPONSE=$(curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$BASE_URL/api/user/target")
echo "GET /api/user/target (after update) response: $GET_TARGET_AFTER_RESPONSE"

# Verify target persisted in database
if echo "$GET_TARGET_AFTER_RESPONSE" | grep -q '"target":"Weight Loss"'; then
    pass "Target persisted: GET returns target='Weight Loss'"
else
    fail "Target did not persist: $GET_TARGET_AFTER_RESPONSE"
fi

if echo "$GET_TARGET_AFTER_RESPONSE" | grep -q '"hasTarget":true'; then
    pass "hasTarget is true after setting target"
else
    fail "Expected hasTarget=true after update: $GET_TARGET_AFTER_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 5: PUT /api/user/target — empty target should return 400"
echo "------------------------------------------------------------------------"

EMPTY_TARGET_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/user/target" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target": ""}')

echo "PUT /api/user/target with empty target response: $EMPTY_TARGET_RESPONSE"

if echo "$EMPTY_TARGET_RESPONSE" | grep -q '"status":400'; then
    pass "Empty target returns 400 per API contract"
else
    fail "Expected 400 for empty target: $EMPTY_TARGET_RESPONSE"
fi

if echo "$EMPTY_TARGET_RESPONSE" | grep -q '"Invalid target value"'; then
    pass "400 response title is 'Invalid target value'"
else
    fail "Expected 'Invalid target value' title: $EMPTY_TARGET_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 6: PUT /api/user/target — whitespace-only target should return 400"
echo "------------------------------------------------------------------------"

WHITESPACE_TARGET_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/user/target" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target": "   "}')

echo "PUT /api/user/target with whitespace-only target response: $WHITESPACE_TARGET_RESPONSE"

if echo "$WHITESPACE_TARGET_RESPONSE" | grep -q '"status":400'; then
    pass "Whitespace-only target returns 400"
else
    fail "Expected 400 for whitespace-only target: $WHITESPACE_TARGET_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 7: GET /api/user/target without auth token → 401"
echo "------------------------------------------------------------------------"

UNAUTH_GET_RESPONSE=$(curl -s "$BASE_URL/api/user/target")
echo "GET /api/user/target (no auth) response: $UNAUTH_GET_RESPONSE"

if echo "$UNAUTH_GET_RESPONSE" | grep -q '"status":401'; then
    pass "Unauthenticated GET returns 401 per API contract"
else
    fail "Expected 401 for unauthenticated GET: $UNAUTH_GET_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 8: PUT /api/user/target without auth token → 401"
echo "------------------------------------------------------------------------"

UNAUTH_PUT_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/user/target" \
  -H "Content-Type: application/json" \
  -d '{"target": "Muscle Gain"}')

echo "PUT /api/user/target (no auth) response: $UNAUTH_PUT_RESPONSE"

if echo "$UNAUTH_PUT_RESPONSE" | grep -q '"status":401'; then
    pass "Unauthenticated PUT returns 401 per API contract"
else
    fail "Expected 401 for unauthenticated PUT: $UNAUTH_PUT_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 9: GET /api/user/target with garbage token → 401"
echo "------------------------------------------------------------------------"

GARBAGE_TOKEN_RESPONSE=$(curl -s -H "Authorization: Bearer garbage-token-12345" "$BASE_URL/api/user/target")
echo "GET /api/user/target (garbage token) response: $GARBAGE_TOKEN_RESPONSE"

if echo "$GARBAGE_TOKEN_RESPONSE" | grep -q '"status":401'; then
    pass "Garbage token returns 401 per API contract"
else
    fail "Expected 401 for garbage token: $GARBAGE_TOKEN_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 10: PUT /api/user/target — update to a different value"
echo "------------------------------------------------------------------------"

PUT_NEW_TARGET_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/user/target" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"target": "Muscle Gain"}')

echo "PUT /api/user/target (update to Muscle Gain) response: $PUT_NEW_TARGET_RESPONSE"

if echo "$PUT_NEW_TARGET_RESPONSE" | grep -q '"target":"Muscle Gain"'; then
    pass "Target updated to 'Muscle Gain'"
else
    fail "Expected target='Muscle Gain': $PUT_NEW_TARGET_RESPONSE"
fi

# Verify the new value persisted
GET_TARGET_FINAL_RESPONSE=$(curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$BASE_URL/api/user/target")
echo "GET /api/user/target (final check) response: $GET_TARGET_FINAL_RESPONSE"

if echo "$GET_TARGET_FINAL_RESPONSE" | grep -q '"target":"Muscle Gain"'; then
    pass "Updated target 'Muscle Gain' persisted in database"
else
    fail "Updated target did not persist: $GET_TARGET_FINAL_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  RESULTS: $PASS passed, $FAIL failed"
echo "========================================================================"

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}ALL TESTS PASSED!${NC}"
    echo ""
    echo "✓ API contracts verified for Flutter integration:"
    echo "  - GET /api/user/target returns {target, hasTarget} — 200"
    echo "  - PUT /api/user/target with {target} returns {target, message} — 200"
    echo "  - Empty/whitespace target returns 400 'Invalid target value'"
    echo "  - Unauthenticated requests return 401"
    echo "  - Target persists across requests (MongoDB persistence verified)"
    exit 0
else
    echo -e "${RED}SOME TESTS FAILED!${NC}"
    exit 1
fi