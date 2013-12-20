README
======

This is a simple windows form application that has the ability to remotely control Canon Cameras via USB.
It also has a liveview display and the option to choose where you save your picture to.

The CameraController class has the ability to control multiple cameras. Currently the CameraForm only has the ability
to support one camera at a time. The code in the CameraForm can be modified to support multiple cameras, using the 
code found in this class.

The Canon Camera SDK can be downloaded from the Canon website after registration. I'm legally not allowed to post the SDK.
When configuring the SDK, you must copy the SDK c# wrapper (named EDSDK.cs) to the project, and copy all binary dlls to
the bin/debug and bin/release directories.

Suupport for Canon Cameras depends on the version of the SDK.
