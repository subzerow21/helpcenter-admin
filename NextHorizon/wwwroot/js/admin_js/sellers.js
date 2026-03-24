﻿/**
 * --- GLOBAL CONFIG & STATE ---
 */
let currentSeller = "";
let currentDecision = "";
let currentSellerToSuspend = "";
let currentSellerId = 0;
let editSellerId = 0;

document.addEventListener('DOMContentLoaded', () => {
    switchSellerTab('pending');

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
        pending:  document.getElementById('view-pending'),
        active:   document.getElementById('view-active'),
        archived: document.getElementById('view-archived')
    };

    const buttons = {
        pending:  document.getElementById('tab-pending'),
        active:   document.getElementById('tab-active'),
        archived: document.getElementById('tab-archived')
    };

    Object.keys(views).forEach(key => {
        if (views[key])   views[key].classList.add('d-none');
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

    const statusMap = { pending: 'Pending', active: 'Active', archived: 'Suspended' };
    if (statusMap[viewName]) loadSellers(statusMap[viewName], viewName);
}

/**
 * --- LOAD & RENDER SELLERS ---
 */
async function loadSellers(status, viewName) {
    try {
        const res     = await fetch(`/Admin/GetSellers?status=${status}`);
        const sellers = await res.json();

        if      (viewName === 'pending')  renderPending(sellers);
        else if (viewName === 'active')   renderActive(sellers);
        else if (viewName === 'archived') renderSuspended(sellers);
    } catch (e) {
        console.error('Failed to load sellers', e);
    }
}

function renderPending(sellers) {
     document.getElementById('pending-count').textContent = sellers.length;
    const tbody = document.querySelector('#view-pending tbody');
    if (!sellers.length) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">No pending sellers.</td></tr>`;
        return;
    }
    tbody.innerHTML = sellers.map(s => `
        <tr>
            <td class="fw-bold">${s.businessName}</td>
            <td>${s.ownerName}</td>
            <td>${s.businessEmail}</td>
            <td>${s.createdAt ? new Date(s.createdAt).toLocaleDateString('en-US', { month:'short', day:'numeric', year:'numeric' }) : 'N/A'}</td>
            <td>
                ${s.documentPath
                    ? `<button class="btn btn-link btn-sm text-decoration-none"
                               onclick="viewSellerDocuments('${s.businessName}', '${s.documentPath}')">
                           <i class="bi bi-file-earmark-pdf me-1"></i>View KYC
                       </button>`
                    : '<span class="text-muted small">No docs</span>'}
            </td>
            <td>
                <button class="btn btn-sm btn-success rounded-pill px-3 me-1"
                        onclick="openDecisionModal('${s.businessName}', 'approval', ${s.sellerId})">Approve</button>
                <button class="btn btn-sm btn-outline-danger rounded-pill px-3"
                        onclick="openDecisionModal('${s.businessName}', 'rejection', ${s.sellerId})">Reject</button>
            </td>
        </tr>`).join('');
}

function renderActive(sellers) {
    const tbody = document.querySelector('#view-active tbody');
    if (!sellers.length) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">No active sellers.</td></tr>`;
        return;
    }
    tbody.innerHTML = sellers.map(s => `
        <tr>
            <td class="border-start fw-bold">${s.businessName}</td>
            <td class="border-start text-secondary">${s.ownerName}</td>
            <td class="border-start text-secondary">${s.createdAt ? new Date(s.createdAt).toLocaleDateString('en-US', { month:'short', day:'numeric', year:'numeric' }) : 'N/A'}</td>
            <td class="border-start">${s.totalProducts} Items</td>
            <td class="border-start fw-bold">₱${Number(s.totalSales).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</td>
            <td class="border-start"><span class="badge bg-success opacity-75">Active</span></td>
            <td class="border-start">
                <div class="action-icons">
                    <i class="bi bi-eye me-2 text-info" style="cursor:pointer;"
                       onclick="viewSellerDetails('${s.ownerName}', '${s.businessName}', 'N/A', 'N/A')"></i>
                    <i class="bi bi-pencil me-2 text-primary" style="cursor:pointer;"
                       onclick="openEditModal('${s.businessName}', '${s.businessEmail}', ${s.sellerId})"></i>
                    <i class="bi bi-slash-circle text-danger" title="Suspend Seller" style="cursor:pointer;"
                       onclick="openSuspendModal('${s.businessName}', ${s.sellerId})"></i>
                </div>
            </td>
        </tr>`).join('');
}

