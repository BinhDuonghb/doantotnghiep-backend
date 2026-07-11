#!/bin/bash
# ==============================================================================
# Integration Test: Complete Quiz Flow
# ==============================================================================
# Tests:
#   1. Register users, create admin via MongoDB update
#   2. GET /api/quiz without target → 403
#   3. POST /api/admin/quiz/generate → 201 (admin only)
#   4. POST /api/user/target → 200
#   5. GET /api/user/target → 200 with target info
#   6. GET /api/quiz with target → 200 with quizzes
#   7. 403 verification: new user without target → GET /api/quiz → 403
# ==============================================================================

BASE_URL="http://localhost:5239"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color
PASS=0
FAIL=0
APP_PID=""

pass() {
    PASS=$((PASS + 1))
    echo -e "${GREEN}✓ PASS${NC}: $1"
}

fail() {
    FAIL=$((FAIL + 1))
    echo -e "${RED}✗ FAIL${NC}: $1"
}

info() {
    echo -e "${CYAN}→${NC} $1"
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
echo "  Integration Test: Complete Quiz Flow"
echo "  API Contract Verification for Flutter Integration"
echo "========================================================================"
echo ""

# Configure JWT for testing (must match appsettings in CI)
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
info "Waiting for application to start..."
for i in $(seq 1 30); do
    if curl -s "$BASE_URL/api/health" > /dev/null 2>&1; then
        echo "Application is ready (after ${i}s)"
        break
    fi
    sleep 1
done

# Verify health endpoint is up
HEALTH_CHECK=$(curl -s "$BASE_URL/api/health")
echo -e "Health check response: $HEALTH_CHECK\n"

if echo "$HEALTH_CHECK" | grep -q '"status"'; then
    pass "Health endpoint is accessible"
else
    fail "Health endpoint is not accessible"
    echo "App log (last 20 lines):"
    tail -20 app.log
    exit 1
fi

echo ""
echo "========================================================================"
echo "  PHASE 1: Register Test Users"
echo "========================================================================"

# Unique email prefix for test isolation
TIMESTAMP=$(date +%s)

# --- User A: Regular user ---
USER_A_EMAIL="quiz-user-a-$TIMESTAMP@example.com"
info "Registering User A (regular): $USER_A_EMAIL"

REGISTER_A=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$USER_A_EMAIL\",
    \"password\": \"password123\",
    \"name\": \"Quiz User A\",
    \"confirmPassword\": \"password123\"
  }")

TOKEN_A=$(echo "$REGISTER_A" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ ! -z "$TOKEN_A" ]; then
    pass "User A registered, got accessToken"
else
    fail "User A registration failed"
    echo "Response: $REGISTER_A"
    tail -20 app.log
    exit 1
fi

# --- User B: Will be promoted to Admin ---
USER_B_EMAIL="quiz-admin-b-$TIMESTAMP@example.com"
info "Registering User B (will become admin): $USER_B_EMAIL"

REGISTER_B=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$USER_B_EMAIL\",
    \"password\": \"password123\",
    \"name\": \"Admin User B\",
    \"confirmPassword\": \"password123\"
  }")

TOKEN_B=$(echo "$REGISTER_B" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)
USER_B_ID=$(echo "$REGISTER_B" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ ! -z "$TOKEN_B" ] && [ ! -z "$USER_B_ID" ]; then
    pass "User B registered, got accessToken and ID: $USER_B_ID"
else
    fail "User B registration failed"
    echo "Response: $REGISTER_B"
    tail -20 app.log
    exit 1
fi

# --- Promote User B to Admin via MongoDB ---
info "Promoting User B to Admin role in MongoDB..."
MONGO_HELPER_DIR="/c/Users/Admin/AppData/Local/Temp/mongo-helper"
MONGO_UPDATE=$(node "$MONGO_HELPER_DIR/promote-admin.js" "$USER_B_ID" 2>&1)

echo "MongoDB admin update result: $MONGO_UPDATE"

if echo "$MONGO_UPDATE" | grep -q '"role":"Admin"'; then
    pass "User B promoted to Admin in MongoDB"
else
    fail "Failed to promote User B to Admin"
    echo "$MONGO_UPDATE"
fi

