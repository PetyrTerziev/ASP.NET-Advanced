// Real-time notifications over SignalR (hub: /hubs/notifications).
(function () {
    'use strict';

    if (!window.signalR || !window.bootstrap) {
        return;
    }

    var container = document.getElementById('gp-toasts');

    function showToast(notification) {
        var toast = gp.el('div', 'toast gp-toast');
        toast.setAttribute('role', 'status');

        var header = gp.el('div', 'toast-header');
        header.appendChild(gp.el('i', 'bi bi-bell-fill text-warning me-2'));
        header.appendChild(gp.el('strong', 'me-auto', notification.title));
        var close = gp.el('button', 'btn-close');
        close.type = 'button';
        close.setAttribute('data-bs-dismiss', 'toast');
        close.setAttribute('aria-label', 'Затвори');
        header.appendChild(close);

        var body = gp.el('div', 'toast-body');
        body.appendChild(gp.el('div', '', notification.message));
        // Only same-site relative links are followed.
        if (notification.url && notification.url.charAt(0) === '/' && notification.url.charAt(1) !== '/') {
            var link = gp.el('a', 'small', 'Отвори');
            link.href = notification.url;
            body.appendChild(link);
        }

        toast.appendChild(header);
        toast.appendChild(body);
        container.appendChild(toast);

        var instance = new bootstrap.Toast(toast, { delay: 10000 });
        toast.addEventListener('hidden.bs.toast', function () { toast.remove(); });
        instance.show();
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect()
        .build();

    connection.on('notify', showToast);
    connection.start().catch(function (error) {
        console.warn('Notifications are not available:', error);
    });
})();
