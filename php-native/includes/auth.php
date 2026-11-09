<?php
require_once __DIR__ . '/../config/database.php';
if (session_status() === PHP_SESSION_NONE) {
    session_set_cookie_params(['httponly' => true, 'samesite' => 'Lax']);
    session_start();
}
const ROLES = ['super_admin' => 3, 'admin' => 2, 'customer' => 1];

function current_user(): ?array { return $_SESSION['user'] ?? null; }
function has_role(string $min): bool {
    $u = current_user();
    return $u && ROLES[$u['role']] >= ROLES[$min];
}
function require_login(): void {
    if (!current_user()) { header('Location: login.php'); exit; }
}
function csrf_token(): string {
    if (empty($_SESSION['csrf'])) $_SESSION['csrf'] = bin2hex(random_bytes(32));
    return $_SESSION['csrf'];
}
function e($v): string { return htmlspecialchars((string)$v, ENT_QUOTES, 'UTF-8'); }
function notify(?int $userId, string $title, string $message, string $type = 'info'): void {
    db()->prepare('INSERT INTO notifications (user_id, title, message, type) VALUES (?,?,?,?)')
        ->execute([$userId, $title, $message, $type]);
}
