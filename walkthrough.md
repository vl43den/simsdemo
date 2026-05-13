# SIMS AWS Integration & Fixes — Walkthrough

The SIMS application has been successfully modernized for AWS with all required cloud-native integrations.

## Changes Made

### 1. Backend: Redis TLS Fix
- **File:** [RedisDB.cs](file:///c:/Users/bob/Desktop/simsdemo/SIMSAPI/SIMSAPI/RedisDB.cs)
- **Change:** Added `sslprotocols=tls12|tls13` to the connection string to support AWS ElastiCache (Valkey) with Transit Encryption. This fixed the login crash.

### 2. Frontend: Environment Variable Alignment
- **ECS Task Definition:** Updated to Revision 2.
- **Change:** Renamed the database secret from `postgres` to `postgresdb` to match the application's internal expected variable name. This fixed the Incidents page crash.

### 2.1 Backend: Environment Variable Fallback Fix
- **Files:** [AuthService.cs](file:///c:/Users/bob/Desktop/simsdemo/SIMSAPI/SIMSAPI/Controllers/AuthService.cs), [Program.cs](file:///c:/Users/bob/Desktop/simsdemo/SIMSAPI/SIMSAPI/Program.cs), [RedisDB.cs](file:///c:/Users/bob/Desktop/simsdemo/SIMSAPI/SIMSAPI/RedisDB.cs)
- **Change:** Made the API backend robust to varying environment variable names. `AuthService.cs` now correctly falls back to checking the `postgres` environment variable if `postgresdb` is missing. A similar fallback was implemented for `redisdb` / `redis`. This fixed the "Wrong data!" login error in the deployed AWS environment caused by the backend failing to connect to the database.

### 3. Lambda Integration (Escalate)
- **AWS Infrastructure:** Created a **Lambda Function URL** for the `sims-escalate` function.
- **Frontend Link:** Added the `ESCALATE_API_URL` environment variable to the Frontend ECS Task Definition (Revision 3).
- **Result:** The "Escalate" button now successfully triggers the Lambda to stop EC2 resources and send SNS alerts.

### 4. CI/CD Pipeline
- **File:** [.github/workflows/deploy.yml](file:///c:/Users/bob/Desktop/simsdemo/.github/workflows/deploy.yml)
- **Features:**
    - Triggers on push to `main`.
    - Builds both Frontend and API Docker images.
    - Pushes images to Amazon ECR.
    - Forces a new deployment to Amazon ECS.
    
### 5. Security Hardening (Uni Project Ready)
- **API Security:** Credentials are now sent in the **JSON request body** instead of the URL query string, preventing exposure in server logs.
- **Async/Await:** Refactored the entire `BusinessLayer` and `AuthService` to use asynchronous calls, improving scalability and responsiveness.
- **SQL Injection Prevention:** Parameterized all SQL queries in `Incident.cs` and `User.cs` to defend against database attacks.
- **Robust URL Handling:** Replaced manual string replacement with standard parameter binding in `Helper.cs`.

## Live Demo Test Scenario (for the Exam)

Use this real instance found in your account for a successful live demonstration:

- **Test Resource ID:** `i-026cdafbb6e41e409` (Instance Name: `s3-uebung`)

### Step-by-Step Demo Guide

1.  **Login:**
    - Go to [https://sims.ustpcloud01.lab1159.org/](https://sims.ustpcloud01.lab1159.org/)
    - Login with `admin` / `admin`.
    - *Shows: API authentication, Redis token storage, and RDS user lookup work.*

2.  **View & Create Incident:**
    - Navigate to **Incidents**.
    - Click **Add new incident**.
    - Enter a Title (e.g., "CPU Spike on WebServer").
    - Enter `i-026cdafbb6e41e409` in the **Resource ID** field.
    - Save the incident.
    - *Shows: RDS connectivity and the new ResourceID requirement are implemented.*

3.  **Execute Escalation:**
    - Edit the incident you just created.
    - Click the **Escalate** button.
    - Wait for the "Escalated successfully" message.
    - *Shows: App-to-AWS Lambda integration works.*

4.  **Verify Results:**
    - **AWS Console:** Check EC2 instances. `s3-uebung` should be **Stopping** or **Stopped**.
    - **Notification:** Check your email for the SNS alert.
    - *Shows: Lambda logic, IAM roles, and SNS integration work.*

## Next Steps for GitHub Actions

To activate the automatic deployment, add these **Secrets** to your GitHub repository:
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION` (`eu-central-1`)
- `ECS_CLUSTER_NAME` (`sims-cluster`)
- `ECS_SERVICE_FRONTEND` (`sims-frontend-task-service`)
- `ECS_SERVICE_BACKEND` (`sims-api-task-service`)
- `ECR_REPO_FRONTEND` (`sims-frontend`)
- `ECR_REPO_BACKEND` (`sims-api`)
