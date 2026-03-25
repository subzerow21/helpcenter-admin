// Global variables
let currentChallengeId = null;
let isEditMode = false;

// Load data on page load
document.addEventListener('DOMContentLoaded', function() {
    loadChallengeStatistics();
    loadAllChallenges();
    loadLeaderboard();

    // Search functionality
    document.getElementById('leaderboardSearch').addEventListener('keyup', filterLeaderboard);
});

// Load challenge statistics
async function loadChallengeStatistics() {
    try {
        const response = await fetch('/Admin/GetChallengeStatistics');
        const stats = await response.json();
        
        document.getElementById('totalAthletes').innerText = stats.totalAthletes?.toLocaleString() || '0';
        document.getElementById('activeChallenges').innerText = stats.activeChallenges || '0';
        document.getElementById('avgDistance').innerText = (stats.avgDistance?.toFixed(1) || '0') + ' km';
        document.getElementById('totalTime').innerText = (stats.totalTimeHours?.toFixed(0) || '0') + ' h';
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

// Load all challenges
async function loadAllChallenges() {
    try {
        const response = await fetch('/Admin/GetAllChallenges');
        const challenges = await response.json();
        
        const container = document.getElementById('challengesList');
        
        if (challenges.length === 0) {
            container.innerHTML = '<div class="col-12 text-center py-5">No challenges found</div>';
            return;
        }
        
        container.innerHTML = '';
        challenges.forEach(challenge => {
            const statusClass = challenge.status === 'Live' ? 'bg-success' : 
                               (challenge.status === 'Upcoming' ? 'bg-warning' : 'bg-secondary');
            
            const card = document.createElement('div');
            card.className = 'col-md-6 col-lg-4';
            card.innerHTML = `
                <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 challenge-card-hover"
                     onclick="window.location.href='/Admin/ChallengeDetails/${challenge.challengeId}'" style="cursor:pointer;">
                    <div class="position-relative">
                        <img src="${challenge.bannerBase64 || '/images/challenge-placeholder.jpg'}" 
                             class="card-img-top" style="height: 150px; object-fit: cover;" 
                             onerror="this.src='/images/placeholder.png'">
                        <span class="position-absolute top-0 end-0 m-2 badge ${statusClass} shadow-sm">${challenge.status}</span>
                    </div>
                    <div class="p-3">
                        <h6 class="fw-bold mb-0 text-dark">${escapeHtml(challenge.title)}</h6>
                        <p class="text-muted mb-2" style="font-size: 0.75rem;">${formatDate(challenge.startDate)} - ${formatDate(challenge.endDate)}</p>
                        <div class="progress mb-2" style="height: 4px;">
                            <div class="progress-bar bg-success" style="width: ${challenge.completionRate}%"></div>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-2">
                            <span class="small text-primary fw-bold">View Details <i class="bi bi-arrow-right"></i></span>
                            <span class="text-muted small"><i class="bi bi-people"></i> ${challenge.totalParticipants}</span>
                        </div>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    } catch (error) {
        console.error('Error loading challenges:', error);
    }
}

// Load leaderboard
async function loadLeaderboard() {
    try {
        const response = await fetch('/Admin/GetAllChallenges?status=Live');
        const challenges = await response.json();
        
        if (challenges.length === 0) {
            document.getElementById('leaderboardBody').innerHTML = '<td colspan="7" class="text-center py-4">No active challenges</td></tr>';
            return;
        }
        
        // Get the first live challenge for leaderboard
        const liveChallenge = challenges.find(c => c.status === 'Live');
        if (!liveChallenge) {
            document.getElementById('leaderboardBody').innerHTML = '<td colspan="7" class="text-center py-4">No active challenges</td></tr>';
            return;
        }
        
        const detailsResponse = await fetch(`/Admin/GetChallengeDetails?id=${liveChallenge.challengeId}`);
        const data = await detailsResponse.json();
        
        if (!data.success || !data.leaderboard || data.leaderboard.length === 0) {
            document.getElementById('leaderboardBody').innerHTML = '<td colspan="7" class="text-center py-4">No participants yet</td></tr>';
            return;
        }
        
        const tbody = document.getElementById('leaderboardBody');
        tbody.innerHTML = '';
        
        data.leaderboard.forEach(participant => {
            const progressPercent = participant.progressPercent || 0;
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="fw-bold">#${participant.rank}</td>
                <td class="text-start ps-4">
                    <div class="d-flex align-items-center gap-2" style="cursor:pointer" onclick="openUserView('${escapeHtml(participant.athleteName)}', ${participant.rank}, ${participant.totalDistanceKm}, ${participant.totalActivities}, '${participant.totalTimeFormatted}', '${participant.avatarUrl}')">
                        <img src="${participant.avatarUrl}" class="rounded-circle border" width="30" height="30">
                        <span class="fw-bold text-dark">${escapeHtml(participant.athleteName)}</span>
                    </div>
                </td>
                <td><span class="badge rounded-pill bg-light text-dark border">${liveChallenge.activityType}</span></td>
                <td>${participant.totalDistanceKm.toFixed(1)} km</td>
                <td>${participant.totalActivities}</td>
                <td class="fw-bold">${participant.totalTimeFormatted}</td>
                <td style="width: 120px;">
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar bg-success" style="width: ${progressPercent}%"></div>
                    </div>
                    <small class="text-muted">${progressPercent}%</small>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading leaderboard:', error);
    }
}

// Filter leaderboard
function filterLeaderboard() {
    const searchTerm = document.getElementById('leaderboardSearch').value.toLowerCase();
    const category = document.getElementById('categoryFilter').value;
    const rows = document.querySelectorAll('#leaderboardTable tbody tr');
    
    rows.forEach(row => {
        const name = row.querySelector('td:nth-child(2) .fw-bold')?.innerText.toLowerCase() || '';
        const categoryText = row.querySelector('td:nth-child(3) .badge')?.innerText || '';
        
        let show = true;
        if (searchTerm && !name.includes(searchTerm)) show = false;
        if (category !== 'All' && categoryText !== category) show = false;
        
        row.style.display = show ? '' : 'none';
    });
}

// Tab switching
function switchChallengeTab(viewName) {
    const leaderboardView = document.getElementById('view-leaderboard');
    const tasksView = document.getElementById('view-active-tasks');
    const btnLead = document.getElementById('tab-leaderboard');
    const btnTasks = document.getElementById('tab-active-tasks');

    if (viewName === 'leaderboard') {
        leaderboardView.classList.remove('d-none');
        tasksView.classList.add('d-none');
        btnLead.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        btnTasks.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
        loadLeaderboard();
    } else {
        leaderboardView.classList.add('d-none');
        tasksView.classList.remove('d-none');
        btnTasks.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        btnLead.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
        loadAllChallenges();
    }
}


// Preview challenge image


// Handle challenge submit
async function handleChallengeSubmit(event) {
    event.preventDefault();
    
    const title = document.getElementById('challengeTitle').value;
    const description = document.getElementById('challengeDesc').value;
    const rules = document.getElementById('challengeRules').value;
    const prizes = document.getElementById('challengePrizes').value;
    const goalKm = parseFloat(document.getElementById('goalKm').value);
    const activityType = document.getElementById('activityType').value;
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;
    const status = document.getElementById('challengeStatus')?.value;
    let bannerBase64 = null;
    let bannerImageName = null;
    let bannerImageContentType = null;
    
    const fileInput = document.getElementById('challengeImgInput');
    
    // Validate required fields FIRST
    if (!title || !goalKm || !activityType || !startDate || !endDate) {
        showToast('Please fill all required fields', true);
        return;
    }
    
    // Validate goalKm is a valid number
    if (isNaN(goalKm) || goalKm <= 0) {
        showToast('Please enter a valid goal distance (greater than 0)', true);
        return;
    }
    
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0); 
    
    const start = new Date(startDate);
    start.setHours(0, 0, 0, 0);
    
    const end = new Date(endDate);
    end.setHours(0, 0, 0, 0);
    
    // Check if start date is before current date
    if (start < currentDate) {
        showToast('Start date cannot be before current date', true);
        return;
    }
    
    // Check if end date is before start date
    if (end < start) {
        showToast('End date cannot be before start date', true);
        return;
    }
    
    // Check if end date is before current date (optional but recommended)
    if (end < currentDate) {
        showToast('End date cannot be before current date', true);
        return;
    }
    
    // ONLY process image if a file was actually selected
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        const file = fileInput.files[0];
        
        // Validate file type
        if (!file.type.startsWith('image/')) {
            showToast('Please select an image file', true);
            return;
        }
        
        // Validate file size (max 5MB)
        if (file.size > 5 * 1024 * 1024) {
            showToast('Image size must be less than 5MB', true);
            return;
        }
        
        try {
            bannerImageName = file.name;
            bannerImageContentType = file.type;
            
            // Convert to Base64
            const base64 = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result);
                reader.onerror = reject;
                reader.readAsDataURL(file);
            });
            
            bannerBase64 = base64;
        } catch (error) {
            console.error('Error reading file:', error);
            showToast('Error reading image file', true);
            return;
        }
    }
    
    const confirmModal = new bootstrap.Modal(document.getElementById('confirmLaunchModal'));
    document.getElementById('confirmTitle').innerText = isEditMode ? 'Save Changes?' : 'Launch Challenge?';
    document.getElementById('confirmMessage').innerText = isEditMode ? 
        'Updates will be visible to all participants immediately.' : 
        'This will notify all athletes and make the challenge live.';
    
    document.getElementById('confirmExecuteBtn').onclick = async () => {
        confirmModal.hide();
        
        const url = isEditMode ? '/Admin/UpdateChallenge' : '/Admin/CreateChallenge';
        const body = isEditMode ? {
            challengeId: currentChallengeId,
            title: title,
            description: description,
            rules: rules,
            prizes: prizes,
            goalKm: goalKm,
            activityType: activityType,
            startDate: startDate,
            endDate: endDate,
            status: status,
            bannerBase64: bannerBase64,  
            bannerImageName: bannerImageName,
            bannerImageContentType: bannerImageContentType
        } : {
            title: title,
            description: description,
            rules: rules,
            prizes: prizes,
            goalKm: goalKm,
            activityType: activityType,
            startDate: startDate,
            endDate: endDate,
            bannerBase64: bannerBase64,
            bannerImageName: bannerImageName,
            bannerImageContentType: bannerImageContentType
        };
        
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            
            const data = await response.json();
            
            if (data.success) {
                showToast(data.message);
                bootstrap.Modal.getInstance(document.getElementById('launchChallengeModal')).hide();
                
                if (isEditMode && window.location.pathname.includes('ChallengeDetails')) {
                    window.location.reload();
                } else {
                    loadAllChallenges();
                    loadLeaderboard();
                    loadChallengeStatistics();
                }
                
                // Reset form
                document.getElementById('launchForm').reset();
                const preview = document.getElementById('imagePreview');
                const placeholder = document.getElementById('uploadPlaceholder');
                preview.classList.add('d-none');
                preview.src = '#';
                placeholder.classList.remove('d-none');
                document.getElementById('challengeImgInput').value = '';
            } else {
                showToast(data.message, true);
            }
        } catch (error) {
            console.error('Error saving challenge:', error);
            showToast('Error saving challenge', true);
        }
    };
    
    confirmModal.show();
}


// Open user view modal
function openUserView(name, rank, distance, activities, time, avatarUrl) {
    document.getElementById('userNameView').innerText = '@' + name;
    document.getElementById('userRankView').innerText = 'Ranked #' + rank + ' Global';
    document.getElementById('userDistView').innerText = distance.toFixed(1) + ' km';
    document.getElementById('userActView').innerText = activities;
    document.getElementById('userTimeView').innerText = time;
    document.getElementById('userModalPic').src = avatarUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=random`;
    
    new bootstrap.Modal(document.getElementById('userViewModal')).show();
}

// Helper functions
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

