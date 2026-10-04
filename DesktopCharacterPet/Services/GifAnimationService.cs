using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopCharacterPet.Services
{
    public class GifAnimationService : IDisposable
    {
        private readonly List<BitmapSource> _frames = new();
        private readonly List<TimeSpan> _frameDelays = new();
        private readonly DispatcherTimer _timer;
        private int _currentFrameIndex;
        private bool _isDisposed;

        public event EventHandler<BitmapSource>? FrameUpdated;

        public bool IsPlaying { get; private set; }
        public int FrameCount => _frames.Count;
        public int CurrentFrameIndex => _currentFrameIndex;
        public BitmapSource? CurrentFrame => _frames.Count > 0 ? _frames[_currentFrameIndex] : null;

        public GifAnimationService()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Tick += OnTimerTick;
        }

        public bool Load(string gifPathOrUri)
        {
            Stop();
            _frames.Clear();
            _frameDelays.Clear();
            _currentFrameIndex = 0;

            Stream? stream = null;
            try
            {
                // Check if file exists on disk (relative or absolute)
                string fullPath = Path.IsPathRooted(gifPathOrUri)
                    ? gifPathOrUri
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, gifPathOrUri);

                if (File.Exists(fullPath))
                {
                    stream = File.OpenRead(fullPath);
                }
                else
                {
                    // Fall back to WPF Application Resource
                    string packUriString = gifPathOrUri.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)
                        ? gifPathOrUri
                        : $"pack://application:,,,/{gifPathOrUri.TrimStart('/')}";

                    var resourceInfo = System.Windows.Application.GetResourceStream(new Uri(packUriString));
                    if (resourceInfo != null)
                    {
                        stream = resourceInfo.Stream;
                    }
                }

                if (stream == null)
                {
                    return false;
                }

                var decoder = new GifBitmapDecoder(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                foreach (BitmapFrame rawFrame in decoder.Frames)
                {
                    BitmapSource frame = RemoveBackgroundIfOpaque(rawFrame);
                    frame.Freeze();
                    _frames.Add(frame);

                    TimeSpan delay = TimeSpan.FromMilliseconds(100); // 100ms default
                    if (rawFrame.Metadata is BitmapMetadata metadata && metadata.ContainsQuery("/grctlext/Delay"))
                    {
                        object val = metadata.GetQuery("/grctlext/Delay");
                        int delayHundreds = 0;
                        if (val is ushort usVal) delayHundreds = usVal;
                        else if (val is int intVal) delayHundreds = intVal;
                        else if (val is short sVal) delayHundreds = sVal;

                        // GIF delay is stored in hundredths (1/100) of a second
                        if (delayHundreds > 0)
                        {
                            delay = TimeSpan.FromMilliseconds(delayHundreds * 10);
                        }
                    }
                    _frameDelays.Add(delay);
                }

                if (_frames.Count > 0)
                {
                    FrameUpdated?.Invoke(this, _frames[0]);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading GIF: {ex.Message}");
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private static BitmapSource RemoveBackgroundIfOpaque(BitmapSource frame)
        {
            try
            {
                var formatBmp = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
                int width = formatBmp.PixelWidth;
                int height = formatBmp.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                formatBmp.CopyPixels(pixels, stride, 0);

                // Sample corner pixel (0,0)
                byte cornerB = pixels[0];
                byte cornerG = pixels[1];
                byte cornerR = pixels[2];
                byte cornerA = pixels[3];

                // If corner is already transparent, the GIF is already transparent
                if (cornerA == 0)
                {
                    return frame;
                }

                // Check other corners to see if this is a uniform solid background
                int tr = (width - 1) * 4;
                int bl = (height - 1) * stride;
                int br = (height - 1) * stride + (width - 1) * 4;

                bool isUniform =
                    Math.Abs(pixels[tr] - cornerB) < 20 && Math.Abs(pixels[tr + 1] - cornerG) < 20 && Math.Abs(pixels[tr + 2] - cornerR) < 20 &&
                    Math.Abs(pixels[bl] - cornerB) < 20 && Math.Abs(pixels[bl + 1] - cornerG) < 20 && Math.Abs(pixels[bl + 2] - cornerR) < 20 &&
                    Math.Abs(pixels[br] - cornerB) < 20 && Math.Abs(pixels[br + 1] - cornerG) < 20 && Math.Abs(pixels[br + 2] - cornerR) < 20;

                if (!isUniform)
                {
                    return frame;
                }

                // Remove solid background with distance tolerance
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    int db = pixels[i] - cornerB;
                    int dg = pixels[i + 1] - cornerG;
                    int dr = pixels[i + 2] - cornerR;
                    if (db * db + dg * dg + dr * dr < 900) // threshold ~30
                    {
                        pixels[i + 3] = 0;
                    }
                }

                var writeable = new WriteableBitmap(width, height, formatBmp.DpiX, formatBmp.DpiY, PixelFormats.Bgra32, null);
                writeable.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                return writeable;
            }
            catch
            {
                return frame;
            }
        }

        public void Play()
        {
            if (_frames.Count == 0 || IsPlaying) return;

            IsPlaying = true;
            _timer.Interval = _frameDelays[_currentFrameIndex];
            _timer.Start();
        }

        public void Pause()
        {
            if (!IsPlaying) return;

            IsPlaying = false;
            _timer.Stop();
        }

        public void Resume()
        {
            if (_frames.Count == 0 || IsPlaying) return;

            IsPlaying = true;
            _timer.Interval = _frameDelays[_currentFrameIndex];
            _timer.Start();
        }

        public void Stop()
        {
            IsPlaying = false;
            _timer.Stop();
            _currentFrameIndex = 0;
            if (_frames.Count > 0)
            {
                FrameUpdated?.Invoke(this, _frames[0]);
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0 || !IsPlaying) return;

            _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Count;
            _timer.Interval = _frameDelays[_currentFrameIndex];
            FrameUpdated?.Invoke(this, _frames[_currentFrameIndex]);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _frames.Clear();
                _frameDelays.Clear();
            }
            GC.SuppressFinalize(this);
        }
    }
}
