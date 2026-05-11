# WGameFramework

This repository is a source-and-docs mirror for reviewing WGameFramework.

Included:

- `Assets/Scripts/MxFramework/` framework source code and Unity `.meta` files
- `Assets/Scripts/MxFramework/Demo/` C# demo rules, runners, and validation glue used to review framework usage patterns
- `Docs/` development documents, task notes, interface notes, and usage guides
- `AGENTS.md` repository collaboration rules

Excluded:

- Unity generated data such as `Library`, `Temp`, `Logs`, and `UserSettings`
- Unity project settings, scenes, art assets, UI assets, runtime configs, FMOD/third-party plugins, and package cache
- SVN/Git workspace metadata from the source checkout

Mirror rule:

- GitHub mirrors source and docs that help reviewers understand framework APIs and validation code.
- Demo C# code is included because it documents how framework modules are composed in playable examples.
- Demo scenes, UXML/USS, generated materials, FMOD plugin files, banks, project settings, and other Unity assets stay in the SVN project only.

The full Unity project remains in the original SVN workspace. This Git mirror is for code review, documentation review, and agent context sharing, not for opening a complete Unity project.
