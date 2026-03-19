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
        window.location.href = '/Admin/Dashboard';
});