/* =============================================================
   PAYMENT METHODS - FINAL CLEAN VERSION
   ============================================================= */

let editingCardId = null;
let currentPaymentType = 'Card';
let addressData = {};

// Generates a unique ID to prevent collisions without using a global counter
const generateId = () => `pm_${Date.now()}_${Math.floor(Math.random() * 1000)}`;

document.addEventListener('DOMContentLoaded', async function () {
    await loadAddressData();
    const form = document.getElementById('paymentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            handleFormSubmit();
        });
    }
});

/* --- CORE FUNCTIONS --- */

async function loadAddressData() {
    try {
        const response = await fetch('/philippine_provinces_cities_municipalities_and_barangays_2016.json');
        addressData = await response.json();
    } catch (e) {
        console.warn("Address data not found, manual entry enabled.");
    }
}

function toggleView() {
    const v = document.getElementById('payment-view-section');
    const f = document.getElementById('payment-form-section');
    const isShowingForm = (f.style.display === 'none' || f.style.display === '');

    v.style.display = isShowingForm ? 'none' : 'block';
    f.style.display = isShowingForm ? 'block' : 'none';

    if (isShowingForm) window.scrollTo({ top: 0, behavior: 'smooth' });
}

/**
 * Optimized to remove layout gaps by toggling display correctly
 */
function setPaymentType(type) {
    currentPaymentType = type;
    const typeInput = document.getElementById('selectedPaymentType');
    if (typeInput) typeInput.value = type;

    const cardCol = document.getElementById('card-left-column');
    const walletFields = document.getElementById('ewallet-fields');
    const billingSection = document.getElementById('billing-address-section');

    if (type === 'Card') {
        if (cardCol) cardCol.style.display = 'block';
        if (billingSection) billingSection.style.display = 'block';
        if (walletFields) walletFields.style.display = 'none';
    } else {
        if (cardCol) cardCol.style.display = 'none';
        if (billingSection) billingSection.style.display = 'none';
        if (walletFields) walletFields.style.display = 'block';
    }

    document.getElementById('type-card')?.classList.toggle('active', type === 'Card');
    document.getElementById('type-ewallet')?.classList.toggle('active', type === 'EWallet');
}

/* --- CRUD ACTIONS --- */

function prepareAddForm() {
    editingCardId = null;
    const form = document.getElementById('paymentForm');
    form.reset();
    document.getElementById('methodId').value = "";

    // Reset all borders
    form.querySelectorAll('input, select').forEach(el => el.style.borderColor = "#ccc");

    setPaymentType('Card');
    form.querySelector('.btn-submit').innerText = "SAVE PAYMENT METHOD";
    toggleView();
}

function prepareEditForm(data) {
    editingCardId = data.id || data.Id;
    const type = data.type || data.Type;
    toggleView();
    setPaymentType(type);

    const form = document.getElementById('paymentForm');
    form.querySelector('[name="Id"]').value = editingCardId;

    if (type === 'Card') {
        form.querySelector('[name="HolderName"]').value = data.holderName || "";
        const last4 = data.last4 || "";
        form.querySelector('[name="CardNumber"]').value = last4 ? "**** **** **** " + last4 : "";

        const exp = data.expiry || "";
        if (exp && exp.includes('/')) {
            const parts = exp.split('/');
            form.querySelector('[name="ExpiryMonth"]').value = parts[0];
            form.querySelector('[name="ExpiryYear"]').value = parts[1].length === 2 ? "20" + parts[1] : parts[1];
        }

        ["Region", "Province", "City", "Barangay", "Postal", "Street"].forEach(field => {
            const el = form.querySelector(`[name="${field}"]`);
            if (el) el.value = data[field.toLowerCase()] || "";
        });
    } else {
        form.querySelector('[name="Brand"]').value = data.brand || "GCash";
        form.querySelector('[name="Account"]').value = data.account || "";
        form.querySelector('[name="HolderNameEwallet"]').value = data.holderName || "";
    }
    form.querySelector('.btn-submit').innerText = "UPDATE PAYMENT METHOD";
}

