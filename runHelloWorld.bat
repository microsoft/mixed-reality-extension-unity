@echo off
set UnityVersion=2018.1.9f2
set SceneName=HelloWorld

if not exist %~dp0MRETestBed\Assets\Assemblies\MREUnityRuntimeLib.dll (
  call %~dp0buildMREUnityRuntimeDLL.bat
  if not exist %~dp0MRETestBed\Assets\Assemblies\MREUnityRuntimeLib.dll (
    echo Failed building MREUnityRuntimeLib 
    pause
    exit /b 1
  )
)
set UnityTempRoot=%UNITY_ROOT%

@rem check default unity install location for the required version
if exist "%ProgramFiles%\Unity\Hub\Editor\%UnityVersion%\" (
  if "%UnityTempRoot%" EQU "" set UnityTempRoot=%ProgramFiles%\Unity\Hub\Editor\%UnityVersion%\
)

if "%UnityTempRoot%" EQU "" (
  echo %UNITY_ROOT% is empty - please install Unity %UnityVersion% ^(or later^) and set %%UNITY_ROOT%% to its install location.
  pause
  exit /b 1
)

if not exist "%UnityTempRoot%\Editor\Unity.exe" (
  echo Unity not found - please install Unity %UnityVersion% ^(or later^) and set %%UNITY_ROOT%% to its install location.
  pause
  exit /b 1
)

echo Opening Unity sample scene %~dp0MRETestBed\Assets\Scenes\%SceneName%.unity
start /b "" "%UnityTempRoot%\Editor\Unity.exe" -openfile "%~dp0MRETestBed\Assets\Scenes\%SceneName%.unity"
if errorlevel 1 (
  echo Failed launching Unity and loading scene.
  pause
  exit /b 1
)

echo ******************************************************************************************
echo Once open in Unity click play
echo ******************************************************************************************

exit /b 0
