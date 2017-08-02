#include "detex/detex.h"
#include <string.h>

#ifdef _MSC_VER
__declspec(dllexport) bool __stdcall 
#else
bool
#endif
decode_bc7(uint8_t * input, int width, int height, uint8_t * rgba_output) {
    detexTexture texture;
    memset(&texture, 0, sizeof(texture));
    
    texture.format = DETEX_TEXTURE_FORMAT_BPTC;
    texture.data = input;
    texture.width = width;
    texture.height = height;
    texture.width_in_blocks = (width+3)/4;
    texture.height_in_blocks = (height+3)/4;
	
    return detexDecompressTextureLinear(&texture, rgba_output, DETEX_PIXEL_FORMAT_RGBA8);
}
