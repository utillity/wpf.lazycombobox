del /q ..\Releases\*.nupkg
cd ..\LazyComboBox.WPF
..\nuget\nuget pack LazyComboBox.WPF.csproj -Prop Configuration=Release -OutputDirectory ..\Releases -Build -NonInteractive
pause