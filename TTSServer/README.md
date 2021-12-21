**TTS Server**

The primary user interface is verbal.  The robot must be able to talk with a generally understandable voice but it does not have to sound human (in my opinion it should not be mistaken for a  human). This can be achieved with either the Microsoft Speech synthesizer or the coqui-ai TTS. At the time of the prototypes development Microsoft's synthesizer was better.  The coqui-ai TTS is still very much in development and the best models I could find provided understandable speech but with a significantly higher latency, a voice cadence that was a little on the slow side and a few glitches.   This requires the installation of the [coqui-ai TTS](https://github.com/coqui-ai/TTS). The current configuration uses the Microsoft synthesizer.

The TTS server is a Python 3.8 application that implements a simple UDP/IP "server" that uses coqui-ai TTS to provide text to speech.

Supported commands:

- Are you there? - HELLO
- Shutdown - EXIT
- Text to speech - any text string

Models used in this application:

 [Tacotron2 with double decoder consistency with phonemes text-to-spectrogram model](https://coqui.gateway.scarf.sh/v0.2.0/tts_models--en--ljspeech--tacotronDDC_ph.zip)

 [Univnet vocoder](https://coqui.gateway.scarf.sh/v0.3.0/vocoder_models--en--ljspeech--univnet_v2.zip)
    
