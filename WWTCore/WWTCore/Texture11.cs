﻿using System;
using SharpDX.Direct3D11;
using System.IO;
#if WINDOWS_UWP
using SysColor = Windows.UI.Color;
#else
using SysColor = System.Drawing.Color;
#endif

namespace TerraViewer
{
    public class Texture11 : IDisposable
    {
        private static int nextID = 0;
        private Texture2D texture;

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }
        private ShaderResourceView resourceView;
        public int Id = nextID++;
        public Texture11(Texture2D t)
        {
            texture = t;
            resourceView = new ShaderResourceView(texture.Device, texture);
        }

        public ShaderResourceView ResourceView
        {
            get
            {
                return resourceView;
            }
        }

        public int Width
        {
            get
            {
                return texture.Description.Width;
            }
        }

        public int Height
        {
            get
            {
                return texture.Description.Height;
            }
        }


        public void Dispose()
        {
            if (resourceView != null)
            {
                resourceView.Dispose();
                GC.SuppressFinalize(resourceView);
                resourceView = null;
            }
            if (texture != null)
            {
                texture.Dispose();
                GC.SuppressFinalize(texture);
                texture = null;
            }
        }

        static public Texture11 FromFile(string fileName)
        {
            
            
                return FromFile(RenderContext11.PrepDevice, fileName);
           
        }

        static SharpDX.DXGI.Format promoteFormatToSRGB(SharpDX.DXGI.Format format)
        {
            switch (format)
            {
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                    return SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb;
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                    return SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb;
                case SharpDX.DXGI.Format.BC1_UNorm:
                    return SharpDX.DXGI.Format.BC1_UNorm_SRgb;
                case SharpDX.DXGI.Format.BC2_UNorm:
                    return SharpDX.DXGI.Format.BC2_UNorm_SRgb;
                case SharpDX.DXGI.Format.BC3_UNorm:
                    return SharpDX.DXGI.Format.BC3_UNorm_SRgb;
                case SharpDX.DXGI.Format.BC7_UNorm:
                    return SharpDX.DXGI.Format.BC7_UNorm_SRgb;
                default:
                    return format;
            }
        }

        static bool isSRGBFormat(SharpDX.DXGI.Format format)
        {
            switch (format)
            {
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC1_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC2_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC3_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC7_UNorm_SRgb:
                    return true; ;
                default:
                    return false;
            }
        }

        [Flags]
        public enum LoadOptions
        {
            None       = 0x0,
            AssumeSRgb = 0x1,
        };

