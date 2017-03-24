# MSBump
MSBuild task that bumps the version of a Visual Studio 2017 project.
This is a work in progress, with more features coming, currently only tested with `.csproj` files.

## Purpose

I'm working on a lot of packages that are referencing each other, but they are in different solutions.
To make my project references maintainable, I use a local NuGet feed, and reference my projects as NuGet packages.
However, in this setup, I'd have to manually change the version every time I change something in a package
that is referenced by other solutions, to make NuGet pull the updates. This task will increment any given
part of the project version.

## Usage

1. Locate your `MSBuild` folder. It is usually `Program Files\Microsoft Visual Studio\2017\(your edition)\MSBuild`.
2. Create a directory named `MSBump` under `MSBuild`, and copy the `.dll` and `.targets` files.
3. Edit your project file

4. Import `MSBump.targets`

```xml
<Project>
  <Import Project="$(MSBuildExtensionsPath)\MSBump\MSBump.targets" />
```

5. Create a `Target` that runs after the build (doesn't really matter when it runs)

```xml
<Target Name="MyAfterBuild" AfterTargets="Build">
  <BumpVersion ProjectPath="$(ProjectPath)" Revision="Increment"/>
</Target>
```
The above example will increment the revision number of the project after every build.

The `BumpVersion` task accepts the following attributes:

### `ProjectPath`
The full path of the project file.

### `Major`, `Minor`, `Patch` and `Revision`
These attributes control which part of the version is changed. 
To increment a specific part, add the corresponding attribute with the value `Increment`.
If no such attributes are defined, the task will not change the version.

### `Label`, and `LabelDigits`
With these attributes, the task will add or increment a release label. Labels must be alphanumeric, and must not end in a digit.

Example:
```xml
  <BumpVersion ProjectPath="$(ProjectPath)" Label="ci" LabelDigits="6"/>
```

When the task is run, it will look for a release label that begins with the given string, 
and ends with an integer, and creates a new one, if no such label exists. 
The task then increments the numeric portion of the label. 
The above example, when run on a package with version `1.2.3-ci000007` will change the version to `1.2.3-ci000008`.
The `LabelDigits` attribute controls the length of the appended number (6 by default).

## Why is my NuGet package version one step behind the project version?

This is because by the time the task can save the changed version to the project file,
MSBuild has already loaded the file. You can think of your project version as the *next* 
version of your NuGet package. If you insist on having the same version on both ends,
you can execute `NuGet pack` in a task after `BumpVersion` so that NuGet.exe can load the modified project file.

