#include "pch.h"
#include "dds.h"
#include <memory>;

using namespace DirectX;

struct Point
{
	size_t x;
	size_t y;
};

HRESULT WINAPI LoadDDSFromMemory(
	_In_reads_bytes_(size) const void* pSource,
	_In_ size_t size,
	_Inout_ DDSFile* file)
{
	if (pSource == nullptr || file == nullptr)
		return E_INVALIDARG;

	std::unique_ptr<ScratchImage> ddsImage(new(std::nothrow) ScratchImage);
	if (ddsImage == nullptr)
		return E_OUTOFMEMORY;

	TexMetadata info;
	HRESULT hr = LoadFromDDSMemory(pSource, size, DDS_FLAGS_ALLOW_LARGE_FILES, &info, *ddsImage);
	if (hr < S_OK)
		return hr;

	if (IsTypeless(info.format))
	{
		info.format = MakeTypelessUNORM(info.format);
		if (IsTypeless(info.format))
			return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
		ddsImage->OverrideFormat(info.format);
	}

	if (IsPlanar(info.format))
	{
		std::unique_ptr<ScratchImage> interleavedImage(new(std::nothrow) ScratchImage);
		if (interleavedImage == nullptr)
			return E_OUTOFMEMORY;
		hr = ConvertToSinglePlane(
			ddsImage->GetImages(), ddsImage->GetImageCount(), info, *interleavedImage);
		if (hr < S_OK)
			return hr;
		info = interleavedImage->GetMetadata();
		ddsImage.swap(interleavedImage);
	}

	std::unique_ptr<ScratchImage> targetImage(new(std::nothrow) ScratchImage);
	if (targetImage == nullptr)
		return E_OUTOFMEMORY;

	const DXGI_FORMAT targetFormat = IsSRGB(info.format)
		? DXGI_FORMAT_B8G8R8A8_UNORM_SRGB : DXGI_FORMAT_B8G8R8A8_UNORM;
	if (info.format == targetFormat)
	{
		targetImage.swap(ddsImage);
	}
	else
	{
		if (IsCompressed(info.format))
			// PDN source calls ddsImage->GetMetadata() again instead of reusing info. Why?
			hr = Decompress(
				ddsImage->GetImages(), ddsImage->GetImageCount(), info, targetFormat, *targetImage);
		else
			hr = Convert(
				ddsImage->GetImages(), ddsImage->GetImageCount(), info, targetFormat,
				TEX_FILTER_DEFAULT, TEX_THRESHOLD_DEFAULT, *targetImage);
		if (hr < S_OK)
			return hr;
		info = targetImage->GetMetadata();
	}

	// PDN source checks format/alpha here, but we've already converted to a known format?
	//
	// Unclear if checking for premultiplied alpha is also important. The _UNORM definitely has it:
	// https://learn.microsoft.com/en-us/windows/win32/direct2d/supported-pixel-formats-and-alpha-modes
	// But the SRGB version might not?
	if (info.IsPMAlpha())
	{
		std::unique_ptr<ScratchImage> straightImage(new(std::nothrow) ScratchImage);
		if (straightImage == nullptr)
			return E_OUTOFMEMORY;
		hr = PremultiplyAlpha(
			targetImage->GetImages(), targetImage->GetImageCount(), info, TEX_PMALPHA_REVERSE,
			*straightImage);
		if (hr < S_OK)
			return hr;
		info = straightImage->GetMetadata();
		targetImage.swap(straightImage);
	}

	// This is how PDN projects (flattens) a cubemap onto a 2D canvas, but it is probably not
	// appropriate for use with OpenGL shaders which actually want a cube map:
	// https://www.khronos.org/opengl/wiki/Cubemap_Texture
	// Need to revisit this later. Not using cube maps right now anyway.
	if (info.IsCubemap())
	{
		size_t width = info.width;
		size_t height = info.height;
		const Point cubeMapOffsets[6] =
		{
			{ width * 2, height },	// +X
			{ 0, height },			// -X
			{ width, 0 },			// +Y
			{ width, height * 2 },	// -Y
			{ width, height },		// +Z
			{ width * 3, height }	// -Z
		};
		std::unique_ptr<ScratchImage> flattenedImage(new(std::nothrow) ScratchImage);
		if (flattenedImage == nullptr)
			return E_OUTOFMEMORY;
		hr = flattenedImage->Initialize2D(targetFormat, width * 4, height * 3, 1, 1);
		if (hr < S_OK)
			return hr;
		const Rect srcRect = { 0, 0, width, height };
		const Image* flattenedFace = flattenedImage->GetImage(0, 0, 0);
		memset(flattenedFace->pixels, 0, flattenedFace->slicePitch);
		for (size_t i = 0; i < 6; i++)
		{
			const Image* srcFace = targetImage->GetImage(0, i, 0);
			const Point& offset = cubeMapOffsets[i];
			CopyRectangle(
				*srcFace, srcRect, *flattenedFace, TEX_FILTER_DEFAULT, offset.x, offset.y);
		}
		info = flattenedImage->GetMetadata();
		targetImage.swap(flattenedImage);
	}

	const Image* firstImage = targetImage->GetImage(0, 0, 0);
	const size_t outBufferSize = firstImage->slicePitch;
	void* outData = HeapAlloc(GetProcessHeap(), 0, outBufferSize);
	if (outData == nullptr)
		return E_OUTOFMEMORY;
	memcpy_s(outData, outBufferSize, firstImage->pixels, outBufferSize);
	file->width = static_cast<int32_t>(firstImage->width);
	file->height = static_cast<int32_t>(firstImage->height);
	file->stride = static_cast<int32_t>(firstImage->rowPitch);
	file->scan0 = outData;
	return S_OK;
}

void WINAPI FreeDDS(DDSFile* file)
{
	if (file == nullptr)
		return;
	file->width = 0;
	file->height = 0;
	file->stride = 0;
	if (file->scan0 != nullptr)
	{
		HeapFree(GetProcessHeap(), 0, file->scan0);
		file->scan0 = nullptr;
	}
}
