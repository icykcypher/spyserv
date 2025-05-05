namespace spyserv
{
    public static class StaticClaims
    {
        public static string SpyservServiceProcessName => "spyserv_services";

        public static string PathToMonitoredAppsConf => @"../src/monitored-apps.json";
        public static string PathToConfig => @"../src/appsettings.json";
    }
}