function renderSuspended(sellers) {
    const tbody = document.querySelector('#view-archived tbody');
    if (!sellers.length) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No suspended sellers.</td></tr>`;
        return;
    }
    tbody.innerHTML = sellers.map(s => `
        <tr>
            <td class="border-start">${s.businessName}</td>
            <td class="border-start">${s.ownerName}</td>
            <td class="border-start text-danger small">Suspended</td>
            <td class="border-start text-muted">${s.createdAt ? new Date(s.createdAt).toLocaleDateString('en-US', { month:'short', day:'numeric', year:'numeric' }) : 'N/A'}</td>
            <td class="border-start">
                <button class="btn btn-sm btn-outline-dark rounded-pill py-0 px-3"
                        onclick="openConfirmModal('restore', '${s.businessName}', ${s.sellerId})">Restore</button>
            </td>
        </tr>`).join('');
}

/**
 * --- DOCUMENT VIEWER ---
 */
function viewSellerDocuments(shopName, docUrl) {
    const paths    = docUrl.split(';').filter(p => p.trim() !== '');
    const container = document.getElementById('docContainer');
    const baseUrl  = window.location.origin;

    if (paths.length === 1) {
        const fullUrl = baseUrl + paths[0];
        container.innerHTML = `<iframe src="https://docs.google.com/viewer?url=${encodeURIComponent(fullUrl)}&embedded=true"
            style="width:100%; height:100%; border:none;"></iframe>`;
    } else {
        const tabs = paths.map((p, i) =>
            `<button onclick="switchDoc(${i})" id="doc-tab-${i}"
                style="padding:6px 14px; border:1px solid #ccc;
                background:${i===0?'#111':'white'}; color:${i===0?'white':'#111'};
                cursor:pointer; font-size:12px; border-radius:4px; margin-right:4px;">
                Doc ${i + 1}
            </button>`).join('');

        const iframes = paths.map((p, i) => {
            const viewerUrl = `https://docs.google.com/viewer?url=${encodeURIComponent(baseUrl + p)}&embedded=true`;
            return `<iframe id="doc-frame-${i}"
                src="${i===0 ? viewerUrl : ''}" data-src="${viewerUrl}"
                style="width:100%; height:440px; border:none; display:${i===0?'block':'none'};"></iframe>`;
        }).join('');

        container.innerHTML = `
            <div style="padding:8px; background:#f5f5f5; border-bottom:1px solid #ddd;">${tabs}</div>
            ${iframes}`;
    }

    new bootstrap.Modal(document.getElementById('documentViewerModal')).show();
}

function switchDoc(index) {
    document.querySelectorAll('[id^="doc-frame-"]').forEach((frame, i) => {
        frame.style.display = i === index ? 'block' : 'none';
        if (i === index && !frame.src.includes('viewer')) frame.src = frame.dataset.src;
    });
    document.querySelectorAll('[id^="doc-tab-"]').forEach((tab, i) => {
        tab.style.background = i === index ? '#111' : 'white';
        tab.style.color      = i === index ? 'white' : '#111';
    });
}

/**
 * --- APPROVAL / REJECTION WORKFLOW ---
 */
function openDecisionModal(shopName, type, sellerId) {
    currentSeller   = shopName;
    currentDecision = type;
    currentSellerId = sellerId;

    const title = document.getElementById('decisionTitle');
    const btn   = document.getElementById('decisionConfirmBtn');
    const note  = document.getElementById('decisionNote');

    document.getElementById('targetSellerName').innerText  = shopName;
    document.getElementById('decisionTypeLabel').innerText = type === 'approval' ? 'approval' : 'rejection';

    note.value = "";
    note.classList.remove('is-invalid');

    if (type === 'approval') {
        title.innerHTML   = '<i class="bi bi-check-circle-fill text-success me-2"></i>Approve Seller';
        btn.className     = "btn btn-success rounded-pill px-4";
        btn.innerText     = "Confirm Approval";
        note.placeholder  = "Optional: Welcome message or next steps...";
    } else {
        title.innerHTML   = '<i class="bi bi-x-circle-fill text-danger me-2"></i>Reject Application';
        btn.className     = "btn btn-danger rounded-pill px-4";
        btn.innerText     = "Confirm Rejection";
        note.placeholder  = "Required: Please explain why the application was rejected...";
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

    const status = currentDecision === 'approval' ? 'Active' : 'Rejected';

    fetch('/Admin/UpdateSellerStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sellerId: currentSellerId, status })
    })
    .then(r => r.json())
    .then(data => {
        bootstrap.Modal.getInstance(document.getElementById('decisionModal')).hide();
        triggerSellerToast(
            data.success
                ? `${currentSeller} ${currentDecision === 'approval' ? 'approved' : 'rejected'} successfully.`
                : data.message,
            !data.success
        );
        if (data.success) switchSellerTab('pending');
    });
}

/**
 * --- SUSPENSION WORKFLOW ---
 */