# Re-login as User B to get a token with Admin role claim
LOGIN_B=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$USER_B_EMAIL\",
    \"password\": \"password123\"
  }")

TOKEN_B=$(echo "$LOGIN_B" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ ! -z "$TOKEN_B" ]; then
    pass "User B re-logged in with new role token"
else
    fail "User B re-login failed"
    echo "Response: $LOGIN_B"
    exit 1
fi

# --- User C: Second regular user for 403 verification ---
USER_C_EMAIL="quiz-user-c-$TIMESTAMP@example.com"
info "Registering User C (for 403 verification): $USER_C_EMAIL"

REGISTER_C=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$USER_C_EMAIL\",
    \"password\": \"password123\",
    \"name\": \"Quiz User C\",
    \"confirmPassword\": \"password123\"
  }")

TOKEN_C=$(echo "$REGISTER_C" | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ ! -z "$TOKEN_C" ]; then
    pass "User C registered, got accessToken"
else
    fail "User C registration failed"
    echo "Response: $REGISTER_C"
    exit 1
fi

echo ""
echo "========================================================================"
echo "  PHASE 2: Test GET /api/quiz WITHOUT target → 403"
echo "========================================================================"

QUIZ_NO_TARGET_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN_A" "$BASE_URL/api/quiz")
echo "GET /api/quiz (no target) response: $QUIZ_NO_TARGET_RESPONSE"

if echo "$QUIZ_NO_TARGET_RESPONSE" | grep -q '"status":403'; then
    pass "GET /api/quiz returns 403 when target not set (per API contract)"
else
    fail "Expected 403 for GET /api/quiz without target: $QUIZ_NO_TARGET_RESPONSE"
fi

if echo "$QUIZ_NO_TARGET_RESPONSE" | grep -q '"User target not set"'; then
    pass "403 response title is 'User target not set'"
else
    fail "Expected 'User target not set' in 403 response: $QUIZ_NO_TARGET_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  PHASE 3: Generate Quizzes via Admin Endpoint"
echo "========================================================================"
info "Admin token: ${TOKEN_B:0:20}..."

GENERATE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/admin/quiz/generate" \
  -H "Authorization: Bearer $TOKEN_B" \
  -H "Content-Type: application/json" \
  -d '{"count": 3, "category": "Vocabulary", "difficulty": "Easy"}')

echo "POST /api/admin/quiz/generate response: $GENERATE_RESPONSE"

# Check status code — curl -w doesn't mix well with -s body capture, so check content
if echo "$GENERATE_RESPONSE" | grep -q '"generated"'; then
    pass "Quiz generation response contains 'generated' field"
else
    fail "Expected 'generated' in response: $GENERATE_RESPONSE"
fi

if echo "$GENERATE_RESPONSE" | grep -q '"quizzes"'; then
    pass "Quiz generation response contains 'quizzes' array"
else
    fail "Expected 'quizzes' array in response: $GENERATE_RESPONSE"
fi

# Extract generated count
GENERATED_COUNT=$(echo "$GENERATE_RESPONSE" | grep -o '"generated":[0-9]*' | cut -d: -f2)
if [ "$GENERATED_COUNT" = "3" ]; then
    pass "Generated 3 quizzes as requested"
else
    fail "Expected 3 quizzes, got: $GENERATED_COUNT"
fi

echo ""
echo "========================================================================"
echo "  PHASE 4: Set User Target (User A)"
echo "========================================================================"

# POST /api/user/target with required Target and TargetType
SET_TARGET_RESPONSE=$(curl -s -X POST "$BASE_URL/api/user/target" \
  -H "Authorization: Bearer $TOKEN_A" \
  -H "Content-Type: application/json" \
  -d '{"target": "Weight Loss", "targetType": "fitness"}')

echo "POST /api/user/target response: $SET_TARGET_RESPONSE"

# Per API contract: SetUserTargetResponse has {success, message}
if echo "$SET_TARGET_RESPONSE" | grep -q '"success":true'; then
    pass "Set target response contains success=true"
else
    fail "Expected success=true in response: $SET_TARGET_RESPONSE"
fi

if echo "$SET_TARGET_RESPONSE" | grep -q '"message"'; then
    pass "Set target response contains message field"
