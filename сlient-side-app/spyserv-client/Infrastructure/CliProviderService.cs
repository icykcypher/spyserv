using spyserv.Core;
using Newtonsoft.Json;
using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace spyserv.Infrastructure
{
    public static class CliProviderService 
    {
        /// <summary>
        /// Creates a new default application configuration.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="AppConfig"/> with default values.
        /// </returns>
        /// <remarks>
        /// This method initializes a new <see cref="AppConfig"/> object and assigns default values 
        /// to its properties, ensuring that no property remains uninitialized. It configures:
        /// <list type="bullet">
        /// <item><description><see cref="AppConfig.AppSettings"/> as an instance of <see cref="ServicesSettings"/>.</description></item>
        /// <item><description><see cref="AppConfig.ResMonSettings"/> as an instance of <see cref="ResourceMonitoringSettings"/>.</description></item>
        /// <item><description><see cref="AppConfig.Debug"/> and <see cref="AppConfig.Release"/> as instances of <see cref="DebugConfig"/> and <see cref="ReleaseConfig"/> respectively.</description></item>
        /// <item><description><see cref="DebugConfig.Pathes"/> and <see cref="ReleaseConfig.Pathes"/> initialized with default paths.</description></item>
        /// <item><description><see cref="AppConfig.User"/> with default values ("null" for Name and Email).</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var defaultConfig = CreateNewAppConfig();
        /// </code>
        /// </example>
        public static void ConfigureCommands(string[] args)
        {
            var rootCommand = new RootCommand("SpyServ CLI - Manage and monitor your system and applications");

            var start = new Command("start", "Launch the system monitoring tools");
            start.SetHandler(StartServices);
            rootCommand.AddCommand(start);

            var stop = new Command("stop", "Stop the system monitoring tools");
            stop.SetHandler(StopServices);
            rootCommand.AddCommand(stop);

            var status = new Command("status", "Display the current status of the application");
            status.SetHandler(ShowStatus);
            rootCommand.AddCommand(status);

            // TRACK START
            var track = new Command("track", "Add the specified application to the monitoring list");

            var trackArgument = new Argument<string>("appName", "Name of the application to monitor");
            track.AddArgument(trackArgument);

            var trackLogs = new Option<string>(
                name: "--logs",
                getDefaultValue: () => string.Empty,
                description: "Specify the path to application logs"
            );
            trackLogs.AddAlias("-l");
            track.AddOption(trackLogs);

            var trackDescription = new Option<string>(
                name: "--description",
                getDefaultValue: () => string.Empty,
                description: "Specify the description of the application"
            );
            trackDescription.AddAlias("-d");
            track.AddOption(trackDescription);

            var trackRestart = new Option<bool>(
                name: "--restart",
                getDefaultValue: () => false,
                description: "Enable auto-restart if the application stops"
            );
            trackRestart.AddAlias("-r");
            var executablePath = new Option<string>(
                name: "--exec-path",
                description: "Path to the executable file",
                getDefaultValue: () => ""
            );
            executablePath.AddAlias("-e");

            trackRestart.AddValidator(result =>
            {
                if (result.GetValueOrDefault<bool>() && result.Parent.GetValueForOption<string>(executablePath) != "--exec-path") 
                {
                    result.ErrorMessage = "The --exec-path option is required when using --restart.";
                }
            });
            track.AddOption(trackRestart);
            track.AddOption(executablePath);

            var trackCheckingInterval = new Option<int>(
                name: "--checking-interval",
                getDefaultValue: () => 60,
                description: "Set the interval in seconds for checking the application status"
            );
            trackCheckingInterval.AddAlias("-ci");
            track.AddOption(trackCheckingInterval);

            var trackRestartDelay = new Option<int>(
                name: "--restart-delay",
                getDefaultValue: () => 0,
                description: "Set the delay in seconds before restarting the application"
            );
            trackRestartDelay.AddAlias("-rd");
            track.AddOption(trackRestartDelay);

            var trackNoNotify = new Option<bool>(
                name: "--no-notify",
                getDefaultValue: () => false,
                description: "Disable notifications for application"
            );
            trackNoNotify.AddAlias("-n");
            track.AddOption(trackNoNotify);

            track.SetHandler(async (appName, logs, description, restart, 
                        checkingInterval, restartDelay, noNotify, execPath) =>
            {
                var app = new MonitoredApp
                {
                    Name = appName,
                    PathToBin = execPath,
                    PathToLogs = logs,
                    Description = description,
                    IsRunning = true,
                    AutoRestart = restart,
                    CheckingIntervalInSec = checkingInterval,
                    RestartDelay = restartDelay,
                    NoNotify = noNotify
                };

                await TrackApplication(app);
                Console.WriteLine($"Tracking application: {app.Name}");
                Console.WriteLine($"Path to exectable file: {execPath}");
                Console.WriteLine($"Logs Path: {app.PathToLogs}");
                Console.WriteLine($"Description: {app.Description}");
                Console.WriteLine($"Auto Restart: {app.AutoRestart}");
                Console.WriteLine($"Checking Interval: {app.CheckingIntervalInSec} seconds");
                Console.WriteLine($"Restart Delay: {app.RestartDelay} seconds");
                Console.WriteLine($"No Notify: {app.NoNotify}");

                await Task.CompletedTask;
            },
            trackArgument,
            trackLogs,
            trackDescription,
            trackRestart,
            trackCheckingInterval,
            trackRestartDelay,
            trackNoNotify,
            executablePath
            );

            rootCommand.AddCommand(track);
            // TRACK END
            // UNTRACK START
            var untrack = new Command("untrack", "Remove the specified application from the monitoring list");
            var untrackArgument = new Argument<string>("appName", "Name of the application to untrack");
            untrack.AddArgument(untrackArgument);

            untrack.SetHandler
            (
                UntrackApplication,
                untrackArgument
            );
            rootCommand.AddCommand(untrack);
            // UNTRACK END
            // CONFIG START
            var config = new Command("config", "Set configuration values");
            var configCommandUserName = new Command("user.name", "Set user's name");
            var configCommandUserEmail = new Command("user.email", "Set user's email'");
            var configCommandApp = new Command("app", "Configure application settings");
            var configCommandResMon = new Command("resmon", "Configure resource monitoring settings");
            
            var configArgumentName = new Argument<string>("user-name", "Value for the specified user's name");
            configCommandUserName.AddArgument(configArgumentName);
            config.Add(configCommandUserName);
            
            var configArgumentEmail= new Argument<string>("user-email", "Value for the specified user's email");
            configCommandUserEmail.AddArgument(configArgumentEmail);
            configCommandUserName.SetHandler
            (
                ConfigureUserName,
                configArgumentName
            );

             configCommandUserEmail.SetHandler
            (
                ConfigureUserEmail,
                configArgumentEmail
            );
            config.Add(configCommandUserEmail);

            var checkApplicationsStatus = new Option<bool>(
                name: "--monitor-apps",
                description: "Enable or disable monitoring apps",
                getDefaultValue: () => true
            );
            checkApplicationsStatus.AddAlias("-ma");
            configCommandApp.AddOption(checkApplicationsStatus);

            var sendMonitoringData = new Option<bool>(
                name: "--send-data",
                getDefaultValue: () => true,
                description: "Send monitoring data on server"
            );
            sendMonitoringData.AddAlias("-sd");
            configCommandApp.AddOption(sendMonitoringData);

            var sendNotifications = new Option<bool>(
                name: "--send-notifications",
                getDefaultValue: () => true,
                description: "Send notifications"
            );
            sendNotifications.AddAlias("-sn");
            configCommandApp.AddOption(sendNotifications);

            var softExiting = new Option<bool>(
                name: "--soft-exit",
                getDefaultValue: () => true,
                description: "Alows to soft exit monitoring services"
            );
            softExiting.AddAlias("-se");
            configCommandApp.AddOption(softExiting);

            var monitoringIntervalForAll = new Option<int>(
                name: "--monitor-cycle",
                getDefaultValue: () => 60,
                description: "Monitoring interval for all applications"
            );
            monitoringIntervalForAll.AddAlias("-mc");
            configCommandApp.AddOption(monitoringIntervalForAll);

            var enableLogging = new Option<bool>(
                name: "--enable-logging",
                getDefaultValue: () => true,
                description: "Enable logging"
            );
            enableLogging.AddAlias("-el");
            configCommandApp.AddOption(enableLogging);
            
            configCommandApp.SetHandler((CheckApplicationsStatus, SendMonitoringData, SendNotifications, SoftExiting, 
                        EnableLogging, MonitoringInterval) =>
            {
                var AppSettings = new ServicesSettings
                {
                    CheckApplicationsStatus = CheckApplicationsStatus,
                    SendMonitoringData = SendMonitoringData,
                    SendNotifications = SendNotifications,
                    EnableLogging = EnableLogging,
                    MonitoringInterval = MonitoringInterval,
                    SoftExiting = SoftExiting
                };
                var config = GetConfig();
                config.AppSettings = AppSettings;
                SaveApplicationConfig(StaticClaims.PathToConfig, config);

                Console.WriteLine($"Check Applications Status: {CheckApplicationsStatus}");
                Console.WriteLine($"Send Monitoring Data: {SendMonitoringData}");
                Console.WriteLine($"Send Notifications: {SendNotifications}");
                Console.WriteLine($"Enable Logging: {EnableLogging}");
                Console.WriteLine($"Soft Exiting: {SoftExiting}");
                Console.WriteLine($"Monitoring Interval: {MonitoringInterval} seconds");
            },
            checkApplicationsStatus,
            sendMonitoringData,
            sendNotifications,
            softExiting,
            enableLogging,
            monitoringIntervalForAll);

            var monitorCpuUsage = new Option<bool>(
                name: "--monitor-cpu",
                getDefaultValue: () => true,
                description: "Monitor CPU usage for sending on server"
            );
            monitorCpuUsage.AddAlias("-mc");
            configCommandResMon.AddOption(monitorCpuUsage);

            var monitorMemsage = new Option<bool>(
                name: "--monitor-ram",
                getDefaultValue: () => true,
                description: "Monitor RAM usage for sending on server"
            );
            monitorMemsage.AddAlias("-mr");
            configCommandResMon.AddOption(monitorMemsage);

            var monitorDiskUsage = new Option<bool>(
                name: "--monitor-disk",
                getDefaultValue: () => true,
                description: "Monitor disk usage for sending on server"
            );
            monitorDiskUsage.AddAlias("-md");
            configCommandResMon.AddOption(monitorDiskUsage);

            var cpuUsageThreshold = new Option<int>(
                name: "--cpu-threshold",
                getDefaultValue: () => 80,
                description: "Set CPU usage threshold in %"
            );
            configCommandResMon.AddOption(cpuUsageThreshold);

            var ramUsageThreshold = new Option<int>(
                name: "--ram-threshold",
                getDefaultValue: () => 90,
                description: "Set RAM usage threshold in %"
            );
            configCommandResMon.AddOption(ramUsageThreshold);

            var disksageThreshold = new Option<int>(
                name: "--disk-threshold",
                getDefaultValue: () => 90,
                description: "Set disk usage threshold in %"
            );
            configCommandResMon.AddOption(disksageThreshold);

            configCommandResMon.SetHandler((monitorCpuUsage, monitorMemUsage, monitorDiskUsage, cpuUsageThreshold, 
                        ramUsageThreshold, disksageThreshold) =>
            {
                
                var config = GetConfig();
                config.ResMonSettings = new ResourceMonitoringSettings
                {
                    MonitorCpuUsage = monitorCpuUsage,
                    MonitorMemoryUsage = monitorMemUsage,
                    MonitorDiskUsage = monitorDiskUsage,
                    CpuUsageThreshold = cpuUsageThreshold,
                    MemoryUsageThreshold = ramUsageThreshold,
                    DiskUsageThreshold = disksageThreshold
                };

                SaveApplicationConfig(StaticClaims.PathToConfig, config);

                Console.WriteLine($"MonitorCpuUsage: {monitorCpuUsage}");
                Console.WriteLine($"MonitorMemoryUsage: {monitorMemUsage}");
                Console.WriteLine($"MonitorDiskUsage: {monitorDiskUsage}");
                Console.WriteLine($"CpuUsageThreshold: {cpuUsageThreshold}%");
                Console.WriteLine($"MemoryUsageThreshold: {ramUsageThreshold}%");
                Console.WriteLine($"DiskUsageThreshold: {disksageThreshold}%");
            },
            monitorCpuUsage,
            monitorMemsage,
            monitorDiskUsage,
            cpuUsageThreshold,
            ramUsageThreshold,
            disksageThreshold);

            config.Add(configCommandApp);
            config.Add(configCommandResMon);
            rootCommand.AddCommand(config);
            // CONFIG END
            rootCommand.Invoke(args);
        }
        /// <summary>
        /// Configures user information for email notifications and remote server control.
        /// </summary>
        /// <param name="args">
        /// Command-line arguments, where:
        /// args[1] specifies the configuration key (e.g., "user.name" or "user.email"),
        /// and args[2] specifies the corresponding value.
        /// </param>
        /// <remarks>
        /// This method validates input parameters and ensures the provided email address
        /// is in a correct format if "user.email" is being configured.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when required arguments are missing.
        /// </exception>
        private static void ConfigureUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                Console.WriteLine("spyserv config: Incorrect parameter");
            else
            {
                if (File.Exists(StaticClaims.PathToConfig))
                {
                    var json = File.ReadAllText(StaticClaims.PathToConfig);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    config.User.Name = value;
                    SaveApplicationConfig(StaticClaims.PathToConfig, config);
                }
                else 
                {
                    var config = CreateNewAppConfig();
                    config.User.Name = value;
                    SaveApplicationConfig(StaticClaims.PathToConfig, config);
                }
            }
        }

        /// <summary>
        /// Configures the user's email in the application configuration file.
        /// </summary>
        /// <param name="value">
        /// The email address to be set for the user. It should follow a valid email format.
        /// </param>
        /// <remarks>
        /// This method first checks if the provided <paramref name="value"/> is not null, empty, or whitespace. 
        /// If invalid, it prints an error message. Then it validates if the email follows a proper format using a regular expression.
        /// If the email format is incorrect, an error message is displayed. If the email is valid, the method updates the user's email
        /// in the configuration file located at <see cref="StaticClaims.PathToConfig"/>. If the file exists, it reads and updates the configuration.
        /// If the file doesn't exist, it creates a new configuration and sets the email before saving it.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// ConfigureUserEmail("user@example.com");
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an error reading from or writing to the configuration file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Thrown if deserialization or serialization of the JSON configuration fails.
        /// </exception>
        /// <seealso cref="SaveApplicationConfig(string, AppConfig)"/>
        private static void ConfigureUserEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                Console.WriteLine("spyserv config: Incorrect parameter");
            else
            {
                if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    Console.WriteLine("spyserv config: Incorrect email format");
                else 
                {
                    if (File.Exists(StaticClaims.PathToConfig))
                    {
                        var json = File.ReadAllText(StaticClaims.PathToConfig);
                        var config = JsonConvert.DeserializeObject<AppConfig>(json);
                        config.User.Email = value;
                        SaveApplicationConfig(StaticClaims.PathToConfig, config);
                    }
                    else 
                    {
                        var config = CreateNewAppConfig();
                        config.User.Email = value;
                        SaveApplicationConfig(StaticClaims.PathToConfig, config);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the monitoring services by killing the associated process.
        /// </summary>
        /// <remarks>
        /// This method attempts to stop the monitoring services by searching for processes
        /// with the name specified in <see cref="StaticClaims.SpyservServiceProcessName"/>.
        /// If one or more processes are found, they are terminated using the <see cref="Process.Kill"/> method.
        /// If no processes are found, a message is printed indicating that the monitoring services are not running.
        /// If an error occurs while attempting to kill the processes, an error message is printed.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// StopServices();
        /// </code>
        /// </example>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if an error occurs while killing a process.
        /// </exception>
        /// <seealso cref="Process.GetProcessesByName(string)"/>
        private static void StopServices()
        {
            try
            {
                var processes = Process.GetProcessesByName(StaticClaims.SpyservServiceProcessName);

                if (processes.Length > 0) foreach (var process in processes) process.Kill();
                else Console.WriteLine($"spyserv stop: Monitoring services was not working");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"spyserv stop: Error while trying to kill process: {ex.Message}");
            }
        }

        /// <summary>
        /// Tracks an application by adding it to the monitored applications configuration.
        /// </summary>
        /// <param name="app">
        /// The <see cref="MonitoredApp"/> object to be tracked and added to the configuration.
        /// </param>
        /// <remarks>
        /// This method checks if the <paramref name="app"/> is not null. If valid, it calls the <see cref="AddAppToConfig"/> method 
        /// to add the application to the monitoring configuration. If the application is null, an error message is printed to the console.
        /// This method is asynchronous and completes the task immediately.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var app = new MonitoredApp { Name = "MyApp", Path = "/path/to/app" };
        /// await TrackApplication(app);
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="app"/> is null when trying to track it.
        /// </exception>
        /// <seealso cref="AddAppToConfig(MonitoredApp)"/>
        private async static Task TrackApplication(MonitoredApp app)
        {
            if (app is not null) AddAppToConfig(app);
            else Console.WriteLine("spyserv track: Invalid arguments for configuring.");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Adds a new application to the monitored apps configuration file.
        /// </summary>
        /// <param name="app">
        /// The <see cref="MonitoredApp"/> object to be added to the configuration.
        /// </param>
        /// <remarks>
        /// This method reads the existing monitored apps configuration file from the specified path.
        /// If the file exists, it deserializes the content into a <see cref="MonitoredAppsConfig"/> object.
        /// If the file does not exist, a new configuration is created using <see cref="CreateNewMonitoringConfig"/>.
        /// The method checks if the application is already present in the configuration before adding it.
        /// The updated configuration is then saved back to the file.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var newApp = new MonitoredApp { Name = "NewApp", Path = "/path/to/app" };
        /// AddAppToConfig(newApp);
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an error reading from or writing to the configuration file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Thrown if there is an error during deserialization of the JSON configuration file.
        /// </exception>
        /// <seealso cref="SaveMonitoringConfig(string, MonitoredAppsConfig)"/>
        private static void AddAppToConfig(MonitoredApp app)
        {
            var configFilePath = Path.Combine(AppContext.BaseDirectory, StaticClaims.PathToMonitoredAppsConf);
            MonitoredAppsConfig config;

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                config = JsonConvert.DeserializeObject<MonitoredAppsConfig>(json);
            }
            else config = CreateNewMonitoringConfig();

            if (!config.MonitoredApps.Contains(app)) config.MonitoredApps.Add(app);

            SaveMonitoringConfig(configFilePath, config);
        }

        /// <summary>
        /// Creates a new default application configuration.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="AppConfig"/> with default values.
        /// </returns>
        /// <remarks>
        /// This method initializes a new <see cref="AppConfig"/> object and assigns default values 
        /// to its properties, ensuring that no property remains uninitialized. It configures:
        /// <list type="bullet">
        /// <item><description><see cref="AppConfig.AppSettings"/> as an instance of <see cref="ServicesSettings"/>.</description></item>
        /// <item><description><see cref="AppConfig.ResMonSettings"/> as an instance of <see cref="ResourceMonitoringSettings"/>.</description></item>
        /// <item><description><see cref="AppConfig.Debug"/> and <see cref="AppConfig.Release"/> as instances of <see cref="DebugConfig"/> and <see cref="ReleaseConfig"/> respectively.</description></item>
        /// <item><description><see cref="DebugConfig.Pathes"/> and <see cref="ReleaseConfig.Pathes"/> initialized with default paths.</description></item>
        /// <item><description><see cref="AppConfig.User"/> with default values ("null" for Name and Email).</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var defaultConfig = CreateNewAppConfig();
        /// </code>
        /// </example>
        private static AppConfig CreateNewAppConfig()
        {
            var config = new AppConfig();
            config.AppSettings = new ServicesSettings();
            config.ResMonSettings = new ResourceMonitoringSettings();
            config.Debug ??= new DebugConfig();
            config.Release ??= new ReleaseConfig();
            config.Debug.Pathes ??= new Pathes
            {
                SpyservApi = config.Debug?.Pathes?.SpyservApi ?? "../",
                SpyservWatcher = config.Debug?.Pathes?.SpyservWatcher ?? "../"
            };
            config.Release.Pathes ??= new Pathes
            {
                SpyservApi = config.Release?.Pathes?.SpyservApi ?? "../",
                SpyservWatcher = config.Release?.Pathes?.SpyservWatcher ?? "../"
            };
            config.User ??= new User
            {
                Name = config.User?.Name ?? "null",
                Email = config.User?.Email ?? "null"
            };

            return config;
        }

        /// <summary>
        /// Retrieves the application configuration from a JSON file.
        /// </summary>
        /// <returns>
        /// An <see cref="AppConfig"/> object containing the application settings.
        /// </returns>
        /// <remarks>
        /// This method checks whether the configuration file exists at <see cref="StaticClaims.PathToConfig"/>.
        /// If the file exists, it reads and deserializes the JSON content into an <see cref="AppConfig"/> object.
        /// If the file does not exist, a new configuration is created using <see cref="CreateNewAppConfig"/>,
        /// saved to the file, and returned.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var config = GetConfig();
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an error occurs while reading or writing the configuration file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Thrown if deserialization fails due to invalid JSON content.
        /// </exception>
        /// <seealso cref="CreateNewAppConfig()"/>
        /// <seealso cref="SaveApplicationConfig(string, AppConfig)"/>
        private static AppConfig GetConfig()
        {
            if (File.Exists(StaticClaims.PathToConfig))
            {
                var json = File.ReadAllText(StaticClaims.PathToConfig);
                return JsonConvert.DeserializeObject<AppConfig>(json) 
                ?? throw new JsonSerializationException($"Error in deserialization {StaticClaims.PathToConfig}.");
            }
            else
            {
                var conf = CreateNewAppConfig();
                SaveApplicationConfig(StaticClaims.PathToConfig, conf);
                return conf;
            }
        }

        private static MonitoredAppsConfig CreateNewMonitoringConfig()
        {
            var config = new MonitoredAppsConfig();

            return config;
        }

        /// <summary>
        /// Loads the monitoring configuration from a JSON file.
        /// </summary>
        /// <param name="configFilePath">
        /// The file path from which the monitoring configuration should be loaded.
        /// </param>
        /// <returns>
        /// A <see cref="MonitoredAppsConfig"/> object containing the monitoring configuration.
        /// </returns>
        /// <remarks>
        /// This method checks whether the specified configuration file exists.
        /// If it does, the method reads the file, deserializes the JSON content, and returns the 
        /// corresponding <see cref="MonitoredAppsConfig"/> object.
        /// If the file does not exist, a new default configuration is created and returned.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var config = LoadMonitoringConfig("monitoring-config.json");
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an error occurs while reading the file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">
        /// Thrown if deserialization fails due to invalid JSON content.
        /// </exception>
        /// <seealso cref="CreateNewMonitoringConfig()"/>
        private static MonitoredAppsConfig LoadMonitoringConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<MonitoredAppsConfig>(json) 
                    ?? throw new JsonSerializationException($"Error in deserialization {configFilePath}.");
            }
            else return CreateNewMonitoringConfig();
        }

        /// <summary>
        /// Saves the monitoring configuration to a JSON file.
        /// </summary>
        /// <param name="configFilePath">
        /// The file path where the monitoring configuration should be saved.
        /// </param>
        /// <param name="config">
        /// The monitoring configuration object to be serialized and written to the file.
        /// </param>
        /// <remarks>
        /// This method checks whether the specified configuration file exists.
        /// If it does, the configuration is serialized to JSON and written to the file.
        /// If the file does not exist, the required directory structure is created, 
        /// and the JSON data is written to a newly created file.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var config = new MonitoredAppsConfig { MonitoredApps = new List<AppInfo>() };
        /// SaveMonitoringConfig("monitoring-config.json", config);
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an issue creating or writing to the file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonException">
        /// Thrown if an error occurs during JSON serialization.
        /// </exception>
        private static void SaveMonitoringConfig(string configFilePath, MonitoredAppsConfig config)
        {
            if (File.Exists(configFilePath))
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFilePath, json);
            }
            else 
            {
                Directory.CreateDirectory("../src/");
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFilePath, json);
            }
        }

        /// <summary>
        /// Saves the application configuration to a JSON file.
        /// </summary>
        /// <param name="configFilePath">
        /// The file path where the configuration should be saved.
        /// </param>
        /// <param name="config">
        /// The application configuration object to be serialized and written to the file.
        /// </param>
        /// <remarks>
        /// This method checks if the specified configuration file exists. If it does, the configuration 
        /// is serialized to JSON and written to the file. If the file does not exist, the necessary 
        /// directory structure is created, a new configuration file is generated, and the JSON data is written to it.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// var config = new AppConfig { Setting1 = "value1", Setting2 = "value2" };
        /// SaveApplicationConfig("config.json", config);
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an issue creating or writing to the file.
        /// </exception>
        /// <exception cref="Newtonsoft.Json.JsonException">
        /// Thrown if an error occurs during JSON serialization.
        /// </exception>
        private static void SaveApplicationConfig(string configFilePath, AppConfig config)
        {
            if (File.Exists(configFilePath))
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFilePath, json);
            }
            else 
            {
                Directory.CreateDirectory("../src/");
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.Create(StaticClaims.PathToConfig).Dispose();
                File.WriteAllText(configFilePath, json);
            }
        }

        /// <summary>
        /// Starts the SpyServ monitoring services.
        /// </summary>
        /// <remarks>
        /// This method constructs the full path to the monitoring services binary, 
        /// verifies its existence, and attempts to start it as a background process.
        /// If the binary is not found, an error message is logged.
        /// If an exception occurs while starting the process, an error message is displayed.
        /// </remarks>
        /// <example>
        /// Example usage:
        /// <code>
        /// StartServices();
        /// </code>
        /// Expected output when the binary is missing:
        /// <code>
        /// spyserv start: Monitoring services binary was not found at /path/to/spyserv-services
        /// </code>
        /// Expected output when the process fails to start:
        /// <code>
        /// spyserv start: Failed to start monitoring services: [Error Message]
        /// </code>
        /// </example>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// Thrown if the process cannot be started due to system restrictions.
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the monitoring service binary does not exist.
        /// </exception>
        private static void StartServices()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var folderPath = Path.Combine(baseDirectory, @"../spyserv-services/spyserv-services");
            var fullPath = Path.GetFullPath(folderPath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"spyserv start: Monitoring services binary was not found at {fullPath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                WorkingDirectory = Path.GetDirectoryName(fullPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            try
            {
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"spyserv start: Failed to start monitoring services: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks whether a process with the specified name is currently running.
        /// </summary>
        /// <param name="processName">
        /// The name of the process to check.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one instance of the specified process is running; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method retrieves all running processes with the given name and checks if any exist.
        /// If an exception occurs while retrieving processes, it logs an error message to the console 
        /// and returns <c>false</c>.
        /// </remarks>
        /// <example>
        /// Checking if "notepad" is running:
        /// <code>
        /// bool isRunning = IsProcessRunning("notepad");
        /// Console.WriteLine(isRunning ? "Notepad is running." : "Notepad is not running.");
        /// </code>
        /// </example>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// Thrown if there is an issue accessing process information.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if an error occurs while enumerating processes.
        /// </exception>
        private static bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"spyserv: Error while checking process '{processName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Displays the current status of the SpyServ service.
        /// </summary>
        /// <remarks>
        /// This method checks whether the SpyServ service is running by verifying 
        /// if the corresponding process is active. The status is then printed to the console.
        /// </remarks>
        /// <example>
        /// Example output when SpyServ is running:
        /// <code>
        /// spyserv status: SpyServ is running.
        /// </code>
        /// Example output when SpyServ is not running:
        /// <code>
        /// spyserv status: SpyServ is not running.
        /// </code>
        /// </example>
        /// <seealso cref="IsProcessRunning(string)"/>
        private static void ShowStatus()
        {
            var isSpyservServicesRunning = IsProcessRunning(StaticClaims.SpyservServiceProcessName);

            if (!isSpyservServicesRunning) Console.WriteLine("spyserv status: SpyServ is not running.");
            else Console.WriteLine("spyserv status: SpyServ is running.");
        }

        /// <summary>
        /// Removes the specified application from the monitoring configuration if it exists.
        /// </summary>
        /// <param name="appName">
        /// The name of the application to be removed from monitoring.
        /// </param>
        /// <remarks>
        /// This method loads the current monitoring configuration from a file, 
        /// checks if the specified application is being monitored, and removes it 
        /// if found. After modification, the updated configuration is saved back to the file.
        /// </remarks>
        /// <example>
        /// To remove an application named "notepad":
        /// <code>
        /// UntrackApplication("notepad");
        /// </code>
        /// </example>
        /// <exception cref="System.IO.IOException">
        /// Thrown if there is an error accessing the configuration file.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the monitoring configuration is unexpectedly null.
        /// </exception>
        /// <seealso cref="LoadMonitoringConfig(string)"/>
        /// <seealso cref="SaveMonitoringConfig(string, MonitoringConfig)"/>
        private static void UntrackApplication(string appName)
        {
            var configFilePath = Path.Combine(AppContext.BaseDirectory, StaticClaims.PathToMonitoredAppsConf);

            var config = LoadMonitoringConfig(configFilePath);

            if (config.MonitoredApps.Select(x => x.Name).Contains(appName))
            {
                var app = config.MonitoredApps.First(a => a.Name == appName);
                config.MonitoredApps.Remove(app);
                SaveMonitoringConfig(configFilePath, config);
                Console.WriteLine($"spyserv untrack: Application '{appName}' removed from the config.");
            }
            else
            {
                Console.WriteLine($"spyserv untrack: Application '{appName}' is not found in the config.");
            }
        }
    }
}