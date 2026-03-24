// Get challenge ID from URL
const urlParams = new URLSearchParams(window.location.search);
const challengeId = window.location.pathname.split('/').pop();

// Load challenge details on page load
document.addEventListener('DOMContentLoaded', function() {
    loadChallengeDetails();
    setupSearch();
});

async function loadChallengeDetails() {
    try {
        const response = await fetch(`/Admin/GetChallengeDetails?id=${challengeId}`);
        const data = await response.json();
        
        if (!data.success) {
            showToast('Error loading challenge details', true);
            return;
        }
        
        const challenge = data.challenge;
        const leaderboard = data.leaderboard;
        
        // Update header
        document.getElementById('challengeTitleBreadcrumb').innerText = challenge.title;
        document.getElementById('challengeTitle').innerText = challenge.title;
        document.getElementById('challengeBanner').src = challenge.bannerBase64 || '/images/challenge-placeholder.jpg';
        
        // Status badge
        const statusBadge = document.getElementById('challengeStatusBadge');
        statusBadge.innerText = challenge.status;
        if (challenge.status === 'Live') statusBadge.className = 'badge bg-success rounded-pill';
        else if (challenge.status === 'Upcoming') statusBadge.className = 'badge bg-warning rounded-pill';
        else if (challenge.status === 'Completed') statusBadge.className = 'badge bg-secondary rounded-pill';
        else statusBadge.className = 'badge bg-danger rounded-pill';
        
        document.getElementById('challengeDates').innerText = `${formatDate(challenge.startDate)} - ${formatDate(challenge.endDate)}`;
        
        // Stats
        document.getElementById('totalParticipants').innerText = challenge.totalParticipants || 0;
        document.getElementById('totalCompleted').innerText = challenge.totalCompleted || 0;
        document.getElementById('avgDistance').innerText = (data.avgDistanceKm || 0).toFixed(1) + ' km';
        document.getElementById('avgTime').innerText = (data.avgTimeMinutes || 0).toFixed(0) + ' min';
        
        // Leaderboard
        const tbody = document.getElementById('leaderboardBody');
        if (!leaderboard || leaderboard.length === 0) {
            tbody.innerHTML = '}<td colspan="7" class="text-center py-4">No participants yet</td></tr>';
            return;
        }
        
        tbody.innerHTML = '';
        leaderboard.forEach(participant => {
            const progressPercent = (participant.totalDistanceKm / challenge.goalKm * 100).toFixed(1);
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="fw-bold">#${participant.rank}</td>
                <td class="text-start">
                    <div class="d-flex align-items-center gap-2">
                        <img src="${participant.avatarUrl}" class="rounded-circle" width="32" height="32">
                        <div>
                            <div class="fw-bold mb-0">${escapeHtml(participant.athleteName)}</div>
                            <small class="text-muted">${escapeHtml(participant.email)}</small>
                        </div>
                    </div>
                </td>
                <td style="width: 180px;">
                    <div class="d-flex align-items-center gap-2">
                        <div class="progress flex-grow-1" style="height: 6px;">
                            <div class="progress-bar bg-success" style="width: ${progressPercent}%"></div>
                        </div>
                        <small class="fw-bold">${progressPercent}%</small>
                    </div>
                </td>
                <td>${participant.totalDistanceKm.toFixed(1)} km</td>
                <td>${participant.totalActivities}</td>
                <td>${participant.totalTimeFormatted}</td>
                <td>
                    <button class="btn btn-sm btn-light border rounded-pill px-3"
                            onclick="viewActivityLog(${participant.participantId}, '${escapeHtml(participant.athleteName)}')">
                        View Logs
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
        
    } catch (error) {
        console.error('Error loading challenge details:', error);
        showToast('Error loading challenge details', true);
    }
}

async function viewActivityLog(participantId, athleteName) {
    try {
        const response = await fetch(`/Admin/GetParticipantActivities?participantId=${participantId}`);
        const data = await response.json();
        
        if (!data.success) {
            showToast('Error loading activities', true);
            return;
        }
        
        document.getElementById('detailAthleteName').innerText = athleteName;
        
        const container = document.getElementById('activityLogList');
        
        if (!data.activities || data.activities.length === 0) {
            container.innerHTML = '<div class="text-center py-4">No activities logged</div>';
        } else {
            container.innerHTML = '';
            data.activities.forEach(activity => {
                const activityDiv = document.createElement('div');
                activityDiv.className = 'list-group-item p-3 border-0 border-bottom text-start';
                activityDiv.innerHTML = `
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="fw-bold small text-dark">${activity.activityType}</span>
                        <span class="badge bg-light text-dark border">${activity.distanceKm.toFixed(1)} km</span>
                    </div>
                    <div class="d-flex justify-content-between small text-muted">
                        <span>${formatDateTime(activity.activityDate)}</span>
                        <span>${activity.durationFormatted}</span>
                        <span class="${activity.isVerified ? 'text-success' : 'text-warning'}">
                            <i class="bi ${activity.isVerified ? 'bi-check-circle-fill' : 'bi-clock-history'} me-1"></i>
                            ${activity.isVerified ? 'Verified' : 'Pending'}
                        </span>
                    </div>
                    ${activity.notes ? `<div class="small text-muted mt-1">${escapeHtml(activity.notes)}</div>` : ''}
                    ${!activity.isVerified ? `<button class="btn btn-sm btn-outline-success rounded-pill mt-2" onclick="verifyActivity(${activity.activityId})">Verify Activity</button>` : ''}
                `;
                container.appendChild(activityDiv);
            });
        }
        
        new bootstrap.Modal(document.getElementById('activityDetailsModal')).show();
    } catch (error) {
        console.error('Error loading activities:', error);
        showToast('Error loading activities', true);
    }
}

async function verifyActivity(activityId) {
    try {
        const response = await fetch('/Admin/VerifyActivity', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ activityId: activityId })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast('Activity verified successfully');
            // Reload the activity log
            const modal = bootstrap.Modal.getInstance(document.getElementById('activityDetailsModal'));
            modal.hide();
            // Refresh leaderboard
            loadChallengeDetails();
        } else {
            showToast(data.message || 'Error verifying activity', true);
        }
    } catch (error) {
        showToast('Error verifying activity', true);
    }
}

