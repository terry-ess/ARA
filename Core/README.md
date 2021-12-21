# Robot Arm Core Components

## Structure
Structural details are provided in [structure.md](structure.md)
## Electronics
Electronics overview in provided in [electronics.md](electronics.md)
## Software
The C# based .NET DLL project includes interface software for each of the primary electronic components and the AI servers:

1. Robotic arm interface (ArmControl.cs)
2. D415 camera (D415Camera.cs)
3. SSC-32U servo controller (CameraPanTilt.cs)
3. Object detection server (VisualObjectDetection.cs)
4. Image segmentation server (ImageSegmentation.cs)
5. Picovoice based speech server (PvSpeech.cs)
6. Microsoft's speech synthesizer or the coqui-ai TTS server (PvSpeech.cs)

In addition it provides:

1. An interface to domain DLLs (DomainInterface.cs and Domains.cs).
2. Workspace mapping (Mapping.cs)
3. Shared definitions and functions (Shared.cs)
4. Logging (Log.cs)

It should be noted that the dynamic work assist domain's definition of workspace is used for the camera calibration in the test user interface's autonomous tab.  The workspace mapping code was therefore placed in the core project since the test visual user interface was not intended to contain any operational code.

