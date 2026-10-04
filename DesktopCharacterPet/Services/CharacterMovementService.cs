using System;
using System.Windows;
using System.Windows.Threading;

namespace DesktopCharacterPet.Services
{
    public enum WalkDirection
    {
        Right,
        Left
    }

    public enum FightPathMode
    {
        GroundClash,      // Path 1: Low above taskbar, sliding and clashing horizontally
        MidAirDogfight,   // Path 2: Floating at mid-screen (eye level), aerial battle
        SkyStrike,        // Path 3: High altitude sky battle near the top of the monitor
        DynamicWave,      // Path 4: Swooping wave trajectory across the entire screen
        AutoCycleAll      // Automatically cycles through all 4 paths across the screen
    }

    public class CharacterMovementService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private double _currentX;
        private double _currentY;
        private double _targetY;
        private double _characterWidth = 280.0;
        private double _characterHeight = 170.0;
        private Rect _screenBounds;
        private bool _isDisposed;
        private int _cycleStage = 0;
        private double _wavePhase = 0.0;

        public WalkDirection CurrentDirection { get; private set; } = WalkDirection.Right;
        public FightPathMode CurrentPathMode { get; set; } = FightPathMode.AutoCycleAll;
        public bool IsMoving { get; private set; }
        public double Speed { get; set; } = 2.5;
        public bool AllowHorizontalFlip { get; set; } = false; // Kept false for 2-fighter clash so Iron Man stays left and Thanos stays right

        public event EventHandler<(double X, double Y)>? PositionChanged;
        public event EventHandler<WalkDirection>? DirectionChanged;

        public double CurrentX => _currentX;
        public double CurrentY => _currentY;

        public Rect ScreenBounds
        {
            get => _screenBounds;
            set
            {
                _screenBounds = value;
                ClampPosition();
            }
        }

        public CharacterMovementService(double initialSpeed = 2.5, int intervalMs = 20)
        {
            Speed = initialSpeed;
            _screenBounds = SystemParameters.WorkArea;

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs > 0 ? intervalMs : 20)
            };
            _timer.Tick += OnMovementTick;
        }

        public void InitializePosition(double width, double height, double? startX = null, double? startY = null)
        {
            _characterWidth = width;
            _characterHeight = height;

            double defaultX = _screenBounds.Left + 30;
            double defaultY = Math.Max(_screenBounds.Top, _screenBounds.Bottom - _characterHeight - 25);

            _currentX = startX ?? defaultX;
            _currentY = startY ?? defaultY;
            _targetY = _currentY;

            ClampPosition();
            PositionChanged?.Invoke(this, (_currentX, _currentY));
            DirectionChanged?.Invoke(this, CurrentDirection);
        }

        public void UpdateCharacterSize(double width, double height)
        {
            _characterWidth = width;
            _characterHeight = height;
            ClampPosition();
            PositionChanged?.Invoke(this, (_currentX, _currentY));
        }

        public void SetPosition(double x, double y)
        {
            _currentX = x;
            _currentY = y;
            _targetY = y;
            ClampPosition();
            PositionChanged?.Invoke(this, (_currentX, _currentY));
        }

        public void SetPathMode(FightPathMode mode)
        {
            CurrentPathMode = mode;
            _wavePhase = 0;
            UpdateTargetAltitude();
        }

        public void Start()
        {
            if (IsMoving) return;
            IsMoving = true;
            _timer.Start();
        }

        public void Pause()
        {
            if (!IsMoving) return;
            IsMoving = false;
            _timer.Stop();
        }

        public void Resume()
        {
            if (IsMoving) return;
            IsMoving = true;
            _timer.Start();
        }

        public void Stop()
        {
            IsMoving = false;
            _timer.Stop();
        }

        private void OnMovementTick(object? sender, EventArgs e)
        {
            if (!IsMoving) return;

            double minX = _screenBounds.Left;
            double maxX = Math.Max(minX, _screenBounds.Right - _characterWidth);

            // 1. Horizontal Movement
            if (CurrentDirection == WalkDirection.Right)
            {
                _currentX += Speed;
                if (_currentX >= maxX)
                {
                    _currentX = maxX;
                    CurrentDirection = WalkDirection.Left;
                    OnScreenEdgeReached();
                }
            }
            else
            {
                _currentX -= Speed;
                if (_currentX <= minX)
                {
                    _currentX = minX;
                    CurrentDirection = WalkDirection.Right;
                    OnScreenEdgeReached();
                }
            }

            // 2. Vertical Movement (4 Path Altitude System)
            UpdateTargetAltitude();

            // Smooth interpolation to target altitude (flying transitions)
            if (Math.Abs(_targetY - _currentY) > 0.5)
            {
                _currentY += (_targetY - _currentY) * 0.07;
            }
            else
            {
                _currentY = _targetY;
            }

            ClampPosition();
            PositionChanged?.Invoke(this, (_currentX, _currentY));
        }

        private void OnScreenEdgeReached()
        {
            if (AllowHorizontalFlip)
            {
                DirectionChanged?.Invoke(this, CurrentDirection);
            }

            // In AutoCycle mode, advance through the 4 fight paths on each screen edge!
            if (CurrentPathMode == FightPathMode.AutoCycleAll)
            {
                _cycleStage = (_cycleStage + 1) % 4;
            }
        }

        private void UpdateTargetAltitude()
        {
            int effectiveStage = (CurrentPathMode == FightPathMode.AutoCycleAll)
                ? _cycleStage
                : (int)CurrentPathMode;

            double groundY = _screenBounds.Bottom - _characterHeight - 25;
            double midAirY = _screenBounds.Top + (_screenBounds.Height - _characterHeight) * 0.48;
            double skyY = _screenBounds.Top + 45;

            switch (effectiveStage)
            {
                case 0: // Path 1: Ground Clash
                    _targetY = groundY;
                    break;

                case 1: // Path 2: Mid-Air Dogfight
                    // Hover with slight bobbing
                    _targetY = midAirY + Math.Sin(_wavePhase * 2) * 8;
                    _wavePhase += 0.05;
                    break;

                case 2: // Path 3: Sky Strike
                    _targetY = skyY;
                    break;

                case 3: // Path 4: Dynamic Wave / Swooping Dive
                    double centerY = _screenBounds.Top + (_screenBounds.Height - _characterHeight) * 0.5;
                    double amplitude = (_screenBounds.Height - _characterHeight) * 0.38;
                    _targetY = centerY + Math.Sin(_wavePhase) * amplitude;
                    _wavePhase += 0.04;
                    break;

                default:
                    _targetY = groundY;
                    break;
            }
        }

        private void ClampPosition()
        {
            double minX = _screenBounds.Left;
            double maxX = Math.Max(minX, _screenBounds.Right - _characterWidth);
            double minY = _screenBounds.Top;
            double maxY = Math.Max(minY, _screenBounds.Bottom - _characterHeight - 15);

            _currentX = Math.Clamp(_currentX, minX, maxX);
            _currentY = Math.Clamp(_currentY, minY, maxY);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _timer.Stop();
                _timer.Tick -= OnMovementTick;
            }
            GC.SuppressFinalize(this);
        }
    }
}
