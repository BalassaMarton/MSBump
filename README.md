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

### `BumpResetMajor`, `BumpResetMinor`, `BumpResetPatch`, `BumpResetRevision` or `BumpResetLabel`

These properties will reset any part of the version. Major, Minor, Patch and Revision is reset to 0. When `BumpResetLabel` is used, the specified label is removed from the version.

Example - Increment the revision number on every Release build, add an incrementing `dev` label on Debug builds.
```xml
  <PropertyGroup Condition=" '$(Configuration)'=='Debug'">
    <BumpLabel>dev</BumpLabel>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)'=='Release'">
    <BumpResetLabel>dev</BumpResetLabel>
    <BumpRevision>True</BumpRevision>
  </PropertyGroup>
```

Reset attributes are prioritized over Bump attributes.

## Usage (standalone)

This might be more convenient when working with a lot of packages that reference each other as NuGet packages.

1. Locate your `MSBuild` folder. It is usually `Program Files\Microsoft Visual Studio\2017\(your edition)\MSBuild`.
2. Extract the contents of the zip file to this folder (you should end up with an `MSBump` folder under `MSBuild`, with `.dll` and `.targets` files.
3. Edit your project file
4. Import `MSBump.Standalone.targets`

```xml
<Project>
  <Import Project="$(MSBuildExtensionsPath)\MSBump\MSBump.Standalone.targets" />
```

5. Create a `Target` that runs before the Build task

```xml
  <Target Name="BumpBeforeBuild" BeforeTargets="Build">
    <BumpVersion ProjectPath="$(ProjectPath)" BumpRevision="True">
      <Output TaskParameter="NewVersion" PropertyName="Version" />
      <Output TaskParameter="NewVersion" PropertyName="PackageVersion" />
    </BumpVersion>
  </Target>
```

But how is this better than adding a NuGet package to my projects? Wait for it... 

MSBuild 15 can load `.props` and `.targets` files automatically, given they have the special name `Directory.Build.props` and `Directory.Build.targets`. We can enforce repositry-wide build processes by simply placing the build script in a `Directory.Build.targets` file at the root of the repository.
Example - Increment revision when releasing, add a `dev` label when debugging, and push the generated NuGet package to a local folder:

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <PropertyGroup>
      <LocalNugetDirectory> Path to your local nuget feed </LocalNugetDirectory>
    </PropertyGroup>

    <!-- Import MSBump -->

    <Import Project="$(MSBuildExtensionsPath)\MSBump\MSBump.Standalone.targets" />
    
    <!-- Increment dev label on Debug build, increment the revision number on Release build -->

    <Target Name="BumpVersionBeforeDebugBuild" BeforeTargets="Build" Condition=" '$(Configuration)'=='Debug' And $(MSBuildProjectExtension) == '.csproj' ">
        <BumpVersion ProjectPath="$(ProjectPath)" BumpLabel="dev">
            <Output TaskParameter="NewVersion" PropertyName="Version" />
            <Output TaskParameter="NewVersion" PropertyName="PackageVersion" />
        </BumpVersion>
    </Target>

    <Target Name="BumpVersionBeforeReleaseBuild" BeforeTargets="Build" Condition=" '$(Configuration)'=='Release' And $(MSBuildProjectExtension) == '.csproj' ">
        <BumpVersion ProjectPath="$(ProjectPath)" ResetLabel="dev" BumpRevision="True">
            <Output TaskParameter="NewVersion" PropertyName="Version" />
            <Output TaskParameter="NewVersion" PropertyName="PackageVersion" />
        </BumpVersion>
    </Target>

    <!-- Push the package to the local nuget feed -->

    <Target Name="CopyPackage" AfterTargets="Pack" Condition=" $(MSBuildProjectExtension) == '.csproj' ">
        <Copy SourceFiles="$(OutputPath)..\$(PackageId).$(PackageVersion).nupkg" DestinationFolder="$(LocalNugetDirectory)" />
    </Target>

</Project>
```

We don't need to add anything to the project files.

The `BumpVersion` task accepts the following attributes (description at the NuGet version):

`ProjectPath`, `BumpMajor`, `BumpMinor`, `BumpPatch`, `BumpRevision`, `BumpLabel`, `LabelDigits`, `ResetMajor`, `ResetMinor`, `ResetPatch`, `RestRevision`, `ResetLabel`.

## Version history

### 2.1.0 (2017-08-12)

* MSBump now correctly bumps the version before build and pack, the built and packaged project always has the same version as the project file.

### 2.0.0 (2017-04-26)

* Added NuGet package
* `Major`, `Minor`, `Patch` and `Revision` are now simple boolean properties.

### 1.0.0
Initial standalone version