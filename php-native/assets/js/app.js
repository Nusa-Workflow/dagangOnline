/* dagangOnline — Bootstrap 5 SPA dashboard (vanilla JS) */
(() => {
  const { csrf, user, flash } = window.APP;
  const ROLE = { super_admin: 3, admin: 2, customer: 1 };
  const can = (min) => ROLE[user.role] >= ROLE[min];
  const $ = (s) => document.querySelector(s);
  const esc = (v) => String(v ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const rupiah = (n) => 'Rp ' + Number(n || 0).toLocaleString('id-ID');
  const formModal = new bootstrap.Modal('#formModal');
  const confirmModal = new bootstrap.Modal('#confirmModal');

  // ---------- API ----------
  async function api(resource, { method = 'GET', id, body, query = '' } = {}) {
    const url = `api/index.php?resource=${resource}${id ? `&id=${id}` : ''}${query}`;
    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json', 'X-CSRF-Token': csrf },
      body: body ? JSON.stringify(body) : undefined,
      credentials: 'same-origin',
    });
    if (res.status === 401) { location.href = 'login.php'; throw new Error('Sesi berakhir'); }
    const json = await res.json().catch(() => ({ success: false, message: 'Respons server tidak valid' }));
    if (!res.ok || !json.success) throw new Error(json.message || `HTTP ${res.status}`);
    return json;
  }

  // ---------- Toast & confirm ----------
  const ICON = { success: 'check-circle-fill', danger: 'x-circle-fill', warning: 'exclamation-triangle-fill', info: 'info-circle-fill' };
  function toast(message, type = 'success', title) {
    const el = document.createElement('div');
    el.className = `toast align-items-center border-0 text-bg-${type}`;
    el.role = 'alert';
    el.innerHTML = `<div class="d-flex"><div class="toast-body"><i class="bi bi-${ICON[type] || ICON.info} me-2"></i>${title ? `<strong>${esc(title)}</strong><br>` : ''}${esc(message)}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
    $('#toasts').appendChild(el);
    const t = new bootstrap.Toast(el, { delay: 4000 });
    el.addEventListener('hidden.bs.toast', () => el.remove());
    t.show();
  }
  function confirmBox({ title = 'Yakin?', text = '', ok = 'Ya, lanjutkan', variant = 'danger' } = {}) {
    return new Promise((resolve) => {
      $('#confirmTitle').textContent = title;
      $('#confirmText').textContent = text;
      const btn = $('#confirmOk');
      btn.textContent = ok;
      btn.className = `btn btn-${variant}`;
      let answered = false;
      btn.onclick = () => { answered = true; confirmModal.hide(); resolve(true); };
      $('#confirmModal').addEventListener('hidden.bs.modal', () => !answered && resolve(false), { once: true });
      confirmModal.show();
    });
  }

  // ---------- Resource definitions ----------
  const lookups = {};
  async function loadLookup(name) { lookups[name] = (await api(name)).data; return lookups[name]; }

  const RES = {
    products: {
      title: 'Produk', write: 'admin',
      columns: [['name', 'Nama'], ['category', 'Kategori'], ['price', 'Harga', rupiah], ['stock', 'Stok', (v) => `<span class="badge text-bg-${v > 10 ? 'success' : v > 0 ? 'warning' : 'danger'}">${v}</span>`]],
      fields: () => [
        { name: 'name', label: 'Nama produk', required: true },
        { name: 'category_id', label: 'Kategori', type: 'select', options: (lookups.categories || []).map((c) => [c.id, c.name]) },
        { name: 'price', label: 'Harga (Rp)', type: 'number', required: true, min: 0 },
        { name: 'stock', label: 'Stok', type: 'number', required: true, min: 0 },
        { name: 'description', label: 'Deskripsi', type: 'textarea' },
      ],
      before: () => loadLookup('categories'),
      extraAction: (row) => (user.role === 'customer' && row.stock > 0 ? `<button class="btn btn-sm btn-success" data-act="buy" data-id="${row.id}"><i class="bi bi-cart-plus"></i> Pesan</button>` : ''),
    },
    categories: {
      title: 'Kategori', write: 'admin',
      columns: [['name', 'Nama'], ['description', 'Deskripsi']],
      fields: () => [{ name: 'name', label: 'Nama kategori', required: true }, { name: 'description', label: 'Deskripsi', type: 'textarea' }],
    },
    orders: {
      title: user.role === 'customer' ? 'Pesanan Saya' : 'Pesanan', write: 'admin', canCreate: 'customer',
      columns: [['id', '#'], ['customer', 'Customer'], ['product', 'Produk'], ['quantity', 'Qty'], ['total', 'Total', rupiah],
        ['status', 'Status', (v) => `<span class="badge status-${v}">${v}</span>`], ['created_at', 'Tanggal', (v) => new Date(v.replace(' ', 'T')).toLocaleString('id-ID')]],
      fields: (row) => row
        ? [{ name: 'status', label: 'Status pesanan', type: 'select', required: true, options: ['pending', 'paid', 'shipped', 'completed', 'cancelled'].map((s) => [s, s]) }]
        : [{ name: 'product_id', label: 'Produk', type: 'select', required: true, options: (lookups.products || []).filter((p) => p.stock > 0).map((p) => [p.id, `${p.name} — ${rupiah(p.price)} (stok ${p.stock})`]) },
           { name: 'quantity', label: 'Jumlah', type: 'number', required: true, min: 1, value: 1 }],
      before: () => loadLookup('products'),
    },
    users: {
      title: 'Pengguna', write: 'super_admin',
      columns: [['name', 'Nama'], ['email', 'Email'], ['role', 'Role', (v) => `<span class="badge text-bg-${{ super_admin: 'dark', admin: 'primary', customer: 'secondary' }[v]}">${v}</span>`],
        ['is_active', 'Aktif', (v) => (Number(v) ? '<i class="bi bi-check-circle-fill text-success"></i>' : '<i class="bi bi-x-circle-fill text-danger"></i>')]],
      fields: (row) => [
        { name: 'name', label: 'Nama', required: true },
        { name: 'email', label: 'Email', type: 'email', required: true },
        { name: 'phone', label: 'Telepon' },
        { name: 'role', label: 'Role', type: 'select', required: true, options: [['customer', 'Customer'], ['admin', 'Admin'], ['super_admin', 'Super Admin']] },
        { name: 'is_active', label: 'Status', type: 'select', options: [['1', 'Aktif'], ['0', 'Nonaktif']], value: '1' },
        { name: 'password', label: row ? 'Password baru (kosongkan jika tetap)' : 'Password', type: 'password', required: !row, minlength: 8 },
      ],
    },
  };

  // ---------- Pages ----------
  let current = null;
  let rowsCache = [];

  async function render(page) {
    if (page !== 'dashboard' && !RES[page]) page = 'dashboard';
    if (page === 'users' && !can('admin')) page = 'dashboard';
    if (page === 'categories' && !can('admin')) page = 'dashboard';
    current = page;
    document.querySelectorAll('[data-page]').forEach((a) => a.classList.toggle('active', a.dataset.page === page));
    bootstrap.Offcanvas.getInstance('#sidebar')?.hide();
    $('#pageActions').innerHTML = '';
    $('#pageBody').innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>';
    try {
      page === 'dashboard' ? await renderDashboard() : await renderTable(page);
    } catch (e) {
      $('#pageBody').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-octagon me-1"></i>${esc(e.message)}</div>`;
    }
  }

  async function renderDashboard() {
    $('#pageTitle').textContent = 'Dashboard';
    const s = (await api('stats')).data;
    const cards = [
      ['Produk', s.products, 'box-seam', 'primary', 'products'],
      ['Kategori', s.categories, 'tags', 'info', can('admin') ? 'categories' : null],
      [user.role === 'customer' ? 'Pesanan Saya' : 'Total Pesanan', s.orders, 'receipt', 'success', 'orders'],
      [user.role === 'customer' ? 'Total Belanja' : 'Pendapatan', rupiah(s.revenue), 'cash-stack', 'warning', 'orders'],
    ];
    if (s.users !== null) cards.push(['Pengguna', s.users, 'people', 'secondary', 'users']);
    $('#pageBody').innerHTML = `
      <div class="alert alert-primary border-0 d-flex align-items-center gap-2"><i class="bi bi-stars"></i>
        <div>Halo <strong>${esc(user.name)}</strong>, selamat datang di panel <strong>${esc(user.role.replace('_', ' '))}</strong>.</div></div>
      <div class="row g-3">${cards.map(([l, v, i, c, link]) => `
        <div class="col-6 col-md-4 col-xl">
          <div class="card stat-card border-0 shadow-sm h-100 ${link ? 'clickable' : ''}" ${link ? `data-go="${link}"` : ''}>
            <div class="card-body d-flex align-items-center gap-3">
              <div class="stat-icon bg-${c}-subtle text-${c}"><i class="bi bi-${i}"></i></div>
              <div><div class="small text-secondary">${l}</div><div class="fs-5 fw-bold">${esc(v)}</div></div>
            </div>
          </div>
        </div>`).join('')}
      </div>`;
  }

  async function renderTable(page) {
    const cfg = RES[page];
    $('#pageTitle').textContent = cfg.title;
    if (cfg.before) await cfg.before();
    const { data } = await api(page);
    rowsCache = data;
    const canWrite = can(cfg.write);
    const canCreate = canWrite || (cfg.canCreate && can(cfg.canCreate) && user.role === 'customer');
    $('#pageActions').innerHTML = `
      <input type="search" class="form-control form-control-sm" id="searchBox" placeholder="Cari..." style="max-width:200px">
      <button class="btn btn-sm btn-outline-secondary" data-act="refresh" title="Muat ulang"><i class="bi bi-arrow-clockwise"></i></button>
      ${canCreate ? '<button class="btn btn-sm btn-primary text-nowrap" data-act="create"><i class="bi bi-plus-lg"></i> Tambah</button>' : ''}`;
    drawRows(cfg, data, canWrite);
    $('#searchBox').addEventListener('input', (e) => {
      const q = e.target.value.toLowerCase();
      drawRows(cfg, rowsCache.filter((r) => Object.values(r).join(' ').toLowerCase().includes(q)), canWrite);
    });
  }

  function drawRows(cfg, rows, canWrite) {
    const cols = cfg.columns.filter(([k]) => !(k === 'customer' && user.role === 'customer'));
    const hasActions = canWrite || cfg.extraAction;
    if (!rows.length) {
      $('#pageBody').innerHTML = '<div class="card border-0 shadow-sm"><div class="card-body text-center text-secondary py-5"><i class="bi bi-inbox display-6 d-block mb-2"></i>Belum ada data</div></div>';
      return;
    }
    $('#pageBody').innerHTML = `
      <div class="card border-0 shadow-sm"><div class="table-responsive">
        <table class="table table-hover align-middle mb-0 table-stack">
          <thead class="table-light"><tr>${cols.map(([, l]) => `<th>${l}</th>`).join('')}${hasActions ? '<th class="text-end">Aksi</th>' : ''}</tr></thead>
          <tbody>${rows.map((r) => `<tr>${cols.map(([k, l, f]) => `<td data-label="${l}">${f ? f(r[k]) : esc(r[k])}</td>`).join('')}
            ${hasActions ? `<td class="text-end text-nowrap">${cfg.extraAction ? cfg.extraAction(r) : ''}
              ${canWrite ? `<button class="btn btn-sm btn-outline-primary" data-act="edit" data-id="${r.id}" title="Ubah"><i class="bi bi-pencil"></i></button>
              <button class="btn btn-sm btn-outline-danger" data-act="delete" data-id="${r.id}" title="Hapus"><i class="bi bi-trash"></i></button>` : ''}</td>` : ''}</tr>`).join('')}
          </tbody>
        </table>
      </div></div>`;
  }

  // ---------- Form ----------
  let formState = null;
  function openForm(page, row = null, preset = {}) {
    const cfg = RES[page];
    formState = { page, id: row?.id ?? null };
    $('#formTitle').textContent = `${row ? 'Ubah' : 'Tambah'} ${cfg.title}`;
    $('#formFields').innerHTML = cfg.fields(row).map((f) => {
      const v = esc(preset[f.name] ?? row?.[f.name] ?? f.value ?? '');
      const req = f.required ? 'required' : '';
      const attrs = `class="form-${f.type === 'select' ? 'select' : 'control'}" id="f_${f.name}" name="${f.name}" ${req} ${f.min !== undefined ? `min="${f.min}"` : ''} ${f.minlength ? `minlength="${f.minlength}"` : ''}`;
      let input;
      if (f.type === 'select') input = `<select ${attrs}><option value="">— pilih —</option>${f.options.map(([ov, ol]) => `<option value="${esc(ov)}" ${String(ov) === v ? 'selected' : ''}>${esc(ol)}</option>`).join('')}</select>`;
      else if (f.type === 'textarea') input = `<textarea ${attrs} rows="3">${v}</textarea>`;
      else input = `<input type="${f.type || 'text'}" ${attrs} value="${f.type === 'password' ? '' : v}">`;
      return `<div class="mb-3"><label class="form-label" for="f_${f.name}">${f.label}${f.required ? ' <span class="text-danger">*</span>' : ''}</label>${input}<div class="invalid-feedback">Wajib diisi dengan benar.</div></div>`;
    }).join('');
    $('#crudForm').classList.remove('was-validated');
    formModal.show();
  }

  $('#crudForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    if (!form.checkValidity()) { form.classList.add('was-validated'); toast('Periksa kembali isian form.', 'warning'); return; }
    const body = Object.fromEntries(new FormData(form));
    const btn = $('#formSubmit');
    btn.disabled = true; btn.querySelector('.spinner-border').classList.remove('d-none');
    try {
      const r = await api(formState.page, { method: formState.id ? 'PUT' : 'POST', id: formState.id, body });
      formModal.hide();
      toast(r.message, 'success');
      render(current);
    } catch (err) {
      toast(err.message, 'danger', 'Gagal menyimpan');
    } finally {
      btn.disabled = false; btn.querySelector('.spinner-border').classList.add('d-none');
    }
  });

  // ---------- Click delegation ----------
  document.addEventListener('click', async (e) => {
    const go = e.target.closest('[data-go]');
    if (go) { location.hash = go.dataset.go; return; }
    const b = e.target.closest('[data-act]');
    if (!b) return;
    const id = b.dataset.id;
    const row = rowsCache.find((r) => String(r.id) === id);
    switch (b.dataset.act) {
      case 'refresh': render(current); toast('Data dimuat ulang', 'info'); break;
      case 'create': openForm(current); break;
      case 'edit': openForm(current, row); break;
      case 'buy':
        await loadLookup('products');
        openForm('orders', null, { product_id: id, quantity: 1 });
        break;
      case 'delete':
        if (await confirmBox({ title: 'Hapus data?', text: `Data "${row?.name ?? '#' + id}" akan dihapus permanen.`, ok: 'Hapus' })) {
          try { const r = await api(current, { method: 'DELETE', id }); toast(r.message, 'success'); render(current); }
          catch (err) { toast(err.message, 'danger', 'Gagal menghapus'); }
        }
        break;
    }
  });

  $('#logoutBtn').addEventListener('click', async (e) => {
    e.preventDefault();
    if (await confirmBox({ title: 'Keluar?', text: 'Anda akan keluar dari sesi ini.', ok: 'Keluar', variant: 'primary' })) location.href = 'logout.php';
  });

  // ---------- Real-time notifications (SSE with polling fallback) ----------
  const notifs = [];
  function drawNotifs() {
    const unread = notifs.filter((n) => !Number(n.is_read)).length;
    const badge = $('#notifBadge');
    badge.textContent = unread > 99 ? '99+' : unread;
    badge.classList.toggle('d-none', !unread);
    $('#notifList').innerHTML = notifs.length
      ? notifs.slice(0, 15).map((n) => `<div class="list-group-item ${Number(n.is_read) ? '' : 'unread'}">
          <div class="d-flex gap-2"><i class="bi bi-${ICON[n.type] || ICON.info} text-${n.type}"></i>
          <div><div class="fw-semibold small">${esc(n.title)}</div><div class="small text-secondary">${esc(n.message)}</div>
          <div class="text-secondary" style="font-size:.7rem">${esc(n.created_at)}</div></div></div></div>`).join('')
      : '<div class="p-3 text-secondary small">Belum ada notifikasi</div>';
  }
  let lastId = 0;
  function onNotif(n, live) {
    if (notifs.some((x) => x.id === n.id)) return;
    notifs.unshift(n);
    lastId = Math.max(lastId, Number(n.id));
    if (live) {
      toast(n.message, n.type, n.title);
      if (current && current !== 'dashboard') render(current); // live refresh of the open table
      if (document.hidden && 'Notification' in window && Notification.permission === 'granted') new Notification(n.title, { body: n.message, icon: 'assets/icons/icon-192.png' });
    }
  }
  async function initNotifications() {
    try {
      const { data } = await api('notifications');
      data.reverse().forEach((n) => onNotif(n, false));
      drawNotifs();
    } catch { /* ignore */ }
    if ('EventSource' in window) {
      const es = new EventSource(`api/stream.php?since=${lastId}`);
      es.addEventListener('notification', (ev) => { onNotif(JSON.parse(ev.data), true); drawNotifs(); });
    } else {
      setInterval(async () => {
        const { data } = await api('notifications', { query: `&since=${lastId}` });
        data.reverse().forEach((n) => onNotif(n, true)); drawNotifs();
      }, 5000);
    }
    if ('Notification' in window && Notification.permission === 'default') Notification.requestPermission();
  }
  $('#markRead').addEventListener('click', async (e) => {
    e.stopPropagation();
    try { const r = await api('notifications', { method: 'PUT' }); notifs.forEach((n) => (n.is_read = 1)); drawNotifs(); toast(r.message, 'info'); }
    catch (err) { toast(err.message, 'danger'); }
  });

  // ---------- Boot ----------
  window.addEventListener('hashchange', () => render(location.hash.slice(1)));
  render(location.hash.slice(1) || 'dashboard');
  initNotifications();
  if (flash) toast(flash[1], flash[0]);
  window.addEventListener('offline', () => toast('Koneksi terputus — mode offline', 'warning'));
  window.addEventListener('online', () => toast('Koneksi kembali online', 'success'));
})();
