using System;
using System.Drawing;

namespace RobotArm
	{
	public interface AutoOpInterface
		{
			void TextOutput(string msg);

			void VideoOutput(Image img);

			void OpDone();
		}
	}
