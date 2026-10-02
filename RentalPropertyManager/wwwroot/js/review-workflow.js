(function () {
    function wireUpReviewModal() {
        const form = document.querySelector('#reviewModal form');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (response.ok) {
                    const result = await response.json();
                    if (result.success) {
                        const modal = bootstrap.Modal.getInstance(document.getElementById('reviewModal'));
                        if (modal) modal.hide();
                        window.location.reload();
                    }
                } else if (response.status === 400 && (response.headers.get('content-type') || '').includes('text/html')) {
                    showModalHtml(await response.text(), false);
                } else {
                    alert('Unable to complete the review.');
                }
            } catch (err) {
                console.error(err);
                alert('Unable to complete the review.');
            }
        });
    }

    function showModalHtml(html, show) {
        const container = document.getElementById('reviewModalContainer');
        const existing = document.getElementById('reviewModal');
        if (existing) {
            const instance = bootstrap.Modal.getInstance(existing);
            if (instance) instance.dispose();
        }
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
        container.innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('reviewModal'));
        modal.show();
        wireUpReviewModal();
    }

    async function openReviewModal(appId) {
        const response = await fetch('/Review/ReviewModal/' + appId, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!response.ok) {
            alert('This application cannot be reviewed.');
            return;
        }
        showModalHtml(await response.text(), true);
    }

    document.addEventListener('click', function (e) {
        const button = e.target.closest('[data-review-app-id]');
        if (!button) return;
        e.preventDefault();
        openReviewModal(button.dataset.reviewAppId);
    });
})();
