:: IF NOT DEFINED APPCMD SET APPCMD=%SystemRoot%\system32\inetsrv\AppCmd.exe
:: IF NOT DEFINED APPCMD SET APPCMD=%ProgramFiles(x86)%\IIS Express\AppCmd.exe
echo %APPCMD% > debug.txt
%APPCMD% unlock config -section:system.webServer/httpErrors 2>&1 >> debug.txt
%APPCMD% set config -section:httpErrors -lockAttributes:allowAbsolutePathsWhenDelegated 2>&1 >> debug.txt