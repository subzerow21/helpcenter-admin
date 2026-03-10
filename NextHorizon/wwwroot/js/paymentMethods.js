/* =============================================================
   PAYMENT METHODS - FINAL SYNCHRONIZED LOGIC
   ============================================================= */

let editingCardId = null;
let currentPaymentType = 'Card';
let addressData = {};
let nextId = 100;

const regionNames = {
    "01": "Region I", "02": "Region II", "03": "Region III", "4A": "Region IV-A",
    "4B": "Region IV-B", "05": "Region V", "06": "Region VI", "07": "Region VII",
    "08": "Region VIII", "09": "Region IX", "10": "Region X", "11": "Region XI",
    "12": "Region XII", "13": "Region XIII", "ARMM": "ARMM", "CAR": "CAR", "NCR": "NCR", "NIR": "NIR"
};

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
    } catch (e) { console.error("Error loading address data", e); }
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
 * UPDATED: Optimized to remove layout gaps by toggling display correctly
 */
function setPaymentType(type) {
    currentPaymentType = type;
    document.getElementById('selectedPaymentType').value = type;

    const cardCol = document.getElementById('card-left-column');
    const walletFields = document.getElementById('ewallet-fields');
    const billingSection = document.getElementById('billing-address-section');

    if (type === 'Card') {
        // Show Card Left Column and Billing Right Column
        if (cardCol) cardCol.style.display = 'block';
        if (billingSection) billingSection.style.display = 'block';
        if (walletFields) walletFields.style.display = 'none';
    } else {
        // Hide both standard columns, Show E-Wallet (which spans full width via CSS)
        if (cardCol) cardCol.style.display = 'none';
        if (billingSection) billingSection.style.display = 'none';
        if (walletFields) walletFields.style.display = 'block';
    }

    document.getElementById('type-card').classList.toggle('active', type === 'Card');
    document.getElementById('type-ewallet').classList.toggle('active', type === 'EWallet');
}

/* --- CRUD ACTIONS --- */

function prepareAddForm() {
    editingCardId = null;
    const form = document.getElementById('paymentForm');
    form.reset();
    document.getElementById('methodId').value = "";

    // Reset border colors
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
        form.querySelector('[name="HolderName"]').value = data.holderName || data.HolderName || "";
        const last4 = data.last4 || data.Last4 || "";
        form.querySelector('[name="CardNumber"]').value = last4 ? "**** **** **** " + last4 : "";

        const exp = data.expiry || data.Expiry || "";
        if (exp && exp.includes('/')) {
            const parts = exp.split('/');
            form.querySelector('[name="ExpiryMonth"]').value = parts[0];
            form.querySelector('[name="ExpiryYear"]').value = parts[1].length === 2 ? "20" + parts[1] : parts[1];
        }

        // Billing Address
        form.querySelector('[name="Region"]').value = data.region || data.Region || "";
        form.querySelector('[name="Province"]').value = data.province || data.Province || "";
        form.querySelector('[name="City"]').value = data.city || data.City || "";
        form.querySelector('[name="Barangay"]').value = data.barangay || data.Barangay || "";
        form.querySelector('[name="Postal"]').value = data.postal || data.Postal || "";
        form.querySelector('[name="Street"]').value = data.street || data.Street || "";
    } else {
        form.querySelector('[name="Brand"]').value = data.brand || data.Brand || "GCash";
        form.querySelector('[name="Account"]').value = data.account || data.Account || "";
        // Mapping the unique E-wallet holder name field
        form.querySelector('[name="HolderNameEwallet"]').value = data.holderName || data.HolderName || "";
    }
    form.querySelector('.btn-submit').innerText = "UPDATE PAYMENT METHOD";
}

function handleFormSubmit() {
    const form = document.getElementById('paymentForm');
    const formData = new FormData(form);
    const type = formData.get('Type');
    let isValid = true;

    // Validation Helper
    const check = (name, required = true) => {
        const el = form.querySelector(`[name="${name}"]`);
        if (!el) return true;
        const val = el.value.trim();
        const isInvalid = required && (!val || val === "MM" || val === "YYYY" || val === "Select");
        el.style.borderColor = isInvalid ? "black" : "#ccc";
        if (isInvalid) isValid = false;
        return !isInvalid;
    };

    if (type === 'Card') {
        check("HolderName");
        ["CardNumber", "ExpiryMonth", "ExpiryYear", "Region", "Province", "City", "Postal"].forEach(f => check(f));
    } else {
        check("HolderNameEwallet");
        check("Brand");
        check("Account");
    }

    if (!isValid) {
        Swal.fire({ title: 'REQUIRED FIELDS', icon: 'error', iconColor: '#000', confirmButtonColor: '#000' });
        return;
    }

    const cardNum = formData.get('CardNumber');
    const newMethod = {
        id: editingCardId || nextId++,
        type: type,
        brand: type === 'Card' ? "Visa" : formData.get('Brand'),
        last4: type === 'Card' ? (cardNum.includes('*') ? cardNum.slice(-4) : cardNum.slice(-4)) : null,
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

    if (editingCardId) {
        const existingItem = document.getElementById(`method-${editingCardId}`);
        if (existingItem) existingItem.outerHTML = renderPaymentMethodCard(newMethod);
    } else {
        const container = document.getElementById('payment-view-section');
        if (container.querySelector('.empty-state-container')) {
            location.reload();
            return;
        }
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
    const icon = method.type === 'Card' ? 'bi-credit-card-2-front' : 'bi-wallet2';
    const detailTitle = method.type === 'Card' ? `${method.brand} Ending in ${method.last4}` : `${method.brand} Account`;
    const detailSub = method.type === 'Card' ? `Expires ${method.expiry}` : method.account;

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
                ${method.isDefault ? '<span class="badge-default">DEFAULT</span>' : `<button class="btn-set-default" onclick="setDefault(${method.id})">SET DEFAULT</button>`}
                <button class="btn-remove" onclick="removeCard(${method.id})">Remove</button>
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
            } else if (remainingItems.length === 1 || (wasDefault && remainingItems.length > 0)) {
                const nextId = remainingItems[0].id.replace('method-', '');
                applyDefaultUI(nextId);
            }
        }
    });
}