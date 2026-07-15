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

rem IMPORTANTE:
rem No compilar src\*.cs. Las copias descargadas como ZIP pueden conservar
rem archivos viejos eliminados en GitHub. Solo se compila la arquitectura B1.
for %%F in (
  "%~dp0src\Program.cs"
  "%~dp0src\SwSession.cs"
  "%~dp0src\SwGeometry.cs"
  "%~dp0src\SwGeometryCompatTypes.cs"
  "%~dp0src\B1Config.cs"
  "%~dp0src\B1Geometry.cs"
  "%~dp0src\B1Stage1Builder.cs"
  "%~dp0src\B1Stage2Builder.cs"
  "%~dp0src\B1Stage3Builder.cs"
  "%~dp0src\B1AssemblyReviewBuilder.cs"
) do (
  if not exist "%%~F" (
    echo ERROR: falta el archivo activo %%~nxF
    echo Ejecute 01_ACTUALIZAR.bat y vuelva a intentar.
    exit /b 4
  )
)

echo Compilando revision B1 con:
echo   CSC: %CSC%
echo   SW : %SWDLL%
echo   Fuentes: Program + B1 + infraestructura comun

del /q "%~dp0bin\NacelleBuilder.exe" >nul 2>&1
del /q "%~dp0bin\NacelleBuilder.pdb" >nul 2>&1
del /q "%~dp0bin\SolidWorks.Interop.sldworks.dll" >nul 2>&1

"%CSC%" /nologo /target:exe /platform:x64 /optimize+ /warn:4 ^
  /out:"%~dp0bin\NacelleBuilder.exe" ^
  /reference:"%SWDLL%" ^
  "%~dp0src\Program.cs" ^
  "%~dp0src\SwSession.cs" ^
  "%~dp0src\SwGeometry.cs" ^
  "%~dp0src\SwGeometryCompatTypes.cs" ^
  "%~dp0src\B1Config.cs" ^
  "%~dp0src\B1Geometry.cs" ^
  "%~dp0src\B1Stage1Builder.cs" ^
  "%~dp0src\B1Stage2Builder.cs" ^
  "%~dp0src\B1Stage3Builder.cs" ^
  "%~dp0src\B1AssemblyReviewBuilder.cs"
if errorlevel 1 (
  echo ERROR DE COMPILACION B1.
  exit /b 5
)

copy /y "%SWDLL%" "%~dp0bin\SolidWorks.Interop.sldworks.dll" >nul
if errorlevel 1 (
  echo ERROR: se compilo el EXE pero no se pudo copiar la DLL de interoperabilidad.
  exit /b 6
)

if not exist "%~dp0bin\SolidWorks.Interop.sldworks.dll" (
  echo ERROR: falta bin\SolidWorks.Interop.sldworks.dll despues del build.
  exit /b 7
)

echo Build B1 correcto:
echo   bin\NacelleBuilder.exe
echo   bin\SolidWorks.Interop.sldworks.dll
exit /b 0
