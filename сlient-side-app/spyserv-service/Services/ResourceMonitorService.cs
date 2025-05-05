using Newtonsoft.Json;
using System.Diagnostics;
using spyserv_services.Core.Dtos;
using System.Runtime.InteropServices;

namespace spyserv_services.Services
{
    public static class ResourceMonitorService
    {
        public static CpuResultDto GetCpuUsagePercentage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var command = GetCommandOutput("./src/resmon", "cpu");
                return JsonConvert.DeserializeObject<CpuResultDto>(command) ?? throw new Exception("Cannot deserialize resmon.");
            }
            else throw new NotImplementedException();
        }

        public static MemoryResultDto GetMemoryUsage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var resStr = GetCommandOutput("./src/resmon", "memory");
                return JsonConvert.DeserializeObject<MemoryResultDto>(resStr) ?? throw new Exception("Cannot deserialize resmon.");
            }
            else throw new NotImplementedException();
        }

        public static DiskResultDto GetDiskUsage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var device = GetMainDiskFromDf();
                var resStr = GetCommandOutput("./src/resmon", $"disk {device}");
                return JsonConvert.DeserializeObject<DiskResultDto>(resStr) ?? throw new Exception("Cannot deserialize resmon.");
            }
            else throw new NotImplementedException();
        }

        public static string GetMainDiskFromDf()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var output = GetCommandOutput("df", "-h");
                var lines = output.Split("\n").Where(line => line.Contains("/"));
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 6 && parts[5] == "/")
                    {
                        var device = parts[0].Replace("/dev/", "");
                        return device;
                    }
                }
                throw new Exception("Cannot locate disk");
            }
            else throw new NotImplementedException();
        }

        private static string GetCommandOutput(string command, string args)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    var output = process?.StandardOutput.ReadToEnd() ?? "";
                    var error = process?.StandardError.ReadToEnd() ?? "";
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Error executing command: {error}");
                    }
                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute command '{command} {args}': {ex.Message}");
            }
        }
    }
}