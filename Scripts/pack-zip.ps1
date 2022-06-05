Remove-Item ..\QuickLook.Plugin.DDSViewer.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path ..\bin\Release\ -Exclude *.pdb,*.xml,System.*,Microsoft.Win32.Primitives.dll,netstandard.dll
Compress-Archive $files ..\QuickLook.Plugin.DDSViewer.zip
Move-Item ..\QuickLook.Plugin.DDSViewer.zip ..\QuickLook.Plugin.DDSViewer.qlplugin