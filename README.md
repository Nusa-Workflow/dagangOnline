# dagangOnline — Platform Solusi Digital & Ekosistem Bisnis

Repository: `https://github.com/Nusa-Workflow/dagangOnline.git`  
Stack: **ASP.NET Core Razor Pages (.NET 10.0), EF Core, PostgreSQL / In-Memory, Identity RBAC, REST v1 & gRPC Services, PWA**

---

## Ringkasan Eksekusi Sprint Audit & Upgrade

Semua task dikerjakan berbasis arsitektur existing tanpa rewrite dari nol, memenuhi **4 Verification Gate** (Mismatch, Failed Script, Problem, Posisi).

### Matriks Status Gate Per Task

| Sprint | Task | Mismatch | Failed Script | Problem | Posisi | Status |
|---|---|:---:|:---:|:---:|:---:|:---:|
| **Sprint 0** | Audit Codebase (Read-Only) | OK | OK | OK | OK | **DONE** |
| **Sprint 1** | 1.1 Design Tokens & Color Consistency | OK | OK | OK | OK | **DONE** |
| | 1.2 Button System & Loading State | OK | OK | OK | OK | **DONE** |
| | 1.3 Form & Placeholder UX (Bahasa Indonesia) | OK | OK | OK | OK | **DONE** |
| | 1.4 Navbar & Global Layout (_Layout.cshtml) | OK | OK | OK | OK | **DONE** |
| | 1.5 Typography & Featured Portfolio Pass | OK | OK | OK | OK | **DONE** |
| | 1.6 CRUD Pattern Consistency (Modal, Toast, Skeletons) | OK | OK | OK | OK | **DONE** |
| | 1.7 Register & Login Flow Audit (Password Toggle, Role Redirect) | OK | OK | OK | OK | **DONE** |
| | 1.8 Role Mitra: Product Content Management & Ownership | OK | OK | OK | OK | **DONE** |
| | 1.9 Role Human Agent: Review Queue, Approve/Reject & Audit | OK | OK | OK | OK | **DONE** |
| **Sprint 2** | 2.1 Responsive Testing & Table/Flex Layout | OK | OK | OK | OK | **DONE** |
| | 2.2 UX States (Empty, Loading, Error, Alerts) | OK | OK | OK | OK | **DONE** |
| | 2.3 Fitur Relevan (Search UX, Service Links) | OK | OK | OK | OK | **DONE** |
| **Sprint 3** | 3.1 PWA Manifest & Multi-size Icons | OK | OK | OK | OK | **DONE** |
| | 3.2 Service Worker & Caching Strategy | OK | OK | OK | OK | **DONE** |
| | 3.3 Installability & Session Security | OK | OK | OK | OK | **DONE** |
| | 3.4 Online/Offline Indicator Banner | OK | OK | OK | OK | **DONE** |
| **Sprint 4** | 4.1 Accessibility (a11y & ARIA) | OK | OK | OK | OK | **DONE** |
| | 4.2 Performance & Bundle Optimization | OK | OK | OK | OK | **DONE** |
| | 4.3 Code Quality & CSP Unobtrusive JS | OK | OK | OK | OK | **DONE** |
| | 4.4 Security Sanity Check & RBAC | OK | OK | OK | OK | **DONE** |
| | 4.5 Full Build & Test Regression | OK | OK | OK | OK | **DONE** |

---

## Rincian Perubahan & File yang Berubah

