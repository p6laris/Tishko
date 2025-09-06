using System;                                       // Base types (TimeSpan, Math, etc.)
using System.Diagnostics;                           // Stopwatch for timing animations
using System.Linq;                                  // Enumerable.Range used to build 00..23 / 00..59 lists
using Avalonia;                                     // Core Avalonia (AvaloniaObject, AvaloniaProperty, etc.)
using Avalonia.Controls;                            // Controls like ListBox, ScrollViewer
using Avalonia.Controls.Primitives;                 // TemplatedControl base class + TemplateAppliedEventArgs
using Avalonia.Input;                               // Pointer/Keyboard events and types
using Avalonia.Interactivity;                       // Routed events (AddHandler with RoutingStrategies)
using Avalonia.Threading;                           // Dispatcher/UI thread scheduling
using Avalonia.VisualTree;                          // Traversing visual tree to find ScrollViewer children

namespace Tishko.Controls
{
    // Public enum exposed as a StyledProperty so it can be set from XAML.
    // We keep two feelings for the spring animation: a gentle one (Soft) and a slightly bouncy one (Bouncy).
    public enum SnapFeel
    {
        Soft,   // calm, slightly elastic
        Bouncy  // playful overshoot
    }

    // This is a *templated control*: its visuals are provided by a ControlTemplate in XAML.
    // The class exposes Avalonia StyledProperties (Hour/Minute/Time/SnapFeel) that the template binds to indirectly.
    public class WheelTimePicker : TemplatedControl
    {
        // Avalonia StyledProperty for hour [0..23].
        // AvaloniaProperty.Register attaches metadata and coercion.
        public static readonly StyledProperty<int> HourProperty =
            AvaloniaProperty.Register<WheelTimePicker, int>(nameof(Hour), 0,
                coerce: static (_, v) => Math.Clamp(v, 0, 23));      // Coerce keeps value in range no matter what

        // Avalonia StyledProperty for minute [0..59].
        public static readonly StyledProperty<int> MinuteProperty =
            AvaloniaProperty.Register<WheelTimePicker, int>(nameof(Minute), 0,
                coerce: static (_, v) => Math.Clamp(v, 0, 59));

        // Optional “combined” time property for convenience (not strictly needed but nice to have).
        public static readonly StyledProperty<TimeSpan> TimeProperty =
            AvaloniaProperty.Register<WheelTimePicker, TimeSpan>(nameof(Time), TimeSpan.Zero);

        // Which spring feel to use when snapping (Soft/Bouncy).
        public static readonly StyledProperty<SnapFeel> SnapFeelProperty =
            AvaloniaProperty.Register<WheelTimePicker, SnapFeel>(nameof(SnapFeel), SnapFeel.Soft);

        // Height of each row in the wheels (must match XAML ItemContainerTheme Height).
        private const double ItemHeight = 32;

        // These are references to the parts resolved from the ControlTemplate (named elements with x:Name).
        private ListBox? _hoursList;
        private ListBox? _minutesList;

        // Internal flags used to avoid feedback loops while we programmatically change selection/scroll.
        private bool _syncing;
        private bool _suppressSyncDuringDrag;

        // Small struct to hold dragging state for one wheel (hours or minutes).
        private sealed class DragState
        {
            public bool Dragging;            // True while left mouse is held and we are panning
            public int PointerId;            // Which pointer started the drag (defensive)
            public double StartPointerY;     // Where the pointer started in local coordinates (pixels)
            public double StartOffsetY;      // Scroll offset Y at press time (pixels)
        }

        // One drag state per wheel
        private readonly DragState _hoursDrag = new();
        private readonly DragState _minutesDrag = new();

        // We run animations with a DispatcherTimer at ~60Hz.
        // We keep separate timers for hours and minutes so they don't fight.
        private DispatcherTimer? _animTimerHours;
        private DispatcherTimer? _animTimerMinutes;

        // CLR wrappers for StyledProperties (so you can write Hour = 8; and bind in XAML as Hour="{...}")
        public int Hour
        {
            get => GetValue(HourProperty);
            set => SetValue(HourProperty, value);
        }

        public int Minute
        {
            get => GetValue(MinuteProperty);
            set => SetValue(MinuteProperty, value);
        }

