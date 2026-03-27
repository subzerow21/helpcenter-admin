/**
 * --- GLOBAL STATE ---
 */
let currentRequestId = "";
let currentAction = "";
let currentAmount = 0;
let currentReviewId = "";
let currentProductId = ""; // Added for Product Approvals
let currentEditingPromoId = null;
let selectedImageFile = null;
let imageBase64Data = null;

document.addEventListener('DOMContentLoaded', function () {
    console.log('Finance & Product Admin Dashboard Initialized');

    // Load all pending lists
    loadPendingPayouts();
    loadPendingProductApprovals(); // NEW: For products needing approval
    loadGlobalPromotions();

    // Delegation for Promo Buttons
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('edit-promo-btn')) {
            const promoId = parseInt(e.target.getAttribute('data-promo-id'));
            const promo = window.promotionsData?.find(p => p.id === promoId);
            if (promo) editPromo(promo);
        }
        if (e.target.classList.contains('delete-promo-btn')) {
            deletePromo(parseInt(e.target.getAttribute('data-promo-id')));
        }
    });
});

/**
 * --- PRODUCT APPROVAL WORKFLOW (NEW) ---
 * Specifically for new product listings awaiting admin green light.
 */
async function loadPendingProductApprovals() {
    try {
        const response = await fetch('/Admin/GetPendingProducts');
        const result = await response.json();
        const products = Array.isArray(result) ? result : (result.data || []);

        const tbody = document.querySelector('#pendingProductsTable tbody');
        if (!tbody) return;

        if (products.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No products awaiting approval.</td></tr>';
            return;
        }

        tbody.innerHTML = products.map(p => `
            <tr>
                <td class="ps-4">
                    <div class="d-flex align-items-center">
                        <img src="${p.mainImage || '/images/placeholder.png'}" class="rounded-3 me-2" style="width: 40px; height: 40px; object-fit: cover;">
                        <div>
                            <span class="d-block fw-bold small">${escapeHtml(p.name)}</span>
                            <span class="text-muted tiny">Category: ${escapeHtml(p.category)}</span>
                        </div>
                    </div>
                </td>
                <td><span class="small fw-bold">${escapeHtml(p.shopName)}</span></td>
                <td><span class="fw-bold">₱${p.price.toLocaleString()}</span></td>
                <td class="small text-muted">${new Date(p.createdAt).toLocaleDateString()}</td>
                <td class="text-center">
                    <div class="d-flex justify-content-center gap-1">
                        <button class="btn btn-sm btn-success rounded-pill px-3 fw-bold" 
                                onclick="processProductApproval('${p.id}', 'approve')">Approve</button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3 fw-bold" 
                                onclick="openProductRejectModal('${p.id}')">Reject</button>
                    </div>
                </td>
            </tr>
        `).join('');
    } catch (err) {
        console.error("Error loading products:", err);
    }
}

// Quick Approval
async function processProductApproval(productId, status, reason = "") {
    showFinanceToast('Updating product status...');
    try {
        const response = await fetch('/Admin/UpdateProductStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId, status, reason })
        });
        const data = await response.json();
        if (data.success) {
            showFinanceToast(data.message);
            loadPendingProductApprovals();
        } else {
            showFinanceToast(data.message, true);
        }
    } catch (err) {
        showFinanceToast('Error updating product', true);
    }
}

// Rejection requires a reason
function openProductRejectModal(productId) {
    currentProductId = productId;
    const reason = prompt("Please provide a reason for rejecting this product:");
    if (reason === null) return; // Cancelled
    if (reason.trim() === "") {
        showFinanceToast("Rejection reason is mandatory", true);
        return;
    }
    processProductApproval(productId, 'reject', reason);
}

/**
 * --- PAYOUT WORKFLOW ---
 */
function openPayoutModal(id, amount, action) {
    currentRequestId = id;
    currentAction = action;
    currentAmount = amount;

    const title = document.getElementById('financeTitle');
    const msg = document.getElementById('financeMessage');
    const btn = document.getElementById('financeConfirmBtn');

    if (action === 'approve') {
        title.innerText = "Approve Payout";
        msg.innerText = `Release ₱${amount.toLocaleString()} for Request #${id}?`;
        btn.className = "btn btn-success w-100 rounded-pill";
    } else {
        title.innerText = "Reject Request";
        msg.innerText = `Mandatory reason required for Request #${id}:`;
        btn.className = "btn btn-danger w-100 rounded-pill";
    }

    btn.onclick = submitPayoutDecision;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('financeActionModal')).show();
}

