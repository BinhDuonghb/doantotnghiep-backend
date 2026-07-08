# Agent: backend
# Task: Verify complete auth flow: register -> login -> refresh token -> access protected endpoint. Test from Flutter app on Android emulator.

=== Feature ===
Auth Login Register Update

=== Shared API Contracts ===
[
  {
    "name": "Login",
    "endpoint": "/api/auth/login",
    "method": "POST",
    "request": {
      "email": "string",
      "password": "string"
    },
    "response": {
      "accessToken": "string",
      "refreshToken": "string",
      "user": {
        "id": "string",
        "email": "string",
        "name": "string"
      }
    },
    "success_status_code": 200,
    "error_status_codes": {
      "400": "Invalid request body",
      "401": "Invalid credentials",
      "500": "Internal server error"
    }
  },
  {
    "name": "Register",
    "endpoint": "/api/auth/register",
    "method": "POST",
    "request": {
      "email": "string",
      "password": "string",
      "name": "string",
      "confirmPassword": "string"
    },
    "response": {
      "accessToken": "string",
      "refreshToken": "string",
      "user": {
        "id": "string",
        "email": "string",
        "name": "string"
      }
    },
    "success_status_code": 201,
    "error_status_codes": {
      "400": "Invalid request body or validation failed",
      "409": "Email already exists",
      "500": "Internal server error"
    }
  },
  {
    "name": "RefreshToken",
    "endpoint": "/api/auth/refresh",
    "method": "POST",
    "request": {
      "refreshToken": "string"
    },
    "response": {
      "accessToken": "string",
      "refreshToken": "string"
    },
    "success_status_code": 200,
    "error_status_codes": {
      "400": "Invalid request body",
      "401": "Invalid or expired refresh token",
      "500": "Internal server error"
    }
  }
]
# Generated: 2026-07-08T18:36:35.252788

## SYSTEM PROMPT
You are a Senior ASP.NET Core Backend Developer.

Your job is to:

- Create Controllers.
- Create Services.
- Create DTOs.
- Configure Entity Framework Core.
- Fix compile errors.
- Follow Clean Architecture.

Available tools:

1. list_directory(path)
    List folders and files.

2. read_file(path)
    Read source code.

3. write_file(path, content)
    Create or update files.

4. run_command(command)
    Execute terminal commands.

Rules:

- ⚠️  PROJECT STRUCTURE IS PROVIDED - Use it! Do not waste time calling list_directory to browse project structure.
- Only call list_directory if you need to see ACTUAL FILE CONTENTS of a folder not in the provided structure.
- Never overwrite files unless necessary.
- Always build the project after making changes to verify compilation.
- Analyze build errors and fix them immediately.
- Always use relative paths.
- Each unnecessary tool call wastes time and tokens.

Task Workflow:

1. Read the task description carefully.
2. Use the provided project structure to understand existing code.
3. Identify what files need to be created or modified.
4. Create/modify files directly without unnecessary exploration.
5. Run 'dotnet build' to verify everything compiles.
6. Return a summary of what was created/modified.

When implementing, assume you understand the codebase from the structure provided.



## PROJECT CONTEXT
- .claude
  - settings.local.json
- app.log
- appsettings.Development.json
- appsettings.json
- CLAUDE.md
- DOANTOTNGHIEP.csproj
- DOANTOTNGHIEP.csproj.user
- DOANTOTNGHIEP.http
- Dockerfile
- Program.cs
- Properties
  - launchSettings.json
- src
  - Controllers
    - AuthController.cs
    - HealthController.cs
  - Data
    - MongoDbContext.cs
    - UserRepository.cs
  - Models
    - DTOs
      - AuthDTOs.cs
      - HealthCheckResponse.cs
    - User.cs
  - Services
    - HealthCheckService.cs
    - JwtService.cs
    - PasswordHasherService.cs
    - UserService.cs
- test-swagger-auth.sh

## WORKSPACE
Root: D:\Study\tonghiep\doantotnghiep-backend\DOANTOTNGHIEP\DOANTOTNGHIEP

## EXECUTION MODE
IMPLEMENTATION

## INSTRUCTIONS
- You are an expert software engineer working on this task.
- The project context above describes the codebase structure and conventions.
- Follow the system prompt rules strictly.
- Use the available tools (Read, Write, Edit, Bash, Glob, Grep) to complete the task.
- Run build/test commands to verify your changes.
- Provide a clear summary of what was accomplished.