        public TimeSpan Time
        {
            get => GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public SnapFeel SnapFeel
        {
            get => GetValue(SnapFeelProperty);
            set => SetValue(SnapFeelProperty, value);
        }

        // Static ctor runs once per type. We subscribe to property change events here.
        // AddClassHandler wires changes of Hour/Minute/Time to our synchronization methods.
        static WheelTimePicker()
        {
            // Whenever Hour changes, recompute Time and update lists (unless we’re already syncing).
            HourProperty.Changed.AddClassHandler<WheelTimePicker>((o, _) => o.UpdateTimeFromParts());
            // Same for Minute.
            MinuteProperty.Changed.AddClassHandler<WheelTimePicker>((o, _) => o.UpdateTimeFromParts());
            // When external code changes Time, split it back to Hour/Minute.
            TimeProperty.Changed.AddClassHandler<WheelTimePicker>((o, _) => o.UpdatePartsFromTime());
        }

        // Called when the template (XAML visuals) gets applied. We grab template parts here.
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Find the ListBox parts by their x:Name in the template.
            _hoursList = e.NameScope.Find<ListBox>("PART_Hours");
            _minutesList = e.NameScope.Find<ListBox>("PART_Minutes");

            // Initialize hours wheel if present
            if (_hoursList is not null)
            {
                // Provide the items 00..23 (strings), so XAML can bind {Binding} in the DataTemplate
                _hoursList.ItemsSource = Enumerable.Range(0, 24).Select(i => i.ToString("00"));
                // Keep Hour property in sync when user changes selection (unless we are dragging/program-setting)
                _hoursList.SelectionChanged += Hours_SelectionChanged;
                // Wire input handlers (wheel/keys/drag) for this list
                WireInput(_hoursList, _hoursDrag, () => Hour, i => Hour = i, 0, 23, isHours: true);
            }

            // Initialize minutes wheel
            if (_minutesList is not null)
            {
                _minutesList.ItemsSource = Enumerable.Range(0, 60).Select(i => i.ToString("00"));
                _minutesList.SelectionChanged += Minutes_SelectionChanged;
                WireInput(_minutesList, _minutesDrag, () => Minute, i => Minute = i, 0, 59, isHours: false);
            }

            // Important: center/scroll to the initial Hour/Minute AFTER layout is complete,
            // otherwise ScrollViewer might not have right Extent/Viewport yet.
            Dispatcher.UIThread.Post(
                () => UpdateListsSelection(center: true),
                DispatcherPriority.Loaded);
        }

        // Helper: walk down the visual tree of a control to find its first ScrollViewer (inside ListBox)
        private static ScrollViewer? GetSV(Control c) =>
            c.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        // Clamp a vertical offset inside the ScrollViewer’s scrollable range:
        // valid range is [0 .. Extent.Height - Viewport.Height]
        private static double ClampOffset(ScrollViewer sv, double y)
        {
            var max = Math.Max(0, sv.Extent.Height - sv.Viewport.Height); // avoid negative if content < viewport
            return Math.Min(Math.Max(0, y), max);                          // clamp to [0, max]
        }

        // Circular wrap helper: ensures value rolls within [min..maxInclusive].
        // Example: Wrap(-1, 0, 23) => 23 ; Wrap(24, 0, 23) => 0
        private static int Wrap(int value, int min, int maxInclusive)
        {
            var n = maxInclusive - min + 1;                 // number of discrete slots
            var m = (value - min) % n;                       // shift to 0..n-1 then mod
            if (m < 0) m += n;                               // C# % can be negative; fix that
            return min + m;                                  // shift back to original range
        }

        // Utility: set ListBox.SelectedIndex if it differs (avoids redundant change events)
        private static void SetSelected(ListBox list, int index)
        {
            if (list.SelectedIndex != index)
                list.SelectedIndex = index;
        }

        // Ask to center the wheel on a given item index with animation.
        // The template puts each item row at y = index * ItemHeight. We animate ScrollViewer.Offset.Y to that.
        private void CenterOnIndexAnimated(ListBox list, int index, bool _unusedBounce, bool isHours)
        {
            var sv = GetSV(list);                // grab the internal ScrollViewer (inside the ListBox)
            if (sv is null) return;

            var target = index * ItemHeight;     // vertical pixel target for that row
            StartSpringAnimation(isHours, sv, target); // run our small spring simulation towards that Y
        }