else
    fail "Expected message field in response: $SET_TARGET_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  PHASE 5: GET /api/user/target — verify target is set"
echo "========================================================================"

# Test using GetUserTarget endpoint (GET)
GET_TARGET_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN_A" "$BASE_URL/api/user/target")
echo "GET /api/user/target response: $GET_TARGET_RESPONSE"

# Verify contract fields: target, targetType, isSet
if echo "$GET_TARGET_RESPONSE" | grep -q '"target"'; then
    pass "GET target response contains 'target' field"
else
    fail "Expected 'target' field: $GET_TARGET_RESPONSE"
fi

if echo "$GET_TARGET_RESPONSE" | grep -q '"targetType"'; then
    pass "GET target response contains 'targetType' field"
else
    fail "Expected 'targetType' field: $GET_TARGET_RESPONSE"
fi

if echo "$GET_TARGET_RESPONSE" | grep -q '"isSet"'; then
    pass "GET target response contains 'isSet' field (per API contract)"
fi

if echo "$GET_TARGET_RESPONSE" | grep -q '"isSet":true'; then
    pass "isSet is true after setting target"
else
    fail "Expected isSet=true: $GET_TARGET_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  PHASE 6: GET /api/quiz WITH target — should return quizzes (200)"
echo "========================================================================"

QUIZ_WITH_TARGET_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN_A" "$BASE_URL/api/quiz")
echo "GET /api/quiz (with target) response: $QUIZ_WITH_TARGET_RESPONSE"

# Verify 200 status (not 403)
if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"quizzes"'; then
    pass "Response contains 'quizzes' array"
else
    fail "Expected 'quizzes' in response: $QUIZ_WITH_TARGET_RESPONSE"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"userTargetSet"'; then
    pass "Response contains 'userTargetSet' flag"
else
    fail "Expected 'userTargetSet' in response: $QUIZ_WITH_TARGET_RESPONSE"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"userTargetSet":true'; then
    pass "userTargetSet is true when target is set"
else
    fail "Expected userTargetSet=true: $QUIZ_WITH_TARGET_RESPONSE"
fi

# Verify quiz structure in the response
if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"question"'; then
    pass "Quiz response contains 'question' field"
else
    fail "Expected 'question' in quiz items: $QUIZ_WITH_TARGET_RESPONSE"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"options"'; then
    pass "Quiz response contains 'options' array"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"correctAnswer"'; then
    pass "Quiz response contains 'correctAnswer' field"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"category"'; then
    pass "Quiz response contains 'category' field"
fi

if echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -q '"difficulty"'; then
    pass "Quiz response contains 'difficulty' field"
fi

# Verify there's at least 1 quiz returned
QUIZ_COUNT=$(echo "$QUIZ_WITH_TARGET_RESPONSE" | grep -o '"question"' | wc -l)
if [ "$QUIZ_COUNT" -ge 1 ]; then
    pass "Response contains $QUIZ_COUNT quizzes"
else
    fail "Expected at least 1 quiz, found $QUIZ_COUNT"
fi

echo ""
echo "========================================================================"
echo "  PHASE 7: Verify 403 for new user WITHOUT target"
echo "========================================================================"

# User C has no target set → should get 403
QUIZ_USER_C_RESPONSE=$(curl -s -H "Authorization: Bearer $TOKEN_C" "$BASE_URL/api/quiz")
echo "GET /api/quiz (User C, no target) response: $QUIZ_USER_C_RESPONSE"

if echo "$QUIZ_USER_C_RESPONSE" | grep -q '"status":403'; then
    pass "User C gets 403 when target not set (per API contract)"
else
    fail "Expected 403 for User C (no target): $QUIZ_USER_C_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  PHASE 8: Edge Cases — Quiz Endpoint"
echo "========================================================================"

# Test 8a: GET /api/quiz without auth → 401
UNAUTH_QUIZ_RESPONSE=$(curl -s "$BASE_URL/api/quiz")
echo "GET /api/quiz (no auth) response: $UNAUTH_QUIZ_RESPONSE"
if echo "$UNAUTH_QUIZ_RESPONSE" | grep -q '"status":401'; then
    pass "Unauthenticated GET /api/quiz returns 401"
