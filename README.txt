PERSIAN DATE COMPACT v7 - DIRECT LOGON TASK

1. Right-click the running date > Exit.
2. Extract ALL ZIP files. Double-click Start.cmd using your usual Windows account.
3. Wait for "Immediate logon startup enabled" and click OK.
   If setup reports an error, send a screenshot; do not assume it succeeded.

This release replaces the Startup-folder shortcut with a Task Scheduler logon
trigger for the current user, with Delay=PT0S. It runs the compiled EXE directly
in the interactive session, at normal privilege and normal process priority.
There is no sleep, network requirement, idle wait or AC-power requirement.
The previous shortcut is removed only after the new task registers successfully.
No global startup settings or other applications' tasks are changed.

The app remains in %LOCALAPPDATA%/PersianDateCompact/App. Files can be removed
from the download location after setup. Appearance and saved position are unchanged.
Task name: PersianDateCompact-<current-user-SID>.
DisableStartup.cmd removes this task and the previous owned shortcut.
To uninstall: run DisableStartup.cmd, Exit the widget, delete the installed App folder.

Windows controls Task Scheduler and Explorer startup. A zero-delay logon trigger
requests the earliest sign-in launch; it does not guarantee frame-exact timing
with taskbar icons. The widget's 250ms refresh handles taskbar appearance.
Task registration can be restricted by local policy; setup reports that error.

Validation: ZIP and source checks completed on Linux. Windows task registration,
compiled executable execution and reboot latency have not been tested here.
