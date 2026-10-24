// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Restrict phone fields to digits and common phone characters only (no letters).
(function () {
    function sanitizePhoneValue(value) {
        return (value || '').replace(/[^0-9+\-\s()]/g, '');
    }

    function bindPhoneInput(input) {
        if (!input || input.dataset.phoneBound === '1') return;
        input.dataset.phoneBound = '1';

        // Block letters and other disallowed characters as the user types / pastes.
        input.addEventListener('input', function () {
            var cleaned = sanitizePhoneValue(this.value);
            if (this.value !== cleaned) {
                var start = this.selectionStart;
                var end = this.selectionEnd;
                this.value = cleaned;
                // Keep cursor position reasonable after stripping characters.
                try {
                    this.setSelectionRange(Math.max(0, start - 1), Math.max(0, end - 1));
                } catch (e) { /* ignore */ }
            }
        });

        // Extra guard on keypress (older browsers / some mobile keyboards).
        input.addEventListener('keypress', function (e) {
            var ch = e.key || String.fromCharCode(e.which || e.keyCode);
            if (ch && ch.length === 1 && /[a-zA-Z]/.test(ch)) {
                e.preventDefault();
            }
        });
    }

    function initPhoneInputs(root) {
        var scope = root || document;
        var nodes = scope.querySelectorAll(
            'input.phone-input, input[type="tel"], input[name="Phone"], input[name="WorkPhone"], input[id="Phone"], input[id="WorkPhone"]'
        );
        nodes.forEach(bindPhoneInput);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initPhoneInputs(); });
    } else {
        initPhoneInputs();
    }

    // Expose for dynamic forms if needed later.
    window.initPhoneInputs = initPhoneInputs;
})();

// Show / hide password toggle (eye icon)
(function () {
    function bindPasswordToggle(btn) {
        if (!btn || btn.dataset.toggleBound === '1') return;
        btn.dataset.toggleBound = '1';

        btn.addEventListener('click', function () {
            var id = this.getAttribute('data-target');
            var input = id ? document.getElementById(id) : null;
            if (!input) return;

            var showing = input.type === 'text';
            input.type = showing ? 'password' : 'text';
            this.classList.toggle('is-visible', !showing);
            this.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
            this.setAttribute('title', showing ? 'Show password' : 'Hide password');
        });
    }

    function initPasswordToggles(root) {
        var scope = root || document;
        scope.querySelectorAll('.password-toggle').forEach(bindPasswordToggle);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initPasswordToggles(); });
    } else {
        initPasswordToggles();
    }

    window.initPasswordToggles = initPasswordToggles;
})();

