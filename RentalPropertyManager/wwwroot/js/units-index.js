/**
 * Units Index Page Handler
 * Manages the units list loading and modal interactions
 * This script is included in the Units/Index.cshtml view
 */

// Load units on page load
document.addEventListener('DOMContentLoaded', async function () {
    await loadUnits();
});

// Load units table
async function loadUnits() {
    try {
        const response = await fetch('/units/list');
        const units = await response.json();

        const tbody = document.getElementById('unitsTableBody');
        tbody.innerHTML = '';

        if (units.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">No units found</td></tr>';
            return;
        }

        units.forEach(unit => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${escapeHtml(unit.propertyName)}</td>
                <td>${escapeHtml(unit.unitNumber)}</td>
                <td>${unit.bedrooms}</td>
                <td>$${unit.monthlyRent.toFixed(2)}</td>
                <td>${escapeHtml(unit.unitTypeName)}</td>
                <td class="text-end">
                    <button class="btn btn-sm btn-warning" onclick="editUnit(${unit.unitID})">Edit</button>
                    <button class="btn btn-sm btn-danger" onclick="deleteUnit(${unit.unitID})">Delete</button>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading units:', error);
        document.getElementById('unitsTableBody').innerHTML = 
            '<tr><td colspan="6" class="text-center text-danger">Error loading units</td></tr>';
    }
}

// Create unit modal
document.getElementById('createUnitBtn').addEventListener('click', async function () {
    try {
        const response = await fetch('/units/createmodal');
        const html = await response.text();
        document.getElementById('unitModalContainer').innerHTML = html;

        wireUpUnitForm();

        const modal = new bootstrap.Modal(document.getElementById('unitModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading create modal:', error);
        alert('Error loading create modal');
    }
});

// Edit unit
async function editUnit(id) {
    try {
        const response = await fetch(`/units/${id}/editmodal`);
        const html = await response.text();
        document.getElementById('unitModalContainer').innerHTML = html;

        wireUpUnitForm();

        const modal = new bootstrap.Modal(document.getElementById('unitModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading edit modal:', error);
        alert('Error loading edit modal');
    }
}

/**
 * Attach the submit handler to the unit form.
 * innerHTML does not execute embedded <script> tags from the partial view,
 * so the form submit handler must be wired up explicitly here, after the
 * modal markup has been injected into the DOM.
 */
function wireUpUnitForm() {
    const form = document.getElementById('unitForm');
    if (!form) {
        console.warn('Unit form not found');
        return;
    }

    form.removeEventListener('submit', handleUnitFormSubmit);
    form.addEventListener('submit', handleUnitFormSubmit);
}

/**
 * Handle unit form submission (create or edit)
 * @param {Event} e - The form submit event
 */
async function handleUnitFormSubmit(e) {
    e.preventDefault();

    const idElement = document.querySelector('input[name="id"]');
    const formId = idElement ? parseInt(idElement.value) : 0;
    const isEdit = formId && formId > 0;

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
        console.log('Form data:', formData);
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        console.log('CSRF token present:', !!csrfToken);

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken || ''
            },
            body: JSON.stringify(formData)
        });

        console.log('Response status:', response.status);

        if (!response.ok) {
            const text = await response.text();
            console.error('Response body:', text);
            const errorDiv = document.getElementById('formErrors');
            try {
                const responseData = JSON.parse(text);
                console.error('Parsed response:', responseData);
                if (errorDiv) {
                    if (responseData.errors && Array.isArray(responseData.errors)) {
                        errorDiv.innerHTML = '<div class="alert alert-danger">' +
                            responseData.errors.map(e => escapeHtml(e)).join('<br>') +
                            '</div>';
                    } else if (responseData.error) {
                        errorDiv.innerHTML = '<div class="alert alert-danger">' + escapeHtml(responseData.error) + '</div>';
                    }
                }
            } catch (parseErr) {
                console.error('Could not parse error response:', parseErr);
                if (errorDiv) {
                    errorDiv.innerHTML = '<div class="alert alert-danger">Error: ' + escapeHtml(text) + '</div>';
                }
            }
            return;
        }

        const responseData = await response.json();
        console.log('Response data:', responseData);

        if (responseData.success) {
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('unitModal'));
            if (modalInstance) {
                modalInstance.hide();
            }
            await new Promise(resolve => setTimeout(resolve, 500));
            await loadUnits();
        } else {
            const errorDiv = document.getElementById('formErrors');
            if (errorDiv) {
                errorDiv.innerHTML = '<div class="alert alert-danger">Error saving unit</div>';
            }
        }
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred. Please try again.');
    }
}

// Delete unit
async function deleteUnit(id) {
    if (!confirm('Are you sure you want to delete this unit?')) return;

    try {
        const response = await fetch(`/units/${id}/delete`, { 
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });
        if (response.ok) {
            await loadUnits();
        } else {
            const data = await response.json();
            alert(data.message || 'Error deleting unit');
        }
    } catch (error) {
        console.error('Error deleting unit:', error);
        alert('Error deleting unit');
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
