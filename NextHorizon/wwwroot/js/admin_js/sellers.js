document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('sellerSearchInput');

    if (searchInput) {
        searchInput.addEventListener('keyup', function () {
            const filter = this.value.toLowerCase();
            // Target rows in the Active Sellers table
            const rows = document.querySelectorAll('#view-active tbody tr');

            rows.forEach(row => {
                // Get text content from the specific columns
                const shopName = row.cells[0].textContent.toLowerCase();
                const owner = row.cells[1].textContent.toLowerCase();
                const contact = row.cells[2].textContent.toLowerCase();
                const dateJoined = row.cells[3].textContent.toLowerCase();
                const products = row.cells[4].textContent.toLowerCase();
                const sales = row.cells[5].textContent.toLowerCase();

                // Check if the search term exists in ANY of these columns
                if (shopName.includes(filter) ||
                    owner.includes(filter) ||
                    contact.includes(filter) ||
                    dateJoined.includes(filter) ||
                    products.includes(filter) ||
                    sales.includes(filter)) {
                    row.style.display = ""; // Show
                } else {
                    row.style.display = "none"; // Hide
                }
            });
        });
    }
});

function switchSellerTab(viewName) {
    // If user clicked performance, the HTML <a> tag handles the redirect.
    // We only handle local tab switching for 'active' and 'archived'.
    if (viewName === 'performance') return;

    const views = {
        active: document.getElementById('view-active'),
        archived: document.getElementById('view-archived')
    };

    const buttons = {
        active: document.getElementById('tab-active'),
        archived: document.getElementById('tab-archived')
    };

    // Reset all local tabs
    Object.keys(views).forEach(key => {
        if (views[key]) views[key].classList.add('d-none');
        if (buttons[key]) {
            buttons[key].classList.remove('bg-dark', 'text-white', 'fw-bold');
            buttons[key].classList.add('text-muted');
        }
    });

    // Show selected local tab
    if (views[viewName]) {
        views[viewName].classList.remove('d-none');
        buttons[viewName].classList.add('bg-dark', 'text-white', 'fw-bold');
        buttons[viewName].classList.remove('text-muted');
    }
}

// --- TOAST HELPER ---
function triggerSellerToast(msg, isError = false) {
    const toastMessage = document.getElementById('sellerToastMessage');
    const toastIcon = document.getElementById('sellerToastIcon');

    if (toastMessage && toastIcon) {
        toastMessage.innerText = msg;
        toastIcon.className = isError ? "bi bi-exclamation-circle-fill text-danger fs-5" : "bi bi-check-circle-fill text-success fs-5";

        const toastEl = document.getElementById('sellerToast');
        if (toastEl) {
            const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
            toast.show();
        }
    }
}

// --- VIEW HANDLER ---
function openViewModal(shopName, owner, sales, products) {
    document.getElementById('viewShopName').innerText = shopName;
    document.getElementById('viewOwner').innerText = owner;
    document.getElementById('viewSales').innerText = sales;
    document.getElementById('viewProducts').innerText = products;
    new bootstrap.Modal(document.getElementById('viewSellerModal')).show();
}

// --- EDIT HANDLER ---
function openEditModal(shopName, email) {
    document.getElementById('editShopName').value = shopName;
    document.getElementById('editEmail').value = email;
    new bootstrap.Modal(document.getElementById('editSellerModal')).show();
}

// --- CONFIRMATION HANDLER (Archive/Restore/Update) ---
function openConfirmModal(type, shopName) {
    const title = document.getElementById('sellerConfirmTitle');
    const msg = document.getElementById('sellerConfirmMessage');
    const icon = document.getElementById('sellerConfirmIcon');
    const btn = document.getElementById('sellerConfirmBtn');

    if (!title || !msg || !btn) return;

    if (type === 'delete') {
        title.innerText = "Archive Shop?";
        msg.innerText = `Are you sure you want to archive ${shopName}?`;
        icon.innerHTML = '<i class="bi bi-archive text-danger fs-1"></i>';
        btn.className = "btn btn-danger rounded-pill px-4";
        btn.onclick = () => confirmAction(`Shop ${shopName} archived.`);
    } else if (type === 'restore') {
        title.innerText = "Restore Shop?";
        msg.innerText = `Bring ${shopName} back to active list?`;
        icon.innerHTML = '<i class="bi bi-arrow-counterclockwise text-success fs-1"></i>';
        btn.className = "btn btn-success rounded-pill px-4";
        btn.onclick = () => confirmAction(`Shop ${shopName} restored successfully.`);
    }

    new bootstrap.Modal(document.getElementById('confirmSellerModal')).show();
}

function confirmAction(toastMsg) {
    const confirmEl = document.getElementById('confirmSellerModal');
    const editEl = document.getElementById('editSellerModal');

    const confirmMod = bootstrap.Modal.getInstance(confirmEl);
    const editMod = bootstrap.Modal.getInstance(editEl);

    if (confirmMod) confirmMod.hide();
    if (editMod) editMod.hide();

    triggerSellerToast(toastMsg || "Action confirmed!");
}