FROM mcr.microsoft.com/dotnet/framework/wcf

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'Continue'; $verbosePreference='Continue';"]
ENTRYPOINT ["powershell"]
CMD dir; c:\src\System.Private.ServiceModel\tools\scripts\SetupWcfIISHostedService.cmd 38 /p:"c:\wcf"; powershell
