# MSBump
MSBuild task that bumps the version of a Visual Studio 2017 project.
This is a work in progress, with more features coming, currently only tested with `.csproj` files.

## Purpose

I'm working on a lot of packages that are referencing each other, but they are in different solutions.
To make my project references maintainable, I use a local NuGet feed, and reference my projects as NuGet packages.
However, in this setup, I'd have to manually change the version every time I change something in a package
that is referenced by other solutions, to make NuGet pull the updates. This task will increment any given
part of the project version.

## Usage (as a NuGet package)

1. Add the `MSBump` NuGet package to your project.
2. Edit the project file. Make sure the file has a `<Version>` property.
3. Add one of the following properties:

### `BumpMajor`, `BumpMinor`, `BumpPatch` or `BumpRevision`
These boolean properties control which part of the version is changed. 
To increment a specific part, add the corresponding property with `True` value.

Example - increment the revision number after every release build:
```xml
    <PropertyGroup Condition=" '$(Configuration)'=='Release'">
        <BumpRevision>True</BumpRevision>
    </PropertyGroup>
```
From an initial version of `1.0.0`, hitting build multiple times will change the version to `1.0.0.1`, `1.0.0.2`, etc.

### `BumpLabel` and `BumpLabelDigits`
Using these properties, the task will add or increment a release label. Labels must be alphanumeric, and must not end in a digit. `BumpLabelDigits` defaults to 6 if not specified.

Example - add a `dev` label with a 4-digit counter on every build:
```xml
    <PropertyGroup>
        <BumpLabel>dev</BumpLabel>
        <BumpLabelDigits>4</BumpLabelDigits>
    </PropertyGroup>   
```

From an initial version of `1.0.0`, hitting build multiple times will change the version to `1.0.0-dev0001`, `1.0.0-dev0002`, etc.


## Usage (standalone)

1. Locate your `MSBuild` folder. It is usually `Program Files\Microsoft Visual Studio\2017\(your edition)\MSBuild`.
2. Extract the contents of the zip file to this folder (you should end up with an `MSBump` folder under `MSBuild`, with `.dll` and `.targets` files.
3. Edit your project file
4. Import `MSBump.Standalone.targets`

```xml
<Project>
  <Import Project="$(MSBuildExtensionsPath)\MSBump\MSBump.Standalone.targets" />
```

5. Create a `Target` that runs after the build (doesn't really matter when it runs)

```xml
<Target Name="MyAfterBuild" AfterTargets="Build">
  <BumpVersion ProjectPath="$(ProjectPath)" Revision="True"/>
</Target>
```
The above example will increment the revision number of the project after every build.

The `BumpVersion` task accepts the following attributes:

### `ProjectPath`
The full path of the project file.

### `Major`, `Minor`, `Patch` and `Revision`
These boolean attributes control which part of the version is changed. 
To increment a specific part, add the corresponding attribute with `True` value.

Example - increment the revision number after every release build:
```xml
<BumpVersion ProjectPath="$(ProjectPath)" Revision="True"/>
```
From an initial version of `1.0.0`, hitting build multiple times will change the version to `1.0.0.1`, `1.0.0.2`, etc.

### `Label` and `LabelDigits`
Using these attributes, the task will add or increment a release label. Labels must be alphanumeric, and must not end in a digit. `LabelDigits` defaults to 6 if not specified.

Example - add a `dev` label with a 4-digit counter on every build:
```xml
<BumpVersion ProjectPath="$(ProjectPath)" Label="dev" LabelDigits="4"/>
```

From an initial version of `1.0.0`, hitting build multiple times will change the version to `1.0.0-dev0001`, `1.0.0-dev0002`, etc.

## Why is my NuGet package version one step behind the project version?

This is because by the time the task can save the changed version to the project file,
MSBuild has already loaded the file. You can think of your project version as the *next* 
version of your NuGet package. If you insist on having the same version on both ends,
you can execute `NuGet pack` in a task after `BumpVersion` so that NuGet.exe can load the modified project file.

## Version history

### 2.0.0 (2017-04-26)

* Added NuGet package
* `Major`, `Minor`, `Patch` and `Revision` are now simple boolean properties.

### 1.0.0
Initial standalone version