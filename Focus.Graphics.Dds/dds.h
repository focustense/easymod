#pragma once

#include "DirectXTex.h"

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

	struct DDSFile
	{
		void* scan0;
		int32_t width;
		int32_t height;
		int32_t stride;
	};

	__declspec(dllexport) HRESULT WINAPI LoadDDSFromMemory(
		_In_reads_bytes_(size) const void* pSource,
		_In_ size_t size,
		_Inout_ DDSFile* file);

	__declspec(dllexport) void WINAPI FreeDDS(DDSFile* file);

#ifdef __cplusplus
}
#endif // __cplusplus