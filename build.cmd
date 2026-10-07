@echo off
rem Builds with the C# compiler that ships with Windows (.NET Framework 4.x) - no SDK required.
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist bin mkdir bin

"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32manifest:app.manifest ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /out:bin\TellMeWhenYouSeeThisChange.exe src\*.cs
if errorlevel 1 (echo BUILD FAILED & exit /b 1)
echo Built bin\TellMeWhenYouSeeThisChange.exe
