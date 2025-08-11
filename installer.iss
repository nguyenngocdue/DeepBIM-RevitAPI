[Setup]
AppId={{A3F8C2C1-2B8E-4A41-95E9-8B6E9D9CDE11}
AppName=DeepBIM
AppVersion=1.0.0
DefaultDirName={userappdata}\Autodesk\Revit\Addins\2025
DisableDirPage=yes
OutputBaseFilename=DeepBIM-Setup-2025
OutputDir=Output
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
CloseApplications=force
CloseApplicationsFilter=Revit.exe

[Files]
; Copy cả thư mục DeepBim (giữ nguyên tên thư mục)
Source: "B:\C# Tool Revit\DeepBIM\Running\DeepBim\*"; \
    DestDir: "{userappdata}\Autodesk\Revit\Addins\2025\DeepBim"; \
    Flags: ignoreversion recursesubdirs createallsubdirs restartreplace

; Copy file .addin vào gốc Addins\2025
Source: "B:\C# Tool Revit\DeepBIM\Running\DeepBIM.addin"; \
    DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; \
    Flags: ignoreversion restartreplace

[UninstallDelete]
; Xóa thư mục DeepBim nếu trống sau khi gỡ
Type: dirifempty; Name: "{userappdata}\Autodesk\Revit\Addins\2025\DeepBim"
