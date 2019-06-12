# Asset Auditing Tools
Editor tool's to audit project assets and correct errors

## Auditor Window
This tool is intended as a way to search your project for Assets, and compare against a Template asset to check for inconsistences between properties of the Asset and the Template.

// TOOD update the window docs, maybe wait until refactoring done.

[Manual Page](Documentation/AssetAuditor.md)

## Tasks

General
- Rename to Import Definition Files
- Drop support for 2017 and upgrade to 2018 LTS
- Use Preset's as a subObject (and display) for ImporterModule
- Look into if MonoScript cache should be used for IPreprocessor. Paving the path to no longer being reliant on AssetImporter.userData for importer versioning. With AssetDatabase V2 we can set custom dependencies for Assets. Allowing an Asset to have a dependency on the MonoScript.
- Track movement and changes to IDF (profiles) to reimport any Assets where necessary.
- Look into custom Importer for the ScriptableObject for IDF. So it can Apply -> Then allowing reimport any required Assets on Apply.
- Ordering of modules
- injection system so modules can share data, and have a way of progressing through an order. This may not be necessary for majority of use-cases. But maybe?

Window
- Replace PropertyDetailView with simply replacing with DetailedView. To display elements of all
- Multiselect and fix working.
- Ability to fix a folder
- Display modules ConformObjectData in the bottom area
