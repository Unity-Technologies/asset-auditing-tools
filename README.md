# This repo is deprecated and development has stopped on this

# Asset Auditing Tools
As a Unity project gets bigger, it can get hard to maintain the many Assets within a Project.
This tool is designed to alleviate the common use of hard coding lots AssetPostprocessor classes.

### Important note
In order for Pre and Post processing tasks to work in this system, editing the AssetImporter.userData of assets is required.
This is also tricky for possible custom tasks.

When using the cache server it stores the imported asset by information must be known before importing.
Such as the GetVersion of the AssetPostprocessor classes and binary of the file + meta file. For our ImportTasks to work correctly with the cache server.
The tasks write to the .userData of the meta file changing its hash for storing. Allowing it to work nicely with the cache server.

I really do not like this, as other code may overwrite the .userData destroying the usage of this tool.
New features are coming to Unity in future versions to get around this.

## Import Definition Profiles
Import Definition Profiles are a way of defining how Assets are imported. Using a set of filters to determine if the profile relates to those Assets.
A set of Import Tasks that can be added onto profiles to define how the filtered Assets should be imported.

Some basic Import Tasks will be included. Design is intended for custom import tasks to be implemented for specific project needs.

[Manual Page](Documentation/ImportDefinitionProfiles.md) (TODO)

## Auditor Window
This tool is intended as a way to search through your project for Assets. Comparing them against Import Definition Profiles within your project.
Through this window you can easily see an Asset that has not been imported in a way you expect and correct.

ImportDefinitionProfiles are used to determine which profiles are displayed in the view. If run on import is enabled, then it is expected that
all the Asset will automatically be in the correct state.

The Auditor Window can be used for finer control and to observe where changes are occuring.

[Manual Page](Documentation/AssetAuditor.md) (Out of date)

## Disclaimer
This is a project developed by a member of the Enterprise Support team and not maintained by Unity's roadmap. (I update and maintain when I can)
Feedback and requests are more than welcome, email: andrewmi@unity3d.com

## Road Map

General
- Branch out 2017 and 2018 versions, ready for utilising new ADV2 feature
- Setup for package manager

Import Definition Profiles
- New Import Tasks
    - 2018+ new task "PresetPropertiesImportTask", this will be good for both importer properties and other objects
- Look into if MonoScript cache should be used for IPreprocessor. Paving the path to no longer being reliant on AssetImporter.userData for importer versioning. With AssetDatabase V2 we can set custom dependencies for Assets. Allowing an Asset to have a dependency on the MonoScript.
    - This would still need a way of saving what versions they were previously imported with for auditing. 
- Track movement and changes to profiles to reimport any Assets where necessary.
- Look into custom Importer for the ScriptableObject for IDF. So it can Apply -> Then allowing reimport any required Assets on Apply.
    - Need to investigate import order and if this is needed to effect that
- Order task into Pre and Post import sections
- rework UX of the profile inspector gui

Auditor Window
- Multiselect and fix working
- Ability to fix a folder
