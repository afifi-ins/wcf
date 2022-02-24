FROM mcr.microsoft.com/dotnet/framework/wcf
#FROM mcr.microsoft.com/dotnet/framework/sdk:4.8
COPY . /wcf
 
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'Continue'; $verbosePreference='Continue';"]
RUN c:\wcf\src\System.Private.ServiceModel\tools\scripts\SetupWcfIISHostedService.cmd 38 /p:"c:\wcf"
ENTRYPOINT ["powershell"]
CMD dir;  powershell