        // The “spring” animation stepper.
        // This implements the physical model: a mass-spring-damper moving toward targetY.
        // Math (explained simply):
        //   position = y, velocity = v. The “spring” pulls with force proportional to how far y is from target:
        //   F_spring  = -k * (y - targetY)
        //   F_damper  = -c * v   (resists motion to avoid infinite oscillation)
        //   Accel a   = F_total / m ; we take m=1 (unit mass), so a = F_total.
        // We integrate over small time steps dt (~1/60 sec) to update v and y: v += a*dt; y += v*dt.
        private void StartSpringAnimation(bool isHours, ScrollViewer sv, double targetY)
        {
            // Stop any previous animation on this wheel (so the new one takes over cleanly)
            var timer = isHours ? _animTimerHours : _animTimerMinutes;
            timer?.Stop();

            // Initial conditions: start at current scroll position with zero velocity (no ongoing motion)
            double y = sv.Offset.Y;
            double v = 0.0;

            // Choose stiffness (k) and damping (c) based on SnapFeel.
            // Higher k → stronger pull to target (faster).
            // Higher c → more damping (less bounce).
            double k, c;
            if (SnapFeel == SnapFeel.Bouncy)
            {
                k = 2800; c = 38;   // lively: strong spring, light damping → small overshoot, then settle
            }
            else // Soft
            {
                k = 2400; c = 80;   // smoother: slightly weaker spring, more damping → no/low overshoot
            }

            // Track time so dt is real elapsed time, not a fixed value.
            var sw = Stopwatch.StartNew();
            double last = 0;

            // Run at ~60 FPS. Each tick advances the physics a little bit.
            var tmr = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) };
            tmr.Tick += (_, __) =>
            {
                var now = sw.Elapsed.TotalSeconds;           // seconds since animation started
                var dt = Math.Max(1e-4, now - last);         // delta time since last frame (avoid 0)
                last = now;

                var dx = y - targetY;                        // how far from target (positive: below target)
                var a = -k * dx - c * v;                     // acceleration = spring + damper forces (mass=1)

                v += a * dt;                                 // integrate velocity
                y += v * dt;                                 // integrate position

                // If the scroll hits top/bottom, clamp it and reset velocity (simulate collision with a wall).
                var clamped = ClampOffset(sv, y);
                if (Math.Abs(clamped - y) > 0.001) { y = clamped; v = 0; }

                // Apply the scroll offset to the ScrollViewer
                sv.Offset = new Vector(sv.Offset.X, y);

                // Stop condition: close enough to target and slow enough not to be visible jitter.
                if (Math.Abs(y - targetY) < 0.25 && Math.Abs(v) < 2.0)
                {
                    sv.Offset = new Vector(sv.Offset.X, targetY); // snap exactly to final pixel
                    tmr.Stop();                                    // stop the timer (animation complete)
                }
            };

