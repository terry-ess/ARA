# OpenVINO Object Detection Server

The server is a Python 3.7 application that implements a simple UDP/IP "server" that uses Intel's OpenVINO for object detection inference.  This is an alternative to the TensorFlow based inference server.  
Testing has shown a significant improvement in performance using OpenVINO rather then TensorFlow inference.  But when used in the actual ARA application this was lost (primarily due to the overhead of the PvServer).  It addition, OpenVINO's python API does not support the ability to remove models.  As a result the current configuration uses the TensorFlow based inference server, ODServer.

Supported commands:

- Are you there? - hello
- Load a model - load,model name, full path to frozen graph, full path to test image
- Unload all the loaded models - unload
- Shutdown - exit
- Run an inference -  model name, full path to image, min. score, object ID

Models used in this application are the same models described in the ODServer converted by the OpenVINO tool set.  This requires the installation of [OpenVINO 2021 4.1 LTS](https://www.intel.com/content/www/us/en/developer/tools/openvino-toolkit/overview.html). 