        static public Texture11 FromFile(Device device, string fileName, LoadOptions options = LoadOptions.AssumeSRgb)
        {
#if WINDOWS_UWP
            using (var bitmap = TextureLoader.LoadBitmap(RenderContext11.WicImagingFactory, fileName))
            {
                return new Texture11(TextureLoader.CreateTexture2DFromBitmap(RenderContext11.PrepDevice, bitmap));
            }
#else

            try
            {
                ImageLoadInformation loadInfo = new ImageLoadInformation();
                loadInfo.BindFlags = BindFlags.ShaderResource;
                loadInfo.CpuAccessFlags = CpuAccessFlags.None;
                loadInfo.Depth = -1;
                loadInfo.Filter = FilterFlags.Box;
                loadInfo.FirstMipLevel = 0;
                loadInfo.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;

                loadInfo.Height = -1;
                loadInfo.MipLevels = -1;
                loadInfo.OptionFlags = ResourceOptionFlags.None;
                loadInfo.Usage = ResourceUsage.Default;
                loadInfo.Width = -1;

                bool shouldPromoteSRgb = RenderContext11.sRGB && (options & LoadOptions.AssumeSRgb) == LoadOptions.AssumeSRgb;

                if (fileName.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                    fileName.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                    fileName.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase))
                {
                    loadInfo.Filter = FilterFlags.Box;
                    if (shouldPromoteSRgb)
                    {
                        loadInfo.Format = promoteFormatToSRGB(loadInfo.Format);
                    }
                    if (isSRGBFormat(loadInfo.Format))
                    {
                        loadInfo.Filter |= FilterFlags.SRgb;
                    }
                }
                else
                {
                    // Promote image format to sRGB
                    ImageInformation? info = ImageInformation.FromFile(fileName);
                    if (info.HasValue && shouldPromoteSRgb)
                    {
                        loadInfo.Format = promoteFormatToSRGB(info.Value.Format);
                    }
                    if (isSRGBFormat(loadInfo.Format) )
                    {
                        loadInfo.Filter |= FilterFlags.SRgb;
                    }
                }

                Texture2D texture = Texture2D.FromFile<Texture2D>(device, fileName, loadInfo);

                return new Texture11(texture);
            }
            catch (Exception e)
            {
                try
                {
                    ImageLoadInformation ili = new ImageLoadInformation()
                                {
                                    BindFlags = BindFlags.ShaderResource,
                                    CpuAccessFlags = CpuAccessFlags.None,
                                    Depth = -1,
                                    Filter = FilterFlags.Box,
                                    FirstMipLevel = 0,
                                    Format = RenderContext11.DefaultTextureFormat,
                                    Height = -1,
                                    MipFilter = FilterFlags.None,
                                    MipLevels = 1,
                                    OptionFlags = ResourceOptionFlags.None,
                                    Usage = ResourceUsage.Default,
                                    Width = -1
                                };
                    if (ili.Format == SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb)
                    {
                        ili.Filter |= FilterFlags.SRgb;
                    }

                    Texture2D texture = Texture2D.FromFile<Texture2D>(device, fileName, ili);
                    return new Texture11(texture);

                }
                catch
                {
                    return null;
                }
            }
#endif
        }

        static public Texture11 FromBitmap(object bmp)
        {
            return FromBitmap(RenderContext11.PrepDevice, bmp);

        }

        static public Texture11 FromBitmap(object bmp, uint transparentColor)
        {

#if !WINDOWS_UWP
            System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)bmp;
            bitmap.MakeTransparent(SysColor.FromArgb((int) transparentColor));
            return FromBitmap(RenderContext11.PrepDevice, bitmap);
#else
               //todo fix this 
            return null;
#endif
        }

        static public Texture11 FromBitmap(Device device, object bitmap)
        {
#if !WINDOWS_UWP
            System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)bitmap;
            MemoryStream ms = new MemoryStream();

            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            ms.Seek(0, SeekOrigin.Begin);

            if (IsPowerOf2((uint)bmp.Width) && IsPowerOf2((uint)bmp.Height))
            {
                ImageLoadInformation loadInfo = new ImageLoadInformation();
                loadInfo.BindFlags = BindFlags.ShaderResource;
                loadInfo.CpuAccessFlags = CpuAccessFlags.None;
                loadInfo.Depth = -1;
                loadInfo.Format = RenderContext11.DefaultTextureFormat;
                loadInfo.Filter = FilterFlags.Box;
                loadInfo.FirstMipLevel = 0;
                loadInfo.Height = -1;
                loadInfo.MipFilter = FilterFlags.Linear;
                loadInfo.MipLevels = 0;
                loadInfo.OptionFlags = ResourceOptionFlags.None;
                loadInfo.Usage = ResourceUsage.Default;
                loadInfo.Width = -1;
                if (loadInfo.Format == SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb)
                {
                    loadInfo.Filter |= FilterFlags.SRgb;
                }

                Texture2D texture = Texture2D.FromStream<Texture2D>(device, ms, (int)ms.Length, loadInfo);

                ms.Dispose();
                return new Texture11(texture);
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                ImageLoadInformation ili = new ImageLoadInformation()
                {
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Depth = -1,
                    Filter = FilterFlags.Box,
                    FirstMipLevel = 0,
                    Format = RenderContext11.DefaultTextureFormat,
                    Height = -1,
                    MipFilter = FilterFlags.None,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Default,
                    Width = -1
                };
                if (ili.Format == SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb)
                {
                    ili.Filter |= FilterFlags.SRgb;
                }

                Texture2D texture = Texture2D.FromStream<Texture2D>(device, ms, (int)ms.Length, ili);
                ms.Dispose();
                return new Texture11(texture);
            }
