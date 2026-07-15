@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "REPO=trivesellar1973/NACELA"
set "BRANCH=main"
set "ZIP_URL=https://github.com/%REPO%/archive/refs/heads/%BRANCH%.zip"

echo ============================================================
echo   ACTUALIZADOR NACELA
echo ============================================================
echo Carpeta local: %CD%
echo.

rem ------------------------------------------------------------
rem Caso 1: copia clonada con Git. Usa actualizacion normal.
rem ------------------------------------------------------------
if exist ".git\" (
    where git >nul 2>&1
    if errorlevel 1 (
        echo ERROR: esta carpeta tiene .git, pero Git no esta instalado
        echo o no esta agregado al PATH.
        pause
        exit /b 1
    )

    echo Se detecto un clon Git. Descargando cambios...
    git pull --ff-only origin %BRANCH%
    if errorlevel 1 (
        echo.
        echo ERROR: Git no pudo actualizar.
        echo Puede haber cambios locales, falta de Internet o una rama distinta.
        pause
        exit /b 1
    )

    echo.
    echo Repositorio actualizado correctamente con Git.
    pause
    exit /b 0
)

rem ------------------------------------------------------------
rem Caso 2: carpeta descargada como ZIP. Descarga main y copia
rem los archivos nuevos sin borrar resultados ni config local.
rem ------------------------------------------------------------
echo No se encontro la carpeta .git.
echo Esta copia fue descargada como ZIP; se usara actualizacion directa.
echo.

set "TMP_ROOT=%TEMP%\NACELA_UPDATE_%RANDOM%_%RANDOM%"
set "TMP_ZIP=%TMP_ROOT%\NACELA-main.zip"
set "TMP_EXTRACT=%TMP_ROOT%\extract"

mkdir "%TMP_ROOT%" >nul 2>&1
mkdir "%TMP_EXTRACT%" >nul 2>&1

echo Descargando la ultima version desde GitHub...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop'; Invoke-WebRequest -UseBasicParsing -Uri '%ZIP_URL%' -OutFile '%TMP_ZIP%'; Expand-Archive -LiteralPath '%TMP_ZIP%' -DestinationPath '%TMP_EXTRACT%' -Force"

if errorlevel 1 (
    echo.
    echo ERROR: no se pudo descargar o descomprimir la actualizacion.
    rmdir /s /q "%TMP_ROOT%" >nul 2>&1
    pause
    exit /b 1
)

set "SOURCE=%TMP_EXTRACT%\NACELA-%BRANCH%"
if not exist "%SOURCE%\README.md" (
    echo.
    echo ERROR: la descarga no contiene la estructura esperada.
    rmdir /s /q "%TMP_ROOT%" >nul 2>&1
    pause
    exit /b 1
)

echo Copiando archivos actualizados...
robocopy "%SOURCE%" "%CD%" /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP ^
  /XD ".git" "generated" ".vs" "bin" "obj" ^
  /XF "local.ini" "ultimo_ejecucion.log"

set "ROBOCOPY_CODE=%ERRORLEVEL%"
rmdir /s /q "%TMP_ROOT%" >nul 2>&1

if %ROBOCOPY_CODE% GEQ 8 (
    echo.
    echo ERROR: Robocopy fallo con codigo %ROBOCOPY_CODE%.
    pause
    exit /b 1
)

echo.
echo Proyecto actualizado correctamente desde el ZIP de GitHub.
echo Se conservaron generated\, config\local.ini y ultimo_ejecucion.log.
pause
exit /b 0
