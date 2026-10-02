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
    Add-Type -TypeDefinition $source -ReferencedAssemblies System.Windows.Forms,System.Drawing -OutputAssembly $executablePath -OutputType WindowsApplication
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
    [System.Windows.Forms.MessageBox]::Show('Immediate logon startup enabled. The old Startup-folder shortcut has been replaced with a zero-delay sign-in task.', 'Persian Date') | Out-Null
    $null = $verified.Run($null)
} catch {
    [System.Windows.Forms.MessageBox]::Show(('Setup could not complete: ' + $_.Exception.Message + "`nSend a screenshot of this error. No global Windows startup settings were changed."), 'Persian Date - setup error') | Out-Null
    exit 1
}
