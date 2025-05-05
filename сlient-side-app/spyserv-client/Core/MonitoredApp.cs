namespace spyserv.Core
{
    public class MonitoredApp
    {
        public string Name { get; set; } = string.Empty;
        public string PathToBin { get; set; } = string.Empty;
        public string PathToLogs { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRunning { get; set; } = true;
        public bool AutoRestart { get; set; } = false;
        public int CheckingIntervalInSec { get; set; } = 60;

        private int _restartDelay = 0;
        public int RestartDelay
        {
            get => _restartDelay;
            set => _restartDelay = value > 0 || AutoRestart ? value : 0;
        }

        public bool NoNotify { get; set; } = false;

        public override bool Equals(object? obj) => obj is MonitoredApp app && app.Name == Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}