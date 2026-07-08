#!/bin/bash

# ==============================================================================
# Integration Test: Token-based Authentication Flow
# ==============================================================================
# Tests:
#   1. Login → receive accessToken, refreshToken, expiresIn, user
#   2. Make authenticated request with access token → 200
#   3. Receive 401 after token expiry
#   4. Refresh token successfully → get new token pair
#   5. Retry original request with new access token → 200
#   6. Refresh token rotation → old refresh token invalidated
#   7. Failure case: using invalid/expired refresh token → 401
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
echo "  Integration Test: Refresh Token Authentication Flow"
echo "========================================================================"
echo ""

# For testing short-lived tokens, override JWT config via env vars
# AccessTokenExpirationMinutes=1 gives us 60-second tokens for the expiry test
export Jwt__AccessTokenExpirationMinutes=1
export Jwt__RefreshTokenExpirationDays=1
export Jwt__Key="test-integration-secret-key-1234567890"
export Jwt__Issuer="DoAnTotNghiep"
export Jwt__Audience="DoAnTotNghiepUsers"

# Start the application in the background
echo -e "${YELLOW}Starting the application with short-lived tokens (1 min access, 1 day refresh)...${NC}"
dotnet run > app.log 2>&1 &
APP_PID=$!

# Wait for the application to start
echo "Waiting for application to start..."
for i in $(seq 1 30); do
    if curl -s "$BASE_URL/swagger/v1/swagger.json" > /dev/null 2>&1; then
        echo "Application is ready (after ${i}s)"
        break
    fi
    sleep 1
done

# Verify Swagger is up
if curl -s "$BASE_URL/swagger/v1/swagger.json" | grep -q "DOANTOTNGHIEP API"; then
    pass "Swagger JSON is accessible"
else
    fail "Swagger JSON is not accessible"
    exit 1
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 1: Register a new user"
echo "------------------------------------------------------------------------"

REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "integration-test@example.com",
    "password": "password123",
    "name": "Integration Test User",
    "confirmPassword": "password123"
  }')

echo "Register response: $REGISTER_RESPONSE"

# Check registration succeeded
if echo "$REGISTER_RESPONSE" | grep -q '"accessToken"'; then
    pass "Registration returned accessToken"
else
    fail "Registration did not return accessToken"
fi

if echo "$REGISTER_RESPONSE" | grep -q '"refreshToken"'; then
    pass "Registration returned refreshToken"
else
    fail "Registration did not return refreshToken"
fi

if echo "$REGISTER_RESPONSE" | grep -q '"expiresIn"'; then
    pass "Registration returned expiresIn"
else
    fail "Registration did not return expiresIn"
fi

if echo "$REGISTER_RESPONSE" | grep -q '"user"'; then
    pass "Registration returned user info"
else
    fail "Registration did not return user info"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 2: Login with valid credentials"
echo "------------------------------------------------------------------------"

LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "integration-test@example.com",
    "password": "password123"
  }')

echo "Login response: $LOGIN_RESPONSE"

# Extract tokens from login response
ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)
REFRESH_TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"refreshToken":"[^"]*"' | cut -d'"' -f4)
EXPIRES_IN=$(echo "$LOGIN_RESPONSE" | grep -o '"expiresIn":[0-9]*' | cut -d':' -f2)

if [ ! -z "$ACCESS_TOKEN" ]; then
    pass "Login returned accessToken"
else
    fail "Login did not return accessToken"
fi

if [ ! -z "$REFRESH_TOKEN" ]; then
    pass "Login returned refreshToken"
else
    fail "Login did not return refreshToken"
fi

if [ ! -z "$EXPIRES_IN" ] && [ "$EXPIRES_IN" -gt 0 ]; then
    pass "Login returned expiresIn ($EXPIRES_IN seconds)"
else
    fail "Login did not return valid expiresIn"
fi

if echo "$LOGIN_RESPONSE" | grep -q '"email":"integration-test@example.com"'; then
    pass "Login returned correct user info"
else
    fail "Login did not return correct user info"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 3: Make authenticated request with access token"
echo "------------------------------------------------------------------------"

