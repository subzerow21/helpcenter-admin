document.addEventListener("DOMContentLoaded", function () {
    const nameRow = document.getElementById('name-row');
    const modal = document.getElementById('nameModal');
    const closeBtn = document.getElementById('closeModal');

    if (nameRow && modal && closeBtn) {
        // Open modal
        nameRow.addEventListener('click', function () {
            modal.style.display = 'flex';
        });

        // Close modal via X
        closeBtn.addEventListener('click', function () {
            modal.style.display = 'none';
        });

        // Close modal by clicking background
        window.addEventListener('click', function (event) {
            if (event.target === modal) {
                modal.style.display = 'none';
            }
        });
    } else {
        console.error("Modal elements not found!");
    }
});