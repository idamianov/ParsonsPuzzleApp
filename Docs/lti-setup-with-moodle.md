# Moodle LTI 1.3 Testing Environment Setup Guide

This guide explains how to set up a local Moodle instance using Docker and configure LTI 1.3 integration with ParsonsPuzzleApp for development and testing.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Starting the Moodle Docker Environment](#starting-the-moodle-docker-environment)
3. [Accessing Moodle](#accessing-moodle)
4. [Configuring LTI 1.3 in ParsonsPuzzleApp](#configuring-lti-13-in-parsonspuzzleapp)
5. [Registering the External Tool in Moodle](#registering-the-external-tool-in-moodle)
6. [Creating an LTI Activity](#creating-an-lti-activity)
7. [Testing the Integration](#testing-the-integration)
8. [Troubleshooting](#troubleshooting)
9. [Stopping the Environment](#stopping-the-environment)

---

## Prerequisites

Before starting, ensure you have the following installed:

- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux)
- **Docker Compose** (usually included with Docker Desktop)
- **.NET 8.0 SDK** (to run ParsonsPuzzleApp)
- **Git** (to clone the repository)

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

## Configuring LTI 1.3 in ParsonsPuzzleApp

Before registering Moodle as a platform, you need to get your tool's configuration URLs.

### Step 1: Start ParsonsPuzzleApp

```bash
cd ParsonsPuzzleApp
dotnet run
```

The app typically runs at: https://localhost:7294 (or http://localhost:5149)

### Step 2: Note the following URLs

These URLs are needed for Moodle configuration:

- **Redirection URL (Launch URL)**: `https://localhost:7294/lti/launch`
- **Initiate login URL**: `https://localhost:7294/lti/login`
- **Public keyset URL**: `https://localhost:7294/.well-known/jwks.json`

> **Important**: If your app runs on a different port, update these URLs accordingly.

### Step 3: Get Moodle's Platform Configuration

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
- **Tool URL**: Leave empty (we'll use deep linking)
- **LTI version**: `LTI 1.3`

**Tool configuration:**
- **Public key type**: `Keyset URL`
- **Public keyset URL**: `https://host.docker.internal:7294/.well-known/jwks.json`
  - Note: Use `host.docker.internal` instead of `localhost` to allow Moodle (running in Docker) to reach your local machine
- **Initiate login URL**: `https://host.docker.internal:7294/lti/login`
- **Redirection URI(s)**: `https://host.docker.internal:7294/lti/launch`

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

> **Important SSL Note**: Since ParsonsPuzzleApp uses HTTPS with a self-signed certificate in development, you may need to trust the certificate in your browser before Moodle can connect. Visit https://localhost:7294 in your browser and accept the certificate warning.

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

1. Navigate to https://localhost:7294
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
   - **Accept grades from the tool**: Yes (if you want grading)
4. Expand **Privacy** section:
   - ✅ Share launcher's name with the tool
   - ✅ Share launcher's email with the tool
5. Click **Save and display**

### Step 4: Get the Deployment ID

1. After creating the activity, **launch it once** by clicking on it
2. You should see an error or message about unconfigured deployment
3. This triggers Moodle to generate a **Deployment ID**

### Step 5: Link Deployment to a Bundle

1. Go back to ParsonsPuzzleApp → **LTI Platforms** → **Edit** (the Moodle platform)
2. Switch to the **Deployments** tab
3. You should see the new deployment ID detected
4. Select a **Puzzle Bundle** to link to this deployment
5. Click **Add**

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

**Cause**: JWKS public key mismatch or SSL certificate issues.

**Solution**:
- Ensure the JWKS URL is accessible from both sides
- Visit `https://localhost:7294/.well-known/jwks.json` to verify it returns JSON
- Visit `http://localhost:8085/mod/lti/certs.php` to verify Moodle's keys
- Trust the ParsonsPuzzleApp SSL certificate in your browser

### Issue: Moodle can't reach `localhost:7294`

**Cause**: Moodle is running inside Docker and can't access `localhost` on the host machine.

**Solution**:
- Use `host.docker.internal` instead of `localhost` in Moodle's tool configuration
- On Linux, you may need to add `--add-host=host.docker.internal:host-gateway` to docker-compose or use the host's IP address

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
2. **Test grade passback** (future feature - not yet implemented)
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
