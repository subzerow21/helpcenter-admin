/**
 * Search functionality for the Participant Leaderboard
 */
document.querySelector('input[placeholder*="Search participant"]')?.addEventListener('keyup', function (e) {
    const term = e.target.value.toLowerCase();
    const rows = document.querySelectorAll("tbody tr");

    rows.forEach(row => {
        // Specifically search within the Athlete name column (2nd column)
        const athleteName = row.querySelector('td:nth-child(2)')?.innerText.toLowerCase() || "";
        row.style.display = athleteName.includes(term) ? "" : "none";
    });
});

/**
 * Export Participants to CSV
 * Updated to include Average KM and Average Time columns
 */
function exportParticipants() {
    const table = document.querySelector(".table");
    const rows = table.querySelectorAll("tr");
    let csvContent = "";

    rows.forEach((row, index) => {
        const cells = row.querySelectorAll("th, td");
        let rowData = [];

        cells.forEach((cell, cellIndex) => {
            // Skip the "Actions" column (the last one)
            if (cellIndex === cells.length - 1) return;

            // Clean data: remove newlines, extra spaces, and handle commas
            let data = cell.innerText.replace(/(\r\n|\n|\r)/gm, " ").trim();
            data = data.replace(/"/g, '""'); // Escape double quotes

            if (data.includes(",")) {
                data = `"${data}"`;
            }
            rowData.push(data);
        });

        csvContent += rowData.join(",") + "\n";
    });

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");

    const date = new Date().toISOString().split('T')[0];
    const challengeTitle = document.querySelector("h3")?.innerText.replace(/\s+/g, '_') || "Challenge";

    link.setAttribute("href", url);
    link.setAttribute("download", `${challengeTitle}_Stats_${date}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Challenge Edit & Update Logic
 * Replaces localhost confirmation with the custom modal
 */
function handleChallengeSubmit(event) {
    event.preventDefault(); // Stop form from refreshing page

    // Open the stylized confirmation modal
    const confirmModal = new bootstrap.Modal(document.getElementById('confirmLaunchModal'));
    confirmModal.show();
}

/**
 * Final execution after user clicks "Confirm" in the custom modal
 */
function executeLaunch() {
    // 1. Hide Confirmation Modal
    const confirmModalEl = document.getElementById('confirmLaunchModal');
    const confirmInstance = bootstrap.Modal.getInstance(confirmModalEl);
    if (confirmInstance) confirmInstance.hide();

    // 2. Hide Main Edit Modal (ID synchronized with the HTML launchChallengeModal)
    const editModalEl = document.getElementById('launchChallengeModal');
    const editInstance = bootstrap.Modal.getInstance(editModalEl);
    if (editInstance) editInstance.hide();

    // 3. Trigger Success Notification
    handleAction("Challenge updated successfully! Users have been notified.", "success");
}

/**
 * Open Modal Logic
 */
function openEditChallengeModal() {
    const modalEl = document.getElementById('launchChallengeModal');
    if (modalEl) {
        new bootstrap.Modal(modalEl).show();
    }
}

/**
 * Activity Log View
 */
function viewActivityLog(athleteName) {
    const nameSpan = document.getElementById('detailAthleteName');
    const modalEl = document.getElementById('activityDetailsModal');

    if (nameSpan) {
        nameSpan.innerText = athleteName;
    }

    if (modalEl) {
        // Create or get the existing modal instance
        let modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (!modalInstance) {
            modalInstance = new bootstrap.Modal(modalEl);
        }
        modalInstance.show();
    } else {
        console.error("Activity Details Modal (ID: activityDetailsModal) not found in the HTML.");
    }
}

/**
 * Unified Toast/Notification System
 */
function handleAction(message, type = 'success') {
    const toastEl = document.getElementById('challengeToast');
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');

    if (toastEl && toastMsg) {
        toastMsg.innerText = message;

        // Reset and apply icon/color
        if (type === 'success') {
            toastIcon.className = "bi bi-check-circle-fill text-success fs-5";
        } else if (type === 'warning') {
            toastIcon.className = "bi bi-exclamation-triangle-fill text-warning fs-5";
        }

        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();
    }
}

/**
 * Image Preview for the Modal
 */
function previewChallengeImage(input) {
    const preview = document.getElementById('imagePreview');
    if (input.files && input.files[0] && preview) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.classList.remove('d-none');
        }
        reader.readAsDataURL(input.files[0]);
    }
}