# Mixed Reality Extension SDK Unity

<img width='200' height='200' 
src='https://github.com/Microsoft/mixed-reality-extension-sdk/blob/master/branding/MRe-RGB.png'/>

The Mixed Reality Extension SDK Unity library makes it easy for any Unity3D app or game to support user generated content (UGC) built with the [Mixed Reality Extension SDK](
https://github.com/Microsoft/mixed-reality-extension-sdk). It also makes it easy to test an MRE within a simple environment inside the Unity3D editor.


## Prerequisites
* Install Git
* Use [Unity Hub](https://store.unity.com/) to Install Unity 2018.1.9f2 (or later) - Set environment variable UNITY_ROOT to the installation path.
* Install [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/), including Visual Studio Tools for Unity and .NET development tools


## How to build and run Hello World in the Unity3D editor
From command prompt:
* `git clone http://github.com/microsoft/mixed-reality-extension-unity`
* Run `buildMREUnityRuntimeDLL.bat`
* Open Unity3D, project folder `mixed-reality-extension-unity/MRETestBed`
* open the scene `Assets/Scenes/HelloWorld.unity`
* click play
You should now see a slowly spinning Hello World label and a clickable cube.


## Scene Descriptions
The MRETestbed project contains 4 Unity3d scenes set up for different testing purposes
* `HelloWorld.Unity`: Connects to a single MRE in the cloud on startup, no interaction needed
* `FunctionalTest-localhost.Unity`. Requires a locally deployed functional test MRE from the [SDK Repository](https://github.com/Microsoft/mixed-reality-extension-sdk#How-to-Build-and-Deploy-the-SDK-functional-tests). Generates a launch pad for each of the functional tests - just walk close to trigger them. Or touch the "run all" trigger to load every single functional test. There is also a pad for changing the global root - it moves, scales, and rotates everything in the world, which simplifies checking the 3d math.
* `TriggerVolumeTestBed-localhost.Unity`: Connects to a localhost MRE when you walk close - useful for testing user join/leave.
* `SynchronizationTest-localhost.Unity`: Connects twice to twice to a localhost MREs with the same session ID. When you click on the two spheres you will see 2 different connections to the same server, so you can perform basic multiuser testing without multiple machines or multiple unity instances.

The Localhost samples requires a local node server running, see the [Sample repository](
https://github.com/Microsoft/mixed-reality-extension-sdk-samples#How-to-Build-and-Run-the-Hello-World-sample) for localhost deployment.


## To Debug the Unity Runtime DLL 
* From within the Unity3D editor click `Assets->Open C# Project`. This opens Visual Studio and generates a solution file and project files
* In the Solution Explorer, right click on `Solution 'MRETestBed' (3 projects)`, click `Add->Existing Project...`, and select MREUnityRuntime\MREUnityRuntimeLib\MREUnityRuntimeLib.csproj
* Press play in the editor
* Select `Debug->Attach Unity Debugger` (requires the Visual Studio Unity Tools installed to show up), and choose Project MRETestBed, type Editor

Putting breakpoints inside the MREUnityRuntimeLib DLL is not always working, but pressing stop in Unity Editor, rebuilding the MREUnityRuntimeLib project in Visual Studio, and pressing play in the Unity Editor tends to fix it.


## Integration guide
If you want to integrate the MRE SDK into your own Unity3D project, please see [INTEGRATING.md](INTEGRATING.md)


## Overview
* For more information, please see 
the [Mixed Reality Extension SDK](
https://github.com/Microsoft/mixed-reality-extension-sdk) repository's [README.md](https://github.com/Microsoft/mixed-reality-extension-sdk/blob/master/README.md) is the best source of information about features, current state, limitations, goal, major known issues, and roadmap.
* We welcome contributions. Please see [CONTRIBUTING.md](CONTRIBUTING.md)
if you are interested in helping out.

## Getting In Touch
To report issues and feature requests: [Github issues page](
https://github.com/microsoft/mixed-reality-extension-sdk/issues).

To chat with the team and other users: join the [MRE SDK discord community](
https://discord.gg/ypvBkWz).

Or attend the biweekly [AltspaceVR developer meetups](
https://account.altvr.com/channels/altspacevr).

---
## Reporting Security Issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security
Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should
receive a response within 24 hours. If for some reason you do not, please follow up via
email to ensure we received your original message. Further information, including the
[MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in
the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).


## License
Code licensed under the [MIT License](https://github.com/Microsoft/mixed-reality-extension-sdk-unity/blob/master/LICENSE.txt).


## Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
