# MSBump
MSBump is a MSBuild 15 task that bumps the version of a Visual Studio 2017 project.
Currently only tested on `.csproj` files.

## Purpose

I'm working on a lot of packages that are referencing each other, but they are in different solutions.
To make my project references maintainable, I use a local NuGet feed, and reference my projects as NuGet packages.
However, in this setup, I'd have to manually change the version every time I change something in a package
that is referenced by other solutions, to make NuGet pull the updates. This task will increment any given
part of the project version.

## Usage (as a NuGet package)

1. Add the `MSBump` NuGet package to your project.
2. Edit the project file. Make sure the file has a `<Version>` property.
3. Create a `.msbump` settings file (see below) OR go to step 5 of the next section.

Note: Until [this](https://github.com/NuGet/Home/issues/4125) NuGet issue is fixed, you should add `PrivateAssets="All"` to the `PackageReference` declaration,
otherwise your package will list `MSBump` as a dependency.

## Usage (standalone)

1. Locate your `MSBuild` folder. It is usually `Program Files\Microsoft Visual Studio\2017\(your edition)\MSBuild`.
2. Extract the contents of the zip file to this folder (you should end up with an `MSBump` folder under `MSBuild`, with `.dll` and `.targets` files.
3. Edit your project file OR `Directory.Build.targets` file, if you want to enforce these build settings to a solution or entire repository (see [the MSBuild documentation](https://docs.microsoft.com/en-us/visualstudio/msbuild/what-s-new-in-msbuild-15-0))
4. Import `MSBump.Standalone.targets`

```xml
  <Import Project="$(MSBuildExtensionsPath)\MSBump\MSBump.Standalone.targets" />
```

5. Add properties that control the MSBump task. The following table contains the property names that the MSBump target uses. 
For details about each property, see the next section.

|Property name|Task attribute|
|-------------|--------------------------------|
|BumpMajor|BumpMajor|
|BumpMinor|BumpMinor|
|BumpPatch|BumpPatch|
|BumpRevision|BumpRevision|
|BumpLabel|BumpLabel|
|BumpLabelDigits|LabelDigits|
|BumpResetMajor|ResetMajor|
|BumpResetMinor|ResetMinor|
|BumpResetPatch|ResetPatch|
|BumpResetRevision|ResetRevision|
|BumpResetLabel|ResetLabel|

Example - Increment revision on every Release build, add a label on Debug builds.
```xml
  <PropertyGroup Condition=" '$(Configuration)'=='Debug' ">
    <BumpLabel>dev</BumpLabel>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)'=='Release' ">
    <BumpRevision>True</BumpRevision>
    <BumpResetLabel>dev</BumpResetLabel>
  </PropertyGroup>
```

## Settings

MSBump settings can be declared in a separate `.msbump` file.
This file must be placed next to the project file, and must have the same name as the project file, but with the `.msbump` extension.
The file itself is a JSON file that contains the properties for the task object. 
When per-configuration settings are desireable, the settings file should be structured like this:
```js
{
  Configurations: {
    "Debug": {
      /* properties */
    },
    
    "Release": {
      /* properties */
    }
  }
}
```

Note that when a `.msbump` file is present, all other properties declared in `.targets` files are ignored for the current project. This is helpful when we use repository-wide MSBump configuration, but want to override this behavior for some projects. 

The settings file should contain any of the following properties:

### `BumpMajor`, `BumpMinor`, `BumpPatch` and `BumpRevision`
These boolean properties control which part of the version is changed. 
To increment a specific part, add the corresponding property with true value.

Example - increment the revision number:
```js
{
  BumpRevision: true
}    
```
From an initial version of `1.0.0`, hitting build multiple times will change the version to `1.0.0.1`, `1.0.0.2`, etc.

### `BumpLabel` and `LabelDigits`
Using these properties, the task will add or increment a release label. Labels must be alphanumeric, and must not end in a digit. `LabelDigits` defaults to 6 if not specified.

Example - add a `dev` label with a 4-digit counter on every build:
```js
{
  BumpLabel: "dev",
  LabelDigits: 4
}
```

### `ResetMajor`, `ResetMinor`, `ResetPatch`, `ResetRevision` and `ResetLabel`

These properties will reset any part of the version. Major, Minor, Patch and Revision is reset to 0. When `ResetLabel` is used, the specified label is removed from the version.

Example - Increment the revision number on every Release build, add `dev` label with a 4-digit counter on Debug builds.
```js
{
  Configurations: {
    "Debug": {
      BumpLabel: "dev"
      LabelDigits: 4
    },
    
    "Release": {
      BumpRevision: true,
      ResetLabel: "dev"
    }
  }
}
```

Reset properties are prioritized over Bump properties.

## Usage (as a NuGet package, older versions)

Edit the project file. Add any of the properties listed for the standalone version.

## Using the `BumpVersion` task directly

When the above methods are not optimal, we can always use MSBump as a build task in a MSBuild project file. 
For this, we need to
1. Import the standalone version
2. Clear or modify the `BumpVersionBeforeBuild` target so that MSBump is not executed automatically before every build.
3. Call the task manually. Task parameter names are the same as `.msbump` configuration properties. 

Example - Increment the revision number before build if a condition is set

```xml
  <Target Name="MyBeforeBuild" BeforeTargets="Build" Condition="$(MyCondition) == 'OK' ">
    <BumpVersion
    ProjectPath="$(ProjectPath)" 
        BumpRevision="True">
        
        <Output TaskParameter="NewVersion" PropertyName="Version" />
        <Output TaskParameter="NewVersion" PropertyName="PackageVersion" />
        
    </BumpVersion>
  </Target>
```

Don't forget to output the new version if other targets may depend on these changed versions.
Otherwise, MSBuild will use the properties in the project file before it was saved by MSBump.


## Version history

### 2.3.0 (2017-08-19)

* .NET Standard support. MSBump now works with `dotnet build`.

### 2.2.0 (2017-08-15)

* Added support for settings file. No need to modify the project file at all when using the NuGet version.
* Cleaned up `.targets` files so that the standalone and NuGet version can work side by side.


### 2.1.0 (2017-08-12)

* MSBump now correctly bumps the version before build and pack, the built and packaged project always has the same version as the project file.

### 2.0.0 (2017-04-26)

* Added NuGet package
* `Major`, `Minor`, `Patch` and `Revision` are now simple boolean properties.

### 1.0.0
Initial standalone version