/**
 * Tab Switching Logic
 */
function switchChallengeTab(viewName) {
    const leaderboardView = document.getElementById('view-leaderboard');
    const tasksView = document.getElementById('view-active-tasks');
    const btnLead = document.getElementById('tab-leaderboard');
    const btnTasks = document.getElementById('tab-active-tasks');

    if (viewName === 'leaderboard') {
        leaderboardView.classList.remove('d-none');
        tasksView.classList.add('d-none');
        btnLead.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        btnTasks.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
    } else {
        leaderboardView.classList.add('d-none');
        tasksView.classList.remove('d-none');
        btnTasks.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        btnLead.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
    }
}

/**
 * Filter and Search Logic
 */
function filterLeaderboard() {
    const selectedCategory = document.getElementById('categoryFilter').value;
    const rows = document.querySelectorAll('#leaderboardTable tbody tr');

    rows.forEach(row => {
        const categoryBadge = row.querySelector('td:nth-child(3)').innerText.trim();
        if (selectedCategory === "All" || categoryBadge.includes(selectedCategory)) {
            row.style.display = "";
        } else {
            row.style.display = "none";
        }
    });
}

document.getElementById('leaderboardSearch')?.addEventListener('keyup', function (e) {
    const term = e.target.value.toLowerCase();
    const rows = document.querySelectorAll('#leaderboardTable tbody tr');

    rows.forEach(row => {
        const username = row.querySelector('td:nth-child(2)').innerText.toLowerCase();
        if (username.includes(term)) {
            row.style.display = "";
        } else {
            row.style.display = "none";
        }
    });
});

/**
 * Launch Challenge Handler
 */
function handleChallengeSubmit(event) {
    event.preventDefault(); // Stop refresh to allow validation and custom modal

    const startDate = new Date(document.getElementById('startDate').value);
    const endDate = new Date(document.getElementById('endDate').value);

    if (endDate < startDate) {
        alert("End date cannot be earlier than the start date.");
        return;
    }

    // Instead of window.confirm (localhost), open our custom Bootstrap modal
    const confirmModal = new bootstrap.Modal(document.getElementById('confirmLaunchModal'));
    confirmModal.show();
}

/**
 * Final Execution (Called from the Confirm Modal)
 */
function executeLaunch() {
    // 1. Hide Confirmation Modal
    const confirmModalEl = document.getElementById('confirmLaunchModal');
    const confirmInstance = bootstrap.Modal.getInstance(confirmModalEl);
    if (confirmInstance) confirmInstance.hide();

    // 2. Hide Main Launch Modal
    const launchModalEl = document.getElementById('launchChallengeModal');
    const launchInstance = bootstrap.Modal.getInstance(launchModalEl);
    if (launchInstance) launchInstance.hide();

    // 3. Trigger Toast
    saveChallengeAction();

    // 4. Reset Form
    document.getElementById('launchForm').reset();
}

/**
 * Image Preview Logic
 */
function previewChallengeImage(input) {
    const preview = document.getElementById('imagePreview');
    const placeholder = document.getElementById('uploadPlaceholder');

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.classList.remove('d-none');
            placeholder.classList.add('d-none');
        }
        reader.readAsDataURL(input.files[0]);
    }
}

/**
 * Modal Openers
 */
function openCreateChallengeModal() {
    const form = document.getElementById('launchForm');
    if (form) form.reset();

    const preview = document.getElementById('imagePreview');
    const placeholder = document.getElementById('uploadPlaceholder');
    if (preview && placeholder) {
        preview.src = "#";
        preview.classList.add('d-none');
        placeholder.classList.remove('d-none');
    }

    const modalEl = document.getElementById('launchChallengeModal');
    if (modalEl) new bootstrap.Modal(modalEl).show();
}

/**
 * User Profile View Logic
 */
function openUserView(username, rank, dist, act, time, picUrl) {
    const nameEl = document.getElementById('userNameView');
    const rankEl = document.getElementById('userRankView');
    const distEl = document.getElementById('userDistView');
    const actEl = document.getElementById('userActView');
    const timeEl = document.getElementById('userTimeView');
    const picEl = document.getElementById('userModalPic');

    if (nameEl) nameEl.innerText = username;
    if (rankEl) rankEl.innerText = rank.includes('#') ? "Ranked " + rank : "Ranked #" + rank;
    if (distEl) distEl.innerText = dist;
    if (actEl) actEl.innerText = act;
    if (timeEl) timeEl.innerText = time;

    if (picEl) {
        const fallback = `https://ui-avatars.com/api/?name=${encodeURIComponent(username)}&background=random&size=128`;
        picEl.src = picUrl && picUrl.trim() !== "" ? picUrl : fallback;
    }

    const modalEl = document.getElementById('userViewModal');
    if (modalEl) new bootstrap.Modal(modalEl).show();
}

/**
 * Toast Notifications
 */
function saveChallengeAction() {
    triggerToast("Challenge live! Users will receive a notification.", "text-success", "bi-lightning-fill");
}

function confirmOverride() {
    const modalEl = document.getElementById('overrideModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) modalInstance.hide();

    triggerToast("Challenge archived. Final results calculated.", "text-warning", "bi-shield-fill-check");
}

function triggerToast(msg, colorClass = "text-success", icon = "bi-check-circle-fill") {
    const toastMsgEl = document.getElementById('toastMsg');
    const toastIconEl = document.getElementById('toastIcon');
    const toastEl = document.getElementById('challengeToast');

    if (toastMsgEl && toastIconEl && toastEl) {
        toastMsgEl.innerText = msg;
        toastIconEl.className = `bi ${icon} ${colorClass} fs-5`;

        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();
    }
}