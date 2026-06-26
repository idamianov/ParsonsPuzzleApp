# Moodle LTI 1.3 Testing Environment Setup Guide

This guide explains how to set up a local Moodle instance using Docker and configure LTI 1.3 integration with ParsonsPuzzleApp for development and testing.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Critical Setup Notes](#critical-setup-notes)
3. [Starting the Moodle Docker Environment](#starting-the-moodle-docker-environment)
4. [Accessing Moodle](#accessing-moodle)
5. [Configuring LTI 1.3 in ParsonsPuzzleApp](#configuring-lti-13-in-parsonspuzzleapp)
6. [Registering the External Tool in Moodle](#registering-the-external-tool-in-moodle)
7. [Creating an LTI Activity](#creating-an-lti-activity)
8. [Testing the Integration](#testing-the-integration)
9. [Troubleshooting](#troubleshooting)
10. [Stopping the Environment](#stopping-the-environment)

---

## Prerequisites

Before starting, ensure you have the following installed:

- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
- **Docker Compose** (usually included with Docker Desktop)
- **.NET 8.0 SDK** (to run ParsonsPuzzleApp)
- **Git** (to clone the repository)

---

## Critical Setup Notes

Read these before starting. Each point corresponds to a real failure mode.

### Run over plain HTTP on port 5055 with Kestrel (`dotnet run`)

The local dev harness runs the tool over **plain HTTP on port 5055** — not HTTPS, and not IIS Express. There are two reasons, both proven by testing:

- **HTTPS breaks grade passback.** During grade passback Moodle's *server* fetches the tool's `jwks.json`. The .NET dev certificate is issued for `localhost`, so a fetch to `host.docker.internal` fails hostname + self-signed validation. Plain HTTP avoids it.
- **The tool is reached by two different hostnames** (see the gotcha in [Automated Setup](#automated-setup-quick-start)): the **browser** uses `http://localhost:5055`, while **Moodle's server** uses `http://host.docker.internal:5055` only for the keyset. IIS Express binds loopback only and can't serve the `host.docker.internal` keyset fetch, so use **Kestrel**.

```bash
dotnet run            # default "http" profile → http://localhost:5055
```

> This HTTP setup is **local-dev only**. In production the tool runs over HTTPS with a real certificate, where the session cookie is `SameSite=None; Secure` and the keyset fetch works normally.

---

## Starting the Moodle Docker Environment

The repository includes a `docker-compose.moodle.yml` file that sets up a complete Moodle environment with MariaDB.

### Step 1: Navigate to the project root

```bash
cd ParsonsPuzzleApp
```

### Step 2: Start the Docker containers

```bash
docker-compose -f docker-compose.moodle.yml up -d
```

This command will:
- Download the Bitnami Moodle and MariaDB images (first time only)
- Create and start two containers: `moodle` and `mariadb`
- Set up persistent volumes for data storage

### Step 3: Wait for Moodle to initialize

The first startup takes **5-10 minutes** as Moodle installs and configures itself.

You can monitor the progress with:

```bash
docker-compose -f docker-compose.moodle.yml logs -f moodle
```

Press `Ctrl+C` to stop following the logs (containers will keep running).

Look for a message like:
```
moodle  | ** Moodle setup finished! **
```

---

## Accessing Moodle

Once Moodle is ready, you can access it at:

- **URL**: http://localhost:8085
- **Admin Username**: `admin`
- **Admin Password**: `admin123`

### Initial Setup

1. Navigate to http://localhost:8085
2. Log in with the admin credentials
3. You should see the Moodle dashboard

---

## Automated Setup (Quick Start)

Instead of registering the tool, creating a course/student, and adding an activity by hand (the manual sections below), you can run a single command that does all of it for you. The repo ships `scripts/moodle-lti-setup.php`, which runs **inside** the Moodle container.

Once the containers are up and Moodle has finished installing, run **one** command from the repository root:

**PowerShell (Windows):**
```powershell
Get-Content scripts/moodle-lti-setup.php | docker compose -f docker-compose.moodle.yml exec -T moodle php
```

**Bash / Git Bash / macOS / Linux:**
```bash
docker compose -f docker-compose.moodle.yml exec -T moodle php < scripts/moodle-lti-setup.php
```

The script pipes straight into the container's PHP — no copying or cleanup needed. It is **idempotent**: re-running it heals any stale configuration. It will:

- Register **Parsons Puzzle** as an LTI 1.3 tool, with **grade sync (AGS) enabled** (browser URLs on `localhost:5055`, keyset on `host.docker.internal:5055` — see the gotcha below)
- Allow the app port in Moodle's cURL security settings
- Create a test student (`student1` / `Student123!`)
- Create the **LTI Test Course** and enroll the student + admin
- Add a **Parsons Puzzle Activity** External Tool, set to **open in a new window** (see the gotcha below)

When it finishes it prints the **Issuer, Client ID, and endpoint URLs**. The script assumes the app runs on **`http://localhost:5055`**; to use a different port, add `-e APP_PORT=<port>` before `moodle` in the command above.

> The remaining manual sections document the same steps in detail if you prefer to do them by hand or need to troubleshoot.

### End-to-end test (launch a puzzle + return a grade)

This is the full flow, matching how the code actually works (`LtiController.Launch` → `/SolvePuzzle` → `BundleComplete` → `LtiAgsService.SendGradeAsync`):

1. **Start Moodle:** `docker compose -f docker-compose.moodle.yml up -d` and wait for "Moodle setup finished!" (first boot ~5–10 min).
2. **Run the auto-setup command above** and copy the printed Client ID + endpoints.
3. **Make sure a Puzzle Bundle exists.** Bundles are *not* seeded — log into the app as an instructor and create one under **Instructor → Bundles** if you haven't.
4. **Run the app on HTTP port 5055:** from `ParsonsPuzzleApp/`, run `dotnet run` (the default **http** profile). The `Development` config (`appsettings.Development.json`) already sets `Lti:ToolBaseUrl` to `http://localhost:5055` — **use HTTP, not the https profile** (see the gotcha below).
5. **Register the platform in the app:** **LTI Platforms → Create New Platform** → Issuer `http://localhost:8085`, plus the Client ID / Auth / Token / JWKS values the script printed.
6. **Bind the deployment:** in Moodle, logged in as **admin / admin123**, open `LTI Test Course` and click **Parsons Puzzle Activity once**. The app redirects you to Bundles with a "deployment not configured" notice. Back in the app, **LTI Platforms → Edit → Deployments** (the Deployment ID is pre-filled) → pick your bundle → **Add**.
7. **Test as the student:** log into Moodle as **student1 / Student123!**, open the course, launch the activity → the puzzle loads in a new window.
8. **Solve the whole bundle.** On completion the app posts the score (percent of puzzles solved correctly) back to Moodle.
9. **Verify the grade:** in Moodle as admin, open the course → **Grades** → confirm `student1` has a score for the activity. Check the app logs for `Grade {n}% sent for session …` (or any AGS error).

### Critical gotcha: two hostnames, HTTP, cookies, and the new window

The tool is reached from **two different places that need different hostnames**, and the setup script registers them separately (`Manage tools` → the tool config shows them):

| URL | Used by | Must be |
|-----|---------|---------|
| Initiate login + Redirection URI | the student's **browser** (on the host) | `http://localhost:5055/...` |
| Public keyset URL | **Moodle's server** (inside the container) | `http://host.docker.internal:5055/.well-known/jwks.json` |

Why the split:

- The **browser** runs on the host. `host.docker.internal` does *not* resolve to the app from the host (the app's Kestrel listens on loopback), so a browser redirect there just **times out** (`ERR_CONNECTION_TIMED_OUT`). It must use `localhost`. The app's `Lti:ToolBaseUrl` is therefore `http://localhost:5055`, so the `redirect_uri` it sends matches the registered `localhost` launch URL.
- **Moodle's server** runs inside the container, where `localhost` is the container itself. To fetch the tool's keyset during grade passback it must use `host.docker.internal`.

Everything stays on **plain HTTP** on purpose: Moodle's server-side keyset fetch over HTTPS **fails**, because the .NET dev certificate is issued for `localhost`, not `host.docker.internal` (hostname mismatch + self-signed). HTTP sidesteps that.

Plain HTTP also means the session cookie can't be `Secure`, and the app's bundle-access check + grade passback both rely on the session. Two things make it survive:

- The session cookie is **`SameSite=Lax` + non-`Secure` in `Development`** (and stays `None` + `Secure` in production) — see `Program.cs`.
- The activity **opens in a new window**, so the tool is top-level (first-party). The setup script sets this automatically; if you configure the activity by hand, set **Launch container = New window**.

If the launch times out, check the browser URLs are `localhost`. If grade passback silently does nothing, check the keyset URL is `host.docker.internal` and the two cookie settings above.

---

## Configuring LTI 1.3 in ParsonsPuzzleApp

Before registering Moodle as a platform, you need to get your tool's configuration URLs.

### Step 1: Configure the app's base URL

Both `appsettings.json` and `appsettings.Development.json` ship with `Lti:ToolBaseUrl` set to the local dev value:

```json
"Lti": {
  "ToolBaseUrl": "http://localhost:5055"
}
```

This is the **browser-facing** base URL, so the `redirect_uri` the app sends matches the `localhost` launch URL registered in Moodle. (In production, override `Lti:ToolBaseUrl` with your real HTTPS public URL via environment config.)

### Step 2: Start ParsonsPuzzleApp

```bash
cd ParsonsPuzzleApp
dotnet run            # default "http" profile → http://localhost:5055
```

The app runs at: `http://localhost:5055`

### Step 3: Note the following URLs

These URLs are needed for Moodle configuration. Note the **split hostnames** (see the gotcha in [Automated Setup](#automated-setup-quick-start)):

- **Redirection URL (Launch URL)**: `http://localhost:5055/lti/launch`
- **Initiate login URL**: `http://localhost:5055/lti/login`
- **Public keyset URL**: `http://host.docker.internal:5055/.well-known/jwks.json` *(this one only — fetched by Moodle's server)*

> **Important**: If your app runs on a different port, update `Lti:ToolBaseUrl` in `appsettings.json` and use the correct port in all URLs below.

### Step 4: Get Moodle's Platform Configuration

You'll need these values from Moodle (we'll get them in the next section):

- Platform URL/Issuer
- Client ID
- Public keyset URL
- Authorization Endpoint
- Token Endpoint

---

## Registering the External Tool in Moodle

### Step 1: Enable External Tool Management

1. In Moodle, go to **Site administration** (gear icon) → **Plugins** → **Activity modules** → **External tool** → **Manage tools**
2. Click **Configure a tool manually**

### Step 2: Configure the LTI Tool

Fill in the following details:

**General:**
- **Tool name**: `Parsons Puzzle Toolkit`
- **Tool URL**: http://localhost:5055/lti/launch
- **LTI version**: `LTI 1.3`

**Tool configuration:**
- **Public key type**: `Keyset URL`
- **Public keyset URL**: `http://host.docker.internal:5055/.well-known/jwks.json` *(host.docker.internal — fetched by Moodle's server)*
- **Initiate login URL**: `http://localhost:5055/lti/login`
- **Redirection URI(s)**: `http://localhost:5055/lti/launch`

**Services:**
Enable the following services (scroll down):
- ✅ IMS LTI Assignment and Grade Services
- ✅ IMS LTI Names and Role Provisioning
- ✅ Tool Settings

**Privacy:**
- ✅ Share launcher's name with tool
- ✅ Share launcher's email with tool
- ✅ Accept grades from the tool

Click **Save changes**.

> **Important (local dev runs over HTTP)**: The dev harness uses plain HTTP, so there is no certificate to trust. But Moodle's anti-SSRF cURL security blocks private IP ranges by default, and `host.docker.internal` resolves to one — so Moodle can't fetch the keyset until those ranges are removed from **Site administration → Server → HTTP → cURL blocked hosts**. The setup script does this automatically; if configuring by hand, clear the private/loopback ranges (`192.168.0.0/16`, `10.0.0.0/8`, `172.16.0.0/12`, `127.0.0.0/8`, `localhost`).

### Step 3: Get the Platform Configuration Details

After saving, Moodle will show the tool configuration page. **Copy these values** (you'll need them for ParsonsPuzzleApp):

1. **Platform ID (Issuer)**: Usually `http://localhost:8085`
2. **Client ID**: A long string (e.g., `3yXjK9l2Mn8Pq4Rs`)
3. **Public keyset URL**: `http://localhost:8085/mod/lti/certs.php`
4. **Access token URL**: `http://localhost:8085/mod/lti/token.php`
5. **Authentication request URL**: `http://localhost:8085/mod/lti/auth.php`
6. **Deployment ID**: Will be shown when you create an activity (next section)

---

## Creating an LTI Platform in ParsonsPuzzleApp

Now register Moodle as an LTI platform in ParsonsPuzzleApp.

### Step 1: Log in to ParsonsPuzzleApp

1. Navigate to http://localhost:5055
2. Register/login as an instructor

### Step 2: Navigate to LTI Platforms

Go to **LTI Platforms** (or navigate to `/Instructor/LtiPlatforms`)

### Step 3: Create New Platform

Click **Create New Platform** and fill in the details from Moodle:

- **Name**: `Moodle Local (Docker)`
- **Issuer**: `http://localhost:8085`
- **Client ID**: (The client ID from Moodle)
- **Authorization Endpoint**: `http://localhost:8085/mod/lti/auth.php`
- **Token Endpoint**: `http://localhost:8085/mod/lti/token.php`
- **JWKS URL**: `http://localhost:8085/mod/lti/certs.php`
- **Is Active**: ✅ Checked

Click **Save**.

---

## Creating an LTI Activity

Now create a course and add an LTI activity in Moodle.

### Step 1: Create a Test Course

1. In Moodle, go to **Site administration** → **Courses** → **Add a new course**
2. Fill in basic details:
   - **Course full name**: `LTI Test Course`
   - **Course short name**: `LTI101`
3. Click **Save and display**

### Step 2: Turn Editing On

Click **Turn editing on** button (top right)

### Step 3: Add an External Tool Activity

1. Click **+ Add an activity or resource**
2. Select **External tool**
3. Fill in:
   - **Activity name**: `Parsons Puzzles - Basics`
   - **Preconfigured tool**: Select `Parsons Puzzle Toolkit`
4. Expand **Privacy** section:
   - ✅ Share launcher's name with the tool
   - ✅ Share launcher's email with the tool
5. Click **Save and display**

### Step 4: Trigger Deployment ID Generation

1. After creating the activity, **launch it once** by clicking on it while logged in as an **instructor**
2. The app will redirect you to the **Bundles** page — this is expected. The launch registered a new Deployment ID in the session
3. Navigate to **LTI Platforms** (or `/Instructor/LtiPlatforms`). You should see a setup warning message at the top identifying the new deployment

### Step 5: Link Deployment to a Bundle

1. From the **LTI Platforms** list, click **Edit** next to the Moodle platform
2. The **Deployments** tab will open automatically, with the new Deployment ID already pre-filled in the "Add Deployment" form
3. Select a **Puzzle Bundle** to link to this deployment
4. Click **Add**

---

## Testing the Integration

### Step 1: Launch from Moodle

1. In Moodle, go to the test course
2. Click on the **External Tool activity** you created
3. You should be redirected to ParsonsPuzzleApp and see the linked puzzle bundle

### Step 2: Verify Student Experience

1. Create a test student account in Moodle (or use an existing one)
2. Enroll the student in the test course
3. Log in as the student
4. Launch the LTI activity
5. Verify the student can access and solve puzzles

### Step 3: Verify Data Flow (Optional)

Check that the following data is being passed correctly:

- Student name and email (check ParsonsPuzzleApp session)
- Context ID (course identifier)
- Resource link ID (activity identifier)
- Roles (Instructor vs Learner)

You can view this in the browser's developer console or check ParsonsPuzzleApp logs.

---

## Troubleshooting

### Issue: "Platform not found" error

**Cause**: The Issuer or Client ID doesn't match between Moodle and ParsonsPuzzleApp.

**Solution**:
- Verify the Issuer URL is exactly the same (check for trailing slashes)
- Verify the Client ID matches exactly (copy-paste to avoid typos)

### Issue: "Invalid signature" or "Invalid JWT" error

**Cause**: JWKS public key mismatch, app restart, or SSL certificate issues.

**Solution**:
- Visit `http://localhost:5055/.well-known/jwks.json` to verify the tool returns JSON
- Verify Moodle's *server* can fetch it: `docker compose -f docker-compose.moodle.yml exec moodle php -r 'echo file_get_contents("http://host.docker.internal:5055/.well-known/jwks.json");'`
- Visit `http://localhost:8085/mod/lti/certs.php` to verify Moodle's keys
- **If signatures started failing after a restart**: the app regenerated its RSA key. Configure `Lti:PrivateKeyPath` in `appsettings.json` to persist the key, then re-launch (Moodle re-fetches the keyset automatically)

### Issue: token request fails / `fix_jwks_alg(): ... null given` (no grade)

**Cause**: Moodle's cURL security is blocking the keyset fetch to `host.docker.internal` (a private IP).

**Solution**:
- Re-run the setup script (it relaxes the blocked ranges), or manually clear the private/loopback ranges in **Site administration → Server → HTTP → cURL blocked hosts**

### Issue: launch times out at `host.docker.internal:5055`

**Cause**: The browser can't reach `host.docker.internal` (only Moodle's container can).

**Solution**:
- The browser-facing URLs (Initiate login, Redirection URI) must be `http://localhost:5055/...`; only the keyset URL uses `host.docker.internal`. Re-run the setup script to fix the registration.

### Issue: "Deployment not configured" message

**Cause**: The deployment hasn't been linked to a bundle yet.

**Solution**:
- Launch the activity once to generate the deployment ID
- Go to ParsonsPuzzleApp → LTI Platforms → Edit → Deployments tab
- Link the deployment to a puzzle bundle

### Issue: Database connection errors in Moodle

**Cause**: MariaDB container not fully initialized.

**Solution**:
- Wait a few more minutes for MariaDB to finish starting
- Check logs: `docker-compose -f docker-compose.moodle.yml logs mariadb`
- Restart containers: `docker-compose -f docker-compose.moodle.yml restart`

### Issue: Port conflicts (8085 already in use)

**Solution**: Edit `docker-compose.moodle.yml` and change the port mapping:
```yaml
ports:
  - '8086:8080'  # Changed from 8085 to 8086
```

Then restart the containers.

---

## Stopping the Environment

### Stop containers (preserves data)

```bash
docker-compose -f docker-compose.moodle.yml stop
```

### Stop and remove containers (preserves data in volumes)

```bash
docker-compose -f docker-compose.moodle.yml down
```

### Stop and remove everything including data (⚠️ destructive)

```bash
docker-compose -f docker-compose.moodle.yml down -v
```

---

## Data Persistence

The Docker setup uses named volumes for data persistence:

- **mariadb_data**: Database files
- **moodle_data**: Moodle application files
- **moodledata_data**: Moodle user data and uploads

These volumes persist even when containers are stopped or removed (unless you use `down -v`).

### Viewing Volumes

```bash
docker volume ls | grep moodle
```

### Backing Up Data

To backup the Moodle database:

```bash
docker-compose -f docker-compose.moodle.yml exec mariadb mysqldump -u bn_moodle bitnami_moodle > moodle_backup.sql
```

---

## Advanced Configuration

### Changing Admin Password

Edit `docker-compose.moodle.yml` before first startup:

```yaml
- MOODLE_PASSWORD=YourSecurePassword123
```

### Using a Custom Domain

If you want to access Moodle via a custom domain (e.g., `moodle.local`):

1. Add to your hosts file:
   - **Windows**: `C:\Windows\System32\drivers\etc\hosts`
   - **Mac/Linux**: `/etc/hosts`

   ```
   127.0.0.1 moodle.local
   ```

2. Access Moodle at http://moodle.local:8085

3. Update ParsonsPuzzleApp platform configuration accordingly

---

## Next Steps

After successfully setting up the testing environment:

1. **Create diverse puzzle bundles** to test different scenarios
2. **Test grade passback** — implemented via LTI AGS (`LtiAgsService`); the score is sent to the LMS gradebook when a student completes a bundle. See [End-to-end test](#end-to-end-test-launch-a-puzzle--return-a-grade)
3. **Test with multiple students** and deployments
4. **Explore Moodle's LTI Advantage features** (Deep Linking, Assignment and Grades Service, Names and Role Provisioning Service)

---

## References

- [LTI 1.3 Core Specification](https://www.imsglobal.org/spec/lti/v1p3/)
- [Moodle LTI Documentation](https://docs.moodle.org/en/LTI)
- [Bitnami Moodle Docker Image](https://hub.docker.com/r/bitnami/moodle)

---

## Support

If you encounter issues not covered in this guide:

1. Check the application logs in ParsonsPuzzleApp
2. Check Moodle logs: **Site administration** → **Reports** → **Logs**
3. Check Docker logs: `docker-compose -f docker-compose.moodle.yml logs`
