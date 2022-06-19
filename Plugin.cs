using System;
using System.IO;
using System.Linq;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer;
using System.Text;
using QuickLook.Common.Helpers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.DDSViewer
{
    public class Plugin : IViewer
    {
        private string _imagePath;
        private ImagePanel _ip;
        private MetaProvider _meta;
        private Pfim.IImage _image;
        private PixelFormat format;
        public int Priority => 0;
        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && new[] { ".dds" }.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            _imagePath = ExtractFile(path);
            _meta = new MetaProvider(_imagePath);
            var size = _meta.GetSize();
            if (!size.IsEmpty)
                context.SetPreferredSizeFit(size, 0.8);
            else
                context.PreferredSize = new System.Windows.Size(800, 600);

            //context.Theme = (Themes)SettingHelper.Get("LastTheme", 1, "QuickLook.Plugin.ImageViewer");
        }

        public void View(string path, ContextObject context)
        {
            _imagePath = ExtractFile(path);
            _ip = new ImagePanel();
            _ip.ContextObject = context;
            _ip.Meta = _meta;
            //_ip.Theme = context.Theme;

            var size = _meta.GetSize();
            context.ViewerContent = _ip;
            context.Title = size.IsEmpty
                ? $"{Path.GetFileName(path)}"
                : $"{size.Width}×{size.Height}: {Path.GetFileName(path)}";

            _ip.ImageUriSource = FilePathToFileUrl(_imagePath);
        }

        public void Cleanup()
        {
            _ip?.Dispose();
            _ip = null;
        }

        private string ExtractFile(string path)
        {
            string curAssemblyFolder = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            curAssemblyFolder = Path.GetDirectoryName(curAssemblyFolder);
            string destinationPath = Path.GetFullPath(Path.Combine(curAssemblyFolder, "thumbnail.png"));
            _image = Pfim.Pfim.FromFile(path);
            if (_image.Compressed)
            {
                _image.Decompress();
            }

            switch (_image.Format)
            {
                // https://github.com/nickbabcock/Pfim/blob/master/src/Pfim.Skia/Program.cs
                case Pfim.ImageFormat.Rgba32:
                    format = PixelFormat.Format32bppArgb;
                    //Image.LoadPixelData<Bgra32>(_image.Data, _image.Width, _image.Height).Save(destinationPath);
                    break;
                case Pfim.ImageFormat.Rgb24:
                    format = PixelFormat.Format24bppRgb;
                    //Image.LoadPixelData<Bgr24>(_image.Data, _image.Width, _image.Height).Save(destinationPath);
                    break;
                case Pfim.ImageFormat.R5g5b5a1:
                    format = PixelFormat.Format16bppArgb1555;
                    break;
                case Pfim.ImageFormat.Rgb8:
                    format = PixelFormat.Format8bppIndexed;
                    break;
                case Pfim.ImageFormat.R5g6b5:
                    format = PixelFormat.Format16bppRgb565;
                    break;
                case Pfim.ImageFormat.R5g5b5:
                    format = PixelFormat.Format16bppRgb555;
                    break;
                case Pfim.ImageFormat.Rgba16:
                    throw new Exception("Unsupported pixel format (" + _image.Format + ")");
                default:
                    //Log.Logger.Warning("Image {inputPath} had unknown format {format}", path, _image.Format);
                    throw new Exception("Unsupported pixel format (" + _image.Format + ")");
            }


            var data = Marshal.UnsafeAddrOfPinnedArrayElement(_image.Data, 0);
            var bitmap = new Bitmap(_image.Width, _image.Height, _image.Stride, format, data);
            bitmap.Save(destinationPath, ImageFormat.Png);
            return destinationPath;
        }



        public Uri FilePathToFileUrl(string filePath)
        {
            var uri = new StringBuilder();
            foreach (var v in filePath)
                if (v >= 'a' && v <= 'z' || v >= 'A' && v <= 'Z' || v >= '0' && v <= '9' ||
                    v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                    v > '\x80')
                    uri.Append(v);
                else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                    uri.Append('/');
                else
                    uri.Append($"%{(int)v:X2}");
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");

            try
            {
                return new Uri(uri.ToString());
            }
            catch
            {
                return new Uri(filePath);
            }
        }
    }
}
