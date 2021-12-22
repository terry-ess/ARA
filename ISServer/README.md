# Image Segmentation Server

The server is a Python 3.7 application that implements a simple UDP/IP "server" that uses TensorFlow 1.15 for image segmentation inference.

Supported commands:

- Are you there? - hello
- Load a model - load,model name, full path to frozen graph, full path to test image
- Unload all the loaded models - unload
- Shutdown - exit
- Run an inference -  model name, full path to image

Models used in this application are based on TensorFlow's [DeepLab v3+ pre-trained mobilenet__v2 model](http://download.tensorflow.org/models/deeplabv3_mnv2_pascal_train_aug_2018_01_29.tar.gz). Separate models were trained for boxes, box tops, shafts and end blocks using transfer learning.  The trained models are included in the software image you can download [here](https://1drv.ms/u/s!Akd6rkUaBWr4gTQub8I82e7nirgK?e=sacdyS).