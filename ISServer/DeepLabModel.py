
import tensorflow as tf
import numpy as np
from PIL import Image


class DeepLabModel():
 
  INPUT_TENSOR_NAME = 'ImageTensor:0'
  OUTPUT_TENSOR_NAME = 'SemanticPredictions:0'


  def Open(self, graph_name):

    self.graph = tf.Graph()
    graph_def = None
    file_handle = open(graph_name,'rb')
    graph_def = tf.GraphDef.FromString(file_handle.read())
    if graph_def is None:
      raise RuntimeError('Cannot find inference graph.')
    with self.graph.as_default():
      tf.import_graph_def(graph_def, name='')
    self.sess = tf.Session(graph=self.graph)



  def Run(self, image,input_size):

    width, height = image.size
    resize_ratio = 1.0 * input_size / max(width, height)
    target_size = (int(resize_ratio * width), int(resize_ratio * height))
    resized_image = image.convert('RGB').resize(target_size, Image.ANTIALIAS)
    batch_seg_map = self.sess.run(self.OUTPUT_TENSOR_NAME,feed_dict={self.INPUT_TENSOR_NAME: [np.asarray(resized_image)]})
    seg_map = batch_seg_map[0]
    return resized_image, seg_map

