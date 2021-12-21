print("Loading imports")

from openvino.inference_engine import IECore
import cv2
import numpy as np
import Log
import socket
import traceback
import os


HOST = 'localhost'
PORT = 60000


lf = Log.Log("Openvino server log")
md = dict()
ie = IECore()


def Load(line):

	data = []
	row = line.split(',')
	name = row[0]
	print("loading " + name + " model")
	net = ie.read_network(row[1] + row[2])
	inputs = net.input_info
	input_name = next(iter(net.input_info))
	outputs = net.outputs
	output_name = next(iter(net.outputs))
	exec_net = ie.load_network(net,"CPU")
	image = cv2.imread(row[1] + row[3])
	h, w = exec_net.input_info[input_name].tensor_desc.dims[2:]
	iimage = cv2.resize(src=image,dsize=(w,h),interpolation=cv2.INTER_AREA)
	input_data = iimage.transpose(2,0,1)[np.newaxis, ...]
	result = exec_net.infer({input_name: input_data})
	data.append(input_name)
	data.append(output_name)
	data.append(exec_net)
	data.append(h)
	data.append(w)
	md[name] = data



def Unload():

#	for name in md:			currently have no way to unload the network
#		data = md[name]
#		data[1].close()
	md.clear()
	print("unloaded models")



def Server():

	print("Running Openvino object detection inference server")
	print("Opening UDP socket")
	lf.Open()

	try:
		sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
		sock.bind((HOST,PORT))

	except:
		print("Could not open UDP socket")
		traceback.print_exc()
		lf.WriteLine("Could not open UDP socket")
		return(1)

	print("Starting server loop")
	try:
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
					lf.WriteLine("file incorrect format")
					sa = str.split(s,",")
					if len(sa) == 4:
						if (os.path.exists(sa[1])):
							image = cv2.imread(sa[1])
							if (image.size > 0):
								ih, iw = image.shape[:2]
								score_limit = float(sa[2])
								rid = int(sa[3])
								try:
									data = md[sa[0]]
								except:
									lf.WriteLine("Dictionary key exception")
									data = None
								if (data != None):
									iimage = cv2.resize(src=image,dsize=(data[4],data[3]),interpolation=cv2.INTER_AREA)
									input_data = iimage.transpose(2,0,1)[np.newaxis, ...]
									result = data[2].infer({data[0]: input_data})
									output = result[data[1]][0][0]
									if (len(output) > 0):
										sboxes = []
										for _,id,score,xmin,ymin,xmax,ymax in output:
											cont = False
											if score >= float(sa[2]):
												if (rid == 0):
													cont = True
												else:
													cont = (rid == int(id))
											if (cont):
												p = int(score * 100)
												x = int(xmin * iw)
												y = int(ymin * ih)
												w = int((xmax - xmin) * iw)
												h = int((ymax - ymin) * ih)
												if (rid == 0):
													sboxes.append([p,int(id),x,y,w,h])
												else:
													sboxes.append([p,x,y,w,h])
										if len(sboxes) > 0:
											rsp = "OK "
											for b in sboxes:
												rsp += str(b)
											sock.sendto(bytes(rsp,"ascii"),conn)
											lf.WriteLine(rsp)
										else:
											sock.sendto(bytes("FAIL no detection","ascii"),conn)
											lf.WriteLine("no detection")
									else:
										sock.sendto(bytes("FAIL no detection","ascii"),conn)
										lf.WriteLine("no detection")
								else:
									sock.sendto(bytes("FAIL,unknown object","ascii"),conn)
									lf.WriteLine("unknown object")
							else:
								sock.sendto(bytes("FAIL,image size 0","ascii"),conn)
								lf.WriteLine("image size 0")
						else:
							sock.sendto(bytes("FAIL,incorrect format","ascii"),conn)
		
			else:
				lf.WriteLine("no data reception")
				break

	except:
		traceback.print_exc()

	lf.WriteLine("Openvino object detection inference server closed.")
	lf.Close()


if __name__ == "__main__":
	Server()



