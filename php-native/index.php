<?php
require_once __DIR__ . '/includes/auth.php';
require_login();
$user = current_user();
$flash = $_SESSION['flash'] ?? null; unset($_SESSION['flash']);
$title = 'Dashboard';
$menu = [
    ['dashboard',  'speedometer2', 'Dashboard',  'customer'],
    ['products',   'box-seam',     'Produk',     'customer'],
    ['categories', 'tags',         'Kategori',   'admin'],
    ['orders',     'receipt',      $user['role'] === 'customer' ? 'Pesanan Saya' : 'Pesanan', 'customer'],
    ['users',      'people',       'Pengguna',   'admin'],
];
$roleLabel = ['super_admin' => 'Super Admin', 'admin' => 'Admin', 'customer' => 'Customer'][$user['role']];
include __DIR__ . '/includes/head.php';
?>
<body>
<nav class="navbar navbar-expand-lg bg-white border-bottom sticky-top">
  <div class="container-fluid">
    <button class="btn btn-outline-secondary d-lg-none me-2" data-bs-toggle="offcanvas" data-bs-target="#sidebar" aria-label="Menu"><i class="bi bi-list"></i></button>
    <a class="navbar-brand fw-bold d-flex align-items-center gap-2" href="#dashboard">
      <img src="assets/icons/icon-192.png" width="28" height="28" class="rounded" alt=""><?= APP_NAME ?>
    </a>
    <div class="ms-auto d-flex align-items-center gap-2">
      <button id="installBtn" class="btn btn-sm btn-outline-primary d-none"><i class="bi bi-download"></i><span class="d-none d-sm-inline ms-1">Install App</span></button>
      <div class="dropdown">
        <button class="btn btn-light position-relative" data-bs-toggle="dropdown" aria-label="Notifikasi">
          <i class="bi bi-bell"></i>
          <span id="notifBadge" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger d-none">0</span>
        </button>
        <div class="dropdown-menu dropdown-menu-end p-0 shadow notif-menu">
          <div class="d-flex justify-content-between align-items-center px-3 py-2 border-bottom">
            <strong>Notifikasi</strong><button id="markRead" class="btn btn-link btn-sm p-0">Tandai dibaca</button>
          </div>
          <div id="notifList" class="list-group list-group-flush"><div class="p-3 text-secondary small">Belum ada notifikasi</div></div>
        </div>
      </div>
      <div class="dropdown">
        <button class="btn btn-light d-flex align-items-center gap-2" data-bs-toggle="dropdown">
          <i class="bi bi-person-circle"></i><span class="d-none d-md-inline"><?= e($user['name']) ?></span>
        </button>
        <ul class="dropdown-menu dropdown-menu-end">
          <li><span class="dropdown-item-text small"><?= e($user['email']) ?><br><span class="badge text-bg-primary"><?= $roleLabel ?></span></span></li>
          <li><hr class="dropdown-divider"></li>
          <li><a class="dropdown-item text-danger" href="#" id="logoutBtn"><i class="bi bi-box-arrow-right me-1"></i>Keluar</a></li>
        </ul>
      </div>
    </div>
  </div>
</nav>

<div class="d-flex">
  <aside class="offcanvas-lg offcanvas-start sidebar bg-white border-end" id="sidebar" tabindex="-1">
    <div class="offcanvas-header d-lg-none"><h5 class="offcanvas-title"><?= APP_NAME ?></h5><button class="btn-close" data-bs-dismiss="offcanvas" data-bs-target="#sidebar"></button></div>
    <div class="offcanvas-body flex-column p-3">
      <ul class="nav nav-pills flex-column gap-1 w-100">
        <?php foreach ($menu as [$key, $icon, $label, $min]): if (!has_role($min)) continue; ?>
          <li class="nav-item"><a class="nav-link" href="#<?= $key ?>" data-page="<?= $key ?>"><i class="bi bi-<?= $icon ?> me-2"></i><?= e($label) ?></a></li>
        <?php endforeach; ?>
      </ul>
      <div class="mt-auto pt-4 small text-secondary">&copy; <?= date('Y') ?> <?= APP_COMPANY ?></div>
    </div>
  </aside>

  <main class="flex-grow-1 p-3 p-lg-4 main-content">
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <h1 id="pageTitle" class="h4 fw-bold mb-0">Dashboard</h1>
      <div class="d-flex gap-2" id="pageActions"></div>
    </div>
    <div id="pageBody"></div>
  </main>
</div>

<!-- Form modal (create / edit) -->
<div class="modal fade" id="formModal" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-fullscreen-sm-down">
    <form class="modal-content" id="crudForm" novalidate>
      <div class="modal-header"><h5 class="modal-title" id="formTitle">Form</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
      <div class="modal-body" id="formFields"></div>
      <div class="modal-footer">
        <button type="button" class="btn btn-light" data-bs-dismiss="modal">Batal</button>
        <button type="submit" class="btn btn-primary" id="formSubmit"><span class="spinner-border spinner-border-sm d-none me-1"></span>Simpan</button>
      </div>
    </form>
  </div>
</div>

<!-- Confirm modal -->
<div class="modal fade" id="confirmModal" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered modal-sm">
    <div class="modal-content text-center">
      <div class="modal-body p-4">
        <i id="confirmIcon" class="bi bi-exclamation-triangle text-warning display-5"></i>
        <h5 id="confirmTitle" class="mt-2">Yakin?</h5>
        <p id="confirmText" class="text-secondary small mb-0"></p>
      </div>
      <div class="modal-footer justify-content-center border-0 pt-0">
        <button class="btn btn-light" data-bs-dismiss="modal">Batal</button>
        <button class="btn btn-danger" id="confirmOk">Ya, lanjutkan</button>
      </div>
    </div>
  </div>
</div>

<div class="toast-container position-fixed bottom-0 end-0 p-3" id="toasts"></div>

<script>
  window.APP = {
    csrf: <?= json_encode(csrf_token()) ?>,
    user: <?= json_encode(['id' => $user['id'], 'name' => $user['name'], 'role' => $user['role']]) ?>,
    flash: <?= json_encode($flash) ?>
  };
</script>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
<script src="assets/js/pwa.js"></script>
<script src="assets/js/app.js"></script>
</body>
</html>
