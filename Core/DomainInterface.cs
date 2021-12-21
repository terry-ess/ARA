using System;

namespace RobotArm
	{
	public interface DomainInterface
		{
		bool Open(AutoOpInterface aoi);

		void Close();

		void Stop();
		}
	}
