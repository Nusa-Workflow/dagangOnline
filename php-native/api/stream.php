<?php
// Server-Sent Events: pushes new notifications to the browser in real time.
require_once __DIR__ . '/../includes/auth.php';
$user = current_user();
if (!$user) { http_response_code(401); exit; }
$isAdmin = has_role('admin');
session_write_close(); // release session lock so other requests are not blocked
header('Content-Type: text/event-stream');
header('Cache-Control: no-cache');
header('X-Accel-Buffering: no');
set_time_limit(0);
while (ob_get_level()) ob_end_flush();

$last = (int)($_SERVER['HTTP_LAST_EVENT_ID'] ?? ($_GET['since'] ?? 0));
$end = time() + 25; // browser EventSource reconnects automatically
$sql = 'SELECT * FROM notifications WHERE id > ? AND (user_id = ?' . ($isAdmin ? ' OR user_id IS NULL' : '') . ') ORDER BY id ASC';
while (time() < $end && !connection_aborted()) {
    $st = db()->prepare($sql);
    $st->execute([$last, $user['id']]);
    foreach ($st->fetchAll() as $n) {
        $last = (int)$n['id'];
        echo "id: $last\nevent: notification\ndata: " . json_encode($n) . "\n\n";
    }
    echo ": ping\n\n";
    flush();
    sleep(2);
}
echo "retry: 1000\n\n";
