// Refreshes the "coverage right now" widget every minute without reloading the page (AJAX).
(function () {
    'use strict';

    var widget = document.querySelector('[data-coverage-url]');
    if (!widget) {
        return;
    }

    var url = widget.getAttribute('data-coverage-url');
    var updated = document.getElementById('gp-coverage-updated');

    function refresh() {
        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : Promise.reject(response.status); })
            .then(function (sites) {
                sites.forEach(function (site) {
                    var row = widget.querySelector('[data-site-id="' + site.siteId + '"]');
                    if (!row) {
                        return;
                    }
                    var covered = site.onDuty >= site.required;
                    var percent = site.required === 0 ? 100 : Math.min(100, Math.floor(site.onDuty * 100 / site.required));
                    row.querySelector('.gp-coverage-count').textContent = site.onDuty + ' / ' + site.required;
                    row.querySelector('.gp-coverage-count').className = 'gp-coverage-count fw-semibold ' + (covered ? 'text-success' : 'text-danger');
                    var bar = row.querySelector('.progress-bar');
                    bar.style.width = percent + '%';
                    bar.className = 'progress-bar ' + (covered ? 'bg-success' : 'bg-danger');
                });
                if (updated) {
                    updated.textContent = 'обновено ' + new Date().toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' });
                }
            })
            .catch(function () { /* keep the last known values */ });
    }

    setInterval(refresh, 60000);
})();