            // Remember this timer so we can stop/replace it next time
            if (isHours) _animTimerHours = tmr; else _animTimerMinutes = tmr;
            tmr.Start();                                         // off we go
        }

        // Center instantly (no animation); used for initial layout or force-sync.
        private static void CenterOnIndexImmediate(ListBox list, int index)
        {
            var sv = GetSV(list);
            if (sv is null) return;
            sv.Offset = new Vector(sv.Offset.X, index * ItemHeight);   // direct jump to row Y
        }

        // Attach input handlers to one of the lists (hours or minutes).
        // We:
        //   - Handle Up/Down arrows
        //   - Handle mouse wheel
        //   - Implement click-drag panning with capture, and snap on release
        private void WireInput(ListBox list,
                               DragState drag,
                               Func<int> getIndex,             // getter for Hour or Minute (depending on list)
                               Action<int> setIndex,           // setter for Hour or Minute
                               int minIndex,                   // 0 for hours/minutes
                               int maxIndex,                   // 23 or 59
                               bool isHours)                   // which list this is (for separate timers)
        {
            list.Focusable = true;                             // allow keyboard focus so keys work

            // Keyboard navigation (Up/Down) — wraps 23->0 or 0->23 etc.
            list.AddHandler(InputElement.KeyDownEvent, (_, e) =>
            {
                if (e.Key != Key.Up && e.Key != Key.Down) return;     // ignore other keys
                var step = e.Key == Key.Up ? -1 : +1;                  // Up decrements, Down increments
                var next = Wrap(getIndex() + step, minIndex, maxIndex);
                setIndex(next);                                        // update property (Hour/Minute)
                SetSelected(list, next);                               // reflect in ListBox selection
                CenterOnIndexAnimated(list, next, _unusedBounce: false, isHours); // smooth snap
                e.Handled = true;                                      // mark as handled so it doesn't bubble
            }, RoutingStrategies.Tunnel);                               // Tunnel = preview phase before control handles it

            // Mouse wheel (classic list stepping) — also wraps.
            list.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) =>
            {
                var step = e.Delta.Y > 0 ? -1 : e.Delta.Y < 0 ? +1 : 0; // wheel up => previous; wheel down => next
                if (step == 0) return;

                var next = Wrap(getIndex() + step, minIndex, maxIndex);
                setIndex(next);
                SetSelected(list, next);
                CenterOnIndexAnimated(list, next, _unusedBounce: false, isHours);
                e.Handled = true;
            }, RoutingStrategies.Tunnel);

            // Pointer pressed: start a drag-pan if left button is down
            list.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
            {
                if (!e.GetCurrentPoint(list).Properties.IsLeftButtonPressed || drag.Dragging) return;

                var sv = GetSV(list);
                if (sv is null) return;

                drag.Dragging = true;                           // we are now dragging
                drag.PointerId = e.Pointer.Id;                  // remember which pointer
                drag.StartPointerY = e.GetPosition(list).Y;     // where it started (local Y)
                drag.StartOffsetY = sv.Offset.Y;                // current scroll offset (Y)

                _suppressSyncDuringDrag = true;                 // temporarily ignore selection->property sync
                list.Focus();                                   // get focus for keys
                e.Pointer.Capture(list);                        // capture pointer so we keep getting move/release events
                e.Handled = true;
            }, RoutingStrategies.Tunnel);

            // Pointer moved: if dragging, update ScrollViewer.Offset directly (no selection change yet)
            list.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
            {
                if (!drag.Dragging || e.Pointer.Id != drag.PointerId) return;

                var sv = GetSV(list);
                if (sv is null) return;

                var dy = e.GetPosition(list).Y - drag.StartPointerY;  // how much did we move since press (pixels)
                var newOffset = ClampOffset(sv, drag.StartOffsetY - dy); // invert so dragging down scrolls content up
                sv.Offset = new Vector(sv.Offset.X, newOffset);        // apply pan

                e.Handled = true;
            }, RoutingStrategies.Tunnel);

            // Called to finish drag: compute nearest row, wrap it, set selection, animate to it.
            void FinishDrag()
            {
                var sv = GetSV(list);
                if (sv is null) return;

                // Convert current scroll offset to the nearest item index:
                // offset / ItemHeight gives “how many item-heights down we are”.
                // Round to nearest to get the snap target row.
                var rawIndex = (int)Math.Round(sv.Offset.Y / ItemHeight, MidpointRounding.AwayFromZero);
                var index = Wrap(rawIndex, minIndex, maxIndex); // keep it in range (wrap hours/minutes)

                setIndex(index);                                // update Hour/Minute property
                SetSelected(list, index);                       // reflect in ListBox
                CenterOnIndexAnimated(list, index, _unusedBounce: false, isHours); // smooth to exact pixel

                _suppressSyncDuringDrag = false;                // re-enable selection<->property syncing
                drag.Dragging = false;                          // end drag
            }

            // Pointer released: if this is the same pointer, release capture and finish the drag.
            list.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
            {
                if (drag.Dragging && e.Pointer.Id == drag.PointerId)
                {
                    e.Pointer.Capture(null);    // release capture so pointer can go elsewhere
                    FinishDrag();               // snap and select
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);

            // If we lose capture unexpectedly (e.g., mouse leaves window), still finish gracefully.
            list.AddHandler(InputElement.PointerCaptureLostEvent, (_, __) =>
            {
                if (drag.Dragging) FinishDrag();
            }, RoutingStrategies.Tunnel);
        }

        // When ListBox selection changes (because of a click or keyboard), update Hour if we’re not in a programmatic sync.
        private void Hours_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_syncing || _suppressSyncDuringDrag) return;    // ignore if we set it ourselves or during drag
            if (_hoursList?.SelectedIndex is int idx && idx >= 0)
                Hour = idx;                                     // push value into the StyledProperty (triggers Time update)
        }

        // Same for minutes
        private void Minutes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_syncing || _suppressSyncDuringDrag) return;
            if (_minutesList?.SelectedIndex is int idx && idx >= 0)
                Minute = idx;
        }

        // If Hour/Minute changes (via properties), compute Time and reflect selection in lists.
        private void UpdateTimeFromParts()
        {
            if (_suppressSyncDuringDrag || _syncing) return;    // avoid feedback loops
            _syncing = true;
            Time = new TimeSpan(Hour, Minute, 0);               // recompute combined Time
            UpdateListsSelection(center: true);                  // update selection/scroll in UI
            _syncing = false;
        }

        // If Time changes (externally), split it back into Hour/Minute and update lists.
        private void UpdatePartsFromTime()
        {
            if (_syncing) return;                                // avoid re-entrancy
            _syncing = true;
            Hour = Wrap(Time.Hours, 0, 23);                      // clamp/wrap the incoming Time components
            Minute = Wrap(Time.Minutes, 0, 59);
            UpdateListsSelection(center: true);
            _syncing = false;
        }

        // Ensure ListBox.SelectedIndex matches Hour/Minute and optionally center the view to that row.
        private void UpdateListsSelection(bool center)
        {
            if (_hoursList != null) SetSelected(_hoursList, Hour);
            if (_minutesList != null) SetSelected(_minutesList, Minute);

            if (center)
            {
                if (_hoursList != null) CenterOnIndexImmediate(_hoursList, Hour);
                if (_minutesList != null) CenterOnIndexImmediate(_minutesList, Minute);
            }
        }
    }
}
