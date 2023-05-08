using Composition.WindowsRuntimeHelpers;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WFInfo.Services.WarframeProcess;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

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
            int width, height;

            lock (_frameLock)
            {
                width = _frame.ContentSize.Width;
                height = _frame.ContentSize.Height;

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

            unsafe
            {
                var mapSource = _d3dDevice.ImmediateContext.MapSubresource(cpuTexture, 0, MapMode.Read, MapFlags.None);

                Bitmap bitmap;
                if (_useHdr)
                {
                    var data = new Span<ushort>(mapSource.DataPointer.ToPointer(), mapSource.SlicePitch / 2);
                    bitmap = CaptureHdr(data, width, height, mapSource.RowPitch / 2);
                }
                else
                {
                    var data = new Span<byte>(mapSource.DataPointer.ToPointer(), mapSource.SlicePitch);
                    bitmap = CaptureSdr(data, width, height, mapSource.RowPitch);
                }

                _d3dDevice.ImmediateContext.UnmapSubresource(cpuTexture, 0);

                var result = new List<Bitmap> { bitmap };
                return Task.FromResult(result);
            }
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

        private Bitmap CaptureSdr(Span<byte> data, int width, int height, int rowPitch)
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
                        Span<byte> span = data.Slice(y * rowPitch + x * 4, 3);
                        *ptr++ = span[2];
                        *ptr++ = span[1];
                        *ptr++ = span[0];
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Bitmap CaptureHdr(Span<ushort> data, int width, int height, int rowPitch)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            float[] floats = new float[data.Length / 4 * 3]; // Pixel components (RGB) as floats
            float[] luminances = new float[data.Length / 4]; // Luminance of individual pixels
            float largestLuminance = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var r = new Half(data[y * rowPitch + x * 4 + 0]);
                    var g = new Half(data[y * rowPitch + x * 4 + 1]);
                    var b = new Half(data[y * rowPitch + x * 4 + 2]);

                    Span<float> span = floats.AsSpan((y * width * 3) + x * 3, 3);

                    span[0] = r;
                    span[1] = g;
                    span[2] = b;

                    var luminance = GetPixelLuminance(span);
                    luminances[y * width + x] = luminance;
                    if (luminance > largestLuminance) largestLuminance = luminance;
                }
            }

            var imageRect = new Rectangle(Point.Empty, bitmap.Size);
            var bitmapData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var largestLuminanceSquared = largestLuminance * largestLuminance;

            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixelPtr = ptr + x * 3;
                        Span<float> span = floats.AsSpan((y * width * 3) + x * 3, 3);
                        ReinhardToneMap(span, luminances[y * width + x], largestLuminanceSquared);

                        *pixelPtr++ = (byte)(span[2] * 255f);
                        *pixelPtr++ = (byte)(span[1] * 255f);
                        *pixelPtr++ = (byte)(span[0] * 255f);
                    }

                    ptr += bitmapData.Stride;
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
        private float GetPixelLuminance(Span<float> pixel)
        {
            return 0.2126f * pixel[0] + 0.7152f * pixel[1] + 0.0722f * pixel[2];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ChangeLuminance(float c_in, float l_ratio)
        {
            return MathUtil.Clamp(c_in * l_ratio, 0.0f, 1.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReinhardToneMap(Span<float> pixel, float l_old, float max_white_l_squared)
        {
            float numerator = l_old * (1.0f + (l_old / max_white_l_squared));
            float l_new = numerator / (1.0f + l_old);
            float l_ratio = l_new / l_old;

            pixel[0] = ChangeLuminance(pixel[0], l_ratio);
            pixel[1] = ChangeLuminance(pixel[1], l_ratio);
            pixel[2] = ChangeLuminance(pixel[2], l_ratio);
        }
    }
}
