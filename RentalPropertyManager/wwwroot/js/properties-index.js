/**
 * Properties Index Page Script
 * Handles loading properties, creating, editing, and deleting properties
 * Includes modal loading and table management
 */

// Initialize on page load
document.addEventListener('DOMContentLoaded', async function () {
    await loadProperties();
    initializeCreateButton();
});

/**
 * Load and display all properties in the table
 */
async function loadProperties() {
    try {
        console.log('Loading properties from /properties/list');
        const response = await fetch('/properties/list');

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const properties = await response.json();
        console.log('Properties loaded:', properties);

        const tbody = document.getElementById('propertiesTableBody');
        tbody.innerHTML = '';

        if (!properties || properties.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">No properties found</td></tr>';
            return;
        }

        properties.forEach(prop => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${escapeHtml(prop.name)}</td>
                <td>${escapeHtml(prop.streetAddress)}</td>
                <td>${escapeHtml(prop.city)}, ${escapeHtml(prop.state)}</td>
                <td>${escapeHtml(prop.zipCode)}</td>
                <td class="text-end">
                    <button class="btn btn-sm btn-warning" onclick="editProperty(${prop.id})">Edit</button>
                    <button class="btn btn-sm btn-danger" onclick="deleteProperty(${prop.id})">Delete</button>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading properties:', error);
        const tbody = document.getElementById('propertiesTableBody');
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Error loading properties</td></tr>';
    }
}

/**
 * Initialize the create property button click handler
 */
function initializeCreateButton() {
    const createBtn = document.getElementById('createPropertyBtn');
    if (createBtn) {
        createBtn.addEventListener('click', openCreateModal);
    }
}

/**
 * Load and display the create property modal
 */
async function openCreateModal() {
    try {
        console.log('Loading create modal from /properties/createmodal');
        const response = await fetch('/properties/createmodal');

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const html = await response.text();
        document.getElementById('propertyModalContainer').innerHTML = html;

        const modalElement = document.getElementById('propertyModal');
        const modal = new bootstrap.Modal(modalElement);

        // Initialize the form submission handler for the newly loaded modal
        const form = document.getElementById('propertyForm');
        if (form) {
            form.removeEventListener('submit', handlePropertyFormSubmit);
            form.addEventListener('submit', handlePropertyFormSubmit);
        }

        modal.show();
    } catch (error) {
        console.error('Error loading create modal:', error);
        alert('Error loading create modal');
    }
}

/**
 * Load and display the edit property modal
 * @param {number} id - The property ID to edit
 */
async function editProperty(id) {
    try {
        console.log(`Loading edit modal for property ID: ${id}`);
        const response = await fetch(`/properties/${id}/editmodal`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const html = await response.text();
        document.getElementById('propertyModalContainer').innerHTML = html;

        const modalElement = document.getElementById('propertyModal');
        const modal = new bootstrap.Modal(modalElement);

        // Initialize the form submission handler for the newly loaded modal
        const form = document.getElementById('propertyForm');
        if (form) {
            form.removeEventListener('submit', handlePropertyFormSubmit);
            form.addEventListener('submit', handlePropertyFormSubmit);
        }

        modal.show();
    } catch (error) {
        console.error('Error loading edit modal:', error);
        alert('Error loading edit modal');
    }
}

/**
 * Delete a property after confirming with the user
 * @param {number} id - The property ID to delete
 */
async function deleteProperty(id) {
    if (!confirm('Are you sure you want to delete this property?')) {
        return;
    }

    try {
        console.log(`Deleting property ID: ${id}`);
        const response = await fetch(`/properties/${id}/delete`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });

        if (response.ok) {
            console.log(`Property ${id} deleted successfully`);
            await loadProperties();
        } else {
            const data = await response.json();
            alert(data.error || 'Error deleting property');
        }
    } catch (error) {
        console.error('Error deleting property:', error);
        alert('Error deleting property');
    }
}

/**
 * Handle property form submission (create or edit)
 * This function is attached to the form dynamically when the modal loads
 * @param {Event} e - The form submit event
 */
async function handlePropertyFormSubmit(e) {
    e.preventDefault();

    const isEdit = document.getElementById('propertyForm').dataset.isEdit === 'true';
    const formData = {
        id: parseInt(document.getElementById('propertyForm').dataset.propertyId || '0'),
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
            // Close modal and refresh grid
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('propertyModal'));
            if (modalInstance) {
                modalInstance.hide();
            }
            await new Promise(resolve => setTimeout(resolve, 500));
            await loadProperties();
        } else {
            alert(responseData.error || 'Error saving property. Please try again.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred. Please try again.');
    }
}

/**
 * Escape HTML special characters to prevent XSS attacks
 * @param {string} text - The text to escape
 * @returns {string} The escaped HTML
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
