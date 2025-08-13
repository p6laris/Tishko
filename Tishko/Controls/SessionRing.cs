using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Tishko.Controls
{
    public class SessionOrb : ContentControl
    {
        // Visual
        public static readonly StyledProperty<IBrush?> FillBrushProperty =
            AvaloniaProperty.Register<SessionOrb, IBrush?>(nameof(FillBrush), Brushes.White);

        // Breath (gentle)
        public static readonly StyledProperty<double> BreathPeriodProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(BreathPeriod), 9.0); // seconds

        public static readonly StyledProperty<double> BreathMinScaleProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(BreathMinScale), 0.96);

        public static readonly StyledProperty<double> BreathMaxScaleProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(BreathMaxScale), 1.00);

        // Wobble (disc)
        public static readonly StyledProperty<double> WobbleAmplitudeProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(WobbleAmplitude), 0.8); // px

        public static readonly StyledProperty<double> WobbleFrequencyProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(WobbleFrequency), 1.0); // Hz

        // Anisotropy (disc)
        public static readonly StyledProperty<double> AxisAnisotropyProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(AxisAnisotropy), 0.02);

        public static readonly StyledProperty<double> AnisotropyFrequencyProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(AnisotropyFrequency), 0.15); // Hz

        // Rotation (applies to disc + rings)
        public static readonly StyledProperty<double> RotationSpeedProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RotationSpeed), 0.3); // deg/sec

        // Twin overlay (organic silhouette)
        public static readonly StyledProperty<bool> TwinEnabledProperty =
            AvaloniaProperty.Register<SessionOrb, bool>(nameof(TwinEnabled), true);

        public static readonly StyledProperty<double> TwinScaleMultiplierProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(TwinScaleMultiplier), 0.985);

        public static readonly StyledProperty<double> TwinOffsetPixelsProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(TwinOffsetPixels), 0.8);

        public static readonly StyledProperty<double> TwinPhaseOffsetProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(TwinPhaseOffset), Math.PI); // radians

        public static readonly StyledProperty<double> TwinOrbitFrequencyProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(TwinOrbitFrequency), 0.05); // Hz

        // Rings
        public static readonly StyledProperty<bool> RingsEnabledProperty =
            AvaloniaProperty.Register<SessionOrb, bool>(nameof(RingsEnabled), true);

        public static readonly StyledProperty<int> RingCountProperty =
            AvaloniaProperty.Register<SessionOrb, int>(nameof(RingCount), 4);

        public static readonly StyledProperty<double> RingSpacingProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingSpacing), 14.0);

        public static readonly StyledProperty<double> RingThicknessProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingThickness), 2.0);

        public static readonly StyledProperty<double> RingOpacityProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingOpacity), 0.35);

        public static readonly StyledProperty<double> RingOpacityFalloffProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingOpacityFalloff), 0.82);

        public static readonly StyledProperty<double> RingWobbleAmplitudeProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingWobbleAmplitude), 0.9);

        public static readonly StyledProperty<double> RingWobbleFrequencyProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingWobbleFrequency), 0.55); // Hz

        public static readonly StyledProperty<double> RingCascadeFractionProperty =
            AvaloniaProperty.Register<SessionOrb, double>(nameof(RingCascadeFraction), 0.15); // 0..1 portion between rings

        // Lifecycle + transitions
        public static readonly StyledProperty<bool> AutoStartProperty =
            AvaloniaProperty.Register<SessionOrb, bool>(nameof(AutoStart), false);

        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<SessionOrb, bool>(nameof(IsActive), false);

        public static readonly StyledProperty<TimeSpan> StartDurationProperty =
            AvaloniaProperty.Register<SessionOrb, TimeSpan>(nameof(StartDuration), TimeSpan.FromMilliseconds(900));

        public static readonly StyledProperty<TimeSpan> StopDurationProperty =
            AvaloniaProperty.Register<SessionOrb, TimeSpan>(nameof(StopDuration), TimeSpan.FromMilliseconds(900));

        public static readonly StyledProperty<int> RandomSeedProperty =
            AvaloniaProperty.Register<SessionOrb, int>(nameof(RandomSeed), 20240812);

        // CLR
        public IBrush? FillBrush { get => GetValue(FillBrushProperty); set => SetValue(FillBrushProperty, value); }
        public double BreathPeriod { get => GetValue(BreathPeriodProperty); set => SetValue(BreathPeriodProperty, value); }
        public double BreathMinScale { get => GetValue(BreathMinScaleProperty); set => SetValue(BreathMinScaleProperty, value); }
        public double BreathMaxScale { get => GetValue(BreathMaxScaleProperty); set => SetValue(BreathMaxScaleProperty, value); }
        public double WobbleAmplitude { get => GetValue(WobbleAmplitudeProperty); set => SetValue(WobbleAmplitudeProperty, value); }
        public double WobbleFrequency { get => GetValue(WobbleFrequencyProperty); set => SetValue(WobbleFrequencyProperty, value); }
        public double AxisAnisotropy { get => GetValue(AxisAnisotropyProperty); set => SetValue(AxisAnisotropyProperty, value); }
        public double AnisotropyFrequency { get => GetValue(AnisotropyFrequencyProperty); set => SetValue(AnisotropyFrequencyProperty, value); }
        public double RotationSpeed { get => GetValue(RotationSpeedProperty); set => SetValue(RotationSpeedProperty, value); }
        public bool TwinEnabled { get => GetValue(TwinEnabledProperty); set => SetValue(TwinEnabledProperty, value); }
        public double TwinScaleMultiplier { get => GetValue(TwinScaleMultiplierProperty); set => SetValue(TwinScaleMultiplierProperty, value); }
        public double TwinOffsetPixels { get => GetValue(TwinOffsetPixelsProperty); set => SetValue(TwinOffsetPixelsProperty, value); }
        public double TwinPhaseOffset { get => GetValue(TwinPhaseOffsetProperty); set => SetValue(TwinPhaseOffsetProperty, value); }
        public double TwinOrbitFrequency { get => GetValue(TwinOrbitFrequencyProperty); set => SetValue(TwinOrbitFrequencyProperty, value); }
        public bool RingsEnabled { get => GetValue(RingsEnabledProperty); set => SetValue(RingsEnabledProperty, value); }
        public int RingCount { get => GetValue(RingCountProperty); set => SetValue(RingCountProperty, value); }
        public double RingSpacing { get => GetValue(RingSpacingProperty); set => SetValue(RingSpacingProperty, value); }
        public double RingThickness { get => GetValue(RingThicknessProperty); set => SetValue(RingThicknessProperty, value); }
        public double RingOpacity { get => GetValue(RingOpacityProperty); set => SetValue(RingOpacityProperty, value); }
        public double RingOpacityFalloff { get => GetValue(RingOpacityFalloffProperty); set => SetValue(RingOpacityFalloffProperty, value); }
        public double RingWobbleAmplitude { get => GetValue(RingWobbleAmplitudeProperty); set => SetValue(RingWobbleAmplitudeProperty, value); }
        public double RingWobbleFrequency { get => GetValue(RingWobbleFrequencyProperty); set => SetValue(RingWobbleFrequencyProperty, value); }
        public double RingCascadeFraction { get => GetValue(RingCascadeFractionProperty); set => SetValue(RingCascadeFractionProperty, value); }
        public bool AutoStart { get => GetValue(AutoStartProperty); set => SetValue(AutoStartProperty, value); }
        public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
        public TimeSpan StartDuration { get => GetValue(StartDurationProperty); set => SetValue(StartDurationProperty, value); }
        public TimeSpan StopDuration { get => GetValue(StopDurationProperty); set => SetValue(StopDurationProperty, value); }
        public int RandomSeed { get => GetValue(RandomSeedProperty); set => SetValue(RandomSeedProperty, value); }

        // Timing / state
        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16.6667);
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _tickClock = new();
        private double _time;                 // running phase time (s)
        private double _activity;             // 0..1 eased progress (render-time)
        private double _activityRaw;          // 0..1 linear progress (time domain)
        private bool _targetActive;           // desired state (IsActive mirrored)

        // Cached pen for rings
        private IPen? _ringPen;

        // Deterministic phases
        private double _phaseBreath, _phaseWobble, _phaseAniso, _phaseRotation, _phaseTwinOrbit;
        private double[] _ringPhases = Array.Empty<double>();

        public SessionOrb()
        {
            _timer = new DispatcherTimer { Interval = FrameInterval };
            _timer.Tick += OnTick;

            AffectsRender<SessionOrb>(
                FillBrushProperty,
                BreathPeriodProperty, BreathMinScaleProperty, BreathMaxScaleProperty,
                WobbleAmplitudeProperty, WobbleFrequencyProperty,
                AxisAnisotropyProperty, AnisotropyFrequencyProperty,
                RotationSpeedProperty,
                TwinEnabledProperty, TwinScaleMultiplierProperty, TwinOffsetPixelsProperty, TwinPhaseOffsetProperty, TwinOrbitFrequencyProperty,
                RingsEnabledProperty, RingCountProperty, RingSpacingProperty, RingThicknessProperty, RingOpacityProperty, RingOpacityFalloffProperty,
                RingWobbleAmplitudeProperty, RingWobbleFrequencyProperty, RingCascadeFractionProperty
            );

            this.GetObservable(IsActiveProperty).Subscribe(OnIsActiveChanged);
            this.GetObservable(RandomSeedProperty).Subscribe(_ => ReseedPhases());
            this.GetObservable(RingThicknessProperty).Subscribe(_ => RebuildPen());
            this.GetObservable(FillBrushProperty).Subscribe(_ => RebuildPen());
            this.GetObservable(RingCountProperty).Subscribe(_ => ReseedPhases());
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ReseedPhases();
            RebuildPen();
            _targetActive = AutoStart || IsActive;
            if (_targetActive) Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Stop(immediate: true);
        }

        public void Start(bool immediate = false)
        {
            _targetActive = true;
            if (immediate) { _activityRaw = 1; _activity = 1; }
            EnsureTicking();
        }

        public void Stop(bool immediate = false)
        {
            _targetActive = false;
            if (immediate) { _activityRaw = 0; _activity = 0; _time = 0; }
            // timer will stop automatically when fully idle in OnTick
        }

        private void OnIsActiveChanged(bool active)
        {
            if (active) Start();
            else Stop();
        }

        private void EnsureTicking()
        {
            if (!_timer.IsEnabled)
            {
                _tickClock.Restart();
                _timer.Start();
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            // Advance time
            var dt = _tickClock.Elapsed.TotalSeconds;
            _tickClock.Restart();
            if (dt > 0.25) dt = 0.25; // guard

            // Advance animation time only when moving or active
            if (_activity > 0 || _targetActive || _activityRaw > 0)
                _time += dt;

            // Update transition progress
            if (_targetActive)
            {
                var dur = Math.Max(0.0001, StartDuration.TotalSeconds);
                _activityRaw = Math.Min(1.0, _activityRaw + dt / dur);
            }
            else
            {
                var dur = Math.Max(0.0001, StopDuration.TotalSeconds);
                _activityRaw = Math.Max(0.0, _activityRaw - dt / dur);
            }

            // Eased activity for rendering (smooth in/out)
            _activity = EaseInOutSine(_activityRaw);

            // Stop ticking when fully idle
            if (_activityRaw <= 0 && !_targetActive)
            {
                _timer.Stop();
                _tickClock.Reset();
                _time = 0;
            }

            InvalidateVisual();
        }

        private static double EaseInOutSine(double x)
        {
            // 0..1 -> 0..1 with sine ease
            return 0.5 - 0.5 * Math.Cos(x * Math.PI);
        }

        private void RebuildPen()
        {
            var brush = FillBrush ?? Brushes.White;
            _ringPen = new Pen(brush, Math.Max(0.5, RingThickness))
            {
                LineJoin = PenLineJoin.Round,
                LineCap = PenLineCap.Round
            };
        }

        private void ReseedPhases()
        {
            uint x = (uint)RandomSeed;
            static uint Next(ref uint s) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
            double R(ref uint s) => Next(ref s) / (double)uint.MaxValue;

            _phaseBreath    = R(ref x) * 2 * Math.PI;
            _phaseWobble    = R(ref x) * 2 * Math.PI;
            _phaseAniso     = R(ref x) * 2 * Math.PI;
            _phaseRotation  = R(ref x) * 360.0;
            _phaseTwinOrbit = R(ref x) * 2 * Math.PI;

            int count = Math.Max(0, Math.Min(64, RingCount));
            if (_ringPhases.Length != count) _ringPhases = new double[count];
            for (int i = 0; i < count; i++)
                _ringPhases[i] = R(ref x) * 2 * Math.PI;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var fill = FillBrush ?? Brushes.White;

            var b = Bounds;
            if (b.Width <= 0 || b.Height <= 0) return;

            double cx = b.X + b.Width * 0.5;
            double cy = b.Y + b.Height * 0.5;
            double halfMin = Math.Min(b.Width, b.Height) * 0.5;

            // Compute full ring extent, then scale by activity so they "grow out"
            int rc = Math.Max(0, Math.Min(64, RingCount));
            double fullRingExtent = (RingsEnabled && rc > 0)
                ? rc * Math.Max(0, RingSpacing) + (Math.Max(0.5, RingThickness) * 0.5) + Math.Max(0.0, RingWobbleAmplitude) + 1.0
                : 0.0;
            double ringExtent = fullRingExtent * _activity;

            // Disc radius budget inside current bounds (reserve space for appearing rings)
            double baseBudget = Math.Max(0, halfMin - 0.5 - ringExtent);

            // Time
            double t = _time;

            // Breath baseline 0..1
            double period = Math.Max(0.2, BreathPeriod);
            double phaseBreath = (2 * Math.PI * (t / period)) + _phaseBreath;
            double breath01 = 0.5 * (1.0 - Math.Cos(phaseBreath));
            double sMin = Math.Min(BreathMinScale, BreathMaxScale);
            double sMax = Math.Max(BreathMinScale, BreathMaxScale);
            double rawBreathScale = sMin + (sMax - sMin) * breath01;
            double breathScale = Lerp(1.0, rawBreathScale, _activity);

            // Wobble
            double wobbleHz = Math.Max(0.0, WobbleFrequency);
            double wobblePx = Math.Max(0.0, WobbleAmplitude);
            double phaseWobble = (2 * Math.PI * wobbleHz * t) + _phaseWobble;
            double rawWobbleScale = 1.0 + (baseBudget > 1e-3 ? (wobblePx * Math.Sin(phaseWobble) / baseBudget) : 0.0);
            double wobbleScale = Lerp(1.0, rawWobbleScale, _activity);

            // Anisotropy
            double anisoHz = Math.Max(0.0, AnisotropyFrequency);
            double phaseAniso = (2 * Math.PI * anisoHz * t) + _phaseAniso;
            double a = Math.Clamp(AxisAnisotropy, 0.0, 0.4) * _activity; // scaled by activity
            double ax = 1.0 + a * Math.Sin(phaseAniso);
            double ay = 1.0 - a * Math.Sin(phaseAniso + Math.PI * 0.5);

            // Final disc radii
            double r = baseBudget * breathScale * wobbleScale;
            double rx = Math.Floor(r * ax) + 0.5;
            double ry = Math.Floor(r * ay) + 0.5;

            // Rotation drift scaled by activity
            double rotDeg = ((RotationSpeed * _activity) * t) + _phaseRotation;
            var rotate = Matrix.CreateTranslation(-cx, -cy)
                      * Matrix.CreateRotation(rotDeg * Math.PI / 180.0)
                      * Matrix.CreateTranslation(cx, cy);

            using (context.PushTransform(rotate))
            {
                // Main filled disc
                var rect = new Rect(cx - rx, cy - ry, rx * 2, ry * 2);
                context.DrawEllipse(fill, null, rect);

                // Twin overlay (also scaled subtly by activity via the shared disc math)
                if (TwinEnabled && _activity > 0.0001)
                {
                    double phaseTwinBreath = phaseBreath + TwinPhaseOffset;
                    double twinBreath01 = 0.5 * (1.0 - Math.Cos(phaseTwinBreath));
                    double twinScale = (sMin + (sMax - sMin) * twinBreath01);
                    twinScale = Lerp(1.0, twinScale, _activity) * TwinScaleMultiplier;

                    double phaseTwinAniso = phaseAniso + TwinPhaseOffset * 0.75;
                    double tax = 1.0 + a * Math.Sin(phaseTwinAniso);
                    double tay = 1.0 - a * Math.Sin(phaseTwinAniso + Math.PI * 0.5);

                    double tr = baseBudget * twinScale * wobbleScale;
                    double trx = Math.Floor(tr * tax) + 0.5;
                    double tryy = Math.Floor(tr * tay) + 0.5;

                    double orbitPhase = (2 * Math.PI * TwinOrbitFrequency * t) + _phaseTwinOrbit;
                    double dx = TwinOffsetPixels * Math.Sin(orbitPhase) * _activity;
                    double dy = TwinOffsetPixels * 0.5 * Math.Cos(orbitPhase * 0.7) * _activity;

                    using (context.PushTransform(Matrix.CreateTranslation(dx, dy)))
                    {
                        var rectTwin = new Rect(cx - trx, cy - tryy, trx * 2, tryy * 2);
                        context.DrawEllipse(fill, null, rectTwin);
                    }
                }

                // Rings (emerge when activity grows; retract on stop)
                if (RingsEnabled && _ringPen is not null && rc > 0 && _activity > 0.0001)
                {
                    double ringWobHz = Math.Max(0.0, RingWobbleFrequency);
                    double ringWobPx = Math.Max(0.0, RingWobbleAmplitude);
                    double opacityBase = Math.Clamp(RingOpacity, 0.0, 1.0);
                    double falloff = Math.Clamp(RingOpacityFalloff, 0.0, 1.0);
                    double cascade = Math.Clamp(RingCascadeFraction, 0.0, 0.9); // fraction per ring

                    // Max allowable ring radius for safety (stroke half inside)
                    double maxRingR = halfMin - 0.5 - (Math.Max(0.5, RingThickness) * 0.5);

                    for (int i = 0; i < rc; i++)
                    {
                        // Staggered gate per ring (cascade outwards)
                        double gate = (_activity - i * cascade) / Math.Max(0.0001, (1.0 - i * cascade));
                        gate = Math.Clamp(gate, 0.0, 1.0);
                        gate = EaseInOutSine(gate);

                        if (gate <= 0.0001) continue;

                        double baseRingR = r + gate * (i + 1) * Math.Max(0, RingSpacing);

                        double wobPhase = (2 * Math.PI * ringWobHz * t) + (i < _ringPhases.Length ? _ringPhases[i] : 0.0);
                        double ringR = baseRingR + (ringWobPx * Math.Sin(wobPhase) * gate);

                        if (ringR <= 0.5 || ringR > maxRingR)
                            continue;

                        double rrX = Math.Floor(ringR * ax) + 0.5;
                        double rrY = Math.Floor(ringR * ay) + 0.5;

                        double op = opacityBase * Math.Pow(falloff, i) * gate;
                        if (op <= 0.001) continue;

                        using (context.PushOpacity(op))
                        {
                            var rrect = new Rect(cx - rrX, cy - rrY, rrX * 2, rrY * 2);
                            context.DrawEllipse(null, _ringPen, rrect);
                        }
                    }
                }
            }
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
