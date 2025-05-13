document.addEventListener("DOMContentLoaded", () => {
  let autoUpdateInterval = setInterval(loadData, 5000);

  // Обработчик кнопок запуска/остановки
  document.addEventListener("click", (event) => {
    if (event.target.classList.contains("icon-2")) {
      const button = event.target;
      const isPlay = button.style.backgroundImage.includes("Play_butt.png");

      button.style.backgroundImage = isPlay
        ? "url('Stop_butt.png')"
        : "url('Play_butt.png')";

      // Переключение автозагрузки
      if (isPlay) {
        autoUpdateInterval = setInterval(loadData, 5000);
      } else {
        clearInterval(autoUpdateInterval);
      }
    }
  });

  // Загрузка данных с сервера
  async function loadData() {
    try {
      const urlParams = new URLSearchParams(window.location.search);
      const deviceName = urlParams.get("deviceName");
      if (!deviceName) return;

      // Таблица
      const statusResponse = await fetch(`http://localhost:32548/api/m/statuses/${deviceName}`, {
        credentials: "include"
      });
      const statusData = await statusResponse.json();
      console.log(statusData)
      if (Array.isArray(statusData)) {
        renderTable(statusData);
      } else {
        console.error("Expected array for statuses, got:", statusData);
      }

      // Графики
      const metricsResponse = await fetch(`http://localhost:32548/api/m/${deviceName}`, {
        credentials: "include"
      });
      const metricsData = await metricsResponse.json();
      console.log(metricsData)

      if (metricsData && typeof metricsData === "object") {
        updateChartData(metricsData);
      } else {
        console.error("Invalid metrics data:", metricsData);
      }

    } catch (error) {
      console.error("Error loading data:", error);
    }
  }

  // Отображение таблицы
  function renderTable(data) {
    const tableBody = document.querySelector("table tbody");
    tableBody.innerHTML = "";

    data.forEach(app => {
      const row = document.createElement("tr");
      const status = app.isRunning ? "green" : "red";
      row.innerHTML = `
        <td class="custom-checkbox">
          <input type="checkbox" class="checkbox-hidden">
          <span class="checkbox-icon"></span>
        </td>
        <th class="table-craft"><span class="status-circle ${status}"></span></th>
        <td class="table-name">${app.appName}</td>
        <td class="table-craft">${app.cpuUsagePercent}%</td>
        <td class="table-craft">${app.memoryUsagePercent}%</td>
        <td class="table-craft">${new Date(app.lastStarted).toLocaleString()}</td>
        <td class="table-action">
          <button class="icon"></button>
          <button class="icon-2" style="background-image: url('Stop_butt.png');"></button>
        </td>
      `;
      tableBody.appendChild(row);
    });
  }

  // Обновление данных графиков
  function updateChartData(metrics) {
  const MAX_POINTS = 20;

  const cpuData = JSON.parse(sessionStorage.getItem("cpuData")) || [];
  const memoryData = JSON.parse(sessionStorage.getItem("memoryData")) || [];
  const diskData = JSON.parse(sessionStorage.getItem("diskData")) || [];
  const networkData = JSON.parse(sessionStorage.getItem("networkData")) || [];

  // Извлекаем актуальные значения
  const cpuUsage = metrics.cpuResult?.usagePercent ?? 0;
  const memoryUsage = metrics.memoryResult?.usedPercent ?? 0;
  const diskUsage = ((metrics.diskResult?.readMbps ?? 0) + (metrics.diskResult?.writeMbps ?? 0)) / 2;

  // Здесь нет networkResult, можно либо убрать, либо ставить заглушку
  const networkUsage = 0;

  // Добавляем новые данные
  cpuData.push(cpuUsage);
  memoryData.push(memoryUsage);
  diskData.push(diskUsage);
  networkData.push(networkUsage);

  // Ограничиваем размер
  while (cpuData.length > MAX_POINTS) cpuData.shift();
  while (memoryData.length > MAX_POINTS) memoryData.shift();
  while (diskData.length > MAX_POINTS) diskData.shift();
  while (networkData.length > MAX_POINTS) networkData.shift();

  // Сохраняем
  sessionStorage.setItem("cpuData", JSON.stringify(cpuData));
  sessionStorage.setItem("memoryData", JSON.stringify(memoryData));
  sessionStorage.setItem("diskData", JSON.stringify(diskData));
  sessionStorage.setItem("networkData", JSON.stringify(networkData));

  // Обновляем графики
  drawChart("cpuChart", cpuData, "#8884d8");
  drawChart("diskChart", diskData, "#ff0000");
  drawChart("memoryChart", memoryData, "#ffa500");
  drawChart("networkChart", networkData, "#00ff00");
}


  // Функция отрисовки графика
  function drawChart(canvasId, data, color) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const padding = 30;
    const paddingRight = 10;
    const graphWidth = canvas.width - padding - paddingRight;
    const graphHeight = canvas.height - padding;
    const maxY = 100;

    // Y-ось
    ctx.fillStyle = "white";
    ctx.font = "10px Arial";
    for (let i = 0; i <= 5; i++) {
      const yValue = i * 20;
      const y = graphHeight - (graphHeight * yValue / maxY) + padding / 2;
      ctx.fillText(`${yValue}%`, 2, y);
    }

    // X-ось
    const stepX = graphWidth / (data.length - 1 || 1);
    for (let i = 0; i < data.length; i++) {
      const x = stepX * i + padding;
      ctx.fillText(i, x - 5, canvas.height - 5);
    }

    // Линия графика
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

  // Первая загрузка
  loadData();
});