# Visual Studio 2022 Debugger Freezes #

Wow, blogging with any kind of regularity is hard I kind of hard I guess. Who knew?

Anyway this was just a quick note for a problem I had recently.

I found that my Visual Studio 2022 debugger would hang, freeze or become unresponsive intermittently.
Prior to the last update the process would freeze completely, however even after updating it would
freeze though the IDE itself would remain responsive.

This freeze seemed to trigger more frequently when using time-travel debugging.

As usual when weird things happen the antivirus was to blame.

Excluding the process named `VsDebugConsole.exe` seemed to resolve at least all the debugger hangs
after adding the exclusion. No doubt some more will occur but at least VS22 is usable again.

To get to exclusions on Windows 10 (old-skool) go to Windows Security -> Virus & threat protection ->
Virus & threat protection settings -> Manage settings -> Exclusions -> Add or remove exclusions.
