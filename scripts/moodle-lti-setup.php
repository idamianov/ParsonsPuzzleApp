<?php
/**
 * Moodle LTI Auto-Setup Script
 *
 * Registers the Parsons Puzzle app as an LTI 1.3 tool in Moodle,
 * creates a test student, course, enrollment, and External Tool activity.
 *
 * This script must run INSIDE the Moodle Docker container. Start the stack
 * first (`docker compose -f docker-compose.moodle.yml up -d`) and wait until
 * Moodle has finished installing, then run ONE command from the repo root:
 *
 *   PowerShell (Windows):
 *     Get-Content scripts/moodle-lti-setup.php | docker compose -f docker-compose.moodle.yml exec -T moodle php
 *
 *   Bash / Git Bash / macOS / Linux:
 *     docker compose -f docker-compose.moodle.yml exec -T moodle php < scripts/moodle-lti-setup.php
 *
 * No copying or cleanup needed — the script is piped straight into PHP.
 *
 * Environment variables:
 *   APP_PORT - Port the Parsons Puzzle app runs on (default: 5055).
 *              Override with: ... exec -T -e APP_PORT=5055 moodle php
 */

define('CLI_SCRIPT', true);

// Bitnami Moodle path
require('/bitnami/moodle/config.php');
require_once($CFG->libdir . '/adminlib.php');
require_once($CFG->libdir . '/moodlelib.php');
require_once($CFG->dirroot . '/mod/lti/lib.php');
require_once($CFG->dirroot . '/mod/lti/locallib.php');
require_once($CFG->dirroot . '/user/lib.php');
require_once($CFG->dirroot . '/course/lib.php');
require_once($CFG->libdir . '/enrollib.php');

$appPort = getenv('APP_PORT') ?: '5055';

// The tool is reached from two different places, which need different hostnames:
//   - $browserHost: where the student's BROWSER is redirected (login/launch). The
//     browser runs on the host, so it must use localhost (which it can reach and
//     which counts as a secure context for cookies).
//   - $serverHost: where MOODLE'S SERVER (inside the container) fetches the tool's
//     public keyset during grade passback. The container reaches the host via
//     host.docker.internal, not localhost.
$browserHost = "http://localhost:$appPort";
$serverHost  = "http://host.docker.internal:$appPort";

echo "=== Moodle LTI Auto-Setup ===\n";
echo "Browser-facing URL: $browserHost\n";
echo "Server keyset URL:  $serverHost\n\n";

// ─── 1. Register LTI Tool Type ───────────────────────────────────────────────

echo "--- Registering LTI tool ---\n";

$toolName = 'Parsons Puzzle';

// The tool's URLs/config are (re)applied below whether the tool is new or
// already exists, so re-running this script always heals stale configuration
// (e.g. a tool left pointing at an old port or with grade sync disabled).
$configs = [
    'sendname'                          => '1',
    'sendemailaddr'                     => '1',
    'acceptgrades'                      => '2',   // always
    'ltiversion'                        => '1.3.0',
    'keytype'                           => 'JWK_KEYSET',
    'publickeyset'                      => "$serverHost/.well-known/jwks.json",  // fetched by Moodle's server
    'initiatelogin'                     => "$browserHost/lti/login",             // browser redirect
    'redirectionuris'                   => "$browserHost/lti/launch",            // browser redirect
    'customparameters'                  => '',
    'coursevisible'                      => '1',
    'contentitem'                       => '0',
    'toolurl_ContentItemSelectionRequest' => '',
    'ltiservice_gradesynchronization'   => '2',  // 2 = use this service (grade sync ON)
    'ltiservice_memberships'            => '0',
];

// Check if tool already exists
$existingTool = $DB->get_record('lti_types', ['name' => $toolName]);

