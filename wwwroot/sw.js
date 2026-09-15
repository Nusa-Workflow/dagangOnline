// dagangOnline PWA Service Worker (v1)
const CACHE_NAME = 'dagangonline-shell-v1';

const PRECACHE_ASSETS = [
    '/',
    '/offline.html',
    '/css/site.css',
    '/js/site.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/manifest.webmanifest',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/favicon.ico'
];

// Sensitive and non-cacheable paths
const NETWORK_ONLY_PATTERNS = [
    /^\/Account\//i,
    /^\/Dashboard\//i,
    /^\/Admin\//i,
    /^\/ApiManagement/i,
    /^\/api\//i,
    /^\/openapi\//i
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(PRECACHE_ASSETS).catch((err) => {
                console.warn('[SW] Pre-caching partial failure:', err);
            });
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) => {
            return Promise.all(
                keys.map((key) => {
                    if (key !== CACHE_NAME) {
                        return caches.delete(key);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const request = event.request;

    // Only handle GET requests
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);

    // Only handle same-origin requests or Google Fonts
    if (url.origin !== self.location.origin) {
        if (url.origin.includes('fonts.googleapis.com') || url.origin.includes('fonts.gstatic.com')) {
            // Cache fonts stale-while-revalidate
            event.respondWith(
                caches.open(CACHE_NAME).then(async (cache) => {
                    const cached = await cache.match(request);
                    const fetched = fetch(request).then((networkRes) => {
                        if (networkRes && networkRes.status === 200) {
                            cache.put(request, networkRes.clone());
                        }
                        return networkRes;
                    }).catch(() => null);
                    return cached || fetched;
                })
            );
        }
        return;
    }

    // Check if path is strictly network-only (Private / Authenticated / API routes)
    const isNetworkOnly = NETWORK_ONLY_PATTERNS.some((pattern) => pattern.test(url.pathname));
    if (isNetworkOnly) {
        return; // Let browser perform native network request
    }

    // Navigation requests (HTML pages): Network-first with offline fallback
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request)
                .then((networkRes) => {
                    if (networkRes && networkRes.status === 200) {
                        const copy = networkRes.clone();
                        caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
                    }
                    return networkRes;
                })
                .catch(async () => {
                    const cached = await caches.match(request);
                    if (cached) {
                        return cached;
                    }
                    const offline = await caches.match('/offline.html');
                    return offline || new Response('Offline', { status: 503, statusText: 'Offline' });
                })
        );
        return;
    }

    // Static assets (CSS, JS, Images, Icons): Cache-first with network fallback
    event.respondWith(
        caches.match(request).then((cached) => {
            if (cached) {
                return cached;
            }
            return fetch(request).then((networkRes) => {
                if (networkRes && networkRes.status === 200 && networkRes.type === 'basic') {
                    const copy = networkRes.clone();
                    caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
                }
                return networkRes;
            });
        })
    );
});