function openEditChallengeModalFromDetails() {
    // Fetch challenge details and open edit modal
    fetch(`/Admin/GetChallengeDetails?id=${challengeId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const challenge = data.challenge;
                openEditChallengeModal(
                    challenge.challengeId,
                    challenge.title,
                    challenge.description,
                    challenge.rules,
                    challenge.prizes,
                    challenge.goalKm,
                    challenge.activityType,
                    challenge.startDate,
                    challenge.endDate,
                    challenge.status,
                    challenge.bannerBase64
                );
            }
        });
}

function exportParticipants() {
    const table = document.querySelector(".table");
    const rows = table.querySelectorAll("tr");
    let csvContent = "";
    
    rows.forEach((row, index) => {
        const cells = row.querySelectorAll("th, td");
        let rowData = [];
        
        cells.forEach((cell, cellIndex) => {
            if (cellIndex === cells.length - 1) return;
            let data = cell.innerText.replace(/(\r\n|\n|\r)/gm, " ").trim();
            data = data.replace(/"/g, '""');
            if (data.includes(",")) data = `"${data}"`;
            rowData.push(data);
        });
        
        csvContent += rowData.join(",") + "\n";
    });
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    
    const challengeTitle = document.getElementById('challengeTitle').innerText.replace(/\s+/g, '_');
    const date = new Date().toISOString().split('T')[0];
    
    link.setAttribute("href", url);
    link.setAttribute("download", `${challengeTitle}_Stats_${date}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

function setupSearch() {
    document.getElementById('leaderboardSearch')?.addEventListener('keyup', function(e) {
        const term = e.target.value.toLowerCase();
        const rows = document.querySelectorAll("#leaderboardBody tr");
        rows.forEach(row => {
            const athleteName = row.querySelector('td:nth-child(2) .fw-bold')?.innerText.toLowerCase() || "";
            row.style.display = athleteName.includes(term) ? "" : "none";
        });
    });
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('en-PH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showToast(message, isError = false) {
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');
    const toastEl = document.getElementById('challengeToast');
    
    toastMsg.innerText = message;
    toastIcon.className = isError ? 'bi bi-exclamation-triangle-fill text-danger fs-5' : 'bi bi-check-circle-fill text-success fs-5';
    
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
}