function openSuspendModal(shopName, sellerId) {
    currentSellerToSuspend = shopName;
    currentSellerId        = sellerId;

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

    fetch('/Admin/UpdateSellerStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sellerId: currentSellerId, status: 'Suspended' })
    })
    .then(r => r.json())
    .then(data => {
        bootstrap.Modal.getInstance(document.getElementById('suspendReasonModal')).hide();
        triggerSellerToast(
            data.success ? `${currentSellerToSuspend} has been suspended.` : data.message,
            !data.success
        );
        if (data.success) switchSellerTab('active');
    });
}

/**
 * --- CONFIRM MODAL (RESTORE) ---
 */
function openConfirmModal(type, shopName, sellerId) {
    currentSellerId = sellerId;

    const title = document.getElementById('sellerConfirmTitle');
    const msg   = document.getElementById('sellerConfirmMessage');
    const icon  = document.getElementById('sellerConfirmIcon');
    const btn   = document.getElementById('sellerConfirmBtn');

    if (type === 'restore') {
        title.innerText    = "Restore Shop?";
        msg.innerText      = `Bring ${shopName} back to the active list?`;
        icon.innerHTML     = '<i class="bi bi-arrow-counterclockwise text-success fs-1"></i>';
        btn.className      = "btn btn-success rounded-pill px-4";
        btn.onclick = () => {
            fetch('/Admin/UpdateSellerStatus', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ sellerId: currentSellerId, status: 'Active' })
            })
            .then(r => r.json())
            .then(data => {
                bootstrap.Modal.getInstance(document.getElementById('confirmSellerModal')).hide();
                triggerSellerToast(
                    data.success ? `${shopName} restored successfully.` : data.message,
                    !data.success
                );
                if (data.success) switchSellerTab('archived');
            });
        };
    }

    new bootstrap.Modal(document.getElementById('confirmSellerModal')).show();
}

/**
 * --- EDIT MODAL ---
 */
function openEditModal(shopName, email, sellerId) {
    editSellerId = sellerId;
    document.getElementById('editShopName').value = shopName;
    document.getElementById('editEmail').value    = email;
    new bootstrap.Modal(document.getElementById('editSellerModal')).show();
}

function confirmAction(toastMsg) {
    const shopName = document.getElementById('editShopName').value.trim();
    const email    = document.getElementById('editEmail').value.trim();

    fetch('/Admin/UpdateSellerInfo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sellerId: editSellerId, businessName: shopName, businessEmail: email })
    })
    .then(r => r.json())
    .then(data => {
        ['confirmSellerModal', 'editSellerModal'].forEach(id => {
            const el = document.getElementById(id);
            if (el) bootstrap.Modal.getInstance(el)?.hide();
        });
        triggerSellerToast(data.success ? "Seller updated successfully." : data.message, !data.success);
        if (data.success) switchSellerTab('active');
    });
}

/**
 * --- SELLER DETAILS MODAL ---
 */
function viewSellerDetails(owner, shop, revenue, orders) {
    document.getElementById('detShopName').innerText  = shop;
    document.getElementById('detSellerName').innerText = owner;
    document.getElementById('detRevenue').innerText   = revenue;
    document.getElementById('detOrders').innerText    = orders;
    new bootstrap.Modal(document.getElementById('sellerDetailModal')).show();
}

/**
 * --- FILTER ---
 */
function filterSellers(type) {
    bootstrap.Modal.getInstance(document.getElementById('filterModal'))?.hide();

    if (type === 'pending') {
        switchSellerTab('pending');
        return;
    }

    switchSellerTab('active');

    setTimeout(() => {
        const rows = document.querySelectorAll('#view-active tbody tr');
        rows.forEach(row => {
            if (type === 'all') {
                row.style.display = '';
            } else if (type === 'top') {
                const salesCell = row.cells[4]?.innerText?.trim();
                const sales     = parseFloat(salesCell?.replace(/[^0-9.]/g, '')) || 0;
                row.style.display = sales > 5000 ? '' : 'none';
            }
        });
    }, 500);
}

/**
 * --- TOAST ---
 */
function triggerSellerToast(msg, isError = false) {
    const toastMessage = document.getElementById('sellerToastMessage');
    const toastIcon    = document.getElementById('sellerToastIcon');

    if (toastMessage && toastIcon) {
        toastMessage.innerText = msg;
        toastIcon.className    = isError
            ? "bi bi-exclamation-triangle-fill text-danger fs-5"
            : "bi bi-check-circle-fill text-success fs-5";

        const toastEl = document.getElementById('sellerToast');
        if (toastEl) new bootstrap.Toast(toastEl, { delay: 3000 }).show();
    }
}