// dagangOnline UI Core Script
document.addEventListener('DOMContentLoaded', function () {
    // 1. Form submit loading state & double submit prevention
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            // Check if form is using jQuery validation and is valid
            if (window.jQuery && typeof jQuery(form).valid === 'function') {
                if (!jQuery(form).valid()) {
                    return;
                }
            } else if (!form.checkValidity()) {
                return;
            }

            if (form.dataset.submitting === 'true') {
                e.preventDefault();
                return;
            }

            var submitBtn = form.querySelector('button[type="submit"]:not([data-no-loading])');
            if (submitBtn) {
                form.dataset.submitting = 'true';
                submitBtn.classList.add('is-loading');
                submitBtn.setAttribute('aria-busy', 'true');
                submitBtn.setAttribute('disabled', 'disabled');
            }
        });
    });

    // 2. Account registration type toggle (Mitra vs User)
    var accountTypeRadios = document.querySelectorAll('.account-type-radio');
    if (accountTypeRadios.length > 0) {
        var mitraContainer = document.getElementById('mitraFieldsContainer');
        accountTypeRadios.forEach(function (radio) {
            radio.addEventListener('change', function () {
                if (mitraContainer) {
                    mitraContainer.style.display = (radio.value === 'mitra' && radio.checked) ? 'block' : 'none';
                }
            });
        });
    }

    // 3. Demo credentials autofill (Login page)
    var demoFillBtns = document.querySelectorAll('.btn-demo-fill');
    demoFillBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var emailInput = document.getElementById('emailInput');
            var passInput = document.getElementById('passwordInput');
            if (emailInput && btn.dataset.demoEmail) {
                emailInput.value = btn.dataset.demoEmail;
            }
            if (passInput && btn.dataset.demoPass) {
                passInput.value = btn.dataset.demoPass;
            }
        });
    });

    // 4. Password visibility toggle (Task 1.7)
    var togglePassBtns = document.querySelectorAll('.btn-toggle-password');
    togglePassBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var targetId = btn.getAttribute('data-target');
            var input = document.getElementById(targetId);
            if (!input) return;

            var isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';

            var eyeIcon = btn.querySelector('.bi-eye');
            var eyeSlashIcon = btn.querySelector('.bi-eye-slash');
            if (eyeIcon && eyeSlashIcon) {
                if (isPassword) {
                    eyeIcon.classList.add('d-none');
                    eyeSlashIcon.classList.remove('d-none');
                } else {
                    eyeIcon.classList.remove('d-none');
                    eyeSlashIcon.classList.add('d-none');
                }
            }
        });
    });

    // 5. Real-time confirm password matcher (Task 1.7)
    var passInput = document.getElementById('passInput');
    var confirmPassInput = document.getElementById('confirmPassInput');
    var matchFeedback = document.getElementById('passwordMatchFeedback');

    function checkPasswordMatch() {
        if (!passInput || !confirmPassInput || !matchFeedback) return;
        if (!confirmPassInput.value) {
            matchFeedback.textContent = '';
            matchFeedback.className = 'small mt-1';
            return;
        }

        if (passInput.value === confirmPassInput.value) {
            matchFeedback.textContent = '✓ Kata sandi cocok';
            matchFeedback.className = 'small mt-1 text-success fw-semibold';
        } else {
            matchFeedback.textContent = '✗ Konfirmasi kata sandi belum sama';
            matchFeedback.className = 'small mt-1 text-danger';
        }
    }

    if (passInput && confirmPassInput) {
        passInput.addEventListener('input', checkPasswordMatch);
        confirmPassInput.addEventListener('input', checkPasswordMatch);
    }

    // 6. Online / Offline Status Indicator (Task 3.4)
    var offlineBanner = document.getElementById('offlineBanner');
    function updateOnlineStatus() {
        if (!offlineBanner) return;
        if (navigator.onLine) {
            offlineBanner.classList.add('d-none');
        } else {
            offlineBanner.classList.remove('d-none');
        }
    }
    window.addEventListener('online', updateOnlineStatus);
    window.addEventListener('offline', updateOnlineStatus);
    updateOnlineStatus();

    // 7. PWA Service Worker Registration (Task 3.2 & 3.3)
    if ('serviceWorker' in navigator && (window.location.protocol === 'https:' || window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { scope: '/' })
                .then(function (registration) {
                    console.log('[PWA] Service Worker registered with scope:', registration.scope);
                })
                .catch(function (error) {
                    console.warn('[PWA] Service Worker registration failed:', error);
                });
        });
    }

    // 8. Mitra Product Edit Modal Autofill (Task 1.8)
    var editButtons = document.querySelectorAll('.btn-edit-product');
    var editForm = document.getElementById('editProductForm');
    var editProdName = document.getElementById('editProdName');
    var editProdSummary = document.getElementById('editProdSummary');
    var editProdDesc = document.getElementById('editProdDesc');
    var editProdPrice = document.getElementById('editProdPrice');
    var editProdCategory = document.getElementById('editProdCategory');

    editButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var prodId = btn.getAttribute('data-id');
            if (editForm) {
                editForm.action = '?handler=UpdateProduct&id=' + prodId;
            }
            if (editProdName) editProdName.value = btn.getAttribute('data-name') || '';
            if (editProdSummary) editProdSummary.value = btn.getAttribute('data-summary') || '';
            if (editProdDesc) editProdDesc.value = btn.getAttribute('data-desc') || '';
            if (editProdPrice) editProdPrice.value = btn.getAttribute('data-price') || '0';
            if (editProdCategory) editProdCategory.value = btn.getAttribute('data-category') || 'General';
        });
    });
});

// Global reusable Toast notification (Task 1.6 & Task 2.2)
window.showAppToast = function (message, type) {
    type = type || 'success'; // success, danger, warning, info
    var toastEl = document.getElementById('appToast');
    var messageEl = document.getElementById('appToastMessage');
    if (!toastEl || !messageEl) return;

    // Reset background classes
    toastEl.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info', 'text-dark', 'text-white');

    if (type === 'warning') {
        toastEl.classList.add('bg-warning', 'text-dark');
    } else if (type === 'danger') {
        toastEl.classList.add('bg-danger', 'text-white');
    } else if (type === 'info') {
        toastEl.classList.add('bg-info', 'text-white');
    } else {
        toastEl.classList.add('bg-success', 'text-white');
    }

    messageEl.textContent = message;

    if (window.bootstrap && bootstrap.Toast) {
        var bsToast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 });
        bsToast.show();
    }
};
