/**
 * SETTINGS.JS - Admin Portal
 * Handles Global Branding, Security, Moderation, and Audit Logging
 */

let pendingAction = null;

document.addEventListener('DOMContentLoaded', () => {

    // --- 1. ENHANCED DEFAULT TAB LOGIC ---
    const defaultTabEl = document.querySelector('#v-pills-tab button[data-bs-target="#tab-account"]');
    const defaultPane = document.getElementById('tab-account');

    if (defaultTabEl && defaultPane) {
        defaultTabEl.classList.add('active');
        defaultPane.classList.add('show', 'active');

        if (typeof bootstrap !== 'undefined') {
            const tabTrigger = bootstrap.Tab.getOrCreateInstance(defaultTabEl);
            tabTrigger.show();
        }
    }

    // --- 2. MODAL CONFIRMATION LOGIC ---
    const confirmBtn = document.getElementById('confirmBtnExecute');
    if (confirmBtn) {
        const newConfirmBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

        newConfirmBtn.addEventListener('click', () => {
            if (typeof pendingAction === 'function') {
                pendingAction();
                pendingAction = null;
            }
        });
    }

    // --- 3. INITIALIZE SEARCH FILTER ---
    const searchInput = document.getElementById('adminSearchInput');
    if (searchInput) {
        searchInput.addEventListener('keyup', filterAdminTable);
    }

    // --- 4. HASH-BASED TAB NAVIGATION ---
    var hash = window.location.hash;
    if (hash) {
        var tabTriggerEl = document.querySelector(`[data-bs-target="${hash}"]`);
        if (tabTriggerEl) {
            var tab = new bootstrap.Tab(tabTriggerEl);
            tab.show();
        }
    }
});

/**
 * 1. UTILITIES
 */

function openConfirmModal(title, message, action, iconClass = 'bi-exclamation-circle', iconColor = 'text-warning') {
    const modalEl = document.getElementById('confirmActionModal');
    const iconEl = document.getElementById('confirmIcon');
    if (!modalEl) return;

    document.getElementById('confirmTitle').innerText = title;
    document.getElementById('confirmBody').innerText = message;

    if (iconEl) {
        iconEl.className = `bi ${iconClass} ${iconColor} display-4 mb-3`;
    }

    pendingAction = action;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}

function showActionToast(message) {
    const toastEl = document.getElementById('settingsToast');
    const toastMsg = document.getElementById('toastMsg');
    if (toastEl && toastMsg) {
        toastMsg.innerText = message;
        const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
        toast.show();
    }
}

function hideModal(modalId = 'confirmActionModal') {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;
    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) modal.hide();
}

function addAuditEntry(user, action, target) {
    const tableBody = document.getElementById('auditLogBody');
    if (!tableBody) return;

    const now = new Date();
    const timestamp = now.getFullYear() + '-' +
        String(now.getMonth() + 1).padStart(2, '0') + '-' +
        String(now.getDate()).padStart(2, '0') + ' ' +
        String(now.getHours()).padStart(2, '0') + ':' +
        String(now.getMinutes()).padStart(2, '0');

    const newRow = `
        <tr class="fade-in">
            <td>${timestamp}</td>
            <td><strong>${user}</strong></td>
            <td>${action}</td>
            <td>${target}</td>
            <td><span class="badge bg-success-subtle text-success rounded-pill">Success</span></td>
        </tr>
    `;
    tableBody.insertAdjacentHTML('afterbegin', newRow);

    const container = tableBody.closest('.scrollable-card-body');
    if (container) container.scrollTop = 0;
}

/**
 * 2. SEARCH & FILTERING
 */
function filterAdminTable() {
    const input = document.getElementById('adminSearchInput');
    const filter = input.value.toLowerCase();
    const rows = document.querySelectorAll('.admin-row');

    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        row.style.display = text.includes(filter) ? "" : "none";
    });
}

/**
 * 3. ACCOUNT SECURITY
 */
function saveProfileInfo() {
    const name = document.getElementById('adminFullName').value;
    const email = document.getElementById('adminEmail').value;

    if (!name || !email) {
        showActionToast("Error: Name and Email cannot be empty.");
        return;
    }

    openConfirmModal(
        "Update Profile",
        `Change account details to ${name}?`,
        () => {
            hideModal();
            addAuditEntry("System Admin", "Profile Update", name);
            showActionToast("Profile information updated.");
        },
        'bi-person-check', 'text-dark'
    );
}

function requestPasswordChange() {
    const currentPass = document.getElementById('currentPass').value;
    const newPass = document.getElementById('newPass').value;
    const confirmPass = document.getElementById('confirmNewPass').value;

    if (!currentPass) {
        showActionToast("Error: Current password is required.");
        return;
    }
    if (newPass.length < 6) {
        showActionToast("Error: New password must be at least 6 characters.");
        return;
    }
    if (newPass !== confirmPass) {
        showActionToast("Error: New passwords do not match.");
        return;
    }

    openConfirmModal(
        "Confirm Password Change",
        "Update your security credentials? You will need the new password for your next login.",
        () => {
            addAuditEntry("System Admin", "Security Update", "Password Changed");
            showActionToast("Password updated successfully.");
            document.getElementById('currentPass').value = "";
            document.getElementById('newPass').value = "";
            document.getElementById('confirmNewPass').value = "";
            hideModal();
        },
        'bi-shield-lock', 'text-danger'
    );
}

