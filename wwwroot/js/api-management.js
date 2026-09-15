// dagangOnline API Management & Sandbox Console
document.addEventListener('DOMContentLoaded', function () {
    const methodSelect = document.getElementById('apiMethod');
    const bodyContainer = document.getElementById('requestBodyContainer');
    const btnExecute = document.getElementById('btnExecuteApi');

    if (methodSelect && bodyContainer) {
        methodSelect.addEventListener('change', () => {
            const val = methodSelect.value;
            bodyContainer.style.display = (val === 'POST' || val === 'PUT' || val === 'PATCH') ? 'block' : 'none';
        });
    }

    // Preset buttons
    document.querySelectorAll('.btn-api-preset').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const method = btn.dataset.method || 'GET';
            const url = btn.dataset.url || '';
            const body = btn.dataset.body || '';

            if (methodSelect) methodSelect.value = method;
            const apiUrl = document.getElementById('apiUrl');
            if (apiUrl) apiUrl.value = url;
            const apiBody = document.getElementById('apiBody');
            if (apiBody) apiBody.value = body;
            if (bodyContainer) {
                bodyContainer.style.display = (method === 'POST' || method === 'PUT' || method === 'PATCH') ? 'block' : 'none';
            }

            executeApiTest();
        });
    });

    if (btnExecute) {
        btnExecute.addEventListener('click', executeApiTest);
    }

    async function executeApiTest() {
        const methodEl = document.getElementById('apiMethod');
        const urlEl = document.getElementById('apiUrl');
        const bodyEl = document.getElementById('apiBody');
        const responseBox = document.getElementById('apiResponseBox');
        const statusBadge = document.getElementById('responseStatusBadge');

        if (!methodEl || !urlEl || !responseBox || !statusBadge) return;

        const method = methodEl.value;
        const url = urlEl.value;
        const bodyContent = bodyEl ? bodyEl.value : '';

        responseBox.textContent = 'Menghubungkan ke endpoint...';
        statusBadge.className = 'badge bg-secondary';
        statusBadge.textContent = 'Loading...';

        try {
            const options = {
                method: method,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            };

            if ((method === 'POST' || method === 'PUT' || method === 'PATCH') && bodyContent.trim().length > 0) {
                options.body = bodyContent;
            }

            const startTime = performance.now();
            const res = await fetch(url, options);
            const duration = Math.round(performance.now() - startTime);

            let data;
            const contentType = res.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                data = await res.json();
            } else {
                data = await res.text();
            }

            if (res.ok) {
                statusBadge.className = 'badge bg-success';
            } else if (res.status === 401 || res.status === 403) {
                statusBadge.className = 'badge bg-warning text-dark';
            } else {
                statusBadge.className = 'badge bg-danger';
            }

            statusBadge.textContent = `HTTP ${res.status} ${res.statusText} (${duration} ms)`;
            responseBox.textContent = typeof data === 'object' ? JSON.stringify(data, null, 2) : data;
        } catch (err) {
            statusBadge.className = 'badge bg-danger';
            statusBadge.textContent = 'Request Failed';
            responseBox.textContent = 'Error: ' + err.message;
        }
    }
});