if ($existingTool) {
    $toolId = $existingTool->id;
    $clientId = $existingTool->clientid;

    // Heal core tool fields in case they were created with different URLs.
    $DB->update_record('lti_types', (object)[
        'id'          => $toolId,
        'baseurl'     => "$browserHost/lti/launch",
        'tooldomain'  => 'localhost',
        'state'       => LTI_TOOL_STATE_CONFIGURED,
        'ltiversion'  => '1.3.0',
        'timemodified' => time(),
    ]);

    echo "Tool '$toolName' already exists (id=$toolId) - updating its configuration.\n";
} else {
    // Build tool type record
    $type = new stdClass();
    $type->name = $toolName;
    $type->baseurl = "$browserHost/lti/launch";
    $type->tooldomain = "localhost";
    $type->state = LTI_TOOL_STATE_CONFIGURED;
    $type->course = 1; // site-level
    $type->ltiversion = '1.3.0';
    $type->clientid = null; // Moodle generates this
    $type->timecreated = time();
    $type->timemodified = time();
    $type->createdby = 2; // admin user

    $toolId = $DB->insert_record('lti_types', $type);

    // Generate client ID (Moodle stores this as a random unique string)
    $clientId = random_string(15);
    $DB->set_field('lti_types', 'clientid', $clientId, ['id' => $toolId]);

    echo "Created tool '$toolName' (id=$toolId).\n";
}

// Upsert every config key so an existing tool is brought to the correct state.
foreach ($configs as $name => $value) {
    $existingConfig = $DB->get_record('lti_types_config', ['typeid' => $toolId, 'name' => $name]);
    if ($existingConfig) {
        $DB->set_field('lti_types_config', 'value', $value, ['id' => $existingConfig->id]);
    } else {
        $DB->insert_record('lti_types_config', (object)[
            'typeid' => $toolId,
            'name'   => $name,
            'value'  => $value,
        ]);
    }
}

echo "Client ID: $clientId\n\n";

// ─── 2. Allow port 5055 in cURL security ─────────────────────────────────────

echo "--- Configuring cURL allowed ports ---\n";

$currentPorts = get_config('moodle', 'curlsecurityallowedport');
$portList = $currentPorts ? array_filter(array_map('trim', explode("\n", $currentPorts))) : [];

if (!in_array($appPort, $portList)) {
    $portList[] = '443';
    $portList[] = $appPort;
    $portList = array_unique($portList);
    set_config('curlsecurityallowedport', implode("\n", $portList));
    echo "Added port $appPort to allowed cURL ports.\n";
} else {
    echo "Port $appPort already in allowed cURL ports.\n";
}

// Moodle's anti-SSRF cURL security blocks private IP ranges by default. During
// grade passback Moodle's server fetches the tool's JWKS via download_file_content(),
// which enforces this list - and host.docker.internal resolves to a private address
// (e.g. 192.168.65.254), so the fetch is blocked and the token request fails with
// "fix_jwks_alg(): ... null given". For this local harness we remove the private /
// loopback ranges so the keyset URL is reachable.
require_once($CFG->libdir . '/filelib.php');
$resolvedIp = gethostbyname('host.docker.internal');
echo "host.docker.internal resolves to $resolvedIp\n";

$blockedHosts = get_config('moodle', 'curlsecurityblockedhosts');
if ($blockedHosts !== false && $blockedHosts !== '') {
    $remove = [
        '127.0.0.0/8', '10.0.0.0/8', '172.16.0.0/12', '192.168.0.0/16',
        '0.0.0.0', '::1', '0000::1', 'localhost', 'host.docker.internal',
    ];
    $lines = array_filter(array_map('trim', explode("\n", $blockedHosts)));
    $kept  = array_values(array_filter($lines, fn($l) => !in_array($l, $remove, true)));
    if (count($kept) !== count($lines)) {
        set_config('curlsecurityblockedhosts', implode("\n", $kept));
        echo "Relaxed cURL blocked hosts for local dev (removed private/loopback ranges).\n";
    } else {
        echo "cURL blocked hosts already relaxed.\n";
    }
}

// Confirm the tool's keyset URL is now reachable by Moodle's security layer.
$sec = new \core\files\curl_security_helper();
if ($sec->url_is_blocked("$serverHost/.well-known/jwks.json")) {
    echo "WARNING: $serverHost is still blocked by Moodle's cURL security - grade passback will fail.\n";
} else {
    echo "Keyset URL is reachable by Moodle (grade passback unblocked).\n";
}

echo "\n";

// ─── 3. Create test student ──────────────────────────────────────────────────

echo "--- Creating test student ---\n";

$studentUsername = 'student1';
$studentPassword = 'Student123!';

$existingStudent = $DB->get_record('user', ['username' => $studentUsername]);

