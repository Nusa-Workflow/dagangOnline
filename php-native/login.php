<?php
require_once __DIR__ . '/includes/auth.php';
if (current_user()) { header('Location: index.php'); exit; }
$error = null;
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (!hash_equals(csrf_token(), $_POST['csrf'] ?? '')) {
        $error = 'Sesi kedaluwarsa, silakan coba lagi.';
    } else {
        $st = db()->prepare('SELECT * FROM users WHERE email = ? AND is_active = 1');
        $st->execute([trim($_POST['email'] ?? '')]);
        $u = $st->fetch();
        if ($u && password_verify($_POST['password'] ?? '', $u['password'])) {
            session_regenerate_id(true);
            unset($u['password']);
            $_SESSION['user'] = $u;
            $_SESSION['flash'] = ['success', 'Selamat datang, ' . $u['name'] . '!'];
            header('Location: index.php'); exit;
        }
        $error = 'Email atau password salah.';
    }
}
$title = 'Masuk';
include __DIR__ . '/includes/head.php';
?>
<body class="auth-bg d-flex align-items-center min-vh-100">
<main class="container" style="max-width:420px">
  <div class="card shadow-sm border-0">
    <div class="card-body p-4">
      <div class="text-center mb-4">
        <img src="assets/icons/icon-192.png" width="56" height="56" class="rounded-3 mb-2" alt="">
        <h1 class="h4 fw-bold mb-0"><?= APP_NAME ?></h1>
        <small class="text-secondary">by <?= APP_COMPANY ?></small>
      </div>
      <?php if (!empty($_SESSION['flash'])): [$t, $m] = $_SESSION['flash']; unset($_SESSION['flash']); ?>
        <div class="alert alert-<?= e($t) ?> py-2"><?= e($m) ?></div>
      <?php endif; ?>
      <?php if ($error): ?><div class="alert alert-danger py-2"><i class="bi bi-exclamation-triangle me-1"></i><?= e($error) ?></div><?php endif; ?>
      <form method="post" novalidate class="needs-validation">
        <input type="hidden" name="csrf" value="<?= csrf_token() ?>">
        <div class="form-floating mb-3">
          <input type="email" class="form-control" id="email" name="email" placeholder="email" required value="<?= e($_POST['email'] ?? '') ?>">
          <label for="email">Email</label>
        </div>
        <div class="form-floating mb-3">
          <input type="password" class="form-control" id="password" name="password" placeholder="password" required>
          <label for="password">Password</label>
        </div>
        <button class="btn btn-primary w-100 py-2"><i class="bi bi-box-arrow-in-right me-1"></i>Masuk</button>
      </form>
      <p class="text-center small mt-3 mb-0">Belum punya akun? <a href="register.php">Daftar sebagai customer</a></p>
    </div>
  </div>
</main>
<script src="assets/js/pwa.js"></script>
</body>
</html>
