param([switch]$DisableStartup)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    $appDirectory = $PSScriptRoot
    $sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $taskName = 'PersianDateCompact-' + $sid
    $shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'PersianDateCompact.lnk'
    $scheduler = New-Object -ComObject Schedule.Service
    $scheduler.Connect()
    $root = $scheduler.GetFolder('\')
    if ($DisableStartup) {
        foreach ($existing in $root.GetTasks(0)) {
            if ($existing.Name -eq $taskName) { $root.DeleteTask($taskName, 0); break }
        }
        if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
        [System.Windows.Forms.MessageBox]::Show('Automatic startup disabled. The current widget can be closed with right-click > Exit.', 'Persian Date') | Out-Null
        exit 0
    }
    $source = [IO.File]::ReadAllText((Join-Path $appDirectory 'Widget.cs'))
    $executablePath = Join-Path $appDirectory ('PersianDate-' + [Guid]::NewGuid().ToString('N') + '.exe')
    # Layered child windows require a Windows 8+ compatibility manifest.
    $manifestPath = Join-Path $appDirectory 'widget.manifest'
    $manifest = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <assemblyIdentity version="9.0.0.0" name="PersianDateCompact" type="win32" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
    <security><requestedPrivileges>
      <requestedExecutionLevel level="asInvoker" uiAccess="false" />
    </requestedPrivileges></security>
  </trustInfo>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}" />
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
  <application xmlns="urn:schemas-microsoft-com:asm.v3"><windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
  </windowsSettings></application>
</assembly>
'@
    [IO.File]::WriteAllText($manifestPath, $manifest)
    $compiler = New-Object Microsoft.CSharp.CSharpCodeProvider
    try {
        $parameters = New-Object System.CodeDom.Compiler.CompilerParameters
        $parameters.GenerateExecutable = $true
        $parameters.GenerateInMemory = $false
        $parameters.OutputAssembly = $executablePath
        $parameters.CompilerOptions = '/target:winexe /optimize+ /win32manifest:"' + $manifestPath + '"'
        foreach ($reference in @('System.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll')) {
            $null = $parameters.ReferencedAssemblies.Add($reference)
        }
        $result = $compiler.CompileAssemblyFromSource($parameters, [string[]]@($source))
        if ($result.Errors.HasErrors) {
            throw (($result.Errors | Where-Object { -not $_.IsWarning } | ForEach-Object { $_.ToString() }) -join "`n")
        }
    } finally { $compiler.Dispose() }

    if (-not (Test-Path -LiteralPath $executablePath)) { throw 'The executable was not created.' }

    # Start in THIS user's interactive desktop, not in the system service session.
    $definition = $scheduler.NewTask(0)
    $definition.RegistrationInfo.Description = 'Display the Persian date immediately at user sign-in.'
    $definition.Principal.UserId = $sid
    $definition.Principal.LogonType = 3
    $definition.Principal.RunLevel = 0
    $trigger = $definition.Triggers.Create(9)
    $trigger.UserId = $sid
    $trigger.Enabled = $true
    $trigger.Delay = 'PT0S'
    $action = $definition.Actions.Create(0)
    $action.Path = $executablePath
    $action.WorkingDirectory = $appDirectory
    $settings = $definition.Settings
    $settings.Enabled = $true
    $settings.AllowDemandStart = $true
    $settings.StartWhenAvailable = $true
    $settings.DisallowStartIfOnBatteries = $false
    $settings.StopIfGoingOnBatteries = $false
    $settings.RunOnlyIfIdle = $false
    $settings.RunOnlyIfNetworkAvailable = $false
    $settings.ExecutionTimeLimit = 'PT0S'
    $settings.MultipleInstances = 2
    $settings.Priority = 5
    $registered = $root.RegisterTaskDefinition($taskName, $definition, 6, $sid, $null, 3, $null)
    $verified = $root.GetTask($taskName)
    if (-not $verified.Enabled -or $verified.Definition.Actions.Item(1).Path -ne $executablePath) {
        throw 'The sign-in task could not be verified. Send a screenshot of this message.'
    }
    # Retire the slow Startup-folder route only AFTER the replacement is registered.
    if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
    [IO.File]::WriteAllText((Join-Path $appDirectory 'current-executable.txt'), $executablePath)
    [IO.File]::WriteAllText((Join-Path $appDirectory 'startup-task.xml'), $verified.Xml)
    Get-ChildItem -LiteralPath $appDirectory -Filter 'PersianDate-*.exe' | Where-Object { $_.FullName -ne $executablePath } | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -ErrorAction SilentlyContinue
    }
    [System.Windows.Forms.MessageBox]::Show('Version 9 installed: the date attaches directly to the taskbar. Immediate logon startup remains enabled. Click OK to start it.', 'Persian Date') | Out-Null
    # Close only our own installed widget executables so the old mutex holder
    # cannot silently prevent version 9 from starting after an upgrade.
    if ($verified.State -eq 4) { $verified.Stop(0) }
    Get-Process -Name 'PersianDate-*' -ErrorAction SilentlyContinue | ForEach-Object {
        $runningPath = $null
        try { $runningPath = $_.Path } catch { }
        if ($runningPath -and [string]::Equals([IO.Path]::GetDirectoryName($runningPath), $appDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            Stop-Process -Id $_.Id -ErrorAction Stop
            $null = $_.WaitForExit(5000)
        }
    }
    $null = $verified.Run($null)
} catch {
    [System.Windows.Forms.MessageBox]::Show(('Setup could not complete: ' + $_.Exception.Message + "`nSend a screenshot of this error. No global Windows startup settings were changed."), 'Persian Date - setup error') | Out-Null
    exit 1
}
