<?php
require_once __DIR__ . '/includes/auth.php';
if (current_user()) { header('Location: index.php'); exit; }
$error = null;
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $name = trim($_POST['name'] ?? '');
    $email = trim($_POST['email'] ?? '');
    $pass = $_POST['password'] ?? '';
    if (!hash_equals(csrf_token(), $_POST['csrf'] ?? '')) $error = 'Sesi kedaluwarsa, silakan coba lagi.';
    elseif ($name === '' || !filter_var($email, FILTER_VALIDATE_EMAIL)) $error = 'Nama dan email valid wajib diisi.';
    elseif (strlen($pass) < 8) $error = 'Password minimal 8 karakter.';
    else {
        try {
            db()->prepare("INSERT INTO users (name,email,password,role) VALUES (?,?,?,'customer')")
                ->execute([$name, $email, password_hash($pass, PASSWORD_DEFAULT)]);
            notify(null, 'Customer baru', "$name mendaftar sebagai customer", 'success');
            $_SESSION['flash'] = ['success', 'Registrasi berhasil, silakan masuk.'];
            header('Location: login.php'); exit;
        } catch (PDOException $e) {
            $error = 'Email sudah terdaftar.';
        }
    }
}
$title = 'Daftar';
include __DIR__ . '/includes/head.php';
?>
<body class="auth-bg d-flex align-items-center min-vh-100">
<main class="container" style="max-width:420px">
  <div class="card shadow-sm border-0">
    <div class="card-body p-4">
      <h1 class="h4 fw-bold text-center mb-4">Daftar Customer</h1>
      <?php if ($error): ?><div class="alert alert-danger py-2"><?= e($error) ?></div><?php endif; ?>
      <form method="post">
        <input type="hidden" name="csrf" value="<?= csrf_token() ?>">
        <div class="form-floating mb-3"><input class="form-control" id="name" name="name" placeholder="n" required value="<?= e($_POST['name'] ?? '') ?>"><label for="name">Nama lengkap</label></div>
        <div class="form-floating mb-3"><input type="email" class="form-control" id="email" name="email" placeholder="e" required value="<?= e($_POST['email'] ?? '') ?>"><label for="email">Email</label></div>
        <div class="form-floating mb-3"><input type="password" class="form-control" id="password" name="password" placeholder="p" minlength="8" required><label for="password">Password (min. 8)</label></div>
        <button class="btn btn-primary w-100 py-2">Daftar</button>
      </form>
      <p class="text-center small mt-3 mb-0"><a href="login.php">Kembali ke login</a></p>
    </div>
  </div>
</main>
</body>
</html>
