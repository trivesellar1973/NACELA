@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "QUIET=0"
if /I "%~1"=="/quiet" set "QUIET=1"

set "TARGET=%USERPROFILE%\Desktop\AlaSW"
set "ASM=%TARGET%\ALA_COMPLETA_MECANISMOS.SLDASM"

if exist "%ASM%" goto CONFIGURE

if "%QUIET%"=="0" (
  echo ============================================================
  echo   PREPARAR ALA BASE
  echo ============================================================
  echo La nacela NO debe existir previamente.
  echo Este paso solo instala el ensamblaje y las piezas del ala.
  echo.
)

set "ZIPFILE="
for %%F in ("%CD%\AlaSW*.zip") do if not defined ZIPFILE if exist "%%~fF" set "ZIPFILE=%%~fF"
for %%F in ("%USERPROFILE%\Downloads\AlaSW*.zip") do if not defined ZIPFILE if exist "%%~fF" set "ZIPFILE=%%~fF"
for %%F in ("%USERPROFILE%\Desktop\AlaSW*.zip") do if not defined ZIPFILE if exist "%%~fF" set "ZIPFILE=%%~fF"

if not defined ZIPFILE (
  if "%QUIET%"=="1" exit /b 0
  echo No se encontro automaticamente AlaSW*.zip.
  echo Descargue el ZIP del ala y dejelo en Descargas, Escritorio
  echo o en esta carpeta del repositorio.
  echo.
  set /p "ZIPFILE=Pegue la ruta completa del ZIP del ala: "
)

if not defined ZIPFILE exit /b 0
if not exist "%ZIPFILE%" (
  echo ERROR: no existe el ZIP indicado:
  echo %ZIPFILE%
  if "%QUIET%"=="0" pause
  exit /b 2
)

echo Instalando ala desde:
echo   %ZIPFILE%
echo Destino:
echo   %TARGET%

set "NACELA_WING_ZIP=%ZIPFILE%"
set "NACELA_WING_TARGET=%TARGET%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$zip=$env:NACELA_WING_ZIP;" ^
  "$target=$env:NACELA_WING_TARGET;" ^
  "$tmp=Join-Path $env:TEMP ('NACELA_WING_'+[guid]::NewGuid().ToString('N'));" ^
  "New-Item -ItemType Directory -Path $tmp -Force ^| Out-Null;" ^
  "try {" ^
  "  Expand-Archive -LiteralPath $zip -DestinationPath $tmp -Force;" ^
  "  $asm=Get-ChildItem -Path $tmp -Recurse -Filter 'ALA_COMPLETA_MECANISMOS.SLDASM' ^| Select-Object -First 1;" ^
  "  if(-not $asm){ throw 'El ZIP no contiene ALA_COMPLETA_MECANISMOS.SLDASM'; }" ^
  "  New-Item -ItemType Directory -Path $target -Force ^| Out-Null;" ^
  "  Copy-Item -Path (Join-Path $asm.Directory.FullName '*') -Destination $target -Recurse -Force;" ^
  "} finally { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }"

if errorlevel 1 (
  echo ERROR: no se pudo extraer el paquete del ala.
  if "%QUIET%"=="0" pause
  exit /b 3
)

if not exist "%ASM%" (
  echo ERROR: despues de extraer sigue faltando:
  echo %ASM%
  if "%QUIET%"=="0" pause
  exit /b 4
)

:CONFIGURE
if not exist "config" mkdir "config"
>"config\local.ini" echo # Configuracion local creada por 00_PREPARAR_ALA.bat
>>"config\local.ini" echo source_assembly=%ASM%

if "%QUIET%"=="0" (
  echo.
  echo Ala base lista.
  echo Ensamblaje: %ASM%
  echo Las nacelas NO se copiaron porque el generador las crea desde cero.
  pause
)
exit /b 0
