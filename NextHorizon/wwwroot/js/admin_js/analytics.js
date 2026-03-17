document.addEventListener('DOMContentLoaded', function () {

    // --- 1. BOOTSTRAP TOOLTIPS ---
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // --- 2. DATE PRESET SELECTOR ---
    const datePreset = document.getElementById('datePreset');
    const customContainer = document.getElementById('customDateContainer');

    if (datePreset) {
        datePreset.addEventListener('change', function () {
            if (this.value === 'custom') {
                customContainer.classList.remove('d-none');
            } else {
                customContainer.classList.add('d-none');
                console.log("Fetching data for preset: " + this.value + " days");
            }
        });
    }

    // --- 3. CHALLENGE vs REVENUE CHART ---
    const labelsElement = document.getElementById('jsonLabels');
    const participantsElement = document.getElementById('jsonParticipants');
    const revElement = document.getElementById('jsonRevenue');
    const mainCanvas = document.getElementById('mainAnalyticsChart');

    if (labelsElement && participantsElement && revElement && mainCanvas) {
        const labels = JSON.parse(labelsElement.value);
        const participantData = JSON.parse(participantsElement.value);
        const revenueData = JSON.parse(revElement.value);

        const ctx = mainCanvas.getContext('2d');
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Challengers Joined',
                        data: participantData,
                        borderColor: '#212529',
                        backgroundColor: '#212529', 
                        fill: false,
                        tension: 0.4,
                        yAxisID: 'y',
                        pointRadius: 4,
                        pointBackgroundColor: '#212529'
                    },
                    {
                        label: 'Revenue (₱)',
                        data: revenueData,
                        borderColor: '#0d6efd',
                        backgroundColor: '#0d6efd',
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
                scales: {
                    y: { type: 'linear', position: 'left', title: { display: true, text: 'Consumers' } },
                    y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'Revenue (₱)' } }
                },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: {
                            usePointStyle: true, 
                            pointStyle: 'circle',
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    // --- 4. PEAK ENGAGEMENT CHART  ---
    const peakCanvas = document.getElementById('peakActivityChart');
    const peakHoursElement = document.getElementById('peakHours');
    const peakSyncsElement = document.getElementById('peakSyncs');
    const peakPurchasesElement = document.getElementById('peakPurchases');

    if (peakCanvas && peakHoursElement) {
        // Logic to convert 0-23 into 12AM-11PM
        const peakLabels = JSON.parse(peakHoursElement.value).map(h => {
            const ampm = h >= 12 ? 'PM' : 'AM';
            const hour = h % 12 || 12; // Converts 0 to 12 and 13 to 1
            return hour + ampm;
        });

        const peakSyncs = JSON.parse(peakSyncsElement.value);
        const peakPurchases = JSON.parse(peakPurchasesElement.value);

        new Chart(peakCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: peakLabels,
                datasets: [
                    {
                        label: 'App Activity',
                        data: peakSyncs,
                        backgroundColor: '#212529',
                        borderRadius: 5
                    },
                    {
                        label: 'Purchases',
                        data: peakPurchases,
                        backgroundColor: '#0d6efd',
                        borderRadius: 5
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: { usePointStyle: true, padding: 15 }
                    }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: {
                        beginAtZero: true,
                        ticks: { stepSize: 50 } // Adjust step size based on your data volume
                    }
                }
            }
        });
    }
});