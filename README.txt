PERSIAN DATE COMPACT v9 - TASKBAR CHILD WINDOW

INSTALL OR UPDATE
1. Extract all six files into one folder.
2. Run Start.cmd with your usual Windows account; administrator rights are not needed.
3. Check the setup message says Version 9. Click OK.
4. Right-click the date: "Version 9 - attached to taskbar" confirms the new code.
Setup closes only PersianDate-*.exe processes from its own installation folder
before starting the new version. Your saved date position is reused and clamped
to the taskbar. Drag the text within the taskbar to reposition it.

VISIBILITY CHANGE
The date is a child window of Shell_TrayWnd, created in the taskbar's DPI context.
It no longer competes with Start as a separate topmost desktop window.
A Windows compatibility manifest enables transparent layered child windows.
The widget checks its host every 250 ms and recreates itself after an Explorer
restart. The context menu still supports black/white text, reset position and Exit.
This is a custom taskbar attachment, not an officially supported taskbar extension.
Windows shell updates or third-party taskbars may require further changes.

STARTUP AND REMOVAL
The existing per-user zero-delay Task Scheduler logon trigger is retained.
The compiled executable runs directly; there is no PowerShell build at sign-in.
Task name: PersianDateCompact-<current-user-SID>.
Installation: %LOCALAPPDATA%/PersianDateCompact/App.
DisableStartup.cmd disables automatic startup. To uninstall, also Exit the
widget and delete the installed App folder.

WINDOWS ACCEPTANCE CHECK
- Confirm Version 9 in the right-click menu.
- Open and close Start using its taskbar button repeatedly, without clicking desktop.
- Repeat with the Windows key. Check the date while Start is open and after closing.
- Drag the date; check right-click and that typing focus stays in the active app.
- Restart Windows and check prompt startup and the saved position.
- If Explorer is restarted, check that the widget reappears automatically.

VALIDATION
Source and package checks only in the Linux build environment. Windows compilation,
Start-menu behavior, DPI behavior and Explorer recovery require a Windows run.
