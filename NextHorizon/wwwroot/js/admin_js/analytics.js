document.addEventListener('DOMContentLoaded', function () {
    // Initialize all tooltips on the page
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })
});


document.addEventListener('DOMContentLoaded', function () {
    // 1. Get the data from the hidden inputs we created in the View
    const labelsElement = document.getElementById('jsonLabels');
    const kmElement = document.getElementById('jsonKm');
    const revElement = document.getElementById('jsonRev');

    // 2. Safety check: Only run if the elements exist on the current page
    if (!labelsElement || !kmElement || !revElement) return;

    const labels = JSON.parse(labelsElement.value);
    const kmData = JSON.parse(kmElement.value);
    const revData = JSON.parse(revElement.value);

    // 3. Initialize the Chart
    const ctx = document.getElementById('activitySalesChart').getContext('2d');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Activity (KM)',
                    data: kmData,
                    borderColor: '#212529', // Dark Gray
                    backgroundColor: 'rgba(33, 37, 41, 0.05)',
                    fill: true,
                    tension: 0.4,
                    yAxisID: 'y',
                    pointRadius: 4,
                    pointBackgroundColor: '#212529'
                },
                {
                    label: 'Revenue (₱)',
                    data: revData,
                    borderColor: '#0d6efd', // Bootstrap Blue
                    backgroundColor: 'transparent',
                    fill: false,
                    tension: 0.4,
                    yAxisID: 'y1',
                    pointRadius: 4,
                    pointBackgroundColor: '#0d6efd'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: { display: true, text: 'Distance (KM)', font: { weight: 'bold' } },
                    grid: { drawOnChartArea: true }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    title: { display: true, text: 'Revenue (₱)', font: { weight: 'bold' } },
                    // This prevents the grid lines from overlapping and looking messy
                    grid: { drawOnChartArea: false }
                }
            },
            plugins: {
                legend: {
                    position: 'top',
                    align: 'end',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                },
                tooltip: {
                    padding: 12,
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleFont: { size: 14 },
                    bodyFont: { size: 13 },
                    cornerRadius: 8
                }
            }
        }
    });
});

document.addEventListener('DOMContentLoaded', function () {
    // Get raw strings from hidden inputs
    const rawLabels = document.getElementById('jsonLabels').value;
    const rawKm = document.getElementById('jsonKm').value;
    const rawRev = document.getElementById('jsonRev').value;

    // Parse strings into JS Objects
    const labels = JSON.parse(rawLabels);
    const kmData = JSON.parse(rawKm);
    const revData = JSON.parse(rawRev);

    const ctx = document.getElementById('activitySalesChart').getContext('2d');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Activity (KM)',
                    data: kmData,
                    borderColor: '#212529',
                    backgroundColor: 'rgba(33, 37, 41, 0.05)',
                    fill: true,
                    tension: 0.4,
                    yAxisID: 'y'
                },
                {
                    label: 'Revenue (₱)',
                    data: revData,
                    borderColor: '#0d6efd',
                    backgroundColor: 'transparent',
                    fill: false,
                    tension: 0.4,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: { display: true, text: 'Distance (KM)' }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    grid: { drawOnChartArea: false },
                    title: { display: true, text: 'Revenue (₱)' }
                }
            }
        }
    });
});

document.getElementById('datePreset').addEventListener('change', function () {
    const customContainer = document.getElementById('customDateContainer');
    if (this.value === 'custom') {
        customContainer.classList.remove('d-none'); // Show inputs
    } else {
        customContainer.classList.add('d-none');    // Hide inputs
        // Here you would typically trigger an AJAX call to refresh the data
        console.log("Loading data for the last " + this.value + " days...");
    }
});

// Add this inside your existing DOMContentLoaded listener
const peakCtx = document.getElementById('peakActivityChart').getContext('2d');
new Chart(peakCtx, {
    type: 'bar',
    data: {
        labels: ['12am', '4am', '8am', '12pm', '4pm', '8pm', '11pm'],
        datasets: [{
            label: 'Active Users',
            data: [12, 5, 45, 30, 85, 120, 40], // Example data
            backgroundColor: '#212529',
            borderRadius: 5
        }]
    },
    options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            y: { beginAtZero: true, display: false },
            x: { grid: { display: false } }
        }
    }
});
