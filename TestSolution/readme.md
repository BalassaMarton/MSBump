# Test solution for MSBump

This is a solution for testing MSBump. Project dependencies:
B -> A
C -> A, B

After building this solution, the following conditions must all be met:
* Version is bumped in all projects
* Produced NuGet packages reference the latest version of these projects
* The multitargeting project's version is bumped only once
