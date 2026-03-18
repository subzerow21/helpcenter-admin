document.addEventListener('DOMContentLoaded', () => {
    const ctx = document.getElementById('revenueShareChart').getContext('2d');

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Top 3 Sellers', 'Others'],
            datasets: [{
                data: [65, 35], // Update these values as needed
                backgroundColor: ['#212529', '#dee2e6'],
                hoverOffset: 0,
                borderWidth: 0,
            }]
        },
        options: {
            cutout: '88%', // Creates a very clean, thin ring
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (item) {
                            return ` ${item.label}: ${item.raw}%`;
                        }
                    }
                }
            }
        }
    });
});


document.addEventListener("DOMContentLoaded", function () {
    // Get the hash from the URL (e.g., #tab-products)
    var hash = window.location.hash;

    if (hash) {
        // Find the tab button that matches the target ID in the hash
        var targetTab = document.querySelector('[data-bs-target="' + hash + '"]');

        if (targetTab) {
            // Use Bootstrap's Tab constructor to show it
            var tab = new bootstrap.Tab(targetTab);
            tab.show();

            // Optional: Scroll to the top of the tabs so the user sees the switch
            targetTab.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
});

function viewSellerDetails(name, shop, revenue, orders) {
    document.getElementById('detSellerName').innerText = name;
    document.getElementById('detShopName').innerText = shop;
    document.getElementById('detRevenue').innerText = revenue;
    document.getElementById('detOrders').innerText = orders;
    new bootstrap.Modal(document.getElementById('sellerDetailModal')).show();
}