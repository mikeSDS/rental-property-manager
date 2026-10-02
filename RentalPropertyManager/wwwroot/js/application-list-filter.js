(function () {
    const form = document.getElementById('application-filter-form');
    const container = document.getElementById('application-table-container');
    if (!form || !container) return;

    async function refresh() {
        const params = new URLSearchParams(new FormData(form));
        const response = await fetch(form.dataset.filterUrl + '?' + params.toString());
        if (response.ok) {
            container.innerHTML = await response.text();
        }
    }

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        refresh();
    });
    form.querySelector('select[name="status"]').addEventListener('change', refresh);
})();
