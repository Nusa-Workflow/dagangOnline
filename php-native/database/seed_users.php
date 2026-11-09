<?php
// Run once: php database/seed_users.php  (creates demo accounts; change passwords after first login)
require __DIR__ . '/../config/database.php';
$users = [
    ['Super Admin',   'superadmin@nusscorp.test', 'super_admin'],
    ['Admin Toko',    'admin@nusscorp.test',      'admin'],
    ['Customer Demo', 'customer@nusscorp.test',   'customer'],
];
$st = db()->prepare('INSERT IGNORE INTO users (name,email,password,role) VALUES (?,?,?,?)');
foreach ($users as [$n, $e, $r]) {
    $st->execute([$n, $e, password_hash('password123', PASSWORD_DEFAULT), $r]);
}
echo "Seeded demo users (password: password123)\n";