if ($existingStudent) {
    echo "Student '$studentUsername' already exists (id={$existingStudent->id}).\n";
    $studentId = $existingStudent->id;
} else {
    $user = new stdClass();
    $user->username = $studentUsername;
    $user->password = hash_internal_user_password($studentPassword);
    $user->firstname = 'Test';
    $user->lastname = 'Student';
    $user->email = 'student1@example.com';
    $user->confirmed = 1;
    $user->mnethostid = $CFG->mnet_localhost_id;
    $user->timecreated = time();
    $user->timemodified = time();
    $user->auth = 'manual';

    $studentId = $DB->insert_record('user', $user);
    echo "Created student '$studentUsername' (id=$studentId).\n";
}

echo "Student credentials: $studentUsername / $studentPassword\n\n";

// ─── 4. Create test course ───────────────────────────────────────────────────

echo "--- Creating test course ---\n";

$courseShortname = 'LTITEST';
$existingCourse = $DB->get_record('course', ['shortname' => $courseShortname]);

if ($existingCourse) {
    echo "Course '$courseShortname' already exists (id={$existingCourse->id}).\n";
    $course = $existingCourse;
} else {
    $courseData = new stdClass();
    $courseData->fullname = 'LTI Test Course';
    $courseData->shortname = $courseShortname;
    $courseData->category = 1; // default category
    $courseData->format = 'topics';
    $courseData->visible = 1;
    $courseData->numsections = 1;

    $course = create_course($courseData);
    echo "Created course '{$course->fullname}' (id={$course->id}).\n";
}

echo "\n";

// ─── 5. Enroll users in course ───────────────────────────────────────────────

echo "--- Enrolling users ---\n";

$enrolPlugin = enrol_get_plugin('manual');
$enrolInstances = enrol_get_instances($course->id, true);
$manualInstance = null;

foreach ($enrolInstances as $instance) {
    if ($instance->enrol === 'manual') {
        $manualInstance = $instance;
        break;
    }
}

if (!$manualInstance) {
    // Create manual enrolment instance
    $enrolId = $enrolPlugin->add_instance($course);
    $manualInstance = $DB->get_record('enrol', ['id' => $enrolId]);
    echo "Created manual enrolment instance.\n";
}

// Get role IDs
$studentRoleId = $DB->get_field('role', 'id', ['shortname' => 'student']);
$editingTeacherRoleId = $DB->get_field('role', 'id', ['shortname' => 'editingteacher']);

// Enroll student
$isStudentEnrolled = $DB->record_exists('user_enrolments', [
    'enrolid' => $manualInstance->id,
    'userid' => $studentId
]);

if (!$isStudentEnrolled) {
    $enrolPlugin->enrol_user($manualInstance, $studentId, $studentRoleId);
    echo "Enrolled student (id=$studentId) as student.\n";
} else {
    echo "Student already enrolled.\n";
}

// Enroll admin as editing teacher
$adminId = 2;
$isAdminEnrolled = $DB->record_exists('user_enrolments', [
    'enrolid' => $manualInstance->id,
    'userid' => $adminId
]);

if (!$isAdminEnrolled) {
    $enrolPlugin->enrol_user($manualInstance, $adminId, $editingTeacherRoleId);
    echo "Enrolled admin (id=$adminId) as editing teacher.\n";
} else {
    echo "Admin already enrolled.\n";
}

echo "\n";

// ─── 6. Add External Tool activity ──────────────────────────────────────────

echo "--- Adding External Tool activity ---\n";

$activityName = 'Parsons Puzzle Activity';
$existingModule = $DB->get_record_sql(
    "SELECT cm.id, l.id as ltiid
     FROM {course_modules} cm
     JOIN {modules} m ON m.id = cm.module
     JOIN {lti} l ON l.id = cm.instance
     WHERE m.name = 'lti' AND l.name = ? AND cm.course = ?",
    [$activityName, $course->id]
);

