/**
 * Application Wizard Page Handler
 * Loaded via <script src> from Views/Application/Wizard.cshtml.
 * Partial views contain HTML only: innerHTML never executes embedded <script> tags,
 * so every handler is defined here and explicitly wired up after markup is injected.
 */

(function () {
    'use strict';

    const wizardContainer = document.getElementById('wizardContainer');
    const modalContainer = document.getElementById('residenceModalContainer');

    function getAntiForgeryToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function showAlert(message, type) {
        const placeholder = document.getElementById('alertPlaceholder');
        if (!placeholder) {
            alert(message);
            return;
        }
        placeholder.innerHTML =
            '<div class="alert alert-' + (type || 'danger') + ' alert-dismissible m-3" role="alert">' +
            '<span></span>' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
            '</div>';
        placeholder.querySelector('span').textContent = message;
    }

    function isHtmlResponse(response) {
        return (response.headers.get('content-type') || '').includes('text/html');
    }

    async function readErrorMessage(response) {
        try {
            const data = await response.json();
            return data.error || data.message || 'Request failed (' + response.status + ').';
        } catch {
            return 'Request failed (' + response.status + ').';
        }
    }

    // ---------------------------------------------------------------
    // Wizard navigation (Continue / Back / Submit)
    // ---------------------------------------------------------------

    function updateStatusBadge() {
        const form = document.getElementById('wizardForm');
        const badge = document.getElementById('applicationStatusBadge');
        if (form && badge && form.dataset.statusName) {
            badge.textContent = form.dataset.statusName;
        }
    }

    async function handleWizardFormSubmit(e) {
        const form = e.target;
        if (!form || form.id !== 'wizardForm') {
            return;
        }

        e.preventDefault();

        const formData = new FormData(form);
        const action = e.submitter && e.submitter.value ? e.submitter.value : 'Continue';
        formData.set('buttonAction', action);

        try {
            const response = await fetch(form.action, { method: 'POST', body: formData });

            if (isHtmlResponse(response)) {
                // 200 (next step) and 400 (validation errors) both return the step markup
                wizardContainer.innerHTML = await response.text();
                updateStatusBadge();
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            showAlert(await readErrorMessage(response), 'danger');
        } catch (error) {
            console.error('Error submitting wizard step:', error);
            showAlert('Error submitting the application step. Please try again.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // Residence modal (add / edit)
    // ---------------------------------------------------------------

    async function openResidenceModal(applicationId, residenceId) {
        try {
            let url = '/Application/ResidenceModal?applicationId=' + encodeURIComponent(applicationId);
            if (residenceId) {
                url += '&residenceId=' + encodeURIComponent(residenceId);
            }

            const response = await fetch(url);
            if (!response.ok) {
                showAlert(await readErrorMessage(response), 'danger');
                return;
            }

            modalContainer.innerHTML = await response.text();

            // Manually wire up the submit handler AFTER injection
            wireUpResidenceModal();

            const modalElement = document.getElementById('residenceModal');
            modalElement.addEventListener('hidden.bs.modal', function () {
                modalContainer.innerHTML = '';
            });

            bootstrap.Modal.getOrCreateInstance(modalElement).show();
        } catch (error) {
            console.error('Error loading residence modal:', error);
            showAlert('Error loading the residence form.', 'danger');
        }
    }

    function wireUpResidenceModal() {
        const form = document.getElementById('residenceForm');
        if (!form) {
            console.warn('Residence form not found');
            return;
        }

        form.removeEventListener('submit', handleResidenceFormSubmit);
        form.addEventListener('submit', handleResidenceFormSubmit);
    }

    async function handleResidenceFormSubmit(e) {
        e.preventDefault();

        const form = e.target;

        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form) });

            if (response.status === 400 && isHtmlResponse(response)) {
                // Validation errors: replace the dialog contents and keep the modal open
                const html = await response.text();
                const doc = new DOMParser().parseFromString(html, 'text/html');
                const newDialog = doc.getElementById('residenceModalDialog');
                const currentDialog = document.getElementById('residenceModalDialog');
                if (newDialog && currentDialog) {
                    currentDialog.innerHTML = newDialog.innerHTML;
                    wireUpResidenceModal();
                }
                return;
            }

            if (!response.ok) {
                showAlert(await readErrorMessage(response), 'danger');
                return;
            }

            const result = await response.json();
            if (result.success) {
                const modalElement = document.getElementById('residenceModal');
                const instance = bootstrap.Modal.getInstance(modalElement);
                if (instance) {
                    instance.hide();
                }
                await refreshResidenceTable(result.applicationId);
            }
        } catch (error) {
            console.error('Error saving residence:', error);
            showAlert('Error saving residence. Please try again.', 'danger');
        }
    }

    async function refreshResidenceTable(applicationId) {
        const container = document.getElementById('residenceTableContainer');
        if (!container) {
            return;
        }

        const response = await fetch('/Application/ResidenceTablePartial?applicationId=' + encodeURIComponent(applicationId));
        if (!response.ok) {
            showAlert(await readErrorMessage(response), 'danger');
            return;
        }

        container.innerHTML = await response.text();
    }

    async function deleteResidence(residenceId, applicationId) {
        if (!confirm('Delete this residence?')) {
            return;
        }

        try {
            const response = await fetch('/Application/DeleteResidence/' + encodeURIComponent(residenceId), {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': getAntiForgeryToken() }
            });

            if (!response.ok) {
                showAlert(await readErrorMessage(response), 'danger');
                return;
            }

            await refreshResidenceTable(applicationId);
        } catch (error) {
            console.error('Error deleting residence:', error);
            showAlert('Error deleting residence.', 'danger');
        }
    }

    // ---------------------------------------------------------------
    // Event delegation (survives wizard step markup being replaced)
    // ---------------------------------------------------------------

    wizardContainer.addEventListener('submit', handleWizardFormSubmit);

    wizardContainer.addEventListener('click', function (e) {
        const button = e.target.closest('[data-action]');
        if (!button) {
            return;
        }

        const container = document.getElementById('residenceTableContainer');
        const applicationId = button.dataset.applicationId || (container ? container.dataset.applicationId : '');

        switch (button.dataset.action) {
            case 'add-residence':
                openResidenceModal(applicationId, null);
                break;
            case 'edit-residence':
                openResidenceModal(applicationId, button.dataset.residenceId);
                break;
            case 'delete-residence':
                deleteResidence(button.dataset.residenceId, applicationId);
                break;
        }
    });
})();
