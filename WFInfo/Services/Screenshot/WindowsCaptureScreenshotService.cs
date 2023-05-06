using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WFInfo.Services.WarframeProcess;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace WFInfo.Services.Screenshot
{
    public class WindowsCaptureScreenshotService : IScreenshotService, IDisposable
    {
        private readonly bool _useHdr;
        private readonly IProcessFinder _process;

        private readonly Device _d3dDevice;
        private readonly IDirect3DDevice _device;

        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;
        private GraphicsCaptureItem _item;

        private object _frameLock = new object();
        private Direct3D11CaptureFrame _frame;
        
        private DirectXPixelFormat pixelFormat => _useHdr ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.R8G8B8A8UIntNormalized;

        public WindowsCaptureScreenshotService(IProcessFinder process, bool useHdr = true)
        {
            _process = process;
            _useHdr = useHdr;

            _d3dDevice = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _device = Direct3D11Helper.CreateDirect3DDeviceFromSharpDXDevice(_d3dDevice);

            if (_process.IsRunning) CreateCaptureSession(_process.Warframe);
            _process.OnProcessChanged += CreateCaptureSession;
        }

        public Task<List<Bitmap>> CaptureScreenshot()
        {
            Texture2D cpuTexture = null;
            var width = _frame.ContentSize.Width;
            var height = _frame.ContentSize.Height;

            lock (_frameLock)
            {
                // Copy resource into memory that can be accessed by the CPU
                using (var capturedTexture = Direct3D11Helper.CreateSharpDXTexture2D(_frame.Surface))
                {
                    var desc = capturedTexture.Description;
                    desc.CpuAccessFlags = CpuAccessFlags.Read;
                    desc.BindFlags = BindFlags.None;
                    desc.Usage = ResourceUsage.Staging;
                    desc.OptionFlags = ResourceOptionFlags.None;

                    cpuTexture = new Texture2D(_d3dDevice, desc);
                    _d3dDevice.ImmediateContext.CopyResource(capturedTexture, cpuTexture);
                }
            }

            var mapSource = _d3dDevice.ImmediateContext.MapSubresource(cpuTexture, 0, MapMode.Read, MapFlags.None);
            byte[] data = new byte[mapSource.SlicePitch];
            Marshal.Copy(mapSource.DataPointer, data, 0, mapSource.SlicePitch);
            _d3dDevice.ImmediateContext.UnmapSubresource(cpuTexture, 0);

            var bitmap = _useHdr ? CaptureHdr(data, width, height, mapSource.RowPitch) : CaptureSdr(data, width, height, mapSource.RowPitch);

            var result = new List<Bitmap> { bitmap };
            return Task.FromResult(result);
        }

        private void CreateCaptureSession(Process process)
        {
            _session?.Dispose();
            _framePool?.Dispose();

            _item = CaptureHelper.CreateItemForWindow(process.MainWindowHandle);
            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(_device, pixelFormat, 2, _item.Size);
            _framePool.FrameArrived += FrameArrived;

            _session = _framePool.CreateCaptureSession(_item);
            _session.IsBorderRequired = false;
            _session.IsCursorCaptureEnabled = false;
            _session.StartCapture();
        }

        private void FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            lock (_frameLock)
            {
                _frame?.Dispose();
                _frame = _framePool.TryGetNextFrame();
            }
        }

        private Bitmap CaptureSdr(byte[] data, int width, int height, int rowPitch)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var imageRect = new Rectangle(Point.Empty, bitmap.Size);
            var bitmapData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Span<byte> span = data.AsSpan(y * rowPitch + x * 4, 3);
                        *ptr++ = span[2];
                        *ptr++ = span[1];
                        *ptr++ = span[0];
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Bitmap CaptureHdr(byte[] data, int width, int height, int rowPitch)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            float[] floats = new float[data.Length / 2];
            float largest = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var r = DecodeHalfFloat(data[y * rowPitch + x * 8 + 0], data[y * rowPitch + x * 8 + 1]);
                    var g = DecodeHalfFloat(data[y * rowPitch + x * 8 + 2], data[y * rowPitch + x * 8 + 3]);
                    var b = DecodeHalfFloat(data[y * rowPitch + x * 8 + 4], data[y * rowPitch + x * 8 + 5]);

                    Span<float> span = floats.AsSpan((y * width * 3) + x * 3, 3);
                    span[0] = r;
                    span[1] = g;
                    span[2] = b;

                    var lum = GetPixelLuminance(span);
                    if (lum > largest) largest = lum;
                }
            }

            var imageRect = new Rectangle(Point.Empty, bitmap.Size);
            var bitmapData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Span<float> span = floats.AsSpan((y * width * 3) + x * 3, 3);
                        ReinhardToneMap(span, largest);

                        *ptr++ = (byte)(span[2] * 255f);
                        *ptr++ = (byte)(span[1] * 255f);
                        *ptr++ = (byte)(span[0] * 255f);
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public void Dispose()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _device.Dispose();

            _process.OnProcessChanged -= CreateCaptureSession;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float DecodeHalfFloat(byte byte2, byte byte1)
        {
            ushort half = (ushort)((byte1 << 8) | byte2);
            int sign = (half >> 15) & 1;
            int exp = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;

            int singleExp = (exp == 0 || exp == 31) ? (exp << 1) - 1 : 127 - 15 + exp;
            int singleMantissa = (exp == 0) ? (mantissa << (23 - 10)) & 0x7FFFFF : mantissa << 13;

            int single = (sign << 31) | (singleExp << 23) | singleMantissa;
            return BitConverter.ToSingle(BitConverter.GetBytes(single), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetPixelLuminance(Span<float> pixel)
        {
            return 0.2126f * pixel[0] + 0.7152f * pixel[1] + 0.0722f * pixel[2];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ChangeLuminance(float c_in, float l_out, float l_in)
        {
            return ClampToUnit(c_in * (l_out / l_in));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ClampToUnit(float f)
        {
            return Math.Max(Math.Min(f, 1.0f), 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReinhardToneMap(Span<float> pixel, float max_white_l)
        {
            float l_old = GetPixelLuminance(pixel);
            float numerator = l_old * (1.0f + (l_old / (max_white_l * max_white_l)));
            float l_new = numerator / (1.0f + l_old);

            pixel[0] = ChangeLuminance(pixel[0], l_new, l_old);
            pixel[1] = ChangeLuminance(pixel[1], l_new, l_old);
            pixel[2] = ChangeLuminance(pixel[2], l_new, l_old);
        }
    }
}
