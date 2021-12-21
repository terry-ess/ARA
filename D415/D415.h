#pragma once


extern "C"
	{
	__declspec(dllexport) bool CaptureImages(unsigned short depth[],unsigned char color[]);
	__declspec(dllexport) void Close();
	__declspec(dllexport) bool Open();
	};
