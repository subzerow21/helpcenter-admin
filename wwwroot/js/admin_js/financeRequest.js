/**
 * NEXT HORIZON - FINANCE & REQUESTS MODULE
 * Handles: Payouts, Discount Approvals, and Global Promos (Launch & Edit)
 */

// 1. Global State Variables
let currentRequestId = "";
let currentAction = "";
let currentReviewId = "";
let currentEditingPromoId = null;

/**
 * --- PAYOUT WORKFLOW ---
 */
function openPayoutModal(id, amount, action) {
    currentRequestId = id;
    currentAction = action;

    const title = document.getElementById('financeTitle');
    const msg = document.getElementById('financeMessage');
    const btn = document.getElementById('financeConfirmBtn');
    const note = document.getElementById('financeNote');
    const icon = document.getElementById('financeIconContainer');
    const feedback = document.getElementById('rejectionFeedback');

    if (note) {
        note.value = "";
        note.classList.remove('is-invalid');
    }
    if (feedback) feedback.style.display = 'none';

    if (action === 'approve') {
        title.innerText = "Approve Payout";
        msg.innerText = `You are about to release ₱${amount} for Request #${id}.`;
        btn.className = "btn btn-success w-100 rounded-pill";
        icon.className = "bg-success-subtle text-success rounded-circle d-inline-flex align-items-center justify-content-center mb-3";
    } else {
        title.innerText = "Reject Request";
        msg.innerText = `Please provide a mandatory reason for rejecting Request #${id}.`;
        btn.className = "btn btn-danger w-100 rounded-pill";
        icon.className = "bg-danger-subtle text-danger rounded-circle d-inline-flex align-items-center justify-content-center mb-3";
    }

    btn.onclick = function () { submitPayoutDecision(); };

    let modalEl = document.getElementById('financeActionModal');
    let modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}

function submitPayoutDecision() {
    const noteField = document.getElementById('financeNote');
    const feedback = document.getElementById('rejectionFeedback');
    const noteValue = noteField ? noteField.value.trim() : "";

    if (currentAction === 'reject' && !noteValue) {
        if (noteField) noteField.classList.add('is-invalid');
        if (feedback) feedback.style.display = 'block';
        if (noteField) noteField.focus();
        return;
    }

    let modalEl = document.getElementById('financeActionModal');
    let modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();

    const isError = currentAction === 'reject';
    showFinanceToast(isError ? `Payout #${currentRequestId} rejected.` : `Payout #${currentRequestId} approved.`, isError);
}

/**
 * --- DISCOUNT REVIEW WORKFLOW ---
 */
function openDiscountReviewModal(id, name, percent, price, dates, image) {
    currentReviewId = id;

    document.getElementById('revProdName').innerText = name;
    document.getElementById('revProdPercent').innerText = `-${percent}%`;
    document.getElementById('revProdPrice').innerText = `₱${price}`;
    document.getElementById('revProdDates').innerText = dates;

    const imgElement = document.getElementById('revProdImage');
    if (imgElement) {
        imgElement.src = (image && image !== '' && image !== 'null') ? image : '/images/placeholder-product.png';
    }

    document.getElementById('rejectionReasonContainer').style.display = 'none';
    document.getElementById('btnApproveFinal').style.display = 'block';

    const rejectBtn = document.getElementById('btnRejectInitial');
    if (rejectBtn) {
        rejectBtn.innerText = "Reject";
        rejectBtn.className = "btn btn-outline-danger w-100 rounded-pill";
        rejectBtn.onclick = showRejectionInput;
    }

    const noteField = document.getElementById('discountReviewNote');
    if (noteField) {
        noteField.value = "";
        noteField.classList.remove('is-invalid');
    }

    let modalEl = document.getElementById('discountReviewModal');
    let modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}

function showRejectionInput() {
    document.getElementById('rejectionReasonContainer').style.display = 'block';
    document.getElementById('btnApproveFinal').style.display = 'none';
    const rejectBtn = document.getElementById('btnRejectInitial');
    if (rejectBtn) {
        rejectBtn.innerText = "Confirm Rejection";
        rejectBtn.className = "btn btn-danger w-100 rounded-pill";
        rejectBtn.onclick = function () { handleDiscountAction('reject'); };
    }
    document.getElementById('discountReviewNote').focus();
}

function handleDiscountAction(decision) {
    const noteField = document.getElementById('discountReviewNote');
    const noteValue = noteField ? noteField.value.trim() : "";

    if (decision === 'reject' && !noteValue) {
        if (noteField) noteField.classList.add('is-invalid');
        document.getElementById('discountNoteFeedback').style.display = 'block';
        return;
    }

    let modalEl = document.getElementById('discountReviewModal');
    let modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();

    showFinanceToast(`Discount #${currentReviewId} ${decision}ed.`, decision === 'reject');
}

/**
 * --- GLOBAL PROMO LOGIC (Launch & Edit) ---
 */

function handleImageSelection(input) {
    const preview = document.getElementById('promoPreview');
    const uploadZoneContent = document.getElementById('uploadZoneText');

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = (e) => {
            preview.src = e.target.result;
            preview.classList.remove('d-none');
            if (uploadZoneContent) uploadZoneContent.classList.add('d-none');
        };
        reader.readAsDataURL(input.files[0]);
    }
}

function toggleEndDate(checkbox) {
    const endDateInput = document.getElementById('promoEnd');
    if (endDateInput) {
        endDateInput.disabled = checkbox.checked;
        if (checkbox.checked) endDateInput.value = "";
    }
}