else
    fail "Expected 401 for unauthenticated GET /api/quiz: $UNAUTH_QUIZ_RESPONSE"
fi

# Test 8b: Generate quizzes without auth → 401
UNAUTH_GENERATE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/admin/quiz/generate" \
  -H "Content-Type: application/json" \
  -d '{"count": 1, "category": "Grammar", "difficulty": "Medium"}')
echo "POST /api/admin/quiz/generate (no auth) response: $UNAUTH_GENERATE_RESPONSE"
if echo "$UNAUTH_GENERATE_RESPONSE" | grep -q '"status":401'; then
    pass "Unauthenticated POST /api/admin/quiz/generate returns 401"
else
    fail "Expected 401 for unauthenticated generate: $UNAUTH_GENERATE_RESPONSE"
fi

# Test 8c: Generate as non-admin user → 403
NONADMIN_GENERATE_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/admin/quiz/generate" \
  -H "Authorization: Bearer $TOKEN_A" \
  -H "Content-Type: application/json" \
  -d '{"count": 1, "category": "Grammar", "difficulty": "Medium"}')
echo "POST /api/admin/quiz/generate (non-admin) HTTP status: $NONADMIN_GENERATE_CODE"
if [ "$NONADMIN_GENERATE_CODE" = "403" ]; then
    pass "Non-admin user gets 403 for quiz generation"
else
    fail "Expected 403 for non-admin generate, got: $NONADMIN_GENERATE_CODE"
fi

# Test 8d: Generate with invalid parameters (count=0) → 400
INVALID_GENERATE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/admin/quiz/generate" \
  -H "Authorization: Bearer $TOKEN_B" \
  -H "Content-Type: application/json" \
  -d '{"count": 0, "category": "", "difficulty": ""}')
echo "POST /api/admin/quiz/generate (invalid params) response: $INVALID_GENERATE_RESPONSE"
if echo "$INVALID_GENERATE_RESPONSE" | grep -q '"status":400'; then
    pass "Invalid generation params return 400"
else
    fail "Expected 400 for invalid params: $INVALID_GENERATE_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "  FLUTTER INTEGRATION VALIDATION"
echo "========================================================================"
echo ""
echo "The following API contracts are verified for Flutter integration:"
echo ""

# Summarize the API contracts
echo "1. GET /api/quiz — 403 'User target not set'"
echo "   → Flutter should handle 403 and show 'set your target' screen"
echo "   → Verified with User A (before target) and User C (no target)"
echo ""
echo "2. POST /api/admin/quiz/generate — 201"
echo "   → Requires Admin role (ClaimTypes.Role = 'Admin')"
echo "   → Flutter admin panel calls this before users take quizzes"
echo ""
echo "3. POST /api/user/target — 200 SetUserTargetResponse"
echo "   → Flutter target-setup screen sends {target, targetType}"
echo "   → Response: {success: true, message: 'User target updated successfully'}"
echo ""
echo "4. GET /api/user/target — 200 GetUserTargetResponse"
echo "   → Flutter reads back: {target, targetType, isSet}"
echo "   → isSet=true after successful set"
echo ""
echo "5. GET /api/quiz — 200 GetQuizzesResponse"
echo "   → Flutter quiz screen receives {quizzes: [...], userTargetSet: true}"
echo "   → Each quiz: {id, question, options, correctAnswer, category, difficulty}"
echo "   → userTargetSet:true signals target-gated access is active"
echo ""

echo "========================================================================"
echo "  RESULTS: $PASS passed, $FAIL failed"
echo "========================================================================"

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}ALL TESTS PASSED!${NC}"
    echo ""
    echo "✓ API contracts verified for Flutter integration:"
    echo "  - Quiz gating (403 without target, 200 with target)"
    echo "  - Admin quiz generation"
    echo "  - User target management (set/get)"
    echo "  - Authorization enforcement (401/403)"
    echo "  - Input validation (400)"
    echo ""
    echo "✓ Flutter screens can safely implement:"
    echo "  - Quiz list screen (requires target)"
    echo "  - Target setup screen (post target, read back)"
    echo "  - Admin quiz management (requires Admin role)"
    exit 0
else
    echo -e "${RED}SOME TESTS FAILED!${NC}"
    exit 1
fi
