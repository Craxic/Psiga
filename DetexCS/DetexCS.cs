using System;
using System.Runtime.InteropServices;

namespace DetexCS
{
    public class DetexCS
    {
        // bool decode_bc7(uint8_t * input, int width, int height, uint8_t * rgba_output)
        [DllImport("DetexCSNative")]
        public static extern bool decode_bc7(IntPtr input, int width, int height, IntPtr rgba_output);

        public static unsafe byte[] DecodeBc7(byte[] data, int width, int height)
        {
            byte[] output = new byte[width * height * 4];
            fixed (byte * data_pinned = data)
            fixed (byte * output_pinned = output)
            {
                if (!decode_bc7((IntPtr)data_pinned, width, height, (IntPtr)output_pinned))
                {
                    throw new Exception("Decode BC7 failed...");
                }
            }
            return output;
        }
    }
}