### 1. File yang Berubah (Per Sprint)
- **Sprint 1 (Fondasi UI, Auth, Mitra Scoping & Human Agent Moderation)**:
  - `Authorization/RoleConstants.cs`: Penambahan role `RoleConstants.Agent = "Agent"`.
  - `Authorization/AuthorizationPolicies.cs`: Penambahan `RequireAgent` dan `RequireAgentOrAdmin`.
  - `Domain/PublicationStatus.cs`: Penambahan status `PendingReview = 3` dan `Rejected = 4`.
  - `Domain/Product.cs`: Penambahan kepemilikan produk (`OwnerId` & `OwnerName`).
  - `Domain/Agents/ReviewTask.cs`: Entitas tugas review antrean kurasi.
  - `Domain/Agents/ModerationDecision.cs`: Entitas audit trail riwayat keputusan moderasi kurator internal.
  - `Data/ApplicationDbContext.cs`: Registrasi DbSets `ReviewTasks` dan `ModerationDecisions` serta konfigurasi relationship.
  - `Services/AgentReviewService.cs`: Service logika bisnis moderasi produk (antrean, persetujuan, penolakan dengan alasan, notifikasi Mitra, dan audit logging).
  - `Controllers/AgentController.cs`: REST API untuk Human Agent queue dan persetujuan/penolakan.
  - `Pages/Agent/Index.cshtml` & `.cs`: Dashboard antrean review khusus Human Agent.
  - `Pages/Agent/Review.cshtml` & `.cs`: Halaman detail kurasi produk dengan form Approve & Reject beralasan.
  - `Pages/Dashboard/Mitra/Index.cshtml` & `.cs`: Scoping ketat produk milik Mitra sendiri di backend, edit modal, delete confirmation modal, notifikasi hasil review, dan auto-enqueue ke review queue saat tambah/ubah produk.
  - `Controllers/Api/v1/ProductsController.cs`: Penegakan otorisasi kepemilikan Mitra pada Create/Update/Delete dan status `PendingReview`.
  - `Pages/Account/Login.cshtml` & `.cs`: Password visibility toggle button, pesan error login generik tanpa membocorkan eksistensi akun, autofill akun demo Admin & Agent, dan redirect sesuai role (Admin, Agent, Mitra, User).
  - `Pages/Account/Register.cshtml` & `.cs`: Panduan kriteria sandi terlihat di awal, password visibility toggle, validasi kesesuaian sandi real-time, dan pesan actionable untuk duplicate email.
  - `Pages/Shared/_Layout.cshtml`: Penambahan item menu moderasi untuk Human Agent, badge Agent, dan container toast aplikasi global (`#appToast`).
  - `wwwroot/js/site.js`: Reusable `showAppToast`, password visibility toggle handler, real-time match checker, double submit prevention, dan Mitra edit modal auto-prefill.
  - `wwwroot/css/site.css`: Definisi CSS design tokens (`--primary`, dsb.), button system, dan loading states.
  - `Application/Services/SecurityHeadersMiddleware.cs`: Penyesuaian CSP untuk mengizinkan font Google (`fonts.googleapis.com` & `fonts.gstatic.com`).

- **Sprint 2 (Responsive & UX States)**:
  - `Pages/Services.cshtml`: Penggantian dead link menjadi direct route ke konsultasi layanan, serta penambahan rich empty state.
  - `Pages/Portfolio.cshtml`: Penambahan rich empty state dengan ikon dan direct CTA.
  - `Pages/Search.cshtml`: Penambahan suggestion chips pencarian populer dan rich empty state.
  - `wwwroot/css/site.css`: Perbaikan flex layout body min-height dan touch scrollbar untuk `.table-responsive`.

- **Sprint 3 (PWA)**:
  - `wwwroot/manifest.webmanifest` & `manifest.json`: PWA manifest metadata standar.
  - `wwwroot/icons/`: Ikon standar PWA (`icon-192.png`, `icon-512.png`, `icon-maskable.png`).
  - `wwwroot/offline.html`: Halaman fallback offline mandiri.
  - `wwwroot/sw.js`: Service worker dengan strategi Cache-First untuk static assets, Network-First untuk public dynamic pages, dan Network-Only untuk auth/dashboard/API endpoints.
  - `Program.cs`: Registrasi MIME type `.webmanifest` pada `StaticFileOptions`.
  - `Pages/Shared/_Layout.cshtml`: Penambahan tag manifest, apple-touch-icon, theme-color, dan offline indicator banner.

- **Sprint 4 (Accessibility, Security & Regresi)**:
  - `Views/ApiManagement/Index.cshtml`: Pemisahan inline script menjadi external script untuk kepatuhan CSP.
  - `wwwroot/js/api-management.js`: Handler modular sandbox API tester tanpa inline script/onclick.
  - `dagangOnline.Tests/UnitTest1.cs`: 24 unit tests komprehensif mencakup Identity, RBAC, Agent Review workflow, Mitra product ownership isolation, dan Public Catalog filtering.

---

### 2. Fitur yang Ditambahkan
1. **PWA (Progressive Web App)**: Installable, cache shell offline-first, offline fallback page, dan dynamic status offline indicator.
2. **Role Mitra & Product Content Management**: Scoping produk milik sendiri di backend, CRUD modal terpadu, review status badges, dan tab notifikasi hasil moderasi.
3. **Role Human Agent & Review Workflow**: Antrean moderasi terpisah dari Admin, approval publikasi katalog, penolakan dengan catatan revisi, dan audit log otomatis.
4. **Enhanced Auth Flow UX & Security**: Password show/hide toggle, real-time match indicator, dan generic safe login errors.
5. **Unified Feedback & Toast**: Komponen toast global dengan styling semantik dan double submit protection.
6. **Featured Portfolio Showcase**: Halaman Beranda (`/Index`) menampilkan portofolio dinamis dari database.
7. **Pencarian Cepat dengan Saran Populer**: Suggestion chips (`Web Development`, `Digitalisasi UMKM`, `Cloud`, dsb.) pada `/Search`.

---

### 3. Hasil Verifikasi Build & Test
- **`dotnet build --no-incremental`**: **Succeeded (0 Error)**
- **`dotnet test`**: **24 Lolos dari 24 Pengujian (100% Pass, 0 Gagal, 0 Dilewati)**
