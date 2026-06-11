<?php
/**
 * Moodle LTI Auto-Setup Script
 *
 * Registers the Parsons Puzzle app as an LTI 1.3 tool in Moodle,
 * creates a test student, course, enrollment, and External Tool activity.
 *
 * Run inside the Moodle container:
 *   php /path/to/moodle-lti-setup.php
 *
 * Environment variables:
 *   APP_PORT - Port the Parsons Puzzle app runs on (default: 5055)
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
$appHost = "http://host.docker.internal:$appPort";

echo "=== Moodle LTI Auto-Setup ===\n";
echo "App URL: $appHost\n\n";

// ─── 1. Register LTI Tool Type ───────────────────────────────────────────────

echo "--- Registering LTI tool ---\n";

$toolName = 'Parsons Puzzle';

// Check if tool already exists
$existingTool = $DB->get_record('lti_types', ['name' => $toolName]);

if ($existingTool) {
    echo "Tool '$toolName' already exists (id={$existingTool->id}).\n";
    $toolId = $existingTool->id;
    $clientId = $existingTool->clientid;
} else {
    // Build tool type record
    $type = new stdClass();
    $type->name = $toolName;
    $type->baseurl = "$appHost/lti/launch";
    $type->tooldomain = "host.docker.internal";
    $type->state = LTI_TOOL_STATE_CONFIGURED;
    $type->course = 1; // site-level
    $type->ltiversion = 'LTI-1p3';
    $type->clientid = null; // Moodle generates this
    $type->timecreated = time();
    $type->timemodified = time();
    $type->createdby = 2; // admin user

    $toolId = $DB->insert_record('lti_types', $type);

    // Generate client ID (Moodle stores this as a random unique string)
    $clientId = random_string(15);
    $DB->set_field('lti_types', 'clientid', $clientId, ['id' => $toolId]);

    // Insert tool configuration
    $configs = [
        'sendname'                          => '1',
        'sendemailaddr'                     => '1',
        'acceptgrades'                      => '2',   // always
        'ltiversion'                        => 'LTI-1p3',
        'keytype'                           => 'JWK_KEYSET',
        'publickeyset'                      => "$appHost/.well-known/jwks.json",
        'initiatelogin'                     => "$appHost/lti/login",
        'redirectionuris'                   => "$appHost/lti/launch",
        'customparameters'                  => '',
        'coursevisible'                      => '1',
        'contentitem'                       => '0',
        'toolurl'                           => "$appHost/lti/launch",
        'toolurl_ContentItemSelectionRequest' => '',
        'ltiservice_gradesynchronization'   => '2',  // use for grade sync
        'ltiservice_memberships'            => '0',
    ];

    foreach ($configs as $name => $value) {
        $config = new stdClass();
        $config->typeid = $toolId;
        $config->name = $name;
        $config->value = $value;
        $DB->insert_record('lti_types_config', $config);
    }

    echo "Created tool '$toolName' (id=$toolId).\n";
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

// Also allow host.docker.internal in cURL blocked hosts (remove it if blocked)
$blockedHosts = get_config('moodle', 'curlsecurityblockedhosts');
if ($blockedHosts && strpos($blockedHosts, 'host.docker.internal') !== false) {
    $lines = array_filter(array_map('trim', explode("\n", $blockedHosts)));
    $lines = array_filter($lines, function($line) {
        return $line !== 'host.docker.internal';
    });
    set_config('curlsecurityblockedhosts', implode("\n", $lines));
    echo "Removed host.docker.internal from blocked hosts.\n";
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
    echo "Activity '$activityName' already exists.\n";
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
    $lti->launchcontainer = LTI_LAUNCH_CONTAINER_DEFAULT;
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

    echo "Created activity '$activityName' (cmid=$cmId).\n";
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
