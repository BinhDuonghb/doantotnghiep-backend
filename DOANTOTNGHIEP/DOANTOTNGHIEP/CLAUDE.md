# Agent: backend
# Task: Verify all auth API contracts work correctly: test register, login, token refresh, protected endpoint access, and logout from Flutter app running on Android emulator.

=== Feature ===
JWT Authentication with MongoDB

=== Shared API Contracts ===
[
  {
    "name": "Register",
    "endpoint": "/api/auth/register",
    "method": "POST",
    "request": {
      "email": "string",
      "password": "string",
      "fullName": "string"
    },
    "response": {
      "user": {
        "id": "string",
        "email": "string",
        "fullName": "string",
        "createdAt": "string"
      },
      "accessToken": "string",
      "refreshToken": "string"
    },
    "success_status_code": 201,
    "error_status_codes": {
      "400": "Validation error",
      "409": "Email already exists"
    }
  },
  {
    "name": "Login",
    "endpoint": "/api/auth/login",
    "method": "POST",
    "request": {
      "email": "string",
      "password": "string"
    },
    "response": {
      "user": {
        "id": "string",
        "email": "string",
        "fullName": "string",
        "createdAt": "string"
      },
      "accessToken": "string",
      "refreshToken": "string"
    },
    "success_status_code": 200,
    "error_status_codes": {
      "400": "Validation error",
      "401": "Invalid credentials"
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
      "400": "Invalid or expired refresh token",
      "401": "Refresh token revoked"
    }
  },
  {
    "name": "GetCurrentUser",
    "endpoint": "/api/auth/me",
    "method": "GET",
    "request": null,
    "response": {
      "id": "string",
      "email": "string",
      "fullName": "string",
      "createdAt": "string"
    },
    "success_status_code": 200,
    "error_status_codes": {
      "401": "Unauthorized - invalid or missing access token"
    }
  },
  {
    "name": "Logout",
    "endpoint": "/api/auth/logout",
    "method": "POST",
    "request": {
      "refreshToken": "string"
    },
    "response": {
      "message": "Successfully logged out"
    },
    "success_status_code": 200,
    "error_status_codes": {
      "400": "Invalid refresh token"
    }
  }
]
# Generated: 2026-07-06T04:13:10.500976

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
    - HealthController.cs
  - Data
  - Models
    - DTOs
      - HealthCheckResponse.cs
  - Services
    - HealthCheckService.cs

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