async function submitPayoutDecision() {
    const note = document.getElementById('financeNote')?.value.trim();
    if (currentAction === 'reject' && !note) {
        document.getElementById('financeNote').classList.add('is-invalid');
        return;
    }

    bootstrap.Modal.getInstance(document.getElementById('financeActionModal'))?.hide();
    showFinanceToast('Processing...');

    try {
        const response = await fetch('/Admin/ProcessPayout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ withdrawalId: currentRequestId, action: currentAction, reason: note, amount: currentAmount })
        });
        const data = await response.json();
        if (data.success) {
            showFinanceToast(data.message);
            loadPendingPayouts();
        } else {
            showFinanceToast(data.message, true);
        }
    } catch (err) { showFinanceToast('Connection error', true); }
}

async function loadPendingPayouts() {
    try {
        const response = await fetch('/Admin/GetPendingPayouts');
        const result = await response.json();
        const payouts = Array.isArray(result) ? result : (result.data || []);
        const tbody = document.querySelector('#payoutsTable tbody');
        if (!tbody) return;

        tbody.innerHTML = payouts.length === 0
            ? '<tr><td colspan="6" class="text-center py-4 text-muted">No pending payouts.</td></tr>'
            : payouts.map(p => `
                <tr>
                    <td class="ps-4 fw-bold">#${p.id}</td>
                    <td><span class="d-block fw-bold small">${p.shopName}</span></td>
                    <td><span class="text-success fw-bold">₱${p.amount.toLocaleString()}</span></td>
                    <td><span class="badge bg-light text-dark border">${p.bankName}</span></td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-success rounded-pill px-3" onclick="openPayoutModal(${p.id}, ${p.amount}, 'approve')">Approve</button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3" onclick="openPayoutModal(${p.id}, ${p.amount}, 'reject')">Reject</button>
                    </td>
                </tr>
            `).join('');
    } catch (err) { console.error(err); }
}

/**
 * --- GLOBAL PROMO LOGIC ---
 */
async function handleGlobalPromoSubmission(event) {
    event.preventDefault();
    const requestData = {
        id: currentEditingPromoId,
        name: document.getElementById('promoTitle').value.trim(),
        description: document.getElementById('promoDesc').value.trim(),
        discountPercent: parseFloat(document.getElementById('promoPercent').value),
        startDate: document.getElementById('promoStart').value,
        endDate: document.getElementById('noEndDate').checked ? null : document.getElementById('promoEnd').value,
        isIndefinite: document.getElementById('noEndDate').checked,
        bannerImageBase64: imageBase64Data || document.getElementById('promoPreview').getAttribute('data-base64')
    };

    try {
        const resp = await fetch('/Admin/SaveGlobalPromotion', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });
        const res = await resp.json();
        if (res.success) {
            showFinanceToast(res.message);
            resetPromoForm();
            loadGlobalPromotions();
        }
    } catch (e) { showFinanceToast('Error saving promo', true); }
}

async function loadGlobalPromotions() {
    const response = await fetch('/Admin/GetGlobalPromotions');
    const result = await response.json();
    const promotions = Array.isArray(result) ? result : (result.data || []);
    window.promotionsData = promotions;

    const list = document.getElementById('activePromoList');
    if (!list) return;

    list.innerHTML = promotions.map(promo => `
        <div class="bg-light p-3 rounded-4 border mb-2 d-flex align-items-center">
            <img src="${promo.bannerImageBase64 || '/images/placeholder.png'}" class="rounded-3 me-3" style="width: 50px; height: 50px; object-fit: cover;">
            <div class="flex-grow-1">
                <div class="fw-bold small">${escapeHtml(promo.name)} (-${promo.discountPercent}%)</div>
                <div class="tiny text-muted">Start: ${new Date(promo.startDate).toLocaleDateString()}</div>
            </div>
            <div class="d-flex gap-1">
                <button class="btn btn-sm btn-white border rounded-pill edit-promo-btn" data-promo-id="${promo.id}">Edit</button>
                <button class="btn btn-sm btn-outline-danger rounded-pill delete-promo-btn" data-promo-id="${promo.id}">End</button>
            </div>
        </div>
    `).join('');
}

/**
 * --- HELPERS ---
 */
function showFinanceToast(msg, isError = false) {
    const toastEl = document.getElementById('financeToast');
    if (!toastEl) return;
    document.getElementById('financeToastMessage').innerText = msg;
    document.getElementById('financeToastIcon').innerHTML = isError ? '⚠️' : '✅';
    bootstrap.Toast.getOrCreateInstance(toastEl).show();
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text || "";
    return div.innerHTML;
}

function resetPromoForm() {
    currentEditingPromoId = null;
    imageBase64Data = null;
    document.getElementById('adminDiscountForm').reset();
    document.getElementById('promoPreview').classList.add('d-none');
}