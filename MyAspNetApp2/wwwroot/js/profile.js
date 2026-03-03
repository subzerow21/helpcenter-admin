/* profile.js */

document.addEventListener("DOMContentLoaded", function () {

    const toggleBtn = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("profileSidebar");

    if (!toggleBtn || !sidebar) return;

    // Toggle sidebar
    toggleBtn.addEventListener("click", function (e) {
        e.stopPropagation(); // Prevent instant closing
        sidebar.classList.toggle("active");
        console.log("Sidebar toggled:", sidebar.className);
    });

    // Close sidebar when clicking outside (mobile only)
    document.addEventListener("click", function (event) {

        if (
            window.innerWidth <= 768 &&
            sidebar.classList.contains("active") &&
            !sidebar.contains(event.target) &&
            !toggleBtn.contains(event.target)
        ) {
            sidebar.classList.remove("active");
        }

    });

});