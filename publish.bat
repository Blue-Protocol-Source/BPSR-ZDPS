
dotnet publish "BPSR-ZDPS/BPSR-ZDPS.csproj" -r win-x64 -c Release -o ./publish /p:PublishSingleFile=true /p:PublishTrimmed=false /p:TrimMode=Link /p:IncludeAllContentForSelfExtract=false --self-contained false
move "publish\BPSR-ZDPS.exe" "publish\BPSR-ZDPS.exe"
copy "BPSR-ZDPS\Data" "publish\Data"