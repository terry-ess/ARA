# Picovoice Server

The primary user interface is verbal.  The robot must be able to talk with a generally understandable voice but it does not have to sound human (in my opinion it should not be mistaken for a  human). For command and control this also requires the local ability to provide "chat bot" level speech recognition with intent classification and named entity recognition.  A key problem is how to do this.  Using high level speech recognition with a domain specific grammar (e.g. Microsoft speech recognition), using high level speech recognition coupled to a NLP program (e.g. coqui_ai STT and RASA) or a speech recognition - NLP engine created specifically to handle domain constrained speech using keyword detection (e.g. Picovoice).  Using a high level speech recognition front end has not worked well in my tests.  One of the key problems is caused by background noise and the robot's inability to determine if speech is directed at it even with a modern "beam forming" far field microphone array.  The use of keyword detection substantially improves the robot's understanding capability. But it lacks the capability for "free" speech when it is required: short robot initiated transactional conversations and user initiated "intense" control cases (i.e. manual mode).  The best results have been with a slightly modified version of Picovoice that supports keyword and non-keyword driven operation.

The server is a C# console application that implements a simple UDP/IP "server" control interface coupled with a asynchronous event channel for inferences. It uses Picovoice's Porcupine and Rhino products to perform keyword detection, speech recognition and intent classification with named entity recognition.  A similar, but not local, server using Python and either a Raspberry Pi 3 B+ or a Raspberry Pi 4 B was also tested.  They had higher latency and lower accuracy then the Windows C# implementation.  It is highly likely that the lower accuracy was due to the audio input, PyAudio vs. OpenTK.Audio, since most of the accuracy difference was due to missed key word detections and tests using a python implementation on Windows had similar results.

It should be noted that unlike the other resources used in this project, these are commercial products. They provide free use for evaluation purposes only (see [Picovoice License & Terms](https://github.com/Picovoice/picovoice)).  This prototype is an experimental evaluation tool that has NO commercial objective.  To use the server you will need to register with [Picovoice](https://picovoice.ai/docs/) and obtain an access key.  You can use the context yml files to create new context with the Picovoice console or download the [software image](https://1drv.ms/u/s!Akd6rkUaBWr4gTQub8I82e7nirgK?e=sacdyS) which includes them. The keyword used in the project is "computer" (a canned keyword provided by Picovoice).

Supported commands:

- Are you there? - HELLO
- Initialize the Porcupine keyword engine - START, keyword sensitivity
- Load a Rhino context and start keyword based processing - CONTEXT, full path to context file, context sensitivity
- Set processing to keyword based - KEYWORD
- Set processing to NLP only - NLP_ONLY, mode (CONTINUAL or CONVERSATION)_
- Set the inference time out parameter - INFER_TIME_OUT, value (seconds)
- Shutdown - EXIT


Models used in this application:

1. Domain verbal selection in the "Autonomous" tab: [Domains.yml](../VisualUI/Domains.yml)
2. Dynamic work assist grammar: [Dynamic Work Assist.yml](../domains/DynamicWorkAssist/DynamicWorkAssist.yml)

Modifications made to Rhino source code:

1. Utils.cs, changed class name from "Utils" to "RUtils" to eliminate interference from Porcupine.
2. Rhino.cs, changed reference to "Utils" to "RUtils".
3. Rhino.cs, made the reset function accessible.
