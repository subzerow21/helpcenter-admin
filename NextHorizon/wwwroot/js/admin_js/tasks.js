// Global variables
let currentChallengeId = null;
let isEditMode = false;
let prizeTierCount = 0;

// Load data on page load
document.addEventListener('DOMContentLoaded', function() {
    loadChallengeStatistics();
    loadAllChallenges();
    loadLeaderboard();
    loadChallengeFilters();

    // Search functionality
    const searchInput = document.getElementById('leaderboardSearch');
    if (searchInput) {
        searchInput.addEventListener('keyup', filterLeaderboardBySearch);
    }
});

// Load challenge statistics
async function loadChallengeStatistics() {
    const totalAthletes = document.getElementById('totalAthletes');
    const activeChallenges = document.getElementById('activeChallenges');
    const avgDistance = document.getElementById('avgDistance');
    const totalTime = document.getElementById('totalTime');
    
    if (!totalAthletes || !activeChallenges || !avgDistance || !totalTime) return;
    
    try {
        const response = await fetch('/Admin/GetChallengeStatistics');
        const stats = await response.json();
        
        totalAthletes.innerText = (stats.totalAthletes || 0).toLocaleString();
        activeChallenges.innerText = stats.activeChallenges || '0';
        avgDistance.innerText = (stats.avgDistance?.toFixed(1) || '0') + ' km';
        totalTime.innerText = Math.round(stats.totalTimeHours || 0) + ' h';
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

// Load all challenges
async function loadAllChallenges() {
    const container = document.getElementById('challengesList');
    if (!container) return;
    
    try {
        const response = await fetch('/Admin/GetAllChallenges');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const challenges = await response.json();
        
        if (!Array.isArray(challenges) || challenges.length === 0) {
            container.innerHTML = '<div class="col-12 text-center py-5">No challenges found</div>';
            return;
        }
        
        container.innerHTML = '';
        challenges.forEach(challenge => {
            const statusClass = challenge.status === 'Live' ? 'bg-success' : 
                               (challenge.status === 'Upcoming' ? 'bg-warning' : 'bg-secondary');
            
            // Handle null/undefined bannerBase64
            const bannerImage = challenge.bannerBase64 && challenge.bannerBase64 !== 'null' ? 
                               challenge.bannerBase64 : '/images/challenge-placeholder.jpg';
            
            const card = document.createElement('div');
            card.className = 'col-md-6 col-lg-4';
            card.innerHTML = `
                <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 challenge-card-hover"
                     onclick="window.location.href='/Admin/ChallengeDetails/${challenge.challengeId}'" style="cursor:pointer;">
                    <div class="position-relative">
                        <img src="${bannerImage}" 
                             class="card-img-top" style="height: 150px; object-fit: cover;" 
                             onerror="this.src='/images/challenge-placeholder.jpg'">
                        <span class="position-absolute top-0 end-0 m-2 badge ${statusClass} shadow-sm">${challenge.status}</span>
                    </div>
                    <div class="p-3">
                        <h6 class="fw-bold mb-0 text-dark">${escapeHtml(challenge.title)}</h6>
                        <p class="text-muted mb-2" style="font-size: 0.75rem;">${formatDate(challenge.startDate)} - ${formatDate(challenge.endDate)}</p>
                        <div class="progress mb-2" style="height: 4px;">
                            <div class="progress-bar bg-success" style="width: ${challenge.completionRate || 0}%"></div>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-2">
                            <span class="small text-primary fw-bold">View Details <i class="bi bi-arrow-right"></i></span>
                            <span class="text-muted small"><i class="bi bi-people"></i> ${challenge.totalParticipants || 0}</span>
                        </div>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    } catch (error) {
        console.error('Error loading challenges:', error);
        container.innerHTML = '<div class="col-12 text-center py-5 text-danger">Error loading challenges</div>';
        showToast('Error loading challenges: ' + error.message, true);
    }
}

// Global variables
let currentLeaderboardData = [];
let selectedChallengeId = null;

// Load leaderboard (global or filtered by challenge)
async function loadLeaderboard() {
    const leaderboardBody = document.getElementById('leaderboardBody');
    if (!leaderboardBody) return;
    
    try {
        let url = '/Admin/GetGlobalLeaderboard';
        if (selectedChallengeId && selectedChallengeId !== 'all') {
            url += `?challengeId=${selectedChallengeId}`;
        }
        
        console.log('Fetching leaderboard from:', url);
        const response = await fetch(url);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        // Check for error response
        if (data.error) {
            throw new Error(data.error);
        }
        
        currentLeaderboardData = data;
        
        if (!Array.isArray(data) || data.length === 0) {
            leaderboardBody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No participants yet</td></tr>';
            return;
        }
        
        leaderboardBody.innerHTML = '';
        
        data.forEach(participant => {
            const progressPercent = participant.progressPercent || 0;
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="fw-bold">#${participant.globalRank}</td>
                <td class="text-start ps-4">
                    <div class="d-flex align-items-center gap-2" style="cursor:pointer" onclick="openUserView('${escapeHtml(participant.athleteName)}', ${participant.globalRank}, ${participant.totalDistanceKm}, ${participant.totalActivities}, '${participant.totalTimeFormatted}', '${participant.avatarUrl}')">
                        <img src="${participant.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(participant.athleteName)}" class="rounded-circle border" width="30" height="30" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(participant.athleteName)}'">
                        <span class="fw-bold text-dark">${escapeHtml(participant.athleteName)}</span>
                    </div>
                </td>
                <td class="text-start">
                    <div>
                        <span class="fw-bold small d-block">${escapeHtml(participant.challengeTitle)}</span>
                        <span class="badge bg-light text-dark border">${escapeHtml(participant.activityType)}</span>
                    </div>
                </td>
                <td><strong>${participant.totalDistanceKm.toFixed(1)} km</strong><br><small class="text-muted">/ ${participant.challengeGoalKm} km</small></td>
                <td>${participant.totalActivities}</td>
                <td class="fw-bold">${participant.totalTimeFormatted}</td>
                <td style="width: 120px;">
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar bg-success" style="width: ${progressPercent}%"></div>
                    </div>
                    <small class="text-muted">${progressPercent.toFixed(1)}%</small>
                </td>
            `;
            leaderboardBody.appendChild(row);
        });
        
    } catch (error) {
        console.error('Error loading leaderboard:', error);
        const leaderboardBody = document.getElementById('leaderboardBody');
        if (leaderboardBody) {
            leaderboardBody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-danger">Error loading leaderboard: ' + error.message + '</td></tr>';
        }
        showToast('Error loading leaderboard: ' + error.message, true);
    }
}

// Load challenges for filter dropdown
async function loadChallengeFilters() {
    try {
        const response = await fetch('/Admin/GetActiveChallenges');
        const challenges = await response.json();
        
        const filterSelect = document.getElementById('challengeFilter');
        if (!filterSelect) return;
        
        // Clear existing options except the "All Challenges" option
        while (filterSelect.options.length > 1) {
            filterSelect.remove(1);
        }
        
        // Add challenges to dropdown
        challenges.forEach(challenge => {
            const option = document.createElement('option');
            option.value = challenge.challengeId;
            option.textContent = `${challenge.title} (${challenge.activityType})`;
            filterSelect.appendChild(option);
        });
        
    } catch (error) {
        console.error('Error loading challenge filters:', error);
    }
}

// Filter leaderboard by challenge
function filterLeaderboardByChallenge() {
    const filterSelect = document.getElementById('challengeFilter');
    if (filterSelect) {
        selectedChallengeId = filterSelect.value;
        loadLeaderboard();
    }
}

// Filter leaderboard by search term (for athlete name)
function filterLeaderboardBySearch() {
    const searchTerm = document.getElementById('leaderboardSearch')?.value.toLowerCase() || '';
    const rows = document.querySelectorAll('#leaderboardBody tr');
    
    rows.forEach(row => {
        const athleteName = row.querySelector('td:nth-child(2) .fw-bold')?.innerText.toLowerCase() || '';
        row.style.display = athleteName.includes(searchTerm) ? '' : 'none';
    });
}

// Tab switching
function switchChallengeTab(viewName) {
    const leaderboardView = document.getElementById('view-leaderboard');
    const tasksView = document.getElementById('view-active-tasks');
    const btnLead = document.getElementById('tab-leaderboard');
    const btnTasks = document.getElementById('tab-active-tasks');

    if (viewName === 'leaderboard') {
        if (leaderboardView) leaderboardView.classList.remove('d-none');
        if (tasksView) tasksView.classList.add('d-none');
        if (btnLead) btnLead.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        if (btnTasks) btnTasks.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
        loadLeaderboard();
    } else {
        if (leaderboardView) leaderboardView.classList.add('d-none');
        if (tasksView) tasksView.classList.remove('d-none');
        if (btnTasks) btnTasks.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        if (btnLead) btnLead.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
        loadAllChallenges();
    }
}

// Prize Management Functions
function togglePrizeManagement(hasPrizes) {
    const prizeSection = document.getElementById('prizeManagementSection');
    if (hasPrizes === 'yes') {
        if (prizeSection) prizeSection.style.display = 'block';
    } else {
        if (prizeSection) prizeSection.style.display = 'none';
        const container = document.getElementById('prizeTiersContainer');
        if (container) container.innerHTML = '';
        prizeTierCount = 0;
    }
}

function addPrizeTier() {
    prizeTierCount++;
    const template = document.getElementById('prizeTypeTemplate');
    if (!template) return;
    
    const clone = template.cloneNode(true);
    clone.removeAttribute('id');
    clone.style.display = 'block';
    
    const tierSpan = clone.querySelector('.tier-number');
    if (tierSpan) tierSpan.textContent = prizeTierCount;
    
    const container = document.getElementById('prizeTiersContainer');
    if (container) container.appendChild(clone);
}

function removePrizeTier(button) {
    const card = button.closest('.prize-tier-card');
    if (card) card.remove();
    
    // Re-number remaining tiers
    const tiers = document.querySelectorAll('.prize-tier-card');
    tiers.forEach((tier, index) => {
        const tierSpan = tier.querySelector('.tier-number');
        if (tierSpan) tierSpan.textContent = index + 1;
    });
    prizeTierCount = tiers.length;
}

function togglePrizeFields(select) {
    const card = select.closest('.prize-tier-card');
    if (!card) return;
    
    const cashFields = card.querySelector('.cash-fields');
    const voucherFields = card.querySelector('.voucher-fields');
    const rewardFields = card.querySelector('.reward-fields');
    const prizeTypeId = card.querySelector('.prize-type-id');
    const voucherType = card.querySelector('.voucher-type');
    
    // Hide all first
    if (cashFields) cashFields.style.display = 'none';
    if (voucherFields) voucherFields.style.display = 'none';
    if (rewardFields) rewardFields.style.display = 'none';
    
    // Show based on selection
    switch(select.value) {
        case 'cash':
            if (cashFields) cashFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '1';
            break;
        case 'shipping_voucher':
            if (voucherFields) voucherFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '2';
            if (voucherType) voucherType.value = 'SHIPPING';
            break;
        case 'product_voucher':
            if (voucherFields) voucherFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '3';
            if (voucherType) voucherType.value = 'PRODUCT';
            break;
        case 'reward':
            if (rewardFields) rewardFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '4';
            break;
    }
}

function getPrizeData() {
    const prizes = [];
    const prizeCards = document.querySelectorAll('.prize-tier-card');
    
    prizeCards.forEach((card, index) => {
        const prizeTypeSelect = card.querySelector('.prize-type-select');
        const prizeType = prizeTypeSelect ? prizeTypeSelect.value : '';
        
        if (!prizeType) return;
        
        const prize = {
            tier: index + 1,
            tierName: card.querySelector('.tier-name')?.value || '',
            description: card.querySelector('.prize-description')?.value || '',
            prizeTypeId: parseInt(card.querySelector('.prize-type-id')?.value || 0),
            quantity: 1
        };
        
        if (prizeType === 'cash') {
            prize.cashAmount = parseFloat(card.querySelector('.cash-amount')?.value) || 0;
        } else if (prizeType === 'shipping_voucher' || prizeType === 'product_voucher') {
            prize.voucherDiscountPercent = parseFloat(card.querySelector('.voucher-percent')?.value) || null;
            prize.voucherDiscountFixed = parseFloat(card.querySelector('.voucher-fixed')?.value) || null;
            prize.voucherMinimumPurchase = parseFloat(card.querySelector('.voucher-min-purchase')?.value) || null;
            prize.voucherType = card.querySelector('.voucher-type')?.value || '';
        } else if (prizeType === 'reward') {
            prize.rewardName = card.querySelector('.reward-name')?.value || '';
            prize.rewardValue = parseFloat(card.querySelector('.reward-value')?.value) || 0;
        }
        
        prizes.push(prize);
    });
    
    return prizes;
}

// Handle challenge submit
async function handleChallengeSubmit(event) {
    event.preventDefault();
    
    const title = document.getElementById('challengeTitle')?.value;
    const description = document.getElementById('challengeDesc')?.value;
    const rules = document.getElementById('challengeRules')?.value;
    const prizes = document.getElementById('challengePrizes')?.value;
    const goalKm = parseFloat(document.getElementById('goalKm')?.value);
    const activityType = document.getElementById('activityType')?.value;
    const startDate = document.getElementById('startDate')?.value;
    const endDate = document.getElementById('endDate')?.value;
    const status = document.getElementById('challengeStatus')?.value;
    const hasPrizesRadio = document.querySelector('input[name="hasPrizes"]:checked');
    const hasPrizes = hasPrizesRadio ? hasPrizesRadio.value : 'no';
    let bannerBase64 = null;
    let bannerImageName = null;
    let bannerImageContentType = null;
    
    const fileInput = document.getElementById('challengeImgInput');
    
    // Validate required fields
    if (!title || !goalKm || !activityType || !startDate || !endDate) {
        showToast('Please fill all required fields', true);
        return;
    }
    
    // Validate goalKm
    if (isNaN(goalKm) || goalKm <= 0) {
        showToast('Please enter a valid goal distance (greater than 0)', true);
        return;
    }
    
    // Get prize data if has prizes
    let prizeData = [];
    if (hasPrizes === 'yes') {
        prizeData = getPrizeData();
        if (prizeData.length === 0) {
            showToast('Please add at least one prize tier', true);
            return;
        }
    }
    
    // Date validation
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0); 
    
    const start = new Date(startDate);
    start.setHours(0, 0, 0, 0);
    
    const end = new Date(endDate);
    end.setHours(0, 0, 0, 0);
    
    if (start < currentDate) {
        showToast('Start date cannot be before current date', true);
        return;
    }
    
    if (end < start) {
        showToast('End date cannot be before start date', true);
        return;
    }
    
    if (end < currentDate) {
        showToast('End date cannot be before current date', true);
        return;
    }
    
    // Process image
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        const file = fileInput.files[0];
        
        if (!file.type.startsWith('image/')) {
            showToast('Please select an image file', true);
            return;
        }
        
        if (file.size > 5 * 1024 * 1024) {
            showToast('Image size must be less than 5MB', true);
            return;
        }
        
        try {
            bannerImageName = file.name;
            bannerImageContentType = file.type;
            
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
    const confirmTitle = document.getElementById('confirmTitle');
    const confirmMessage = document.getElementById('confirmMessage');
    
    if (confirmTitle) confirmTitle.innerText = isEditMode ? 'Save Changes?' : 'Launch Challenge?';
    if (confirmMessage) confirmMessage.innerText = isEditMode ? 
        'Updates will be visible to all participants immediately.' : 
        'This will notify all athletes and make the challenge live.';
    
    const confirmBtn = document.getElementById('confirmExecuteBtn');
    if (confirmBtn) {
        confirmBtn.onclick = async () => {
            confirmModal.hide();
            
            const url = isEditMode ? '/Admin/UpdateChallenge' : '/Admin/CreateChallenge';
            const body = {
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
                bannerImageContentType: bannerImageContentType,
                prizesData: prizeData
            };
            
            if (!isEditMode) delete body.challengeId;
            
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                
                const data = await response.json();
                
                if (data.success) {
                    showToast(data.message);
                    const modal = bootstrap.Modal.getInstance(document.getElementById('launchChallengeModal'));
                    if (modal) modal.hide();
                    
                    if (isEditMode && window.location.pathname.includes('ChallengeDetails')) {
                        window.location.reload();
                    } else {
                        loadAllChallenges();
                        loadLeaderboard();
                        loadChallengeStatistics();
                    }
                    
                    // Reset form
                    const form = document.getElementById('launchForm');
                    if (form) form.reset();
                    
                    const preview = document.getElementById('imagePreview');
                    const placeholder = document.getElementById('uploadPlaceholder');
                    if (preview) {
                        preview.classList.add('d-none');
                        preview.src = '#';
                    }
                    if (placeholder) placeholder.classList.remove('d-none');
                    if (fileInput) fileInput.value = '';
                    
                    // Reset prize section
                    const container = document.getElementById('prizeTiersContainer');
                    if (container) container.innerHTML = '';
                    prizeTierCount = 0;
                    const hasPrizesYes = document.getElementById('hasPrizesYes');
                    if (hasPrizesYes) hasPrizesYes.checked = false;
                    togglePrizeManagement('no');
                } else {
                    showToast(data.message || 'Error saving challenge', true);
                }
            } catch (error) {
                console.error('Error saving challenge:', error);
                showToast('Error saving challenge', true);
            }
        };
    }
    
    confirmModal.show();
}

// Open user view modal
function openUserView(name, rank, distance, activities, time, avatarUrl) {
    const userNameView = document.getElementById('userNameView');
    const userRankView = document.getElementById('userRankView');
    const userDistView = document.getElementById('userDistView');
    const userActView = document.getElementById('userActView');
    const userTimeView = document.getElementById('userTimeView');
    const userModalPic = document.getElementById('userModalPic');
    
    if (userNameView) userNameView.innerText = '@' + name;
    if (userRankView) userRankView.innerText = 'Ranked #' + rank + ' Global';
    if (userDistView) userDistView.innerText = (distance || 0).toFixed(1) + ' km';
    if (userActView) userActView.innerText = activities || 0;
    if (userTimeView) userTimeView.innerText = time || '00:00:00';
    if (userModalPic) userModalPic.src = avatarUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=random`;
    
    const modal = document.getElementById('userViewModal');
    if (modal) new bootstrap.Modal(modal).show();
}

// Helper functions
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatTime(seconds) {
    if (!seconds || seconds <= 0) return '00:00:00';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
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
    
    if (!toastMsg || !toastEl) {
        if (isError) alert('Error: ' + message);
        else console.log('Toast: ' + message);
        return;
    }
    
    toastMsg.innerText = message;
    toastIcon.className = isError ? 'bi bi-exclamation-triangle-fill text-danger fs-5' : 'bi bi-check-circle-fill text-success fs-5';
    
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
}