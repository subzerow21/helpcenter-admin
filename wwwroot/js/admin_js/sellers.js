/**
 * --- GLOBAL CONFIG & STATE ---
 */
let currentSeller = "";
let currentDecision = ""; // "approval" or "rejection"
let currentSellerToSuspend = "";

document.addEventListener('DOMContentLoaded', () => {
    // Search Functionality across all tables
    const searchInput = document.getElementById('sellerSearchInput');
    if (searchInput) {
        searchInput.addEventListener('keyup', function () {
            const filter = this.value.toLowerCase();
            const rows = document.querySelectorAll('.consumer-grid-wrapper tbody tr');
            rows.forEach(row => {
                row.style.display = row.textContent.toLowerCase().includes(filter) ? "" : "none";
            });
        });
    }
});

/**
 * --- TAB SWITCHING ---
 */
function switchSellerTab(viewName) {
    if (viewName === 'performance') return;

    const views = {
        pending: document.getElementById('view-pending'),
        active: document.getElementById('view-active'),
        archived: document.getElementById('view-archived')
    };

    const buttons = {
        pending: document.getElementById('tab-pending'),
        active: document.getElementById('tab-active'),
        archived: document.getElementById('tab-archived')
    };

    Object.keys(views).forEach(key => {
        if (views[key]) views[key].classList.add('d-none');
        if (buttons[key]) {
            buttons[key].classList.remove('bg-dark', 'text-white', 'fw-bold', 'active-tab');
            buttons[key].classList.add('text-muted');
        }
    });

    if (views[viewName]) {
        views[viewName].classList.remove('d-none');
        buttons[viewName].classList.add('bg-dark', 'text-white', 'fw-bold', 'active-tab');
        buttons[viewName].classList.remove('text-muted');
    }
}

/**
 * --- DOCUMENT VIEWER ---
 */
function viewSellerDocuments(shopName, docUrl) {
    const loadingText = document.getElementById('docLoadingText');
    const docFrame = document.getElementById('docFrame');

    if (loadingText) loadingText.innerText = `Previewing KYC Documents for ${shopName}`;
    if (docFrame) docFrame.src = docUrl;

    const docModal = new bootstrap.Modal(document.getElementById('documentViewerModal'));
    docModal.show();
}

/**
 * --- APPROVAL / REJECTION WORKFLOW ---
 */
function openDecisionModal(shopName, type) {
    currentSeller = shopName;
    currentDecision = type;

    const title = document.getElementById('decisionTitle');
    const btn = document.getElementById('decisionConfirmBtn');
    const note = document.getElementById('decisionNote');

    document.getElementById('targetSellerName').innerText = shopName;

    // Reset UI State
    note.value = "";
    note.classList.remove('is-invalid');

    if (type === 'approval') {
        title.innerHTML = '<i class="bi bi-check-circle-fill text-success me-2"></i>Approve Seller';
        btn.className = "btn btn-success rounded-pill px-4";
        btn.innerText = "Confirm Approval";
        note.placeholder = "Optional: Welcome message or next steps...";
    } else {
        title.innerHTML = '<i class="bi bi-x-circle-fill text-danger me-2"></i>Reject Application';
        btn.className = "btn btn-danger rounded-pill px-4";
        btn.innerText = "Confirm Rejection";
        note.placeholder = "Required: Please explain why the application was rejected...";
    }

    btn.onclick = () => submitDecision();
    new bootstrap.Modal(document.getElementById('decisionModal')).show();
}

function submitDecision() {
    const noteField = document.getElementById('decisionNote');
    const noteValue = noteField.value.trim();

    if (currentDecision === 'rejection' && !noteValue) {
        noteField.classList.add('is-invalid');
        noteField.focus();
        return;
    }

    // --- MOCK SUCCESS LOGIC (STAY ON TAB) ---
    const modalEl = document.getElementById('decisionModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) modalInstance.hide();

    const msg = currentDecision === 'approval'
        ? `${currentSeller} approved successfully (Demo).`
        : `Application for ${currentSeller} rejected (Demo).`;

    triggerSellerToast(msg, currentDecision === 'rejection');

    
}

/**
 * --- SUSPENSION WORKFLOW ---
 */
function openSuspendModal(shopName) {
    currentSellerToSuspend = shopName;
    const reasonField = document.getElementById('suspendReasonText');

    document.getElementById('suspendSellerNameDisplay').innerText = shopName;
    reasonField.value = "";
    reasonField.classList.remove('is-invalid');

    new bootstrap.Modal(document.getElementById('suspendReasonModal')).show();
}

function confirmSuspension() {
    const reasonField = document.getElementById('suspendReasonText');
    const reasonValue = reasonField.value.trim();

    if (!reasonValue) {
        reasonField.classList.add('is-invalid');
        reasonField.focus();
        return;
    }

    // --- MOCK SUCCESS LOGIC (STAY ON TAB) ---
    const modalEl = document.getElementById('suspendReasonModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) modalInstance.hide();

    triggerSellerToast(`${currentSellerToSuspend} has been suspended (Demo).`, true);
}

/**
 * --- HELPERS & TOASTS ---
 */
function triggerSellerToast(msg, isError = false) {
    const toastMessage = document.getElementById('sellerToastMessage');
    const toastIcon = document.getElementById('sellerToastIcon');

    if (toastMessage && toastIcon) {
        toastMessage.innerText = msg;
        toastIcon.className = isError ? "bi bi-exclamation-triangle-fill text-danger fs-5" : "bi bi-check-circle-fill text-success fs-5";

        const toastEl = document.getElementById('sellerToast');
        if (toastEl) {
            const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
            toast.show();
        }
    }
}

function viewSellerDetails(owner, shop, revenue, orders) {
    document.getElementById('detShopName').innerText = shop;
    document.getElementById('detSellerName').innerText = owner;
    document.getElementById('detRevenue').innerText = revenue;
    document.getElementById('detOrders').innerText = orders;
    new bootstrap.Modal(document.getElementById('sellerDetailModal')).show();
}

function openEditModal(shopName, email) {
    document.getElementById('editShopName').value = shopName;
    document.getElementById('editEmail').value = email;
    new bootstrap.Modal(document.getElementById('editSellerModal')).show();
}

function openConfirmModal(type, shopName) {
    const title = document.getElementById('sellerConfirmTitle');
    const msg = document.getElementById('sellerConfirmMessage');
    const icon = document.getElementById('sellerConfirmIcon');
    const btn = document.getElementById('sellerConfirmBtn');

    if (type === 'restore') {
        title.innerText = "Restore Shop?";
        msg.innerText = `Bring ${shopName} back to the active list?`;
        icon.innerHTML = '<i class="bi bi-arrow-counterclockwise text-success fs-1"></i>';
        btn.className = "btn btn-success rounded-pill px-4";
        btn.onclick = () => {
            confirmAction(`Shop ${shopName} restored successfully.`);
            // Stay on current tab logic
        };
    }
    new bootstrap.Modal(document.getElementById('confirmSellerModal')).show();
}

function confirmAction(toastMsg) {
    const modals = ['confirmSellerModal', 'editSellerModal'];
    modals.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            const instance = bootstrap.Modal.getInstance(el);
            if (instance) instance.hide();
        }
    });
    triggerSellerToast(toastMsg || "Action confirmed!");
}