print("Loading imports")

from TTS.utils.synthesizer import Synthesizer
import socket
import sounddevice as sd
import numpy as np
import Log

HELLO = "HELLO"
EXIT = "EXIT"
OK = "ok"
FAIL = "fail"
HOST = 'localhost'
PORT = 30000


MODEL_PATH = "C:\\Depository\\THE_Solution\\Development\\Robots\\InterbotixWidowX-250\\ARA\\TTSServer\\model"
MODEL_NAME = "\\model_file.pth.tar"
CONFIG_MODEL_NAME = "\\config.json"
VOCODER_PATH = "C:\\Depository\\THE_Solution\\Development\\Robots\\InterbotixWidowX-250\\ARA\\TTSServer\\vocoder"
VOCODER_NAME = "\\model_file.pth.tar"
VOCODER_CONFIG_NAME = "\\config.json"

lf = Log.Log("TTS server")
synthensizer = None


def Speak(stext):

	global synthensizer
	lines = stext.split('.')
	for line in lines:
		line += '.';
		wavs = synthensizer.tts(text = line)
		sd.wait()
		wav = np.array(wavs)
		sd.play(wav,22050)



def Server():

	global synthensizer
	print("Initializing synthesizer")
	lf.WriteLine("Initializing synthesizer")
	synthensizer = Synthesizer(MODEL_PATH + MODEL_NAME,MODEL_PATH + CONFIG_MODEL_NAME,"",VOCODER_PATH + VOCODER_NAME, VOCODER_PATH + VOCODER_CONFIG_NAME,False)
	if (synthensizer != None):
		print("Opening UDP socket")
		lf.WriteLine("Opening UDP socket")
		try:
			sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
			sock.bind((HOST,PORT))
		except:
			print("Could not open UDP socket")
			lf.WriteLine("Could not open UDP socket")
			return(1)
		print("Starting server loop")
		lf.WriteLine("Starting server loop")
		while True:
			try:
				data,conn = sock.recvfrom(1024)
			except:
				lf.WriteLine("Socket recvfrom exception {0}".format(err.errno))
				break
			if (len(data) > 0):
				s = bytes.decode(data)
				lf.WriteLine(s)
				if (s == EXIT):
					break
				elif (s== HELLO):
					sock.sendto(bytes(OK,"ascii"),conn)
					lf.WriteLine(OK)
				else:
					Speak(s)
					sock.sendto(bytes(OK,"ascii"),conn)
			else:
				lf.WriteLine("no data reception")
				break
	else:
		print("Failed to initialize synthesizer")
		lf.WriteLine("Failed to initialize synthesizer")
	lf.WriteLine("TTS server closed.")
	lf.Close()

if __name__ == "__main__":
	Server()

