@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if not exist bin mkdir bin

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: no se encontro csc.exe de .NET Framework 4.x.
  echo Active .NET Framework 4.8 en Windows.
  exit /b 2
)

set "SWDLL=%~dp0lib\SolidWorks.Interop.sldworks.dll"
if not exist "%SWDLL%" set "SWDLL=%ProgramFiles%\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll"
if not exist "%SWDLL%" set "SWDLL=%ProgramFiles%\SOLIDWORKS Corp\SOLIDWORKS\SolidWorks.Interop.sldworks.dll"
if not exist "%SWDLL%" set "SWDLL=%USERPROFILE%\Desktop\AlaSW\SolidWorks.Interop.sldworks.dll"

if not exist "%SWDLL%" (
  echo ERROR: no se encontro SolidWorks.Interop.sldworks.dll.
  echo Copiela a lib\ o revise la instalacion de SOLIDWORKS 2021.
  exit /b 3
)

echo Compilando con:
echo   CSC: %CSC%
echo   SW : %SWDLL%

"%CSC%" /nologo /target:exe /platform:x64 /optimize+ /warn:4 /out:"%~dp0bin\NacelleBuilder.exe" /reference:"%SWDLL%" "%~dp0src\*.cs"
if errorlevel 1 (
  echo ERROR DE COMPILACION.
  exit /b 4
)

echo Build correcto: bin\NacelleBuilder.exe
exit /b 0
