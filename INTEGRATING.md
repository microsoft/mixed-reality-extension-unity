# Mixed Reality Extension Unity Integration Guide

Integrating the MRE Unity client library into your own project? Excellent 
choice, friend! 


## Files needed
You should be able to just copy the following folders into your own Unity
Host App
* `MRETestBed\Assets\Assemblies`
* `MRETestBed\Assets\Resources`
* `MRETestBed\Assets\TestBed Assets\Scripts`

And then just instantiate an MREComponent and set the URL to
`ws://mre-hello-world.azurewebsites.net`


## Verifying an Integration
We try to always have MREs deployed here for the latest master build:
* `ws://mre-hello-world.azurewebsites.net`
* `ws://mre-solar-system.azurewebsites.net`
* `ws://mre-functional-tests.azurewebsites.net`

Run through all the tests in the functional test MRE to see if everything
works.


## Back compatibility and Updates
One of the most important considerations when implementing the MRE SDK into a 
title is compatibility. Once the MRE SDK exits beta stage, the MRE Unity client
library will guarantee backwards compatiblity with old MRE protocols. This
means any MRE deployed will always run. However, back compatibility only works
one way. As the MRE feature set grows, any host app will need to take regular 
client library updates to keep up-to-date. The MRE SDK will simply reject
connecting to a host app if it uses an outdated client library. 

There are therefore two important host app implementation details
* properly recognize server rejections caused by versioning, and to message to 
the user that they need to update their host app.
* commit to regularly pull in an updated client library.

We have not yet established a release cadence for client libraries and SDK
updates, but after exiting beta stage there will be a grace period between
client library updates and matching sdk updates. 


## Auto-updating MREUnityRuntimeLib DLL
In the MREUnityRuntimeLib solution directory, there is a `MREUnityProjects.xml`
file (generated automatically if it isn't found). This is used to automatically
copy DLLs into a unity project's assemblies folder whenever the DLLs are
recompiled. You should add an additional path (ProjectTargetDir) in the xml
file, for where you want to copy the compiled DLLs.


## Unknown References compiler error
If you got compiler errors (unknown references), it may be because you are 
using different version of Unity - no problem, but you may need to add 
additional hintpaths in two locations in two csproj files. Open the project 
file in a text editor:

* `MREUnityRuntime\MREUnityRuntimeLib\MREUnityRuntimeLib.csproj`

In this file, find the tags `<Reference Include="UnityEditor">` and 
`<Reference Include="UnityEngine">`. These contain absolute paths to your 
Unity installation directory and specific DLLs. Copy-paste new `HintPath` 
lines, filling in your specific installation's absolute paths.  Currently 
there is a checked in version of the `UnityEngine.dll` in the `Libraries`
directory so this should work for you without any work.



## Feedback
We would love to hear about the experience, positive or negative! To report
issues and feature requests: [Github issues page](
https://github.com/microsoft/mixed-reality-extension-sdk/issues). To chat with
the team and other users: join the [MRE SDK discord community](
https://discord.gg/ypvBkWz).






---
## Legal
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
