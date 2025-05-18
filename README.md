# SpyServ - OpenSource Monitoring Solution for Small Projects
## Overview
SpyServ CLI is a command-line tool for managing and monitoring your system and applications. Below is the complete reference of available commands and how to use them.

## Prerequisites
Before using the app, you need to install Docker and Kubernetes.

## Installation
To deploy backend services, use the Python deployment script:

```bash
python3 deploy.py
```
For installing the client-side app, follow the installation instructions in the client app directory.

## Basic Commands
### Start Monitoring Services
```bash
spyserv start
```
Launches the system monitoring tools.

### Stop Monitoring Services
```bash
spyserv stop
```
Stops the system monitoring tools.

### Show Status
```bash
spyserv status
```
Displays the current status of the application.

### Application Tracking Commands
#### Track an Application
```bash
spyserv track <appName> [options]
```
Add the specified application to the monitoring list.

#### Arguments
<appName> — Name of the application to monitor

#### Options
Option	Description	Default
-l, --logs <logs>	Specify the path to application logs	[]
-d, --description <description>	Specify the description of the application	[]
-r, --restart	Enable auto-restart if the application stops	False
-e, --exec-path <exec-path>	Path to the executable file (required with --restart)	[]
-ci, --checking-interval <sec>	Set the interval in seconds for checking the application status	60
-rd, --restart-delay <sec>	Set the delay in seconds before restarting the application	0
-n, --no-notify	Disable notifications for this application	False
-?, -h, --help	Show help and usage information	

#### Examples
```bash
spyserv track myapp --logs /var/log/myapp.log --description "My important app"
spyserv track webserver -e /usr/bin/nginx -r -ci 30
```
### Untrack an Application
```bash
spyserv untrack <appName> [options]
```
Remove the specified application from the monitoring list.

#### Arguments
<appName> — Name of the application to untrack

#### Options
Option	Description
-?, -h, --help	Show help and usage information

#### Example
```bash
spyserv untrack myapp
```
## Configuration Commands
### User Configuration
Set user-related information:

```bash
spyserv config user.name <user-name>
```
Sets the user's name.

```bash
spyserv config user.email <user-email>
```
Sets the user's email.

### Application Settings
```bash
spyserv config app [options]
```
Configure application monitoring settings.

#### Options
Option	Description	Default
-ma, --monitor-apps	Enable or disable monitoring apps	True
-sd, --send-data	Enable or disable sending monitoring data	True
-sn, --send-notifications	Enable or disable notifications	True
-se, --soft-exit	Enable or disable soft exiting of monitoring services	True
-mc, --monitor-cycle <seconds>	Monitoring interval for all applications	60
-el, --enable-logging	Enable or disable logging	True
-?, -h, --help	Show help and usage information	

#### Example
```bash
spyserv config app --monitor-cycle 120 --send-notifications false
```
### Resource Monitoring Settings
```bash
spyserv config resmon [options]
```
Configure system resource monitoring settings.

#### Options
Option	Description	Default
-mc, --monitor-cpu	Monitor CPU usage	True
-mr, --monitor-ram	Monitor RAM usage	True
-md, --monitor-disk	Monitor disk usage	True
--cpu-threshold <percentage>	Set CPU usage threshold in %	80
--ram-threshold <percentage>	Set RAM usage threshold in %	90
--disk-threshold <percentage>	Set disk usage threshold in %	90
-?, -h, --help	Show help and usage information	

#### Example
```bash
spyserv config resmon --cpu-threshold 90 --monitor-disk false
```

### Help
To see help for any command, add --help after it:

```bash
spyserv --help
spyserv track --help
spyserv config app --help
```
