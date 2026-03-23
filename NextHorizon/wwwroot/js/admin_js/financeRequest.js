
// 1. Global State Variables
let currentRequestId = "";
let currentAction = "";
let currentAmount = 0;
let currentReviewId = "";
let currentEditingPromoId = null;
let selectedImageFile = null; 
let imageBase64Data = null; 

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
        msg.innerText = `You are about to release ₱${amount.toLocaleString()} for Request #${id}.`;
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

    // Close modal
    let modalEl = document.getElementById('financeActionModal');
    let modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();

    // Show loading toast
    showFinanceToast('Processing...', false);

    // Call the API
    processPayout(currentRequestId, currentAction, noteValue, currentAmount);
}

async function processPayout(withdrawalId, action, reason, amount) {
    try {
        const response = await fetch('/Admin/ProcessPayout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                withdrawalId: withdrawalId,
                action: action,
                reason: reason,
                amount: amount
            })
        });

        const data = await response.json();

        if (data.success) {
            showFinanceToast(data.message, false);
            // Reload the payouts list
            loadPendingPayouts();
        } else {
            showFinanceToast(data.message, true);
        }
    } catch (error) {
        console.error('Error processing payout:', error);
        showFinanceToast('Error processing payout', true);
    }
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

    // Show loading toast
    showFinanceToast('Processing...', false);

    // Call the API
    processDiscount(currentReviewId, decision, noteValue);
}

async function processDiscount(discountId, action, reason) {
    try {
        const response = await fetch('/Admin/ProcessDiscount', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                discountId: discountId,
                action: action,
                reason: reason
            })
        });

        const data = await response.json();

        if (data.success) {
            showFinanceToast(data.message, false);
            // Reload the discounts list
            loadPendingDiscounts();
        } else {
            showFinanceToast(data.message, true);
        }
    } catch (error) {
        console.error('Error processing discount:', error);
        showFinanceToast('Error processing discount', true);
    }
}

/**
 * --- GLOBAL PROMO LOGIC (Launch & Edit) ---
 */

