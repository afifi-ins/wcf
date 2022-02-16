FROM mcr.microsoft.com/dotnet/framework/wcf
COPY . /wcf
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'Continue'; $verbosePreference='Continue';"]
ENTRYPOINT ["powershell"]
CMD dir; c:\wcf\src\System.Private.ServiceModel\tools\scripts\SetupWcfIISHostedService.cmd 38 /p:"c:\wcf"; powershell
