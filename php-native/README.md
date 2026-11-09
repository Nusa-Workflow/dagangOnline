# dagangOnline — PHP Native Edition

**Developed by NussCorp Technologies**

A light-theme PWA for UMKM commerce, built with **native PHP 8.1+, Bootstrap 5.3, and MySQL 8**. It runs on Laragon (Apache + MySQL + phpMyAdmin). It provides role-based CRUD for **Super Admin, Admin, and Customer**, a REST API, real-time notifications over Server-Sent Events, and Bootstrap modals, toasts, and alerts for every action.

> This lives in `php-native/` beside the existing ASP.NET Core app, so the two stacks stay independent.

---

## 1. Installation (Laragon)

1. Start **Apache** and **MySQL** in Laragon.
2. Create the database: open **phpMyAdmin** → *Import* → choose `database/schema.sql`. Or use the CLI:
   ```bash
   mysql -uroot < database/schema.sql
   ```
3. Create the demo accounts (passwords are hashed with `password_hash`):
   ```bash
   php database/seed_users.php
   ```
4. Expose the folder in Laragon's `www`. Run this in an admin PowerShell:
   ```powershell
   New-Item -ItemType Junction -Path C:\laragon\www\dagangonline-php -Target <repo>\php-native
   ```
5. Open `http://localhost/dagangonline-php/`.

| Role | Email | Password |
|---|---|---|
| Super Admin | superadmin@nusscorp.test | password123 |
| Admin | admin@nusscorp.test | password123 |
| Customer | customer@nusscorp.test | password123 |

Change these passwords before any deployment. You can override the DB settings with the `DB_HOST`, `DB_NAME`, `DB_USER`, and `DB_PASS` environment variables (`config/database.php`).

> **Real-time note:** `api/stream.php` keeps its connection open for 25 seconds. `php -S` handles only one request at a time on Windows, so use Laragon's Apache for the real-time features.

---

## 2. Folder structure

```
php-native/
├── api/
│   ├── index.php        # REST API (GET/POST/PUT/PATCH/DELETE)
│   └── stream.php       # SSE real-time notifications
├── assets/{css,js,icons}
├── config/database.php  # PDO connection
├── database/
│   ├── schema.sql       # DDL + seed categories/products
│   └── seed_users.php   # demo accounts
├── includes/{auth.php, head.php}
├── index.php            # SPA dashboard (Bootstrap 5)
├── login.php · register.php · logout.php
├── manifest.json · sw.js · offline.html   # PWA
└── docs/diagrams/       # tldraw files (.tldr)
```

---

## 3. REST API

Base: `api/index.php?resource={name}[&id={id}]`. All requests require a session. Every write must send the `X-CSRF-Token` header. Responses use the JSON format `{ success, message?, data? }`.

| Resource | GET | POST | PUT/PATCH | DELETE |
|---|---|---|---|---|
| `products` | all roles | admin+ | admin+ | admin+ |
| `categories` | all roles | admin+ | admin+ | admin+ |
| `orders` | customer: own orders only · admin+: all | all roles (stock decremented) | admin+ (status) | admin+ |
| `users` | admin+ | super_admin | super_admin | super_admin (cannot delete self) |
| `notifications` | own + broadcast (admin) | – | mark all as read | – |
| `stats` | dashboard summary | – | – | – |

Real time: `GET api/stream.php?since={lastId}` (EventSource, event `notification`). If the browser does not support SSE, the client falls back to polling every 5 seconds.

Example:
```bash
curl -b ck -X POST -H "X-CSRF-Token: <token>" -H "Content-Type: application/json" \
  -d '{"name":"Kopi","price":20000,"stock":5,"category_id":1}' \
  "http://localhost/dagangonline-php/api/index.php?resource=products"
```

---

## 4. Diagrams

The diagrams are written in Mermaid so they render directly on GitHub. To edit them on **tldraw.com**, see section 5.

### 4.1 System architecture (SDE)

```mermaid
flowchart LR
  subgraph Client["Client (Browser / PWA installed)"]
    UI["Bootstrap 5 SPA<br/>index.php + app.js"]
    SW["Service Worker<br/>sw.js (cache static, offline.html)"]
    ES["EventSource"]
  end
  subgraph Server["Laragon — Apache + PHP 8"]
    AUTH["includes/auth.php<br/>Session · RBAC · CSRF"]
    API["api/index.php<br/>REST CRUD"]
    SSE["api/stream.php<br/>SSE push"]
    PAGES["login / register / logout"]
  end
  DB[("MySQL 8<br/>dagang_online")]
  PMA["phpMyAdmin"]

  UI -- "fetch JSON + X-CSRF-Token" --> API
  ES -- "text/event-stream" --> SSE
  UI --> SW
  PAGES --> AUTH
  API --> AUTH
  SSE --> AUTH
  API -- PDO --> DB
  SSE -- "poll every 2s" --> DB
  PMA --> DB
```

### 4.2 Use case diagram

```mermaid
flowchart LR
  C(("👤 Customer"))
  A(("👤 Admin"))
  S(("👤 Super Admin"))
  subgraph SYS["dagangOnline System"]
    UC1([Register / Login])
    UC2([View products])
    UC3([Create order])
    UC4([View own orders])
    UC5([Receive real-time notifications])
    UC6([CRUD products])
    UC7([CRUD categories])
    UC8([Update order status])
    UC9([View users])
    UC10([CRUD users & roles])
    UC11([Install PWA])
  end
  C --- UC1 & UC2 & UC3 & UC4 & UC5 & UC11
  A --- UC1 & UC2 & UC5 & UC6 & UC7 & UC8 & UC9
  S --- UC10
  S -. "inherits" .-> A
  UC3 -. "&lt;&lt;include&gt;&gt;" .-> UC2
```

