import time
start = time.perf_counter()
print("Loading imports")

import socket
import Log
import os
import DeepLabModel
from PIL import Image
import numpy as np
import csv
import sys
import signal


HOST = 'localhost'
PORT = 60010
RETURN_DIRECTORY = "reply_file"


lf = Log.Log("TensorFlow image segmentation inference server")
md = dict()


def Create_2label_colormap():

	colormap = np.zeros((2, 3), dtype=int)
	colormap[0] = (0,0,0)
	colormap[1] = (255,255,255)
	return colormap



def Handler(signal,frame):
	lf.Close()
	sys.exit(0)



def Load(line):
	
	data = []
	row = line.split(',')
	name = row[0]
	row.pop(0)
	print("loading " + name + " model")
	ismodel = DeepLabModel.DeepLabModel()
	ismodel.Open(row[0] + row[1])
	ismodel.Run(Image.open(row[0] + row[2]),int(row[3]))
	data.append(ismodel)
	colormap = Create_2label_colormap()
	data.append(colormap)
	data.append(int(row[3]))
	md[name] = data



def Unload():

	for name in md:
		data = md[name]
		data[0].sess.close()
	md.clear()
	print("unloaded models")



def Server():

	print("Running TensorFlow image segmentation inference server")
	lf.Open()
	signal.signal(signal.SIGINT,Handler)
	print("Loading DeepLab graph and initializing PIL and TensorFlow")
	print("Opening UDP socket")

	try:
		sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
		sock.bind((HOST,PORT))
	except:
		print("Could not open UDP socket")
		lf.WriteLine("Could not open UDP socket")
		return(1)

	print("Starting server loop")
	stop = time.perf_counter()
	lf.WriteLine("Load {0} sec".format(stop - start))

	while True:
		try:
			data,conn = sock.recvfrom(1024)
		except:
			lf.WriteLine("Socket recvfrom exception {0}".format(err.errno))
			break
		if (len(data) > 0):
			s = bytes.decode(data)
			lf.WriteLine(s)
			if s == "exit":
				break
			elif (s== "hello"):
				sock.sendto(bytes("OK","ascii"),conn)
				lf.WriteLine("OK")
			elif (s.startswith("load,")):
				try:
					Load(s.replace("load,",""))
					sock.sendto(bytes("OK","ascii"),conn)
					lf.WriteLine("OK")
				except:
					sock.sendto(bytes("FAIL","ascii"),conn)
					lf.WriteLine("FAIL, " + traceback.format_exc())
			elif (s == "unload"):
				Unload()
				sock.sendto(bytes("OK","ascii"),conn)
				lf.WriteLine("OK")
			else:
				sa = str.split(s,",")
				if len(sa) == 2:
					if (os.path.exists(sa[1])):
						data = md[sa[0]]
						if data != None:
							original_im = Image.open(sa[1])
							resized_im,seg_map = data[0].Run(original_im,int(data[2]))
							seg_image = Image.fromarray(data[1][seg_map].astype(np.uint8))
							width,height = original_im.size
							simage = seg_image.resize((int(width),int(height)),Image.ANTIALIAS)
							sbf = os.path.join(os.getcwd(),RETURN_DIRECTORY,sa[0] + "blob.png")
							if os.path.exists(sbf):
								os.remove(sbf)
							simage.save(sbf)
							rsp = "OK," + sbf
							sock.sendto(bytes(rsp,"ascii"),conn)
						else:
							sock.sendto(bytes("FAIL,unknown object","ascii"),conn)
							lf.WriteLine("unknown object")
					else:
						sock.sendto(bytes("FAIL,file does not exist","ascii"),conn)
						lf.WriteLine("file does not exist")
				else:
					sock.sendto(bytes("FAIL,incorrect format","ascii"),conn)
					lf.WriteLine("file incorrect format")
		else:
			lf.WriteLine("no data reception")
			break
	lf.WriteLine("TensorFlow multi-detection segmentation inference server closed.")
	lf.Close()


if __name__ == "__main__":
	Server()
