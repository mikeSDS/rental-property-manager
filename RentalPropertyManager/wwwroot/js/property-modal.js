/**
 * Property Modal Form Handler
 * Manages the property form submission logic for both create and edit operations
 * This script is included in the _PropertyModal.cshtml partial view
 */

// Initialize the form handler once the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializePropertyForm();
});

/**
 * Initialize the property form event listeners
 * This function must be called each time a new modal is loaded
 */
function initializePropertyForm() {
    const form = document.getElementById('propertyForm');
    if (!form) {
        console.warn('Property form not found');
        return;
    }

    console.log('Initializing property form');
    form.addEventListener('submit', handlePropertyFormSubmit);
}

/**
 * Handle property form submission (create or edit)
 * @param {Event} e - The form submit event
 */
async function handlePropertyFormSubmit(e) {
    e.preventDefault();

    // Determine if this is an edit or create operation
    const isEdit = document.querySelector('input[name="id"]')?.value && 
                   parseInt(document.querySelector('input[name="id"]').value) > 0;

    const formData = {
        id: parseInt(document.querySelector('input[name="id"]')?.value || '0'),
        Name: document.getElementById('name').value,
        StreetAddress: document.getElementById('streetAddress').value,
        City: document.getElementById('city').value,
        State: document.getElementById('state').value,
        ZipCode: document.getElementById('zipCode').value
    };

    try {
        const url = isEdit ? `/properties/${formData.id}/edit` : '/properties/create';

        console.log('Submitting to:', url);
        console.log('Form data:', formData);
        console.log('Is edit:', isEdit);
        console.log('CSRF token:', document.querySelector('input[name="__RequestVerificationToken"]')?.value);

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(formData)
        });

        console.log('Response status:', response.status);
        const responseData = await response.json();
        console.log('Response data:', responseData);

        if (response.ok && responseData.success) {
            console.log('Form submission successful');
            // Close modal and refresh grid
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('propertyModal'));
            if (modalInstance) {
                modalInstance.hide();
            }

            // Wait for modal to close
            await new Promise(resolve => setTimeout(resolve, 500));

            // Reload properties if the parent page has the function
            if (typeof loadProperties === 'function') {
                await loadProperties();
            } else {
                // Fallback to page reload if function not available
                location.reload();
            }
        } else {
            console.error('Form submission failed:', responseData);
            alert(responseData.error || 'Error saving property. Please try again.');
        }
    } catch (error) {
        console.error('Error submitting form:', error);
        alert('An error occurred. Please try again.');
    }
}
