Aethon's Tools - Common
===

This is a subrepo that other Aethon's Tools repos should include to provide consistency.

Provided here:

### CommonBuild.cake

A Cake build script that will build, test, package and deploy
an Aethon's Tools repo. It is based on a few conventions to make usage easier:

**Uses a Visual Studio 2015 solution**

**Uses .csproj-based projects** (.NET Core will implemented as soon as it is stable)

**All projects are in the **/src** folder under the solution file**

**Test projects use NUnit v3**