#else
            //todo fix this 
            return null;
#endif
        }

        static bool IsPowerOf2(uint x)
        {
            return ((x & (x - 1)) == 0);
        }

        static public Texture11 FromStream(Stream stream)
        {
            return FromStream(RenderContext11.PrepDevice, stream);
        }

        public void SaveToFile(string filename)
        {
            if (SaveStream && savedStrem != null)
            {
                File.WriteAllBytes(filename, savedStrem);
            }
        }

        byte[] savedStrem = null;
        static public bool SaveStream = false;

        static public Texture11 FromStream(Device device, Stream stream)
        {
#if WINDOWS_UWP
            using (var bitmap = TextureLoader.LoadBitmap(RenderContext11.WicImagingFactory, stream))
            {
                return new Texture11(TextureLoader.CreateTexture2DFromBitmap(RenderContext11.PrepDevice, bitmap));
            }
#else
            byte[] data = null;
            if (SaveStream)
            {
                MemoryStream ms = new MemoryStream();

                stream.CopyTo(ms);
                stream.Seek(0, SeekOrigin.Begin);
                data = ms.GetBuffer();        
            }

            try
            {
                ImageLoadInformation loadInfo = new ImageLoadInformation();
                loadInfo.BindFlags = BindFlags.ShaderResource;
                loadInfo.CpuAccessFlags = CpuAccessFlags.None;
                loadInfo.Depth = -1;
                loadInfo.Format = RenderContext11.DefaultTextureFormat;
                loadInfo.Filter = FilterFlags.Box;
                loadInfo.FirstMipLevel = 0;
                loadInfo.Height = -1;
                loadInfo.MipFilter = FilterFlags.Linear;
                loadInfo.MipLevels = 0;
                loadInfo.OptionFlags = ResourceOptionFlags.None;
                loadInfo.Usage = ResourceUsage.Default;
                loadInfo.Width = -1;
                if (loadInfo.Format == SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb)
                {
                    loadInfo.Filter |= FilterFlags.SRgb;
                }

                Texture2D texture = Texture2D.FromStream<Texture2D>(device, stream, (int)stream.Length, loadInfo);

                Texture11 t11 = new Texture11(texture);
                t11.savedStrem = data;
                return t11;
            }
            catch
            {
                return null;
            }
#endif
        }
    }

#if WINDOWS_UWP
    internal class TextureLoader
    {
        /// <summary>
        /// Loads a bitmap using WIC.
        /// </summary>
        /// <param name="deviceManager"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, string filename)
        {
            return LoadBitmap(factory, new SharpDX.WIC.BitmapDecoder(
                factory,
                filename,
                SharpDX.WIC.DecodeOptions.CacheOnDemand));
        }

        public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, System.IO.Stream stream)
        {
            return LoadBitmap(factory, new SharpDX.WIC.BitmapDecoder(
                factory,
                stream,
                SharpDX.WIC.DecodeOptions.CacheOnDemand));
        }

        private static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, SharpDX.WIC.BitmapDecoder bitmapDecoder)
        {
            var formatConverter = new SharpDX.WIC.FormatConverter(factory);
            formatConverter.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);
            return formatConverter;
        }

        /// <summary>
        /// Creates a <see cref="SharpDX.Direct3D11.Texture2D"/> from a WIC <see cref="SharpDX.WIC.BitmapSource"/>
        /// </summary>
        /// <param name="device">The Direct3D11 device</param>
        /// <param name="bitmapSource">The WIC bitmap source</param>
        /// <returns>A Texture2D</returns>
        public static SharpDX.Direct3D11.Texture2D CreateTexture2DFromBitmap(SharpDX.Direct3D11.Device device, SharpDX.WIC.BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }
    }
#endif
}
