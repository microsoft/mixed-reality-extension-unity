# Mixed Reality Extension Unity Contribution Guide

Want to help out? Great! Here are a couple of ways to help:

## Build a cool MRE with the SDK itself
Clone the [samples](
https://github.com/microsoft/mixed-reality-extension-sdk-samples) and start
making your own MRE. Try to deploy it and tell us how it went.


## Integrate the MRE Unity DLL into your own Unity3D project
The MRETestbed Unity3D project is a simple implementation. Look at that to see 
how you can implement the SDK into your own project. Run through the functional
test MRE to see if everything works. For more info, please see 
[INTEGRATING.md](INTEGRATING.md).


## Implement features in the MRE SDK and the Unity DLL
New features often require both a SDK side and Unity side set
of changes. We usually implement both sides simultaneously, and add at least
one functional test to verify the functionality. We highly recommend reaching
out to the [MRE SDK discord community](https://discord.gg/ypvBkWz) to discuss 
any architecture before implementing, as we are always stronger together. 


## Write your own MRE Client library for another 3D engine
Great idea. That's quite a bit more work than dropping a DLL into a Unity3D
project, but it is definitely doable! Please reach out to the MRE maintainers,
so we can make sure your client library keeps up to date with future Unity 
client changes.


## Give Feedback
Submit bugs and feature requests on the [issues page](
https://github.com/microsoft/mixed-reality-extension-sdk/issues) and help us
verify fixes.


## Contribute Code
Submit pull requests for [unity repository](
https://github.com/Microsoft/mixed-reality-extension-unity/pulls) and/or [sdk
repository](https://github.com/Microsoft/mixed-reality-extension-sdk/pulls) 
for bug fixes and features. Please mark if they are interdependent.


## Where to start
Not sure what to implement? There are a number of [issues labeled Help Wanted](
https://github.com/microsoft/mixed-reality-extension-sdk/labels/help%20wanted) 
in the sdk repository that could all be good starting points.

Other areas could be to create additional functional tests and samples, and to
comment on the documentation. 


## Tips to create good pull requests
Before accepting pull requests, we run all functional tests within the host 
app.
* Clear description
* Single issue per pull request
* Note if it there is a matching pull request in the sdk repository


## Coding guidelines
* If using Visual Studio 2017, use the auto-formatter
* add copyright header to new files


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
