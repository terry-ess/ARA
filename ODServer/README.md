# Object Detection Server

The server is a Python 3.7 application that implements a simple UDP/IP "server" that uses TensorFlow 1.15 for object detection inference.

Supported commands:

- Are you there? - hello
- Load a model - load,model name, full path to frozen graph, full path to test image
- Unload all the loaded models - unload
- Shutdown - exit
- Run an inference -  model name, full path to image, min. score, object ID

Models used in this application:

1. [TensorFlow 1 object detection model zoo pre-trained models](https://github.com/tensorflow/models/blob/master/research/object_detection/g3doc/tf1_detection_zoo.md)
2. Calibration mark: [based on ssd_mobilenet_v1](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2018_01_28.tar.gz)
3. Containers (boxes): [based on ssd_mobilenet_v1](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2018_01_28.tar.gz)
4. Parts: [based on ssd_resnet50_v1_fpn](http://download.tensorflow.org/models/object_detection/ssd_resnet50_v1_fpn_shared_box_predictor_640x640_coco14_sync_2018_07_03.tar.gz)
5. Hand (scans): [based on ssd_mobilenet_v1](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2018_01_28.tar.gz)
6. Hand: [based on ssd_mobilenet_v1_fpn](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_fpn_shared_box_predictor_640x640_coco14_sync_2018_07_03.tar.gz)
7. Part in hand: [based on ssd_mobilenet_v1](http://download.tensorflow.org/models/object_detection/ssd_mobilenet_v1_coco_2018_01_28.tar.gz)

The trained models are included in the software image you can download [here](https://1drv.ms/u/s!Akd6rkUaBWr4gTQub8I82e7nirgK?e=sacdyS).

Model selection was determined using trial and error to find the best trade off between accuracy and performance.  Starting with the fastest and least accurate model, ssd_mobilenet_v1, train and check the results.  If accuracy was not sufficient try the next "better" model (ssd_mobilenet_v1_fpn, ssd_resnet50_v1_fpn).  In many cases this also required a multi-level approach:

Take this command:

1. Detect hand with ssd_mobilenet_v1_fpn
2. Crop the image using the hand detection box and detect the object in the hand using ssd_mobilenet_v1.
3. Use the object detection box as input to image segmentation for pixel level detail.
4. Use the pixel level detail fused with 3D data to determine the  pick location and gripper orientation.

Surface part detection:

1. Crop the image to 640 X 640 centered on the workspace.
2. Detect parts using ssd_resnet50_v1_fpn.
3. Use the object detection boxes as input to image segmentation for pixel level detail.
4. Use the pixel level detail fused with 3D data to determine the pick location and gripper orientation.