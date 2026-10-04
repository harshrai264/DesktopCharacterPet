using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopCharacterPet.Models;
using DesktopCharacterPet.Services;
using WinForms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;
using WpfPoint = System.Windows.Point;

namespace DesktopCharacterPet
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly AppSettings _settings;
        private readonly GifAnimationService _gifService;
        private readonly CharacterMovementService _movementService;
        private WinForms.NotifyIcon? _notifyIcon;
        private WinForms.ToolStripMenuItem? _trayShowItem;
        private WinForms.ToolStripMenuItem? _trayHideItem;
        private WinForms.ToolStripMenuItem? _trayPauseItem;
        private WinForms.ToolStripMenuItem? _trayResumeItem;
        private bool _isPaused;

        public MainWindow()
        {
            InitializeComponent();

            _settings = AppSettings.Load();
            _gifService = new GifAnimationService();
            _movementService = new CharacterMovementService(_settings.MovementSpeed, _settings.UpdateIntervalMs);

            ApplyCharacterSize(_settings.CharacterWidth, _settings.CharacterHeight);

            Loaded += OnMainWindowLoaded;
            Closing += OnMainWindowClosing;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            this.Activate();

            InitializeTrayIcon();
            InitializeAnimationAndMovement();
            UpdateMenuStates();
        }

        private void InitializeAnimationAndMovement()
        {
            // Subscribe to GIF frame updates
            _gifService.FrameUpdated += (s, frame) =>
            {
                Dispatcher.Invoke(() => CharacterImage.Source = frame);
            };

            // Subscribe to movement updates
            _movementService.PositionChanged += (s, pos) =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.Left = pos.X;
                    this.Top = pos.Y;
                });
            };

            // Subscribe to direction changes for horizontal flip
            _movementService.DirectionChanged += (s, dir) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CharacterFlipTransform.ScaleX = (dir == WalkDirection.Right) ? 1.0 : -1.0;
                });
            };

            // Load the GIF animation
            bool loaded = _gifService.Load(_settings.GifPath);
            if (!loaded)
            {
                // Fallback to searching in standard directory
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "skeleton.gif");
                _gifService.Load(fallbackPath);
            }

            // 1. Initially set screen bounds from primary monitor WorkArea in DIPs
            _movementService.ScreenBounds = SystemParameters.WorkArea;

            // 2. Set initial position above taskbar
            _movementService.InitializePosition(
                this.Width,
                this.Height,
                _settings.StartX,
                _settings.StartY);

            // 3. Immediately apply coordinates to window
            this.Left = _movementService.CurrentX;
            this.Top = _movementService.CurrentY;

            // 4. Refine screen bounds for multi-monitor if pet was positioned elsewhere
            UpdateScreenBoundsFromCurrentPosition();

            // Configure battle path mode from settings
            if (Enum.TryParse<FightPathMode>(_settings.BattlePath, out var pathMode))
            {
                _movementService.SetPathMode(pathMode);
            }

            if (_settings.GifPath.Contains("ironman", StringComparison.OrdinalIgnoreCase) || _settings.GifPath.Contains("thanos", StringComparison.OrdinalIgnoreCase))
            {
                _movementService.AllowHorizontalFlip = false;
            }
            else
            {
                _movementService.AllowHorizontalFlip = true;
            }

            if (_settings.AutoStart)
            {
                _gifService.Play();
                _movementService.Start();
                _isPaused = false;
            }
            else
            {
                _isPaused = true;
            }
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Text = "Desktop Character Pet",
                Visible = true
            };

            // Create tray icon from character frame or fallback to system icon
            System.Drawing.Icon? customIcon = TryCreateTrayIcon();
            _notifyIcon.Icon = customIcon ?? System.Drawing.SystemIcons.Application;

            // Context menu for System Tray
            var trayMenu = new WinForms.ContextMenuStrip();

            _trayShowItem = new WinForms.ToolStripMenuItem("Show Character", null, (s, e) => ShowCharacter());
            _trayHideItem = new WinForms.ToolStripMenuItem("Hide Character", null, (s, e) => HideCharacter());
            _trayPauseItem = new WinForms.ToolStripMenuItem("Pause", null, (s, e) => PausePet());
            _trayResumeItem = new WinForms.ToolStripMenuItem("Resume", null, (s, e) => ResumePet());
            var exitItem = new WinForms.ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

            var battlePathMenu = new WinForms.ToolStripMenuItem("Battle Path (4 Paths)");
            battlePathMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🔄 Auto-Cycle All 4 Paths", null, (s, e) => SetBattlePath("AutoCycleAll")));
            battlePathMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("💥 Path 1: Ground Clash", null, (s, e) => SetBattlePath("GroundClash")));
            battlePathMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("⚡ Path 2: Mid-Air Dogfight", null, (s, e) => SetBattlePath("MidAirDogfight")));
            battlePathMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🌌 Path 3: Sky Strike", null, (s, e) => SetBattlePath("SkyStrike")));
            battlePathMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🌊 Path 4: Dynamic Wave", null, (s, e) => SetBattlePath("DynamicWave")));

            var changeCharMenu = new WinForms.ToolStripMenuItem("Marvel Heroes & Characters");
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("💥 Iron Man vs Thanos", null, (s, e) => ChangeCharacter("Assets/ironman_vs_thanos.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🕷️ Spider-Man", null, (s, e) => ChangeCharacter("Assets/spiderman.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("⚡ Thor (God of Thunder)", null, (s, e) => ChangeCharacter("Assets/thor_lightning.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🟢 The Incredible Hulk", null, (s, e) => ChangeCharacter("Assets/hulk_smash.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🛡️ Captain America", null, (s, e) => ChangeCharacter("Assets/captain_america.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("⚡ Iron Man Repulsor Beam", null, (s, e) => ChangeCharacter("Assets/ironman_beam.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripSeparator());
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("💀 Walking Skeleton", null, (s, e) => ChangeCharacter("Assets/white_skeleton_walk.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("⚡ Pikachu / Zappy", null, (s, e) => ChangeCharacter("Assets/pikachu_walk.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🦊 Red Fox", null, (s, e) => ChangeCharacter("Assets/fox_walk.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🐶 Akita Dog", null, (s, e) => ChangeCharacter("Assets/dog_walk.gif")));
            changeCharMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🍃 Totoro", null, (s, e) => ChangeCharacter("Assets/totoro_walk.gif")));

            var speedMenu = new WinForms.ToolStripMenuItem("Combat Speed");
            speedMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🚶 Patrol Speed", null, (s, e) => SetSpeed(2.5)));
            speedMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🏃 Fast Combat", null, (s, e) => SetSpeed(4.5)));
            speedMenu.DropDownItems.Add(new WinForms.ToolStripMenuItem("🚀 Super Sonic Blitz", null, (s, e) => SetSpeed(7.0)));

            trayMenu.Items.Add(_trayShowItem);
            trayMenu.Items.Add(_trayHideItem);
            trayMenu.Items.Add(new WinForms.ToolStripSeparator());
            trayMenu.Items.Add(battlePathMenu);
            trayMenu.Items.Add(changeCharMenu);
            trayMenu.Items.Add(speedMenu);
            trayMenu.Items.Add(new WinForms.ToolStripSeparator());
            trayMenu.Items.Add(_trayPauseItem);
            trayMenu.Items.Add(_trayResumeItem);
            trayMenu.Items.Add(new WinForms.ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = trayMenu;

            // Double click tray icon toggles visibility
            _notifyIcon.DoubleClick += (s, e) =>
            {
                if (this.Visibility == Visibility.Visible)
                {
                    HideCharacter();
                }
                else
                {
                    ShowCharacter();
                }
            };
        }

        private System.Drawing.Icon? TryCreateTrayIcon()
        {
            try
            {
                if (_gifService.CurrentFrame != null)
                {
                    using var ms = new MemoryStream();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_gifService.CurrentFrame));
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    using var bmp = new System.Drawing.Bitmap(ms);
                    using var smallBmp = new System.Drawing.Bitmap(bmp, new System.Drawing.Size(32, 32));
                    IntPtr hIcon = smallBmp.GetHicon();
                    var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
                    DestroyIcon(hIcon);
                    return icon;
                }
            }
            catch
            {
                // Fall back safely if icon creation fails
            }
            return null;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                TriggerComicBurst();

                bool wasMoving = !_isPaused;
                if (wasMoving)
                {
                    _movementService.Pause();
                }

                DragMove();

                // Multi-monitor support: detect which monitor pet was dragged to
                UpdateScreenBoundsFromCurrentPosition();
                _movementService.SetPosition(this.Left, this.Top);

                if (wasMoving)
                {
                    _movementService.Resume();
                }
            }
        }

        private static readonly string[] ComicWords = { "💥 POW!", "⚡ THWIP!", "⚡ BOOM!", "💥 SMASH!", "🛡️ CLANG!", "⚡ ZAP!" };
        private static readonly Random _rnd = new Random();
        private System.Windows.Threading.DispatcherTimer? _burstTimer;

        private void TriggerComicBurst()
        {
            if (ComicBurstText == null) return;
            ComicBurstText.Text = ComicWords[_rnd.Next(ComicWords.Length)];
            ComicBurstText.Opacity = 1.0;

            _burstTimer?.Stop();
            _burstTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _burstTimer.Tick += (s, e) =>
            {
                _burstTimer.Stop();
                ComicBurstText.Opacity = 0.0;
            };
            _burstTimer.Start();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Scale maintaining aspect ratio
            double ratio = this.Height / this.Width;
            double delta = (e.Delta > 0) ? 30.0 : -30.0;
            double newWidth = Math.Clamp(this.Width + delta, 140.0, 720.0);
            double newHeight = Math.Round(newWidth * ratio);
            ApplyCharacterSize(newWidth, newHeight);
        }

        private void UpdateScreenBoundsFromCurrentPosition()
        {
            try
            {
                if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
                {
                    _movementService.ScreenBounds = SystemParameters.WorkArea;
                    return;
                }

                // Convert WPF DIP coordinates to physical pixels for WinForms.Screen
                var dpi = VisualTreeHelper.GetDpi(this);
                double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                int physicalX = (int)((this.Left + this.Width / 2) * scaleX);
                int physicalY = (int)((this.Top + this.Height / 2) * scaleY);

                var screen = WinForms.Screen.FromPoint(new System.Drawing.Point(physicalX, physicalY));
                if (screen != null)
                {
                    var wa = screen.WorkingArea;
                    // Convert WinForms physical pixels back to WPF DIPs
                    _movementService.ScreenBounds = new Rect(
                        wa.Left / scaleX,
                        wa.Top / scaleY,
                        wa.Width / scaleX,
                        wa.Height / scaleY);
                    return;
                }
            }
            catch
            {
                // Fall back to primary screen WorkArea
            }

            _movementService.ScreenBounds = SystemParameters.WorkArea;
        }

        private void PausePet()
        {
            _isPaused = true;
            _movementService.Pause();
            _gifService.Pause();
            UpdateMenuStates();
        }

        private void ResumePet()
        {
            _isPaused = false;
            _movementService.Resume();
            _gifService.Resume();
            UpdateMenuStates();
        }

        private void ShowCharacter()
        {
            this.Visibility = Visibility.Visible;
            if (!_isPaused)
            {
                _movementService.Resume();
                _gifService.Resume();
            }
            UpdateMenuStates();
        }

        private void HideCharacter()
        {
            _movementService.Pause();
            _gifService.Pause();
            this.Visibility = Visibility.Collapsed;
            UpdateMenuStates();
        }

        private void ApplyCharacterSize(double width, double height)
        {
            this.Width = width;
            this.Height = height;
            _movementService.UpdateCharacterSize(width, height);

            _settings.CharacterWidth = width;
            _settings.CharacterHeight = height;
            _settings.Save();
        }

        private void UpdateMenuStates()
        {
            bool isVisible = this.Visibility == Visibility.Visible;

            if (MenuPause != null) MenuPause.IsEnabled = !_isPaused;
            if (MenuResume != null) MenuResume.IsEnabled = _isPaused;

            if (_trayShowItem != null) _trayShowItem.Enabled = !isVisible;
            if (_trayHideItem != null) _trayHideItem.Enabled = isVisible;
            if (_trayPauseItem != null) _trayPauseItem.Enabled = !_isPaused;
            if (_trayResumeItem != null) _trayResumeItem.Enabled = _isPaused;
        }

        private void MenuPause_Click(object sender, RoutedEventArgs e)
        {
            PausePet();
        }

        private void MenuResume_Click(object sender, RoutedEventArgs e)
        {
            ResumePet();
        }

        private void MenuBattlePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag is string tag)
            {
                SetBattlePath(tag);
            }
        }

        public void SetBattlePath(string tag)
        {
            if (Enum.TryParse<FightPathMode>(tag, out var mode))
            {
                _movementService.SetPathMode(mode);
                _settings.BattlePath = tag;
                _settings.Save();
            }
        }

        private void MenuChangeCharacter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag is string gifPath)
            {
                ChangeCharacter(gifPath);
            }
        }

        public void ChangeCharacter(string gifPath)
        {
            _settings.GifPath = gifPath;
            _settings.Save();

            bool loaded = _gifService.Load(gifPath);
            if (!loaded)
            {
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, gifPath);
                _gifService.Load(fallbackPath);
            }

            // Adjust aspect ratio and character settings
            if (gifPath.Contains("ironman_vs_thanos", StringComparison.OrdinalIgnoreCase) || gifPath.Contains("ironman_beam", StringComparison.OrdinalIgnoreCase))
            {
                _movementService.AllowHorizontalFlip = false;
                CharacterFlipTransform.ScaleX = 1.0;
                ApplyCharacterSize(320, 192);
            }
            else if (gifPath.Contains("hulk", StringComparison.OrdinalIgnoreCase) || gifPath.Contains("thor", StringComparison.OrdinalIgnoreCase))
            {
                _movementService.AllowHorizontalFlip = true;
                ApplyCharacterSize(240, 240);
            }
            else if (gifPath.Contains("spiderman", StringComparison.OrdinalIgnoreCase) || gifPath.Contains("captain", StringComparison.OrdinalIgnoreCase))
            {
                _movementService.AllowHorizontalFlip = true;
                ApplyCharacterSize(220, 220);
            }
            else
            {
                _movementService.AllowHorizontalFlip = true;
                ApplyCharacterSize(150, 150);
            }

            if (!_isPaused)
            {
                _gifService.Play();
            }

            // Refresh tray icon
            if (_notifyIcon != null)
            {
                var icon = TryCreateTrayIcon();
                if (icon != null)
                {
                    _notifyIcon.Icon = icon;
                }
            }
        }

        private void MenuChangeSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag != null)
            {
                if (double.TryParse(item.Tag.ToString(), out double newSpeed))
                {
                    SetSpeed(newSpeed);
                }
            }
        }

        public void SetSpeed(double newSpeed)
        {
            _movementService.Speed = newSpeed;
            _settings.MovementSpeed = newSpeed;
            _settings.Save();
        }

        private void MenuChangeSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem item && item.Tag != null)
            {
                if (double.TryParse(item.Tag.ToString(), out double newWidth))
                {
                    double ratio = this.Height / this.Width;
                    double newHeight = Math.Round(newWidth * ratio);
                    ApplyCharacterSize(newWidth, newHeight);
                }
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            Close();
        }

        private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _movementService.Dispose();
            _gifService.Dispose();

            WpfApplication.Current.Shutdown();
        }
    }
}