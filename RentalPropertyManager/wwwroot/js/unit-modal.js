/**
 * Units Modal Form Handler
 * Manages the unit form submission logic for both create and edit operations
 * This script is included in the _UnitModal.cshtml partial view
 */

/**
 * Initialize the unit form event listeners
 * This function must be called each time a new modal is loaded
 */
function initializeUnitForm() {
    const form = document.getElementById('unitForm');
    if (!form) {
        console.warn('Unit form not found');
        return;
    }

    console.log('Initializing unit form');

    // Remove any existing listeners to prevent duplicates
    form.removeEventListener('submit', handleUnitFormSubmit);

    // Add the submit listener
    form.addEventListener('submit', handleUnitFormSubmit);
}

/**
 * Handle unit form submission (create or edit)
 * @param {Event} e - The form submit event
 */
async function handleUnitFormSubmit(e) {
    e.preventDefault();
    console.log('Form submit triggered');

    // Determine if this is an edit or create operation
    const idElement = document.querySelector('input[name="id"]');
    const formId = idElement ? parseInt(idElement.value) : 0;
    const isEdit = formId && formId > 0;

    console.log('Form ID:', formId, 'Is Edit:', isEdit);

    const formData = {
        id: formId,
        PropertyID: parseInt(document.getElementById('propertyID').value),
        UnitNumber: document.getElementById('unitNumber').value,
        Bedrooms: parseInt(document.getElementById('bedrooms').value),
        MonthlyRent: parseFloat(document.getElementById('monthlyRent').value),
        UnitTypeID: parseInt(document.getElementById('unitTypeID').value)
    };

    try {
        const url = isEdit ? `/units/${formData.id}/edit` : '/units/create';

        console.log('Submitting to:', url);
        console.log('Method: POST');
        console.log('Form data:', JSON.stringify(formData, null, 2));
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        console.log('CSRF token present:', !!csrfToken, 'Value:', csrfToken?.substring(0, 20) + '...');

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken || ''
            },
            body: JSON.stringify(formData)
        });

        console.log('Response status:', response.status, response.statusText);
        console.log('Response headers:', Array.from(response.headers.entries()));

        if (!response.ok) {
            const text = await response.text();
            console.error('Response body:', text);
            try {
                const responseData = JSON.parse(text);
                console.error('Parsed response:', responseData);
                const errorDiv = document.getElementById('formErrors');
                if (errorDiv) {
                    if (responseData.errors && Array.isArray(responseData.errors)) {
                        errorDiv.innerHTML = '<div class="alert alert-danger">' + 
                            responseData.errors.map(e => escapeHtml(e)).join('<br>') + 
                            '</div>';
                    } else if (responseData.error) {
                        errorDiv.innerHTML = '<div class="alert alert-danger">' + escapeHtml(responseData.error) + '</div>';
                    }
                }
            } catch (e) {
                console.error('Could not parse error response:', e);
                const errorDiv = document.getElementById('formErrors');
                if (errorDiv) {
                    errorDiv.innerHTML = '<div class="alert alert-danger">Error: ' + escapeHtml(text) + '</div>';
                }
            }
            return;
        }

        const responseData = await response.json();
        console.log('Success response data:', responseData);

        if (responseData.success) {
            console.log('Form submission successful');
            // Close modal and refresh grid
            const modalElement = document.getElementById('unitModal');
            const modalInstance = bootstrap.Modal.getInstance(modalElement);
            if (modalInstance) {
                modalInstance.hide();
            }

            // Wait for modal to close
            await new Promise(resolve => setTimeout(resolve, 500));

            // Reload units if the parent page has the function
            if (typeof loadUnits === 'function') {
                console.log('Calling loadUnits()');
                await loadUnits();
            } else {
                console.log('loadUnits not available, reloading page');
                location.reload();
            }
        } else {
            console.error('Server returned success: false', responseData);
            const errorDiv = document.getElementById('formErrors');
            if (errorDiv) {
                errorDiv.innerHTML = '<div class="alert alert-danger">Error saving unit</div>';
            }
        }
    } catch (error) {
        console.error('Fetch error:', error);
        alert('An error occurred. Check the console for details.');
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
