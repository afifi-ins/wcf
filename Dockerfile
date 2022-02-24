FROM mcr.microsoft.com/dotnet/framework/wcf
COPY . /wcf

RUN	NET USER testuser /ADD
RUN	NET LOCALGROUP Administrators testuser /add
USER testuser 

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'Continue'; $verbosePreference='Continue';"]
RUN c:\wcf\src\System.Private.ServiceModel\tools\scripts\SetupWcfIISHostedService.cmd 38 /p:"c:\wcf"
ENTRYPOINT ["powershell"]
CMD dir;  powershell