### 4.3 Activity diagram: customer order (swimlanes)

```mermaid
flowchart TB
  subgraph Customer
    a0((●)) --> a1[Open product page] --> a2[Click 'Order'] --> a3[Fill in quantity in modal] --> a4[Click Save]
    a9[See success toast] --> a10((◉))
    a11[See error toast] --> a3
  end
  subgraph System["System (API)"]
    a4 --> b1{Session & CSRF valid?}
    b1 -- No --> b2[401/419] --> a11
    b1 -- Yes --> b3{Stock sufficient?}
    b3 -- No --> b4[422 Insufficient stock] --> a11
    b3 -- Yes --> b5[Decrement stock · INSERT order] --> b6[INSERT notifications]
    b6 --> a9
  end
  subgraph Admin
    b6 -.SSE.-> c1[Receive toast 'New order'] --> c2[Update status] --> c3[Customer is notified]
  end
```

### 4.4 Activity diagram: login and RBAC

```mermaid
flowchart TB
  s((●)) --> l1[Enter email & password] --> l2{CSRF valid?}
  l2 -- No --> e1[Alert: session expired] --> l1
  l2 -- Yes --> l3{password_verify & is_active?}
  l3 -- No --> e2[Alert: wrong email/password] --> l1
  l3 -- Yes --> l4[session_regenerate_id] --> l5{Role}
  l5 -- super_admin --> m1[Full menu + user CRUD]
  l5 -- admin --> m2[Products, Categories, Orders, Users read-only]
  l5 -- customer --> m3[Products + My Orders]
  m1 & m2 & m3 --> f((◉))
```

### 4.5 Class diagram (UML)

```mermaid
classDiagram
  class User {
    +int id
    +string name
    +string email
    -string password
    +enum role
    +string phone
    +bool is_active
  }
  class Category { +int id +string name +string description }
  class Product {
    +int id
    +int category_id
    +string name
    +decimal price
    +int stock
  }
  class Order {
    +int id
    +int user_id
    +int product_id
    +int quantity
    +decimal total
    +enum status
  }
  class Notification {
    +int id
    +int? user_id
    +string title
    +string message
    +enum type
    +bool is_read
  }
  class ApiController {
    +get(resource, id)
    +post(resource, data)
    +put(resource, id, data)
    +delete(resource, id)
    -authorize(role)
  }
  class Auth {
    +current_user()
    +has_role(min)
    +csrf_token()
    +notify(userId, title, msg, type)
  }
  Category "1" --> "0..*" Product
  User "1" --> "0..*" Order
  Product "1" --> "0..*" Order
  User "1" --> "0..*" Notification
  ApiController ..> Auth
  ApiController ..> Product
  ApiController ..> Order
```

### 4.6 Sequence diagram: real-time notifications

```mermaid
sequenceDiagram
  participant C as Customer (browser)
  participant API as api/index.php
  participant DB as MySQL
  participant SSE as api/stream.php
  participant A as Admin (browser)
  A->>SSE: EventSource(since=lastId)
  C->>API: POST orders {product_id, quantity}
  API->>DB: UPDATE stock, INSERT order, INSERT notification
  API-->>C: 201 {success}
  C->>C: Success toast
  loop every 2s
    SSE->>DB: SELECT notifications WHERE id > last
  end
  SSE-->>A: event: notification
  A->>A: Toast + reload table + OS Notification
```

### 4.7 ERD

```mermaid
erDiagram
  users ||--o{ orders : places
  products ||--o{ orders : contains
  categories ||--o{ products : groups
  users ||--o{ notifications : receives
```

---

## 5. Diagrams on tldraw.com

tldraw does not have an official Mermaid import, so you must create the `.tldr` files yourself:

1. Open https://www.tldraw.com → create a new board.
2. Redraw each diagram from section 4 (architecture, use case, activity, class, sequence, ERD).
3. **Menu → File → Save as** (or *Export → .tldr*), and save it to `docs/diagrams/` using these names:
   `architecture.tldr`, `use-case.tldr`, `activity-order.tldr`, `activity-login.tldr`, `class-uml.tldr`, `sequence-realtime.tldr`.
4. Optional: *Share → Copy link* and add the link to this table.

| Diagram | tldraw file | Share link |
|---|---|---|
| Architecture SDE | `docs/diagrams/architecture.tldr` | _(not yet)_ |
| Use case | `docs/diagrams/use-case.tldr` | _(not yet)_ |
| Activity: order | `docs/diagrams/activity-order.tldr` | _(not yet)_ |
| Activity: login | `docs/diagrams/activity-login.tldr` | _(not yet)_ |
| Class UML | `docs/diagrams/class-uml.tldr` | _(not yet)_ |
| Sequence realtime | `docs/diagrams/sequence-realtime.tldr` | _(not yet)_ |

---

## 6. UI/UX features

- **Light theme only** (`data-bs-theme="light"`, `color-scheme: light`).
- Responsive: the sidebar becomes an offcanvas on mobile, and tables turn into stacked cards below 576px.
- Every action has feedback: a create/edit **modal** with validation, a delete/logout **confirm modal**, success/error/warning **toasts**, an **alert** on login/register, a notification **badge** in the navbar, and an OS **notification** when the tab is in the background.
- PWA: installable (the "Install App" button), offline fallback, and cached static assets. The API is never cached.

## 7. Security

PDO prepared statements · `password_hash` · `session_regenerate_id` on login · CSRF tokens on every form and write request · HttpOnly + SameSite cookies · column whitelist per resource · output escaping (`htmlspecialchars` / `esc()` in JS) · RBAC checked on the server.

---

© NussCorp Technologies — dagangOnline