function handleGlobalPromoSubmission(event) {
    event.preventDefault();

    const title = document.getElementById('promoTitle').value;
    const desc = document.getElementById('promoDesc').value;
    const percent = document.getElementById('promoPercent').value;
    const start = document.getElementById('promoStart').value;
    const noEnd = document.getElementById('noEndDate').checked;
    const end = noEnd ? "Indefinite" : document.getElementById('promoEnd').value;
    const imgSrc = document.getElementById('promoPreview').src;

    const list = document.getElementById('activePromoList');

    if (list.querySelector('.bi-calendar2-x')) {
        list.innerHTML = "";
    }

    const promoCardId = currentEditingPromoId || `promo-${Date.now()}`;

    // Escape quotes for the onclick function to prevent syntax errors
    const escapedDesc = desc.replace(/'/g, "\\'").replace(/"/g, "&quot;");
    const escapedTitle = title.replace(/'/g, "\\'").replace(/"/g, "&quot;");

    // Inside handleGlobalPromoSubmission function...

    const promoHtml = `
            <div class="bg-light p-3 rounded-4 border mb-2 active-promo-card shadow-sm" id="${promoCardId}">
                <div class="d-flex align-items-start justify-content-between">
                    <div class="pe-2 flex-grow-1">
                        <div class="d-flex align-items-center gap-2 mb-1">
                            <span class="fw-bold text-dark">${title}</span>
                            <span class="badge bg-white text-dark border tiny fw-bold">${percent}% off</span>
                        </div>
                        <p class="tiny text-muted mb-2" style="line-height: 1.2;">${desc}</p>
                
                        <div class="d-flex gap-3">
                            <div class="tiny text-muted">
                                <i class="bi bi-calendar-event me-1"></i><b>Start:</b> ${start}
                            </div>
                            <div class="tiny text-muted">
                                <i class="bi bi-calendar-check me-1"></i><b>End:</b> ${end}
                            </div>
                        </div>
                    </div>
                    <div class="d-flex flex-column gap-1 ms-2">
                        <button class="btn btn-sm btn-white border rounded-pill px-3 tiny fw-bold shadow-sm" 
                            onclick="editPromo('${promoCardId}', '${escapedTitle}', '${escapedDesc}', '${percent}', '${start}', '${end}', '${imgSrc}')">Edit</button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3 tiny fw-bold" 
                            onclick="this.closest('.active-promo-card').remove(); showFinanceToast('Promo ended.');">End</button>
                    </div>
                </div>
            </div>
        `;

    if (currentEditingPromoId) {
        document.getElementById(currentEditingPromoId).outerHTML = promoHtml;
        showFinanceToast("Promotion updated successfully.");
    } else {
        list.insertAdjacentHTML('afterbegin', promoHtml);
        showFinanceToast(`Global Campaign "${title}" launched!`);
    }

    resetPromoForm();
}

function editPromo(id, title, desc, percent, start, end, imageUrl) {
    currentEditingPromoId = id;
    document.getElementById('promoTitle').value = title;
    document.getElementById('promoDesc').value = desc;
    document.getElementById('promoPercent').value = percent;
    document.getElementById('promoStart').value = start;

    const preview = document.getElementById('promoPreview');
    const uploadZoneText = document.getElementById('uploadZoneText');

    if (imageUrl && imageUrl !== "" && !imageUrl.includes('window.location.href')) {
        preview.src = imageUrl;
        preview.classList.remove('d-none');
        if (uploadZoneText) uploadZoneText.classList.add('d-none');
    } else {
        preview.classList.add('d-none');
        if (uploadZoneText) uploadZoneText.classList.remove('d-none');
    }

    const noEndCheck = document.getElementById('noEndDate');
    const endInput = document.getElementById('promoEnd');
    if (end === "Indefinite") {
        noEndCheck.checked = true;
        endInput.disabled = true;
        endInput.value = "";
    } else {
        noEndCheck.checked = false;
        endInput.disabled = false;
        endInput.value = end;
    }

    // --- BUTTON UPDATES ---
    document.getElementById('adminPromoFormTitle').innerText = "Edit Promotion";

    const submitBtn = document.getElementById('submitPromoBtn');
    const cancelBtn = document.getElementById('cancelPromoBtn');

    submitBtn.innerText = "Save Changes";
    cancelBtn.classList.remove('d-none'); // Show Cancel button side-by-side

    // Smooth scroll back to form
    document.getElementById('adminPromoFormTitle').scrollIntoView({ behavior: 'smooth' });
}

function resetPromoForm() {
    currentEditingPromoId = null;
    document.getElementById('adminDiscountForm').reset();

    const preview = document.getElementById('promoPreview');
    preview.classList.add('d-none');
    preview.src = "";
    document.getElementById('uploadZoneText').classList.remove('d-none');
    document.getElementById('promoEnd').disabled = false;

    // --- BUTTON RESET ---
    document.getElementById('adminPromoFormTitle').innerText = "Launch Global Promotion";

    const submitBtn = document.getElementById('submitPromoBtn');
    const cancelBtn = document.getElementById('cancelPromoBtn');

    submitBtn.innerText = "Launch Promotion";
    cancelBtn.classList.add('d-none'); // Hide Cancel button
}

/**
 * --- TOAST HELPER ---
 */
function showFinanceToast(msg, isError = false) {
    const toastMessage = document.getElementById('financeToastMessage');
    const toastIcon = document.getElementById('financeToastIcon');
    const toastEl = document.getElementById('financeToast');

    if (!toastEl) return;

    toastMessage.innerText = msg;
    toastIcon.innerHTML = isError
        ? '<i class="bi bi-exclamation-triangle-fill text-danger fs-5"></i>'
        : '<i class="bi bi-check-circle-fill text-success fs-5"></i>';

    const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
    toast.show();
}