if ($existingModule) {
    // Heal the launch container in case it was created as an embedded iframe.
    // A new window keeps the tool first-party so its session cookie survives.
    $DB->set_field('lti', 'launchcontainer', LTI_LAUNCH_CONTAINER_WINDOW, ['id' => $existingModule->ltiid]);
    echo "Activity '$activityName' already exists - set to open in a new window.\n";
} else {
    $moduleId = $DB->get_field('modules', 'id', ['name' => 'lti']);

    // Create the LTI instance
    $lti = new stdClass();
    $lti->course = $course->id;
    $lti->name = $activityName;
    $lti->intro = 'Solve Parsons Puzzles via LTI';
    $lti->introformat = FORMAT_HTML;
    $lti->typeid = $toolId;
    $lti->toolurl = '';  // uses tool type URL
    // Open in a new window so the tool is top-level (first-party); its session
    // cookie then survives the launch on a plain-HTTP dev origin.
    $lti->launchcontainer = LTI_LAUNCH_CONTAINER_WINDOW;
    $lti->instructorchoicesendname = 1;
    $lti->instructorchoicesendemailaddr = 1;
    $lti->instructorchoiceacceptgrades = 1;
    $lti->grade = 100;
    $lti->timecreated = time();
    $lti->timemodified = time();

    $ltiId = $DB->insert_record('lti', $lti);

    // Create the course module
    $cm = new stdClass();
    $cm->course = $course->id;
    $cm->module = $moduleId;
    $cm->instance = $ltiId;
    $cm->section = 1;
    $cm->visible = 1;
    $cm->added = time();

    $cmId = $DB->insert_record('course_modules', $cm);

    // Add to the first section
    $section = $DB->get_record('course_sections', [
        'course' => $course->id,
        'section' => 1
    ]);

    if ($section) {
        $sequence = $section->sequence ? $section->sequence . ',' . $cmId : (string)$cmId;
        $DB->set_field('course_sections', 'sequence', $sequence, ['id' => $section->id]);
    }

    // Invalidate the course modinfo cache so the new activity actually shows up.
    rebuild_course_cache($course->id, true);

    echo "Created activity '$activityName' (cmid=$cmId).\n";
}

// ─── 6b. Create the gradebook item (required for LTI AGS grade passback) ──────
// Because the activity is created with raw DB inserts (not Moodle's module API),
// the gradebook grade item is never created. Without it Moodle exposes no AGS
// line item, the launch carries no line-item endpoint, and the tool has nowhere
// to POST the score. Create it explicitly via Moodle's own helper.
$ltiInstance = $DB->get_record('lti', ['name' => $activityName, 'course' => $course->id]);
$ltiInstance->cmidnumber = '';   // lti_grade_item_update() reads this field
lti_grade_item_update($ltiInstance);

// Create the *coupled* line item (ltiservice_gradebookservices row) for this
// activity. Without it the LTI 1.3 launch only advertises the line-items
// container, not the specific "lineitem" endpoint the tool posts the score to.
\ltiservice_gradebookservices\local\service\gradebookservices::update_coupled_gradebookservices(
    $ltiInstance, null, null, null, null);

$hasGradeItem = $DB->record_exists('grade_items', ['itemmodule' => 'lti', 'iteminstance' => $ltiInstance->id]);
$hasLineItem  = $DB->record_exists('ltiservice_gradebookservices', ['ltilinkid' => $ltiInstance->id]);
if ($hasGradeItem && $hasLineItem) {
    echo "Gradebook item + AGS line item ready (score endpoint will be sent on launch).\n";
} else {
    echo "WARNING: grade passback plumbing incomplete (grade_item=" .
         ($hasGradeItem ? 'yes' : 'no') . ", line_item=" . ($hasLineItem ? 'yes' : 'no') . ").\n";
}

echo "\n";

// ─── 7. Get platform info for app configuration ─────────────────────────────

$moodleUrl = 'http://localhost:8085';

echo "============================================\n";
echo "  SETUP COMPLETE!\n";
echo "============================================\n\n";
echo "Add this platform in your Parsons Puzzle app:\n\n";
echo "  Name:                   Moodle Dev\n";
echo "  Issuer:                 $moodleUrl\n";
echo "  Client ID:              $clientId\n";
echo "  Auth Endpoint:          $moodleUrl/mod/lti/auth.php\n";
echo "  Token Endpoint:         $moodleUrl/mod/lti/token.php\n";
echo "  JWKS URL:               $moodleUrl/mod/lti/certs.php\n\n";
echo "Moodle admin login:       admin / admin123\n";
echo "Test student login:       $studentUsername / $studentPassword\n";
echo "Course:                   LTI Test Course\n\n";
echo "Next steps:\n";
echo "  1. Register the platform in your app (Instructor > LTI Platforms page)\n";
echo "  2. Run your app on port $appPort (HTTP, not HTTPS)\n";
echo "  3. Log into Moodle as student1, open LTI Test Course, click the activity\n";
echo "============================================\n";
