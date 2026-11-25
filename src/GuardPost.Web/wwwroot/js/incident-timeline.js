// Incident timeline – loads and adds entries with AJAX (api/incidents/{id}/updates).
(function () {
    'use strict';

    var root = document.querySelector('[data-incident-timeline]');
    if (!root) {
        return;
    }

    var url = root.getAttribute('data-url');
    var list = root.querySelector('.gp-timeline');
    var form = root.querySelector('form');
    var textarea = form.querySelector('textarea');
    var error = form.querySelector('.gp-timeline-error');
    var button = form.querySelector('button[type="submit"]');

    function renderItem(item) {
        var li = gp.el('li', 'gp-timeline-item');
        var meta = gp.el('div', 'gp-timeline-meta');
        meta.appendChild(gp.el('strong', '', item.authorName));
        meta.appendChild(gp.el('span', 'text-muted ms-2', item.createdOnText));
        li.appendChild(meta);
        li.appendChild(gp.el('div', 'gp-pre', item.text));
        return li;
    }

    function load() {
        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error(response.status);
                }
                return response.json();
            })
            .then(function (items) {
                list.textContent = '';
                if (items.length === 0) {
                    list.appendChild(gp.el('li', 'text-muted fst-italic', 'Все още няма действия.'));
                }
                items.forEach(function (item) { list.appendChild(renderItem(item)); });
            })
            .catch(function () {
                list.textContent = '';
                list.appendChild(gp.el('li', 'text-danger', 'Хронологията не може да бъде заредена.'));
            });
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        error.textContent = '';
        button.disabled = true;

        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': gp.antiForgeryToken()
            },
            body: JSON.stringify({ text: textarea.value })
        })
            .then(function (response) {
                return response.json().then(function (data) {
                    return { ok: response.ok, data: data };
                });
            })
            .then(function (result) {
                if (!result.ok) {
                    var errors = result.data && result.data.errors ? result.data.errors : {};
                    var messages = Object.keys(errors).reduce(function (all, key) { return all.concat(errors[key]); }, []);
                    error.textContent = messages.join(' ') || 'Действието не беше записано.';
                    return;
                }
                textarea.value = '';
                load();
            })
            .catch(function () {
                error.textContent = 'Действието не беше записано.';
            })
            .finally(function () {
                button.disabled = false;
            });
    });

    load();
})();
