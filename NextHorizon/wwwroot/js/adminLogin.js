function showToast(message) {
    document.getElementById('toastMessage').innerText = message;
    const toastEl = document.getElementById('monochromeToast');
    const toast = new bootstrap.Toast(toastEl);
    toast.show();
}

function sendResetLink() {
    const email = document.getElementById('resetEmail').value;
    if (email) {
        const modalEl = document.getElementById('forgotPasswordModal');
        const modal = bootstrap.Modal.getInstance(modalEl);
        modal.hide();
        showToast("A reset link has been sent to " + email);
    } else {
        showToast("Please enter a valid email address.");
    }
}

function selectRole(label, value) {
    document.getElementById('selectedRole').innerText = label;
    document.getElementById('accessLevelInput').value = value;
}


document.getElementById('mainLoginForm').addEventListener('submit', function (e) {
    e.preventDefault();

    // Get the value from the hidden input
    const selectedRole = document.getElementById('accessLevelInput').value;

    if (selectedRole === 'admin') {
        // Route for Super Admin
        window.location.href = '/Admin/Dashboard';
    } else if (selectedRole === 'finance' || selectedRole === 'support') {
        // Route for everyone else (Staff/Other page)
        window.location.href = '/Staff/Portal';
    } else {
        // Fallback if no role is detected
        showToast("Please select a valid access level.");
    }
});