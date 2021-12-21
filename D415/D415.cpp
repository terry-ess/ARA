
#include "rs.hpp"
#include <string.h>
#include "D415.h"
#include "log.h"
#include <windows.h>

#define WIDTH 1280
#define HEIGHT 720
#define FRAME_RATE 6
#define CI_TIME 500


rs2::align align_to_color(RS2_STREAM_COLOR);
rs2::pipeline *pl = NULL;
Log alog;
bool started = false;
HANDLE wait;


void PipeLine()

{
	try
	{
	rs2::pipeline pipe;

	pl = &pipe;
	WaitForSingleObject(wait, INFINITE);
	}

	catch (const rs2::error & e)
	{
	pl = NULL;
	alog.LogEntry((char *)e.what());
	}

	catch(const std::exception & e)
	{
	pl = NULL;
	alog.LogEntry((char *) e.what());
	}

}



bool Open()

{
	bool rtn = true;
	rs2::config cfg;

	try
	{
	alog.OpenLog("D415.log","starting",true);
	wait = CreateSemaphoreA(NULL,0,1,NULL);
	if (wait != NULL)
		{
		if (CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE) PipeLine, 0, 0, NULL) != NULL)
			{
			Sleep(1000);
			if (pl != NULL)
				{
				cfg.enable_stream(RS2_STREAM_DEPTH,WIDTH,HEIGHT,RS2_FORMAT_Z16,FRAME_RATE);
				cfg.enable_stream(RS2_STREAM_COLOR,WIDTH,HEIGHT,RS2_FORMAT_BGR8,FRAME_RATE);
				pl->start(cfg);
				alog.LogEntry("Pipeline started.");
				started = true;
				}
			else
				alog.LogEntry("Pipeline was not created");
			}
		else
			alog.LogEntry("Could not create pipeline thread");
		}
	else
		alog.LogEntry("Could not create semaphore.");
	}

	catch (const rs2::error & e)
	{
	rtn = false;
	alog.LogEntry((char *)e.what());
	}

	catch(const std::exception & e)
	{
	rtn = false;
	alog.LogEntry((char *) e.what());
	}

	return(true);
}



void Close()

{
	alog.CloseLog("closed");
	if (started)
		{
		pl->stop();
		ReleaseSemaphore(wait,1,NULL);
		}
}



bool CaptureImages(unsigned short depth[],unsigned char color[])

{
	rs2::frame frame;
	rs2::frameset frameset;
	bool rtn = false;

	try
	{
	frameset = pl->wait_for_frames(CI_TIME);
	frameset = align_to_color.process(frameset);
	frame = frameset.get_depth_frame();
	memcpy(depth, frame.get_data(), frame.get_data_size());
	frame = frameset.get_color_frame();
	memcpy(color,frame.get_data(),frame.get_data_size());
	rtn = true;
	}
	
	catch (const rs2::error & e)
	{
	rtn = false;
	alog.LogEntry((char *)e.what());
	}

	catch(const std::exception & e)
	{
	alog.LogEntry((char *)e.what());
	rtn = false;
	} 

	catch(...)
	{
	alog.LogEntry("Unknown exception type");
	rtn = false;
	}

	return(rtn);
}



