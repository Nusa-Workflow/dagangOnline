<?php
/**
 * REST API — GET/POST/PUT/DELETE  api/index.php?resource=products&id=1
 * Role matrix:
 *   categories, products : read = all logged in, write = admin+
 *   orders               : customer reads/creates own; admin+ full
 *   users                : admin read, super_admin write
 *   notifications        : own + broadcast (admin); PUT marks all read
 */
require_once __DIR__ . '/../includes/auth.php';
header('Content-Type: application/json; charset=utf-8');

function out($data, int $code = 200): never { http_response_code($code); echo json_encode($data); exit; }
function fail(string $msg, int $code = 400): never { out(['success' => false, 'message' => $msg], $code); }

$user = current_user() ?? fail('Unauthorized', 401);
$method = $_SERVER['REQUEST_METHOD'];
$resource = $_GET['resource'] ?? '';
$id = isset($_GET['id']) ? (int)$_GET['id'] : null;
$input = json_decode(file_get_contents('php://input'), true) ?? [];

if ($method !== 'GET' && !hash_equals(csrf_token(), $_SERVER['HTTP_X_CSRF_TOKEN'] ?? '')) fail('Invalid CSRF token', 419);

$schema = [
    'categories' => ['fields' => ['name', 'description'], 'read' => 'customer', 'write' => 'admin'],
    'products'   => ['fields' => ['category_id', 'name', 'description', 'price', 'stock'], 'read' => 'customer', 'write' => 'admin'],
    'orders'     => ['fields' => ['product_id', 'quantity', 'status'], 'read' => 'customer', 'write' => 'customer'],
    'users'      => ['fields' => ['name', 'email', 'role', 'phone', 'is_active', 'password'], 'read' => 'admin', 'write' => 'super_admin'],
];

