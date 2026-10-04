Option Explicit
Dim shell, fso, sourceDir, dataDir, installDir, linkPath, mode, item, executable, command
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
sourceDir = fso.GetParentFolderName(WScript.ScriptFullName)
linkPath = fso.BuildPath(shell.SpecialFolders("Startup"), "PersianDateCompact.lnk")
mode = ""
If WScript.Arguments.Count > 0 Then mode = LCase(WScript.Arguments(0))
If mode = "--uninstall" Then
    executable = shell.ExpandEnvironmentStrings("%SystemRoot%") & "\System32\WindowsPowerShell\v1.0\powershell.exe"
    command = Chr(34) & executable & Chr(34) & " -NoProfile -STA -WindowStyle Hidden -ExecutionPolicy Bypass -File " & Chr(34) & fso.BuildPath(sourceDir, "Build.ps1") & Chr(34) & " -DisableStartup"
    shell.Run command, 0, False
    WScript.Quit
End If
On Error Resume Next
dataDir = fso.BuildPath(shell.ExpandEnvironmentStrings("%LOCALAPPDATA%"), "PersianDateCompact")
installDir = fso.BuildPath(dataDir, "App")
If Not fso.FolderExists(dataDir) Then fso.CreateFolder dataDir
If Not fso.FolderExists(installDir) Then fso.CreateFolder installDir
If Err.Number <> 0 Then Fail "Could not create installation folder"
For Each item In Array("Widget.cs", "Build.ps1", "Launch.vbs", "Start.cmd", "DisableStartup.cmd")
    If Not fso.FileExists(fso.BuildPath(sourceDir, item)) Then
        MsgBox "Missing " & item & ". Extract ALL files before running Start.cmd.", 16, "Persian Date"
        WScript.Quit 1
    End If
Next
If LCase(fso.GetAbsolutePathName(sourceDir)) <> LCase(fso.GetAbsolutePathName(installDir)) Then
    For Each item In Array("Widget.cs", "Build.ps1", "Launch.vbs", "Start.cmd", "DisableStartup.cmd")
        fso.CopyFile fso.BuildPath(sourceDir, item), fso.BuildPath(installDir, item), True
        If Err.Number <> 0 Then Fail "Could not copy " & item
    Next
End If
On Error GoTo 0
executable = shell.ExpandEnvironmentStrings("%SystemRoot%") & "\System32\WindowsPowerShell\v1.0\powershell.exe"
command = Chr(34) & executable & Chr(34) & " -NoProfile -STA -WindowStyle Hidden -ExecutionPolicy Bypass -File " & Chr(34) & fso.BuildPath(installDir, "Build.ps1") & Chr(34)
shell.Run command, 0, False
Sub Fail(context)
    MsgBox context & ": " & Err.Description, 16, "Persian Date"
    WScript.Quit 1
End Sub
