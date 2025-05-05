document.addEventListener("DOMContentLoaded", () => {
  const sidebar = document.getElementById("sidebar");
  const toggleButton = document.getElementById("sidebarToggle");
  const startButtons = document.querySelectorAll(".icon-2");

  toggleButton.addEventListener("click", () => {
    const isOpen = sidebar.classList.contains("open");
    if (isOpen) {
      sidebar.classList.remove("open");
      sidebar.style.left = "-180px";
      toggleButton.style.left = "0px";
      sidebar.style.backgroundColor = "#0D1017"; // Початковий колір
    } else {
      sidebar.classList.add("open");
      sidebar.style.left = "0";
      toggleButton.style.left = "180px";
      sidebar.style.backgroundColor = "#07090D"; // Білий колір при відкритті
    }
  });

  startButtons.forEach(button => {
    let isImageChanged = false;

    button.addEventListener("click", () => {
        if (isImageChanged) {
            button.style.backgroundImage = "url('Stop_butt.png')"; // Початкове зображення
        } else {
            button.style.backgroundImage = "url('Play_butt.png')"; // Нове зображення
        }
        isImageChanged = !isImageChanged; // Перемикання стану
    });
  });
});

function drawChart(canvasId, data, color) {
  const canvas = document.getElementById(canvasId);
  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  
  const padding = 30; // Відступ для підписів
  const paddingRight = 10; // Відступ справа
  const graphWidth = canvas.width - padding - paddingRight;
  const graphHeight = canvas.height - padding;
  const maxY = 100; // Максимальне значення по осі Y
  
  // Малювання підписів по осі Y (0% - 100%)
  ctx.fillStyle = "white";
  ctx.font = "10px Arial";
  for (let i = 0; i <= 5; i++) {
    let yValue = i * 20;
    let y = graphHeight - (graphHeight * yValue / maxY) + padding / 2;
    ctx.fillText(`${yValue}%`, 2, y);
  }
  
  // Малювання підписів по осі X (5 значень)
  const stepX = graphWidth / (data.length - 1);
  for (let i = 0; i < data.length; i++) {
      let x = stepX * i + padding;
      ctx.fillText(i, x - 5, canvas.height - 5);
  }
  
  // Малювання графіка
  ctx.strokeStyle = color;
  ctx.lineWidth = 1;
  ctx.beginPath();
  data.forEach((point, index) => {
      const x = stepX * index + padding;
      const y = graphHeight - (graphHeight * point / maxY) + padding / 2;
      if (index === 0) {
          ctx.moveTo(x, y);
      } else {
          ctx.lineTo(x, y);
      }
  });
  ctx.stroke();
}

const cpuData = [10, 30, 20, 40, 25];
const diskData = [15, 25, 20, 30, 35];
const memoryData = [5, 15, 20, 15, 10];
const networkData = [20, 25, 30, 25, 35];

drawChart("cpuChart", cpuData, "#8884d8");
drawChart("diskChart", diskData, "#ff0000");
drawChart("memoryChart", memoryData, "#ffa500");
drawChart("networkChart", networkData, "#00ff00");