document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("sidebar");
    const toggleButton = document.getElementById("sidebarToggle");
  
    toggleButton.addEventListener("click", () => {
      const isOpen = sidebar.classList.contains("open");
      if (isOpen) {
        sidebar.classList.remove("open");
        sidebar.style.left = "-250px";
        toggleButton.style.left = "10px";
      } else {
        sidebar.classList.add("open");
        sidebar.style.left = "0";
        toggleButton.style.left = "260px";
      }
    });
  });
  