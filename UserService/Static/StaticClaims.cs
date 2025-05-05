using System.Runtime.InteropServices;

namespace UserService.Static
{
    public static class StaticClaims
    {
        public static string PathToLogs => (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? @"/var/log/user-srv.log" : @"\logs\user-srv.log");
    }
}