// Continue after the previous code
document.addEventListener("DOMContentLoaded", () => {
  const startButtons = document.querySelectorAll(".icon-2");

  startButtons.forEach(button => {
    let isImageChanged = false;

    button.addEventListener("click", () => {
      if (isImageChanged) {
        button.style.backgroundImage = "url('Stop_butt.png')";
      } else {
        button.style.backgroundImage = "url('Play_butt.png')";
      }
      isImageChanged = !isImageChanged;
    });
  });

  // Function to fetch data from the server and load into the table and charts
  async function loadData() {
    try {
      const urlParams = new URLSearchParams(window.location.search);
      const deviceName = urlParams.get('deviceName');
      console.log(urlParams)
      console.log(deviceName)
        const response = await fetch(`http://localhost:32548/api/m/statuses/${deviceName}`, {
          credentials: 'include'
        });
        const data = await response.json();
    
        if (Array.isArray(data)) {
          const tableBody = document.querySelector('table tbody');
          tableBody.innerHTML = ''; // Clear existing rows
    
          data.forEach(app => {
            const row = document.createElement('tr');
            const status = app.isRunning ? 'green' : 'red';
            console.log(app.isRunning)
            row.innerHTML = `
              <td class="custom-checkbox"><input type="checkbox" class="checkbox-hidden"><span class="checkbox-icon"></span></td>
              <th class="table-craft"><span class="status-circle ${status}"></span></th>
              <td class="table-name">${app.appName}</td>
              <td class="table-craft">${app.cpuUsagePercent}%</td>
              <td class="table-craft">${app.memoryUsagePercent}%</td>
              <td class="table-craft">${new Date(app.lastStarted).toLocaleString()}</td>
              <td class="table-action"><button class="icon"></button><button class="icon-2"></button></td>
            `;
            tableBody.appendChild(row);
          });
    
          // Populate charts with the data
          drawChart('cpuChart', data.map(app => app.cpuUsagePercent), '#8884d8');
          drawChart('diskChart', data.map(app => app.memoryUsagePercent), '#ff0000');
          drawChart('memoryChart', data.map(app => app.memoryUsagePercent), '#ffa500');
          drawChart('networkChart', data.map(app => app.memoryUsagePercent), '#00ff00');
    
        } else {
          console.error("Expected array but received something else");
        }
      } catch (error) {
        console.error('Error loading data:', error);
      }
  }

  loadData(); // Call the function on page load
});

// Additional function for drawing charts
function drawChart(canvasId, data, color) {
  const canvas = document.getElementById(canvasId);
  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  const padding = 30; // Padding for labels
  const paddingRight = 10; // Padding on the right
  const graphWidth = canvas.width - padding - paddingRight;
  const graphHeight = canvas.height - padding;
  const maxY = 100; // Max Y-axis value

  // Draw Y-axis labels (0% - 100%)
  ctx.fillStyle = "white";
  ctx.font = "10px Arial";
  for (let i = 0; i <= 5; i++) {
    let yValue = i * 20;
    let y = graphHeight - (graphHeight * yValue / maxY) + padding / 2;
    ctx.fillText(`${yValue}%`, 2, y);
  }

  // Draw X-axis labels (5 values)
  const stepX = graphWidth / (data.length - 1);
  for (let i = 0; i < data.length; i++) {
    let x = stepX * i + padding;
    ctx.fillText(i, x - 5, canvas.height - 5);
  }

  // Draw the graph
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