// ---------------------------------------------------------------------------
// Live UI: poll /Live/Summary, update badge + dashboard KPIs, toast on new alerts
// ---------------------------------------------------------------------------
(function () {
    var POLL_MS = 8000;
    var lastUnread = null;
    var lastLatestId = null;
    var timer = null;
    var hidden = false;

    function $(id) { return document.getElementById(id); }

    function formatCurrency(n) {
        try {
            return 'R ' + Number(n || 0).toLocaleString('en-ZA', { maximumFractionDigits: 0 });
        } catch (e) {
            return 'R ' + Math.round(n || 0);
        }
    }

    function setBadge(count) {
        var badge = $('notif-badge');
        if (!badge) return;
        var n = Number(count) || 0;
        badge.dataset.count = String(n);
        if (n <= 0) {
            badge.classList.add('is-hidden');
            badge.textContent = '0';
            return;
        }
        badge.classList.remove('is-hidden');
        badge.textContent = n > 99 ? '99+' : String(n);
        badge.classList.remove('badge-pop');
        // restart animation
        void badge.offsetWidth;
        badge.classList.add('badge-pop');
    }

    function setClock(timeStr) {
        var el = $('live-clock');
        if (el && timeStr) el.textContent = timeStr;
    }

    function flashValue(el) {
        if (!el) return;
        el.classList.remove('value-flash');
        void el.offsetWidth;
        el.classList.add('value-flash');
    }

    function updateDashboard(data) {
        if (!document.querySelector('[data-live-dashboard]')) return;
        var map = {
            totalVehicles: data.totalVehicles,
            activeJobs: data.activeJobs,
            readyForCollection: data.readyForCollection,
            todaysAppointments: data.todaysAppointments,
            outstandingPayments: data.outstandingPayments
        };
        Object.keys(map).forEach(function (key) {
            var el = document.querySelector('[data-live="' + key + '"]');
            if (!el) return;
            var next;
            if (el.getAttribute('data-live-format') === 'currency') {
                next = formatCurrency(map[key]);
            } else {
                next = String(map[key] == null ? '—' : map[key]);
            }
            if (el.textContent.trim() !== next) {
                el.textContent = next;
                flashValue(el);
                var card = el.closest('.card');
                if (card) {
                    card.classList.remove('card-pulse');
                    void card.offsetWidth;
                    card.classList.add('card-pulse');
                }
            }
        });
    }

    function showToast(item) {
        var host = $('live-toast-host');
        if (!host || !item) return;
        var toast = document.createElement('div');
        toast.className = 'live-toast';
        toast.innerHTML =
            '<div class="live-toast-title">' + escapeHtml(item.type || 'Alert') + '</div>' +
            '<div class="live-toast-body">' + escapeHtml(item.message || '') + '</div>' +
            '<a class="live-toast-link" href="/Notifications?filter=staff">View</a>';
        host.appendChild(toast);
        requestAnimationFrame(function () { toast.classList.add('is-visible'); });
        setTimeout(function () {
            toast.classList.remove('is-visible');
            toast.classList.add('is-leaving');
            setTimeout(function () { toast.remove(); }, 320);
        }, 5500);
    }

    function escapeHtml(s) {
        return String(s || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function handleSummary(data) {
        if (!data) return;
        setClock(data.serverTime);
        var unread = data.unreadNotifications || 0;

        if (lastUnread !== null && unread > lastUnread) {
            var latest = (data.latest && data.latest[0]) || null;
            if (latest && latest.id !== lastLatestId) {
                showToast(latest);
                lastLatestId = latest.id;
            }
            var bell = $('notif-bell');
            if (bell) {
                bell.classList.remove('bell-ring');
                void bell.offsetWidth;
                bell.classList.add('bell-ring');
            }
        }
        if (data.latest && data.latest[0]) lastLatestId = data.latest[0].id;
        lastUnread = unread;
        setBadge(unread);
        updateDashboard(data);
    }

    function poll() {
        if (hidden) return;
        fetch('/Live/Summary', {
            method: 'GET',
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (r) {
                if (!r.ok) throw new Error('poll ' + r.status);
                return r.json();
            })
            .then(handleSummary)
            .catch(function () { /* silent — offline / auth */ });
    }

    function start() {
        // Seed badge from server-rendered count
        var badge = $('notif-badge');
        if (badge && lastUnread === null) {
            lastUnread = parseInt(badge.dataset.count || '0', 10) || 0;
        }
        poll();
        timer = setInterval(poll, POLL_MS);
    }

    document.addEventListener('visibilitychange', function () {
        hidden = document.hidden;
        if (!hidden) poll();
    });

    // Auto-dismiss flash messages
    function initFlashes() {
        document.querySelectorAll('.flash.auto-dismiss').forEach(function (el) {
            setTimeout(function () {
                el.classList.add('is-leaving');
                setTimeout(function () { el.remove(); }, 350);
            }, 4500);
        });
    }

    // Soft hover / enter for cards on pages that just loaded
    function initCardEnters() {
        document.querySelectorAll('.card, .stat').forEach(function (el, i) {
            el.style.setProperty('--enter-delay', (Math.min(i, 12) * 40) + 'ms');
            el.classList.add('card-enter');
        });
    }

    function boot() {
        initFlashes();
        initCardEnters();
        start();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();

// ---------------------------------------------------------------------------
// Customer → Vehicle + Job card dropdown filter on any form
// Classes: js-filter-customer, js-filter-vehicle, js-filter-jobs
// ---------------------------------------------------------------------------
(function () {
    function fillSelect(sel, items, emptyLabel, noneLabel, selectedId) {
        if (!sel) return;
        var keep = selectedId != null ? selectedId : sel.value;
        sel.innerHTML = '';
        var ph = document.createElement('option');
        ph.value = '';
        ph.textContent = items.length ? emptyLabel : (noneLabel || '— None —');
        sel.appendChild(ph);
        items.forEach(function (item) {
            var o = document.createElement('option');
            o.value = item.id;
            o.textContent = item.text;
            if (keep && keep === item.id) o.selected = true;
            sel.appendChild(o);
        });
        sel.disabled = false;
    }

    function fetchList(url, cb) {
        fetch(url, {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (items) { cb(items || []); })
            .catch(function () { cb([]); });
    }

    function loadVehicles(customerId, vehicleSel, keepSelected) {
        if (!vehicleSel) return;
        if (!customerId) {
            fillSelect(vehicleSel, [], '— Select vehicle —', '— Select customer first —', null);
            return;
        }
        vehicleSel.disabled = true;
        var selected = keepSelected ? vehicleSel.value : null;
        fetchList('/Vehicles/JsonByCustomer?customerId=' + encodeURIComponent(customerId), function (items) {
            fillSelect(vehicleSel, items, '— Select vehicle —', '— No vehicles for this customer —', selected);
        });
    }

    function loadJobs(customerId, jobSel, keepSelected) {
        if (!jobSel) return;
        if (!customerId) {
            fillSelect(jobSel, [], '— No job / select —', '— Select customer first —', null);
            return;
        }
        jobSel.disabled = true;
        var selected = keepSelected ? jobSel.value : null;
        fetchList('/JobCards/JsonByCustomer?customerId=' + encodeURIComponent(customerId), function (items) {
            fillSelect(jobSel, items, '— No job / select —', '— No job cards for this customer —', selected);
        });
    }

    function bindPair(customerSel, vehicleSel, jobSel) {
        if (!customerSel || customerSel.dataset.filterBound === '1') return;
        customerSel.dataset.filterBound = '1';
        customerSel.addEventListener('change', function () {
            loadVehicles(this.value, vehicleSel, false);
            loadJobs(this.value, jobSel, false);
        });
        if (customerSel.value) {
            loadVehicles(customerSel.value, vehicleSel, true);
            loadJobs(customerSel.value, jobSel, true);
        } else {
            if (vehicleSel && vehicleSel.options.length > 15)
                fillSelect(vehicleSel, [], '— Select vehicle —', '— Select customer first —', null);
            if (jobSel && jobSel.options.length > 15)
                fillSelect(jobSel, [], '— No job / select —', '— Select customer first —', null);
        }
    }

    function initCustomerVehicleFilters(root) {
        var scope = root || document;
        scope.querySelectorAll('form').forEach(function (form) {
            var cust = form.querySelector('select.js-filter-customer, select#CustomerId');
            var veh = form.querySelector('select.js-filter-vehicle, select#VehicleId, select#VehichleId');
            var jobs = form.querySelector('select.js-filter-jobs, select#JobCardId');
            if (cust && (veh || jobs)) bindPair(cust, veh, jobs);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initCustomerVehicleFilters(); });
    } else {
        initCustomerVehicleFilters();
    }
    window.initCustomerVehicleFilters = initCustomerVehicleFilters;
})();
