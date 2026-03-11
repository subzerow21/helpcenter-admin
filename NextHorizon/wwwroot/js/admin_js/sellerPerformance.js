document.addEventListener('DOMContentLoaded', () => {
    // 1. Initialize Chart
    const chartCanvas = document.getElementById('revenueShareChart');
    if (chartCanvas) {
        const ctxPie = chartCanvas.getContext('2d');
        new Chart(ctxPie, {
            type: 'doughnut',
            data: {
                labels: ['Top 3 Sellers', 'Other Active', 'New Sellers'],
                datasets: [{
                    data: [65, 25, 10],
                    backgroundColor: ['#212529', '#6c757d', '#dee2e6'],
                    borderWidth: 0
                }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12, font: { size: 11 } } }
                }
            }
        });
    }
});

// 2. View Details Logic
function viewSellerDetails(name, shop, revenue, orders) {
    document.getElementById('detSellerName').innerText = name;
    document.getElementById('detShopName').innerText = shop;
    document.getElementById('detRevenue').innerText = revenue;
    document.getElementById('detOrders').innerText = orders;

    // Trigger Modal
    const modalEl = document.getElementById('sellerDetailModal');
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
}

// 3. Filtering Logic
function filterSellers(criteria) {
    const rows = document.querySelectorAll('#performanceTable tbody tr');

    rows.forEach(row => {
        row.style.display = ""; // Reset visibility

        const rank = parseInt(row.getAttribute('data-rank'));
        const growth = row.getAttribute('data-growth');

        if (criteria === 'top') {
            if (rank > 3) row.style.display = "none";
        }
        else if (criteria === 'growth') {
            if (growth !== 'positive') row.style.display = "none";
        }
    });

    // Close Modal safely
    const filterModEl = document.getElementById('filterModal');
    const filterModal = bootstrap.Modal.getInstance(filterModEl);
    if (filterModal) filterModal.hide();
}