try {
    if ($resource === 'notifications') {
        $scope = '(user_id = ?' . (has_role('admin') ? ' OR user_id IS NULL' : '') . ')';
        if ($method === 'GET') {
            $st = db()->prepare("SELECT * FROM notifications WHERE id > ? AND $scope ORDER BY id DESC LIMIT 20");
            $st->execute([(int)($_GET['since'] ?? 0), $user['id']]);
            out(['success' => true, 'data' => $st->fetchAll()]);
        }
        if ($method === 'PUT') {
            db()->prepare("UPDATE notifications SET is_read = 1 WHERE $scope")->execute([$user['id']]);
            out(['success' => true, 'message' => 'Semua notifikasi ditandai dibaca']);
        }
        fail('Method not allowed', 405);
    }

    if ($resource === 'stats') {
        $q = fn($sql) => db()->query($sql)->fetchColumn();
        $own = has_role('admin') ? '' : ' WHERE user_id = ' . (int)$user['id'];
        out(['success' => true, 'data' => [
            'products'   => (int)$q('SELECT COUNT(*) FROM products'),
            'categories' => (int)$q('SELECT COUNT(*) FROM categories'),
            'orders'     => (int)$q('SELECT COUNT(*) FROM orders' . $own),
            'users'      => has_role('admin') ? (int)$q('SELECT COUNT(*) FROM users') : null,
            'revenue'    => (float)$q('SELECT COALESCE(SUM(total),0) FROM orders' . $own),
        ]]);
    }

    $cfg = $schema[$resource] ?? fail('Unknown resource', 404);
    if (!has_role($method === 'GET' ? $cfg['read'] : $cfg['write'])) fail('Akses ditolak untuk role ' . $user['role'], 403);
    $isCustomer = $user['role'] === 'customer';

    // ---------- READ ----------
    if ($method === 'GET') {
        $sql = match ($resource) {
            'products' => 'SELECT p.*, c.name AS category FROM products p LEFT JOIN categories c ON c.id = p.category_id',
            'orders'   => 'SELECT o.*, u.name AS customer, p.name AS product FROM orders o JOIN users u ON u.id = o.user_id JOIN products p ON p.id = o.product_id',
            'users'    => 'SELECT id, name, email, role, phone, is_active, created_at FROM users u',
            default    => "SELECT * FROM $resource c",
        };
        $alias = ['products' => 'p', 'orders' => 'o', 'users' => 'u'][$resource] ?? 'c';
        $where = []; $args = [];
        if ($id) { $where[] = "$alias.id = ?"; $args[] = $id; }
        if ($resource === 'orders' && $isCustomer) { $where[] = 'o.user_id = ?'; $args[] = $user['id']; }
        if ($where) $sql .= ' WHERE ' . implode(' AND ', $where);
        $sql .= " ORDER BY $alias.id DESC";
        $st = db()->prepare($sql); $st->execute($args);
        $rows = $st->fetchAll();
        if ($id && !$rows) fail('Data tidak ditemukan', 404);
        out(['success' => true, 'data' => $id ? $rows[0] : $rows]);
    }

    // ---------- WRITE ----------
    $data = array_intersect_key($input, array_flip($cfg['fields']));
    foreach ($data as $k => $v) if ($v === '') $data[$k] = null;

    if ($resource === 'users') {
        if (!empty($data['password'])) $data['password'] = password_hash($data['password'], PASSWORD_DEFAULT);
        else unset($data['password']);
        if (isset($data['email']) && !filter_var($data['email'], FILTER_VALIDATE_EMAIL)) fail('Email tidak valid', 422);
        if ($method === 'POST' && empty($data['password'])) fail('Password wajib untuk user baru', 422);
        if ($id === (int)$user['id'] && $method === 'DELETE') fail('Tidak bisa menghapus akun sendiri', 422);
    }

    if ($resource === 'orders') {
        if ($isCustomer && $method !== 'POST') fail('Customer hanya dapat membuat pesanan', 403);
        if ($method === 'POST') {
            unset($data['status']);
            $qty = max(1, (int)($data['quantity'] ?? 1));
            $p = db()->prepare('SELECT price, stock FROM products WHERE id = ?');
            $p->execute([(int)($data['product_id'] ?? 0)]);
            $prod = $p->fetch() ?: fail('Produk tidak ditemukan', 422);
            if ($prod['stock'] < $qty) fail('Stok tidak mencukupi', 422);
            $data = ['user_id' => $user['id'], 'product_id' => (int)$data['product_id'], 'quantity' => $qty, 'total' => $prod['price'] * $qty];
            db()->prepare('UPDATE products SET stock = stock - ? WHERE id = ?')->execute([$qty, $data['product_id']]);
        }
    }

    if ($method === 'POST') {
        if (!$data) fail('Data kosong', 422);
        $cols = array_keys($data);
        db()->prepare("INSERT INTO $resource (" . implode(',', $cols) . ') VALUES (' . implode(',', array_fill(0, count($cols), '?')) . ')')
            ->execute(array_values($data));
        $newId = (int)db()->lastInsertId();
        notify(null, ucfirst($resource) . ' baru', "{$user['name']} menambahkan $resource #$newId", 'success');
        if ($resource === 'orders') notify((int)$user['id'], 'Pesanan dibuat', "Pesanan #$newId berhasil dibuat", 'success');
        out(['success' => true, 'message' => 'Data berhasil ditambahkan', 'id' => $newId], 201);
    }

    if (!$id) fail('ID wajib diisi', 422);

    if ($method === 'PUT' || $method === 'PATCH') {
        if (!$data) fail('Data kosong', 422);
        $set = implode(',', array_map(fn($c) => "$c = ?", array_keys($data)));
        db()->prepare("UPDATE $resource SET $set WHERE id = ?")->execute([...array_values($data), $id]);
        if ($resource === 'orders' && isset($data['status'])) {
            $o = db()->prepare('SELECT user_id FROM orders WHERE id = ?'); $o->execute([$id]);
            notify((int)$o->fetchColumn(), 'Status pesanan', "Pesanan #$id kini berstatus {$data['status']}", 'info');
        }
        notify(null, ucfirst($resource) . ' diperbarui', "{$user['name']} memperbarui $resource #$id", 'info');
        out(['success' => true, 'message' => 'Data berhasil diperbarui']);
    }

    if ($method === 'DELETE') {
        $st = db()->prepare("DELETE FROM $resource WHERE id = ?"); $st->execute([$id]);
        if (!$st->rowCount()) fail('Data tidak ditemukan', 404);
        notify(null, ucfirst($resource) . ' dihapus', "{$user['name']} menghapus $resource #$id", 'warning');
        out(['success' => true, 'message' => 'Data berhasil dihapus']);
    }
    fail('Method not allowed', 405);
} catch (PDOException $ex) {
    fail($ex->getCode() == 23000 ? 'Data duplikat atau masih dipakai relasi lain' : 'Database error', 500);
}