ME_RESPONSE=$(curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$BASE_URL/api/auth/me")
echo "GET /api/auth/me response: $ME_RESPONSE"

if echo "$ME_RESPONSE" | grep -q '"email":"integration-test@example.com"'; then
    pass "Authenticated request succeeded (200 with correct user)"
else
    fail "Authenticated request failed: $ME_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 4: Receive 401 after token expiry"
echo "------------------------------------------------------------------------"

echo "Waiting 65 seconds for access token to expire (token lifetime: 60s)..."
sleep 65

EXPIRED_ME_RESPONSE=$(curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$BASE_URL/api/auth/me")
echo "GET /api/auth/me with expired token response: $EXPIRED_ME_RESPONSE"

# The response should be a 401 (returned as a JSON ProblemDetails)
if echo "$EXPIRED_ME_RESPONSE" | grep -q '"status":401'; then
    pass "Expired access token returns 401"
else
    fail "Expired access token should return 401 but got: $EXPIRED_ME_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 5: Refresh token successfully → get new token pair"
echo "------------------------------------------------------------------------"

REFRESH_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\": \"$REFRESH_TOKEN\"}")

echo "Refresh response: $REFRESH_RESPONSE"

NEW_ACCESS_TOKEN=$(echo "$REFRESH_RESPONSE" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)
NEW_REFRESH_TOKEN=$(echo "$REFRESH_RESPONSE" | grep -o '"refreshToken":"[^"]*"' | cut -d'"' -f4)
NEW_EXPIRES_IN=$(echo "$REFRESH_RESPONSE" | grep -o '"expiresIn":[0-9]*' | cut -d':' -f2)

if [ ! -z "$NEW_ACCESS_TOKEN" ]; then
    pass "Refresh returned new accessToken"
else
    fail "Refresh did not return new accessToken: $REFRESH_RESPONSE"
fi

if [ ! -z "$NEW_REFRESH_TOKEN" ]; then
    pass "Refresh returned new refreshToken (token rotation)"
else
    fail "Refresh did not return new refreshToken"
fi

if [ ! -z "$NEW_EXPIRES_IN" ] && [ "$NEW_EXPIRES_IN" -gt 0 ]; then
    pass "Refresh returned expiresIn ($NEW_EXPIRES_IN seconds)"
else
    fail "Refresh did not return valid expiresIn"
fi

# Verify tokens are actually different (rotation)
if [ "$ACCESS_TOKEN" != "$NEW_ACCESS_TOKEN" ]; then
    pass "New access token is different from old one"
else
    fail "New access token should be different from old one"
fi

if [ "$REFRESH_TOKEN" != "$NEW_REFRESH_TOKEN" ]; then
    pass "New refresh token is different from old one (rotation)"
else
    fail "New refresh token should be different from old one"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 6: Retry original request with new access token → 200"
echo "------------------------------------------------------------------------"

RETRY_ME_RESPONSE=$(curl -s -H "Authorization: Bearer $NEW_ACCESS_TOKEN" "$BASE_URL/api/auth/me")
echo "GET /api/auth/me with new token response: $RETRY_ME_RESPONSE"

if echo "$RETRY_ME_RESPONSE" | grep -q '"email":"integration-test@example.com"'; then
    pass "Retry with new access token succeeded (200 with correct user)"
else
    fail "Retry with new access token failed: $RETRY_ME_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 7: Refresh token rotation — old refresh token is invalidated"
echo "------------------------------------------------------------------------"

ROTATION_TEST_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\": \"$REFRESH_TOKEN\"}")

echo "Refresh with OLD (revoked) refresh token response: $ROTATION_TEST_RESPONSE"

if echo "$ROTATION_TEST_RESPONSE" | grep -q '"status":401'; then
    pass "Old refresh token was invalidated after rotation (401)"
else
    fail "Old refresh token should return 401 after rotation but got: $ROTATION_TEST_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 8: Failure case — using garbage refresh token"
echo "------------------------------------------------------------------------"

GARBAGE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "this-is-a-completely-made-up-token"}')

echo "Refresh with garbage token response: $GARBAGE_RESPONSE"

if echo "$GARBAGE_RESPONSE" | grep -q '"status":401'; then
    pass "Garbage refresh token returns 401"
else
    fail "Garbage refresh token should return 401 but got: $GARBAGE_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 9: Failure case — missing refresh token in request body"
echo "------------------------------------------------------------------------"

MISSING_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d '{}')

echo "Refresh with missing refreshToken response: $MISSING_RESPONSE"

if echo "$MISSING_RESPONSE" | grep -q '"status":400'; then
    pass "Missing refreshToken returns 400 (validation error)"
else
    fail "Missing refreshToken should return 400 but got: $MISSING_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 10: Failure case — login with wrong password"
echo "------------------------------------------------------------------------"

WRONG_LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "integration-test@example.com",
    "password": "wrongpassword"
  }')

echo "Login with wrong password response: $WRONG_LOGIN_RESPONSE"

if echo "$WRONG_LOGIN_RESPONSE" | grep -q '"status":401'; then
    pass "Wrong password returns 401"
else
    fail "Wrong password should return 401 but got: $WRONG_LOGIN_RESPONSE"
fi

echo ""
echo "------------------------------------------------------------------------"
echo "  TEST 11: Logout revokes refresh token"
echo "------------------------------------------------------------------------"

LOGOUT_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/logout" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\": \"$NEW_REFRESH_TOKEN\"}")

echo "Logout response: $LOGOUT_RESPONSE"

if echo "$LOGOUT_RESPONSE" | grep -q '"message":"Successfully logged out"'; then
    pass "Logout succeeded"
else
    fail "Logout failed: $LOGOUT_RESPONSE"
fi

# Try to use the revoked refresh token
AFTER_LOGOUT_REFRESH=$(curl -s -X POST "$BASE_URL/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\": \"$NEW_REFRESH_TOKEN\"}")

echo "Refresh after logout response: $AFTER_LOGOUT_REFRESH"

if echo "$AFTER_LOGOUT_REFRESH" | grep -q '"status":401'; then
    pass "Revoked refresh token (after logout) returns 401"
else
    fail "Revoked refresh token should return 401 but got: $AFTER_LOGOUT_REFRESH"
fi

echo ""
echo "========================================================================"
echo "  RESULTS: $PASS passed, $FAIL failed"
echo "========================================================================"

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}ALL TESTS PASSED!${NC}"
    exit 0
else
    echo -e "${RED}SOME TESTS FAILED!${NC}"
    exit 1
fi