/**
 * 4. PERMISSIONS / ADMINISTRATIVE ACCESS
 */

function updateRoleUI(roleName, roleValue) {
    document.getElementById('selectedRoleText').innerText = roleName;
    document.getElementById('accessLevelInput').value = roleValue;
    // Support Assignment visibility logic removed as requested
}

// Toggle for the credential override section
function toggleCredentialOverride() {
    const section = document.getElementById('credentialOverrideSection');
    const btn = document.getElementById('overrideToggleBtn');
    const isEditAction = document.getElementById('modModalLabel').innerText.includes("Edit");

    if (section.classList.contains('d-none')) {
        section.classList.remove('d-none');
        btn.innerHTML = '<i class="bi bi-x-circle me-1"></i> Cancel Credential Update';
        btn.classList.replace('btn-outline-dark', 'btn-outline-danger');
    } else {
        section.classList.add('d-none');

        // Reset label based on whether we are adding or editing
        const label = isEditAction ? "Update User's Login Credentials" : "Manually assign login credentials";
        btn.innerHTML = `<i class="bi bi-shield-lock me-1"></i> ${label}`;

        btn.classList.replace('btn-outline-danger', 'btn-outline-dark');
        document.getElementById('overrideUsername').value = "";
        document.getElementById('overridePassword').value = "";
    }
}

function openInviteModal() {
    const modalEl = document.getElementById('inviteModModal');
    const btn = document.getElementById('overrideToggleBtn');

    document.getElementById('modModalLabel').innerText = "Add New User";
    btn.innerHTML = '<i class="bi bi-shield-lock me-1"></i> Manually assign login credentials';

    // Reset standard inputs
    document.getElementById('modName').value = "";
    document.getElementById('modEmail').value = "";
    document.getElementById('modPhone').value = "";

    // Reset Custom Dropdowns
    updateRoleUI('Moderator', 'mod');

    const section = document.getElementById('credentialOverrideSection');
    if (!section.classList.contains('d-none')) {
        section.classList.add('d-none');
        btn.classList.replace('btn-outline-danger', 'btn-outline-dark');
    }

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

function editModerator(name, email, role, phone) {
    const modalEl = document.getElementById('inviteModModal');
    const btn = document.getElementById('overrideToggleBtn');

    document.getElementById('modModalLabel').innerText = "Edit Permissions: " + name;
    btn.innerHTML = '<i class="bi bi-shield-lock me-1"></i> Update User\'s Login Credentials';

    document.getElementById('modName').value = name;
    document.getElementById('modEmail').value = email;
    document.getElementById('modPhone').value = phone || "";

    // Update Custom Role UI
    updateRoleUI(role, role.toLowerCase());

    const section = document.getElementById('credentialOverrideSection');
    if (!section.classList.contains('d-none')) {
        section.classList.add('d-none');
        btn.classList.replace('btn-outline-danger', 'btn-outline-dark');
    }

    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

function processModerator() {
    const name = document.getElementById('modName').value;
    const email = document.getElementById('modEmail').value;
    const role = document.getElementById('selectedRoleText').innerText;
    const isOverriding = !document.getElementById('credentialOverrideSection').classList.contains('d-none');

    if (!name || !email) {
        showActionToast("Error: Name and Email are required.");
        return;
    }

    let confirmationMsg = `Apply ${role} settings for ${name}?`;
    if (isOverriding) confirmationMsg += ` [Login Credentials Updated]`;

    openConfirmModal(
        "Confirm Permissions",
        confirmationMsg,
        () => {
            hideModal();
            hideModal('inviteModModal');

            addAuditEntry("System Admin", "Moderator Access Update", `${name} (${role})`);
            showActionToast("Saved successfully.");
        },
        'bi-person-gear', 'text-dark'
    );
}

function requestRemoveAccess(name, rowId) {
    openConfirmModal(
        "Revoke Access",
        `Move ${name} to revoked status? They will lose all portal access immediately.`,
        () => {
            const row = document.getElementById(rowId);
            if (row) {
                row.classList.add('fade-out');
                setTimeout(() => {
                    row.remove();
                    addAuditEntry("System Admin", "Revoked Access", name);
                    showActionToast(`Access revoked for ${name}.`);
                }, 300);
            }
            hideModal();
        },
        'bi-trash', 'text-danger' // Updated to trash icon per your request
    );
}

function reinstateAccess(name) {
    openConfirmModal(
        "Reinstate Access",
        `Restore administrative permissions for ${name}?`,
        () => {
            hideModal();
            addAuditEntry("System Admin", "Reinstated Access", name);
            showActionToast(`${name} has been restored to active status.`);
        },
        'bi-arrow-counterclockwise', 'text-dark'
    );
}