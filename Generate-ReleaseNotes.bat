@echo off
setlocal

for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "[xml]$props = Get-Content 'Directory.Build.props'; $props.Project.PropertyGroup | ForEach-Object { $_.VersionPrefix } | Where-Object { $_ } | Select-Object -First 1"`) do set version=%%v

if "%version%"=="" (
	echo Unable to determine VersionPrefix from Directory.Build.props.
	exit /b 1
)

GitHubReleaseNotes --output ReleaseNotes.md --skip-empty-releases --exclude-labels question invalid documentation --version %version%
if errorlevel 1 exit /b %errorlevel%

GitHubReleaseNotes --output PackageReleaseNotes.txt --skip-empty-releases --exclude-labels question invalid documentation --template PackageReleaseNotes.template --version %version%
if errorlevel 1 exit /b %errorlevel%