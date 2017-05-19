@echo off
echo Creating NugetPackage based on Assembly Version
echo %~dp0OutputNugetPackage
MKDIR %~dp0OutputNugetPackage
nuget pack %~dp0LazyComboBox.WPF.csproj -OutputDir %~dp0OutputNugetPackage -Prop Configuration=Release


if NOT ["%errorlevel%"]==["0"] (
    pause
    exit /b %errorlevel%
)