function handleFormSubmit() {
    const form = document.getElementById('paymentForm');
    const formData = new FormData(form);
    const type = formData.get('Type') || currentPaymentType;

    // Dynamic Validation: Only check visible fields
    let isValid = true;
    form.querySelectorAll('input, select').forEach(el => {
        if (el.offsetParent !== null) { // If visible
            const val = el.value.trim();
            const isMissing = !val || val === "MM" || val === "YYYY" || val === "Select";

            if (isMissing && el.hasAttribute('required')) {
                el.style.borderColor = "black";
                isValid = false;
            } else {
                el.style.borderColor = "#ccc";
            }
        }
    });

    if (!isValid) {
        Swal.fire({ title: 'REQUIRED FIELDS', icon: 'error', iconColor: '#000', confirmButtonColor: '#000' });
        return;
    }

    const cardNum = formData.get('CardNumber');
    const newMethod = {
        id: editingCardId || generateId(),
        type: type,
        brand: type === 'Card' ? "Visa" : formData.get('Brand'),
        last4: type === 'Card' ? cardNum.slice(-4) : null,
        expiry: type === 'Card' ? `${formData.get('ExpiryMonth')}/${formData.get('ExpiryYear').slice(-2)}` : null,
        account: type === 'EWallet' ? formData.get('Account') : null,
        holderName: type === 'Card' ? formData.get('HolderName') : formData.get('HolderNameEwallet'),
        isDefault: false,
        region: formData.get('Region'),
        province: formData.get('Province'),
        city: formData.get('City'),
        postal: formData.get('Postal'),
        barangay: formData.get('Barangay'),
        street: formData.get('Street')
    };
    a
    if (editingCardId) {
        const existingItem = document.getElementById(`method-${editingCardId}`);
        if (existingItem) existingItem.outerHTML = renderPaymentMethodCard(newMethod);
    } else {
        const container = document.getElementById('payment-view-section');
        container.querySelector('.empty-state-container')?.remove();
        container.insertAdjacentHTML('beforeend', renderPaymentMethodCard(newMethod));
    }

    Swal.fire({
        title: editingCardId ? 'UPDATED' : 'SAVED',
        icon: 'success',
        iconColor: '#000',
        confirmButtonColor: '#000'
    }).then(() => toggleView());
}

function renderPaymentMethodCard(method) {
    const isCard = method.type === 'Card';
    const icon = isCard ? 'bi-credit-card-2-front' : 'bi-wallet2';
    const detailTitle = isCard ? `${method.brand} Ending in ${method.last4}` : `${method.brand} Account`;
    const detailSub = isCard ? `Expires ${method.expiry}` : method.account;

    return `
        <div class="saved-card-item ${method.isDefault ? 'active-selection' : ''}" id="method-${method.id}">
            <div class="card-info-left">
                <i class="bi ${icon}"></i>
                <div class="card-details">
                    <span class="card-brand">${detailTitle}</span>
                    <span class="card-expiry">${detailSub}</span>
                </div>
            </div>
            <div class="card-actions-right">
                <button class="btn-edit-card" onclick='prepareEditForm(${JSON.stringify(method)})'>
                    <i class="bi bi-pencil"></i>
                </button>
                ${method.isDefault ? '<span class="badge-default">DEFAULT</span>' : `<button class="btn-set-default" onclick="setDefault('${method.id}')">SET DEFAULT</button>`}
                <button class="btn-remove" onclick="removeCard('${method.id}')">Remove</button>
            </div>
        </div>`;
}

/* --- UI HELPERS --- */

function applyDefaultUI(id) {
    document.querySelectorAll('.saved-card-item').forEach(item => {
        item.classList.remove('active-selection');
        const badge = item.querySelector('.badge-default');
        if (badge) {
            const actionDiv = item.querySelector('.card-actions-right');
            badge.remove();
            const btn = document.createElement('button');
            btn.className = 'btn-set-default';
            btn.innerText = 'SET DEFAULT';
            const itemId = item.id.replace('method-', '');
            btn.onclick = function () { setDefault(itemId); };
            actionDiv.insertBefore(btn, actionDiv.querySelector('.btn-remove'));
        }
    });

    const target = document.getElementById(`method-${id}`);
    if (!target) return;
    target.classList.add('active-selection');
    const actionDiv = target.querySelector('.card-actions-right');
    const setBtn = actionDiv.querySelector('.btn-set-default');
    if (setBtn) setBtn.remove();

    const badge = document.createElement('span');
    badge.className = 'badge-default';
    badge.innerText = 'DEFAULT';
    actionDiv.insertBefore(badge, actionDiv.querySelector('.btn-remove'));
}

function setDefault(id) {
    Swal.fire({
        title: 'SET AS DEFAULT?',
        icon: 'info',
        iconColor: '#000',
        showCancelButton: true,
        confirmButtonColor: '#000'
    }).then((result) => {
        if (result.isConfirmed) applyDefaultUI(id);
    });
}

function removeCard(id) {
    Swal.fire({
        title: 'REMOVE?',
        icon: 'warning',
        iconColor: '#000',
        showCancelButton: true,
        confirmButtonColor: '#000'
    }).then((result) => {
        if (result.isConfirmed) {
            const itemToRemove = document.getElementById(`method-${id}`);
            const wasDefault = itemToRemove.classList.contains('active-selection');
            itemToRemove.remove();

            const remainingItems = document.querySelectorAll('.saved-card-item');
            if (remainingItems.length === 0) {
                location.reload();
            } else if (wasDefault && remainingItems.length > 0) {
                const nextId = remainingItems[0].id.replace('method-', '');
                applyDefaultUI(nextId);
            }
        }
    });
}