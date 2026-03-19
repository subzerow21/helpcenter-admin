/**
 * SETTINGS.JS - Admin Portal
 * Handles Global Branding, Security, Moderation, and Audit Logging
 */

let pendingAction = null;

document.addEventListener('DOMContentLoaded', () => {

    // --- 1. ENHANCED DEFAULT TAB LOGIC ---
    // Target the specific button and pane for Account Security
    const defaultTabEl = document.querySelector('#v-pills-tab button[data-bs-target="#tab-account"]');
    const defaultPane = document.getElementById('tab-account');

    if (defaultTabEl && defaultPane) {
        // Ensure classes are present for immediate visibility
        defaultTabEl.classList.add('active');
        defaultPane.classList.add('show', 'active');

        // Initialize and show via Bootstrap to ensure internal state is synced
        if (typeof bootstrap !== 'undefined') {
            const tabTrigger = bootstrap.Tab.getOrCreateInstance(defaultTabEl);
            tabTrigger.show();
        }
    }

    // --- 2. MODAL CONFIRMATION LOGIC ---
    const confirmBtn = document.getElementById('confirmBtnExecute');
    if (confirmBtn) {
        // Use cloneNode to wipe existing listeners and prevent double-execution
        const newConfirmBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

        newConfirmBtn.addEventListener('click', () => {
            if (typeof pendingAction === 'function') {
                pendingAction();
                pendingAction = null;
            }
        });
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

    // Optional: Update icon based on action (e.g., shield for security, triangle for danger)
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

    const now = new Date().toLocaleString([], {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });

    const newRow = `
        <tr>
            <td>${now}</td>
            <td><strong>${user}</strong></td>
            <td>${action}</td>
            <td>${target}</td>
            <td><span class="badge bg-success-subtle text-success rounded-pill">Success</span></td>
        </tr>
    `;
    tableBody.insertAdjacentHTML('afterbegin', newRow);
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
        "Are you sure you want to update your password? You will be required to use the new password for your next login.",
        () => {
            // Execution logic
            addAuditEntry("System Admin", "Security Update", "Password Changed");
            showActionToast("Password updated successfully.");

            // Clear fields
            document.getElementById('currentPass').value = "";
            document.getElementById('newPass').value = "";
            document.getElementById('confirmNewPass').value = "";

            hideModal();
        },
        'bi-shield-lock', 'text-danger' // Security-specific icon and color
    );
}

/**
 * 4. PERMISSIONS
 */

function openInviteModal() {
    const modalEl = document.getElementById('inviteModModal');
    document.getElementById('modModalLabel').innerText = "Add New Moderator";
    document.getElementById('modName').value = "";
    document.getElementById('modEmail').value = "";
    document.getElementById('modRole').value = "Moderator";
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

function editModerator(name, email, role) {
    const modalEl = document.getElementById('inviteModModal');
    document.getElementById('modModalLabel').innerText = "Edit Permissions: " + name;
    document.getElementById('modName').value = name;
    document.getElementById('modEmail').value = email;
    document.getElementById('modRole').value = role;
    bootstrap.Modal.getOrCreateInstance(modalEl).show();
}

function processModerator() {
    const name = document.getElementById('modName').value;
    const email = document.getElementById('modEmail').value;
    if (!email || !name) {
        showActionToast("Error: Name and Email are required.");
        return;
    }
    openConfirmModal(
        "Confirm Permissions",
        `Apply these access settings for ${name}?`,
        () => {
            hideModal();
            hideModal('inviteModModal');
            addAuditEntry("System Admin", "Moderator Access Update", email);
            showActionToast("Permissions updated.");
        },
        'bi-person-gear', 'text-dark'
    );
}

function requestRemoveAccess(name, rowId) {
    openConfirmModal(
        "Revoke Access",
        `Remove ${name} from the administrative team? This action is immediate.`,
        () => {
            const row = document.getElementById(rowId);
            if (row) row.remove();
            hideModal();
            addAuditEntry("System Admin", "Revoked Access", name);
            showActionToast(`Access revoked for ${name}.`);
        },
        'bi-person-x', 'text-danger'
    );
}