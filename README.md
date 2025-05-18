# SpyServ - OpenSourcce monitoring solution for small projects

Overview
SpyServ CLI is a command-line tool for managing and monitoring your system and applications. Below is the complete reference of available commands and how to use them.

Basic Commands
Start Monitoring Services
bash
spyserv start
Launches the system monitoring tools.

Stop Monitoring Services
```bash
spyserv stop
```
Stops the system monitoring tools.

Show Status
bash
spyserv status
Displays the current status of the application.

Application Tracking Commands
Track an Application
bash
spyserv track <appName> [options]
Adds an application to the monitoring list.

Options:
-l, --logs <path>: Specify the path to application logs

-d, --description <text>: Specify the description of the application

-r, --restart: Enable auto-restart if the application stops (requires --exec-path)

-e, --exec-path <path>: Path to the executable file (required with --restart)

-ci, --checking-interval <seconds>: Set the interval for checking status (default: 60)

-rd, --restart-delay <seconds>: Set delay before restarting (default: 0)

-n, --no-notify: Disable notifications for this application

Examples:
bash
spyserv track myapp --logs /var/log/myapp.log --description "My important app"
spyserv track webserver -e /usr/bin/nginx -r -ci 30
Untrack an Application
bash
spyserv untrack <appName>
Removes an application from the monitoring list.

Example:
bash
spyserv untrack myapp
Configuration Commands
User Configuration
bash
spyserv config user.name <user-name>
Sets the user's name.

bash
spyserv config user.email <user-email>
Sets the user's email.

Application Settings
bash
spyserv config app [options]
Configures application monitoring settings.

Options:
-ma, --monitor-apps: Enable/disable monitoring apps (default: true)

-sd, --send-data: Enable/disable sending monitoring data (default: true)

-sn, --send-notifications: Enable/disable notifications (default: true)

-se, --soft-exit: Enable/disable soft exiting (default: true)

-mc, --monitor-cycle <seconds>: Monitoring interval for all apps (default: 60)

-el, --enable-logging: Enable/disable logging (default: true)

Example:
bash
spyserv config app --monitor-cycle 120 --send-notifications false
Resource Monitoring Settings
bash
spyserv config resmon [options]
Configures system resource monitoring.

Options:
-mc, --monitor-cpu: Monitor CPU usage (default: true)

-mr, --monitor-ram: Monitor RAM usage (default: true)

-md, --monitor-disk: Monitor disk usage (default: true)

--cpu-threshold <%>: CPU usage threshold (default: 80)

--ram-threshold <%>: RAM usage threshold (default: 90)

--disk-threshold <%>: Disk usage threshold (default: 90)

Example:
bash
spyserv config resmon --cpu-threshold 90 --monitor-disk false
Help
To see help for any command, add --help after it:

bash
spyserv --help
spyserv track --help
spyserv config app --help