function handleImageSelection(input) {
    const preview = document.getElementById('promoPreview');
    const uploadZoneText = document.getElementById('uploadZoneText');
    
    if (input.files && input.files[0]) {
        selectedImageFile = input.files[0];
        
        const reader = new FileReader();
        reader.onload = (e) => {
            // Store the Base64 string
            imageBase64Data = e.target.result;
            preview.src = e.target.result;
            preview.setAttribute('data-base64', e.target.result);
            preview.classList.remove('d-none');
            if (uploadZoneText) uploadZoneText.classList.add('d-none');
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

// Handle form submission - SAVE TO DATABASE
async function handleGlobalPromoSubmission(event) {
    event.preventDefault();
    
    // Get form values
    const title = document.getElementById('promoTitle').value.trim();
    const desc = document.getElementById('promoDesc').value.trim();
    const percent = parseFloat(document.getElementById('promoPercent').value);
    const start = document.getElementById('promoStart').value;
    const noEnd = document.getElementById('noEndDate').checked;
    const end = noEnd ? null : document.getElementById('promoEnd').value;
    
    // Validate required fields
    if (!title) {
        showFinanceToast('Please enter a campaign name', true);
        return;
    }
    
    if (isNaN(percent) || percent <= 0) {
        showFinanceToast('Please enter a valid discount percentage', true);
        return;
    }
    
    if (!start) {
        showFinanceToast('Please select a start date', true);
        return;
    }
    
    // Get image data
    const previewImg = document.getElementById('promoPreview');
    let bannerImageBase64 = null;
    let bannerImageName = null;
    let bannerImageContentType = null;
    
    // Check if there's a new image selected
    if (selectedImageFile) {
        bannerImageName = selectedImageFile.name;
        bannerImageContentType = selectedImageFile.type;
        bannerImageBase64 = imageBase64Data;
    } 
    // Check if there's an existing image from edit
    else if (previewImg && previewImg.getAttribute('data-base64')) {
        bannerImageBase64 = previewImg.getAttribute('data-base64');
        // For existing images, we might not have the name/type, but that's okay
        bannerImageName = 'existing_image';
        bannerImageContentType = 'image/png'; // Default fallback
    }
    
    const requestData = {
        id: currentEditingPromoId,
        name: title,
        description: desc,
        bannerImageBase64: bannerImageBase64,
        bannerImageName: bannerImageName,
        bannerImageContentType: bannerImageContentType,
        discountPercent: percent,
        startDate: start,
        endDate: end,
        isIndefinite: noEnd
    };
    
    console.log('Saving promotion:', requestData);
    
    try {
        showFinanceToast('Saving promotion...', false);
        
        const response = await fetch('/Admin/SaveGlobalPromotion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.success) {
            showFinanceToast(result.message, false);
            resetPromoForm();
            await loadGlobalPromotions(); // Reload the list from database
        } else {
            showFinanceToast(result.message || 'Failed to save promotion', true);
        }
    } catch (error) {
        console.error('Error saving promotion:', error);
        showFinanceToast('Error saving promotion: ' + error.message, true);
    }
}


// Delete promotion from database
async function deletePromo(promoId) {
    if (!confirm('Are you sure you want to end this promotion?')) return;
    
    try {
        const response = await fetch('/Admin/DeleteGlobalPromotion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ id: promoId })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showFinanceToast(result.message, false);
            await loadGlobalPromotions(); // Reload the list from database
        } else {
            showFinanceToast(result.message, true);
        }
    } catch (error) {
        console.error('Error deleting promotion:', error);
        showFinanceToast('Error ending promotion', true);
    }
}

// Edit promotion - load from database
// Edit promotion - load from database
function editPromo(promo) {
    console.log('Edit promo called with:', promo);
    
    // Handle different input types
    let promoData;
    
    if (typeof promo === 'number') {
        // If it's just an ID, find it in stored promotions
        if (window.promotionsData) {
            promoData = window.promotionsData.find(p => p.id === promo);
            if (!promoData) {
                console.error('Promotion not found with ID:', promo);
                showFinanceToast('Promotion data not found', true);
                return;
            }
        } else {
            console.error('No promotions data available');
            showFinanceToast('Unable to load promotion data', true);
            return;
        }
    } else if (typeof promo === 'string') {
        // If it's a JSON string, parse it
        try {
            promoData = JSON.parse(promo);
        } catch (e) {
            console.error('Failed to parse promo data:', e);
            showFinanceToast('Invalid promotion data', true);
            return;
        }
    } else if (typeof promo === 'object' && promo !== null) {
        // If it's already an object, use it directly
        promoData = promo;
    } else {
        console.error('Invalid promo data type:', typeof promo);
        showFinanceToast('Invalid promotion data', true);
        return;
    }
    
    // Validate we have the required data
    if (!promoData || !promoData.id) {
        console.error('Invalid promotion data - missing ID:', promoData);
        showFinanceToast('Invalid promotion data - missing ID', true);
        return;
    }
    
    console.log('Editing promotion:', promoData);
    
    // Set current editing ID
    currentEditingPromoId = promoData.id;
    
    // Fill form fields with promotion data
    const titleInput = document.getElementById('promoTitle');
    const descInput = document.getElementById('promoDesc');
    const percentInput = document.getElementById('promoPercent');
    const startDateInput = document.getElementById('promoStart');
    const endDateInput = document.getElementById('promoEnd');
    const noEndCheckbox = document.getElementById('noEndDate');
    
    if (titleInput) titleInput.value = promoData.name || '';
    if (descInput) descInput.value = promoData.description || '';
    if (percentInput) percentInput.value = promoData.discountPercent || 0;
    
    // Handle start date
    if (promoData.startDate && startDateInput) {
        try {
            let startDate = new Date(promoData.startDate);
            if (!isNaN(startDate.getTime())) {
                startDateInput.value = startDate.toISOString().split('T')[0];
            } else {
                console.warn('Invalid start date:', promoData.startDate);
                startDateInput.value = '';
            }
        } catch (e) {
            console.error('Error parsing start date:', e);
            startDateInput.value = '';
        }
    }
    
    // Handle end date and indefinite flag
    if (noEndCheckbox && endDateInput) {
        if (promoData.isIndefinite || !promoData.endDate) {
            noEndCheckbox.checked = true;
            endDateInput.disabled = true;
            endDateInput.value = '';
        } else {
            noEndCheckbox.checked = false;
            endDateInput.disabled = false;
            try {
                let endDate = new Date(promoData.endDate);
                if (!isNaN(endDate.getTime())) {
                    endDateInput.value = endDate.toISOString().split('T')[0];
                } else {
                    endDateInput.value = '';
                }
            } catch (e) {
                console.error('Error parsing end date:', e);
                endDateInput.value = '';
            }
        }
    }
    
    // Handle image preview
    const preview = document.getElementById('promoPreview');
    const uploadZoneText = document.getElementById('uploadZoneText');
    
    if (preview && uploadZoneText) {
        if (promoData.bannerImageBase64 && promoData.bannerImageBase64 !== 'undefined' && promoData.bannerImageBase64 !== 'null') {
            preview.src = promoData.bannerImageBase64;
            preview.setAttribute('data-base64', promoData.bannerImageBase64);
            preview.classList.remove('d-none');
            uploadZoneText.classList.add('d-none');
        } else {
            preview.classList.add('d-none');
            preview.removeAttribute('data-base64');
            preview.src = '';
            uploadZoneText.classList.remove('d-none');
        }
    }
    
    // Update form title and buttons
    const formTitle = document.getElementById('adminPromoFormTitle');
    if (formTitle) formTitle.innerText = 'Edit Promotion';
    
    const submitBtn = document.getElementById('submitPromoBtn');
    const cancelBtn = document.getElementById('cancelPromoBtn');
    
    if (submitBtn) {
        submitBtn.innerText = 'Save Changes';
        submitBtn.classList.add('btn-primary');
        submitBtn.classList.remove('btn-dark');
    }
    
    if (cancelBtn) {
        cancelBtn.classList.remove('d-none');
    }
    
    // Scroll to form
    if (formTitle) {
        formTitle.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    
    // Show success message
    showFinanceToast(`Editing promotion: ${promoData.name}`, false);
}

function resetPromoForm() {
    currentEditingPromoId = null;
    selectedImageFile = null;  // Reset the selected image file
    imageBase64Data = null;     // Reset the base64 data
    
    const form = document.getElementById('adminDiscountForm');
    if (form) form.reset();
    
    const preview = document.getElementById('promoPreview');
    const uploadZoneText = document.getElementById('uploadZoneText');
    
    if (preview) {
        preview.classList.add('d-none');
        preview.src = "";
        preview.removeAttribute('data-base64');
    }
    
    if (uploadZoneText) {
        uploadZoneText.classList.remove('d-none');
    }
    
    const endDateInput = document.getElementById('promoEnd');
    const noEndCheckbox = document.getElementById('noEndDate');
    
    if (endDateInput) {
        endDateInput.disabled = false;
        endDateInput.value = "";
    }
    
    if (noEndCheckbox) {
        noEndCheckbox.checked = false;
    }
    
    const formTitle = document.getElementById('adminPromoFormTitle');
    if (formTitle) formTitle.innerText = "Launch Global Promotion";
    
    const submitBtn = document.getElementById('submitPromoBtn');
    const cancelBtn = document.getElementById('cancelPromoBtn');
    
    if (submitBtn) {
        submitBtn.innerText = "Launch Promotion";
        submitBtn.classList.remove('btn-primary');
        submitBtn.classList.add('btn-dark');
    }
    
    if (cancelBtn) {
        cancelBtn.classList.add('d-none');
    }
}

async function loadGlobalPromotions() {
    try {
        const response = await fetch('/Admin/GetGlobalPromotions');
        const result = await response.json();
        
        // Handle different response formats
        let promotions = [];
        
        if (Array.isArray(result)) {
            promotions = result;
        } else if (result.data && Array.isArray(result.data)) {
            promotions = result.data;
        } else if (result.promotions && Array.isArray(result.promotions)) {
            promotions = result.promotions;
        } else if (result.error) {
            console.error('Error loading promotions:', result.error);
            showFinanceToast('Error loading promotions: ' + result.error, true);
            return;
        } else {
            console.warn('Unexpected promotions response format:', result);
            promotions = [];
        }
        
        // Store promotions globally for editing
        window.promotionsData = promotions;
        
        const list = document.getElementById('activePromoList');
        if (!list) return;
        
        if (!promotions || promotions.length === 0) {
            list.innerHTML = `
                <div id="emptyPromoState" class="text-center py-5">
                    <i class="bi bi-calendar2-x text-muted fs-1"></i>
                    <p class="text-muted small mt-2">No promotions currently running.</p>
                </div>
            `;
            return;
        }
        
        list.innerHTML = '';
        promotions.forEach(promo => {
            const promoCardId = `promo-${promo.id}`;
            
            // Safely format dates
            let startDateDisplay = 'Not set';
            let endDateDisplay = 'Indefinite';
            
            try {
                if (promo.startDate) {
                    const startDate = new Date(promo.startDate);
                    if (!isNaN(startDate.getTime())) {
                        startDateDisplay = startDate.toLocaleDateString('en-PH', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric' 
                        });
                    }
                }
                
                if (promo.isIndefinite) {
                    endDateDisplay = 'Indefinite';
                } else if (promo.endDate) {
                    const endDate = new Date(promo.endDate);
                    if (!isNaN(endDate.getTime())) {
                        endDateDisplay = endDate.toLocaleDateString('en-PH', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric' 
                        });
                    }
                }
            } catch (e) {
                console.error('Error formatting dates for promo:', promo.id, e);
            }
            
            // Create image data URL if banner image exists
            let imageHtml = '';
            if (promo.bannerImageBase64 && promo.bannerImageBase64 !== 'undefined' && promo.bannerImageBase64 !== 'null') {
                imageHtml = `<img src="${promo.bannerImageBase64}" class="rounded-3 me-3" style="width: 60px; height: 60px; object-fit: cover;" onerror="this.style.display='none'; this.nextSibling.style.display='flex';">`;
                imageHtml += `<div class="bg-secondary bg-opacity-10 rounded-3 me-3 d-flex align-items-center justify-content-center" style="width: 60px; height: 60px; display: none;">
                                <i class="bi bi-image text-muted fs-4"></i>
                            </div>`;
            } else {
                imageHtml = `<div class="bg-secondary bg-opacity-10 rounded-3 me-3 d-flex align-items-center justify-content-center" style="width: 60px; height: 60px;">
                                <i class="bi bi-image text-muted fs-4"></i>
                            </div>`;
            }
            
            // Create the promo HTML with proper data attributes
            const promoHtml = `
                <div class="bg-light p-3 rounded-4 border mb-2 active-promo-card shadow-sm" id="${promoCardId}" data-id="${promo.id}">
                    <div class="d-flex align-items-start">
                        ${imageHtml}
                        <div class="flex-grow-1">
                            <div class="d-flex align-items-center gap-2 mb-1">
                                <span class="fw-bold text-dark">${escapeHtml(promo.name || '')}</span>
                                <span class="badge bg-white text-dark border tiny fw-bold">${promo.discountPercent || 0}% off</span>
                            </div>
                            <p class="tiny text-muted mb-2" style="line-height: 1.2;">${escapeHtml(promo.description || '')}</p>
                            <div class="d-flex gap-3">
                                <div class="tiny text-muted">
                                    <i class="bi bi-calendar-event me-1"></i><b>Start:</b> ${startDateDisplay}
                                </div>
                                <div class="tiny text-muted">
                                    <i class="bi bi-calendar-check me-1"></i><b>End:</b> ${endDateDisplay}
                                </div>
                            </div>
                        </div>
                        <div class="d-flex flex-column gap-1 ms-2">
                            <button class="btn btn-sm btn-white border rounded-pill px-3 tiny fw-bold shadow-sm edit-promo-btn" 
                                data-promo-id="${promo.id}">Edit</button>
                            <button class="btn btn-sm btn-outline-danger rounded-pill px-3 tiny fw-bold delete-promo-btn" 
                                data-promo-id="${promo.id}">End</button>
                        </div>
                    </div>
                </div>
            `;
            list.insertAdjacentHTML('beforeend', promoHtml);
        });
        
    } catch (error) {
        console.error('Error loading promotions:', error);
        showFinanceToast('Error loading promotions: ' + error.message, true);
    }
}

function safeFormatDate(dateString, defaultValue = 'Not set') {
    if (!dateString) return defaultValue;
    
    try {
        const date = new Date(dateString);
        if (isNaN(date.getTime())) {
            console.warn('Invalid date:', dateString);
            return defaultValue;
        }
        return date.toLocaleDateString('en-PH', { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric' 
        });
    } catch (e) {
        console.error('Error formatting date:', e);
        return defaultValue;
    }
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

document.addEventListener('DOMContentLoaded', function() {
    console.log('Finance page loaded');
    
    // Load initial data
    loadPendingPayouts();
    loadPendingDiscounts();
    loadGlobalPromotions();
    
    // Set up event listeners for edit and delete buttons (delegation)
    document.addEventListener('click', function(e) {
        // Handle edit button clicks
        if (e.target.classList.contains('edit-promo-btn')) {
            const promoId = parseInt(e.target.getAttribute('data-promo-id'));
            console.log('Edit button clicked for promo ID:', promoId);
            
            if (window.promotionsData) {
                const promo = window.promotionsData.find(p => p.id === promoId);
                if (promo) {
                    editPromo(promo);
                } else {
                    console.error('Promotion not found with ID:', promoId);
                    showFinanceToast('Promotion data not found', true);
                }
            } else {
                console.error('No promotions data available');
                showFinanceToast('Unable to load promotion data', true);
            }
            e.preventDefault();
        }
        
        // Handle delete button clicks
        if (e.target.classList.contains('delete-promo-btn')) {
            const promoId = parseInt(e.target.getAttribute('data-promo-id'));
            if (confirm('Are you sure you want to end this promotion?')) {
                deletePromo(promoId);
            }
            e.preventDefault();
        }
    });
});

// Load pending payouts from server
async function loadPendingPayouts() {
    try {
        const response = await fetch('/Admin/GetPendingPayouts');
        const result = await response.json();
        
        // Handle different response formats
        let payouts = [];
        
        if (Array.isArray(result)) {
            payouts = result;
        } else if (result.data && Array.isArray(result.data)) {
            payouts = result.data;
        } else if (result.payouts && Array.isArray(result.payouts)) {
            payouts = result.payouts;
        } else if (result.error) {
            console.error('Error loading payouts:', result.error);
            showFinanceToast('Error loading payouts: ' + result.error, true);
            return;
        } else {
            console.warn('Unexpected payouts response format:', result);
            payouts = [];
        }
        
        const tbody = document.querySelector('#payouts tbody');
        if (!tbody) return;
        
        if (payouts.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No pending payout requests</td></tr>';
            return;
        }
        
        tbody.innerHTML = '';
        payouts.forEach(p => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="ps-4 fw-bold">#${p.withdrawalId || p.id || 'N/A'}</td>
                <td>
                    <span class="d-block fw-bold small">${escapeHtml(p.shopName || 'N/A')}</span>
                    <span class="text-muted tiny">${escapeHtml(p.sellerEmail || 'N/A')}</span>
                </td>
                <td><span class="fw-bold text-success">₱${formatNumber(p.amount || 0)}</span></td>
                <td><span class="badge bg-light text-dark border">${escapeHtml(p.bankName || 'N/A')}</span></td>
                <td class="small">${p.requestedAt ? formatDate(p.requestedAt) : 'N/A'}</td>
                <td class="text-center">
                    <div class="d-flex justify-content-center gap-1">
                        <button class="btn btn-sm btn-success rounded-pill px-3 fw-bold"
                                onclick="openPayoutModal(${p.withdrawalId || p.id}, ${p.amount || 0}, 'approve')">
                            Approve
                        </button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3 fw-bold"
                                onclick="openPayoutModal(${p.withdrawalId || p.id}, ${p.amount || 0}, 'reject')">
                            Reject
                        </button>
                    </div>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading payouts:', error);
        showFinanceToast('Error loading payouts: ' + error.message, true);
        
        const tbody = document.querySelector('#payouts tbody');
        if (tbody) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">Error loading payouts. Please refresh the page.</td></tr>';
        }
    }
}

// Load pending discounts from server
async function loadPendingDiscounts() {
    try {
        const response = await fetch('/Admin/GetPendingDiscounts');
        const result = await response.json();
        
        // Handle different response formats
        let discounts = [];
        
        if (Array.isArray(result)) {
            discounts = result;
        } else if (result.data && Array.isArray(result.data)) {
            discounts = result.data;
        } else if (result.discounts && Array.isArray(result.discounts)) {
            discounts = result.discounts;
        } else if (result.error) {
            console.error('Error loading discounts:', result.error);
            showFinanceToast('Error loading discounts: ' + result.error, true);
            return;
        } else {
            // If it's an object but not an array, check if it has values
            console.warn('Unexpected discounts response format:', result);
            discounts = [];
        }
        
        const tbody = document.querySelector('#discounts tbody');
        if (!tbody) return;
        
        if (!discounts || discounts.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No pending discount requests</td></tr>';
            return;
        }
        
        tbody.innerHTML = '';
        discounts.forEach(d => {
            // Safely calculate discounted price
            let originalPrice = parseFloat(d.originalPrice) || 0;
            let discountPercent = parseFloat(d.totalDiscountPercent) || 0;
            let discountedPrice = originalPrice - (originalPrice * discountPercent / 100);
            
            // Safely format dates
            let startDateDisplay = 'N/A';
            let endDateDisplay = 'N/A';
            
            try {
                if (d.createdAt) {
                    const createdDate = new Date(d.createdAt);
                    if (!isNaN(createdDate.getTime())) {
                        startDateDisplay = formatDate(d.createdAt);
                        const endDate = new Date(createdDate);
                        endDate.setDate(endDate.getDate() + 7);
                        endDateDisplay = formatDate(endDate);
                    }
                }
            } catch (e) {
                console.error('Error formatting dates:', e);
            }
            
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="ps-4">
                    <span class="fw-bold small d-block">${escapeHtml(d.productName || 'N/A')}</span>
                    <span class="text-muted tiny">By ${escapeHtml(d.shopName || 'Unknown')}</span>
                </td>
                <td class="text-muted small">₱${formatNumber(originalPrice)}</td>
                <td><span class="badge bg-danger-subtle text-danger fw-bold">-${discountPercent}%</span></td>
                <td class="fw-bold">₱${formatNumber(discountedPrice)}</td>
                <td class="small">${startDateDisplay} - ${endDateDisplay}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-dark rounded-pill px-4 shadow-sm"
                            onclick="openDiscountReviewModal(${d.id}, '${escapeHtml(d.productName || 'Product')}', ${discountPercent}, ${discountedPrice}, '${startDateDisplay} - ${endDateDisplay}', '${d.productImage || ''}')">
                        Review
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading discounts:', error);
        showFinanceToast('Error loading discounts: ' + error.message, true);
        
        // Show empty state in table
        const tbody = document.querySelector('#discounts tbody');
        if (tbody) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">Error loading discounts. Please refresh the page.</td></tr>';
        }
    }
}

async function loadGlobalPromotions() {
    try {
        const response = await fetch('/Admin/GetGlobalPromotions');
        const result = await response.json();
        
        // Handle different response formats
        let promotions = [];
        
        if (Array.isArray(result)) {
            promotions = result;
        } else if (result.data && Array.isArray(result.data)) {
            promotions = result.data;
        } else if (result.promotions && Array.isArray(result.promotions)) {
            promotions = result.promotions;
        } else if (result.error) {
            console.error('Error loading promotions:', result.error);
            showFinanceToast('Error loading promotions: ' + result.error, true);
            return;
        } else {
            console.warn('Unexpected promotions response format:', result);
            promotions = [];
        }
        
        const list = document.getElementById('activePromoList');
        if (!list) return;
        
        if (!promotions || promotions.length === 0) {
            list.innerHTML = `
                <div id="emptyPromoState" class="text-center py-5">
                    <i class="bi bi-calendar2-x text-muted fs-1"></i>
                    <p class="text-muted small mt-2">No promotions currently running.</p>
                </div>
            `;
            return;
        }
        
        list.innerHTML = '';
        promotions.forEach(promo => {
            const promoCardId = `promo-${promo.id}`;
            
            // Safely format dates
            let startDateDisplay = 'Not set';
            let endDateDisplay = 'Indefinite';
            
            try {
                if (promo.startDate) {
                    const startDate = new Date(promo.startDate);
                    if (!isNaN(startDate.getTime())) {
                        startDateDisplay = startDate.toLocaleDateString('en-PH', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric' 
                        });
                    }
                }
                
                if (promo.isIndefinite) {
                    endDateDisplay = 'Indefinite';
                } else if (promo.endDate) {
                    const endDate = new Date(promo.endDate);
                    if (!isNaN(endDate.getTime())) {
                        endDateDisplay = endDate.toLocaleDateString('en-PH', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric' 
                        });
                    }
                }
            } catch (e) {
                console.error('Error formatting dates for promo:', promo.id, e);
            }
            
            // Create image data URL if banner image exists
            let imageHtml = '';
            if (promo.bannerImageBase64 && promo.bannerImageBase64 !== 'undefined') {
                imageHtml = `<img src="${promo.bannerImageBase64}" class="rounded-3 me-3" style="width: 60px; height: 60px; object-fit: cover;">`;
            } else {
                imageHtml = `<div class="bg-secondary bg-opacity-10 rounded-3 me-3 d-flex align-items-center justify-content-center" style="width: 60px; height: 60px;">
                                <i class="bi bi-image text-muted fs-4"></i>
                            </div>`;
            }
            
            // Create the promo HTML with proper onclick handler that passes the full object
            const promoHtml = `
                <div class="bg-light p-3 rounded-4 border mb-2 active-promo-card shadow-sm" id="${promoCardId}" data-id="${promo.id}">
                    <div class="d-flex align-items-start">
                        ${imageHtml}
                        <div class="flex-grow-1">
                            <div class="d-flex align-items-center gap-2 mb-1">
                                <span class="fw-bold text-dark">${escapeHtml(promo.name || '')}</span>
                                <span class="badge bg-white text-dark border tiny fw-bold">${promo.discountPercent || 0}% off</span>
                            </div>
                            <p class="tiny text-muted mb-2" style="line-height: 1.2;">${escapeHtml(promo.description || '')}</p>
                            <div class="d-flex gap-3">
                                <div class="tiny text-muted">
                                    <i class="bi bi-calendar-event me-1"></i><b>Start:</b> ${startDateDisplay}
                                </div>
                                <div class="tiny text-muted">
                                    <i class="bi bi-calendar-check me-1"></i><b>End:</b> ${endDateDisplay}
                                </div>
                            </div>
                        </div>
                        <div class="d-flex flex-column gap-1 ms-2">
                            <button class="btn btn-sm btn-white border rounded-pill px-3 tiny fw-bold shadow-sm edit-promo-btn" 
                                data-promo-id="${promo.id}">Edit</button>
                            <button class="btn btn-sm btn-outline-danger rounded-pill px-3 tiny fw-bold delete-promo-btn" 
                                data-promo-id="${promo.id}">End</button>
                        </div>
                    </div>
                </div>
            `;
            list.insertAdjacentHTML('beforeend', promoHtml);
        });
        
        // Store promotions data globally for editing
        window.promotionsData = promotions;
        
    } catch (error) {
        console.error('Error loading promotions:', error);
        showFinanceToast('Error loading promotions: ' + error.message, true);
    }
}


// Helper functions
function formatNumber(value) {
    return new Intl.NumberFormat('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function addDays(date, days) {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}