using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TellMeWhenYouSeeThisChange
{
    internal sealed class MainForm : Form
    {
        private enum State { NoTarget, Idle, Armed, Changed, NoReading }

        // Logical (96 dpi) layout; scaled by _s.
        private const int W = 300, H = 406;
        private static readonly Rectangle TitleRect = new Rectangle(12, 12, 276, 48);
        private static readonly Rectangle GaugePanel = new Rectangle(12, 70, 276, 184);
        private static readonly Rectangle TargetGauge = new Rectangle(42, 86, 100, 100);
        private static readonly Rectangle LiveGauge = new Rectangle(186, 104, 64, 64);
        private static readonly Rectangle StatusPanel = new Rectangle(12, 264, 276, 48);
        private static readonly Rectangle LampRect = new Rectangle(32, 279, 18, 18);
        private static readonly Rectangle SpeakerRect = new Rectangle(137, 372, 26, 22);

        private readonly float _s;
        private readonly BrassButton _selectButton;
        private readonly BrassButton _armButton;
        private readonly BrassButton _testButton;
        private readonly BrassCheckBox _topMost;
        private readonly System.Windows.Forms.Timer _poll;
        private readonly System.Windows.Forms.Timer _testTimer;
        private readonly ToneGenerator _tone;

        private bool _hasTarget;
        private Point _target;
        private Color _baseline;
        private Color? _live;
        private bool _armed;
        private State _state = State.NoTarget;
        private double _pulse;

        public MainForm()
        {
            using (Graphics g = CreateGraphics()) _s = g.DpiX / 96f;

            Text = "TMWYSTC";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Iron;
            ClientSize = new Size(S(W), S(H));
            DoubleBuffered = true;
            Icon = BuildIcon();

            _selectButton = new BrassButton { Text = "SELECT PIXEL", Bounds = Sc(new Rectangle(12, 324, 134, 36)) };
            _selectButton.Click += delegate { PickPixel(); };

            _armButton = new BrassButton { Text = "ARM", Bounds = Sc(new Rectangle(154, 324, 134, 36)), Enabled = false };
            _armButton.Click += delegate { SetArmed(!_armed); };

            _testButton = new BrassButton { Text = "TEST TONE", Font = Theme.LabelFont, Bounds = Sc(new Rectangle(204, 372, 84, 22)) };
            _testButton.Click += delegate { TestTone(); };

            _topMost = new BrassCheckBox { Text = "Keep on top", Bounds = Sc(new Rectangle(14, 372, 120, 22)) };
            _topMost.CheckedChanged += delegate { TopMost = _topMost.Checked; };

            Controls.Add(_selectButton);
            Controls.Add(_armButton);
            Controls.Add(_testButton);
            Controls.Add(_topMost);

            _tone = new ToneGenerator(880);

            _poll = new System.Windows.Forms.Timer { Interval = 100 };
            _poll.Tick += delegate { Poll(); };

            _testTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _testTimer.Tick += delegate { _testTimer.Stop(); if (_state != State.Changed) SetTone(false); };
        }

        private int S(int v) { return (int)Math.Round(v * _s); }
        private Point Sc(Point p) { return new Point(S(p.X), S(p.Y)); }
        private Rectangle Sc(Rectangle r) { return new Rectangle(S(r.X), S(r.Y), S(r.Width), S(r.Height)); }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Dark title bar on Windows 10 20H1+ / 11; silently ignored elsewhere.
            try { int on = 1; DwmSetWindowAttribute(Handle, 20, ref on, 4); } catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _poll.Stop();
            _tone.Dispose();
            base.OnFormClosed(e);
        }

        // ---------------------------------------------------------------- picking

        private void PickPixel()
        {
            SetArmed(false);
            Hide();
            Application.DoEvents();
            Thread.Sleep(250); // let the window-hide animation finish before capturing

            Rectangle vs = SystemInformation.VirtualScreen;
            Bitmap shot = new Bitmap(vs.Width, vs.Height, PixelFormat.Format32bppPArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(shot))
                    g.CopyFromScreen(vs.Left, vs.Top, 0, 0, vs.Size, CopyPixelOperation.SourceCopy);

                using (PickerForm picker = new PickerForm(shot, vs))
                {
                    if (picker.ShowDialog() == DialogResult.OK)
                    {
                        _target = picker.SelectedScreenPoint;
                        Color live;
                        // Prefer the live reading as the baseline; fall back to the frozen capture.
                        _baseline = NativeMethods.TryReadScreenPixel(_target.X, _target.Y, out live)
                            ? live : Color.FromArgb(picker.SelectedColor.R, picker.SelectedColor.G, picker.SelectedColor.B);
                        _live = _baseline;
                        _hasTarget = true;
                        _armButton.Enabled = true;
                        _state = State.Idle;
                        _poll.Start();
                    }
                }
            }
            finally
            {
                shot.Dispose();
                Show();
                Activate();
                Invalidate();
            }
        }

        // ---------------------------------------------------------------- arming / polling

        private void SetArmed(bool armed)
        {
            _armed = armed && _hasTarget;
            _armButton.Text = _armed ? "DISARM" : "ARM";
            if (!_armed) SetTone(false);
            _state = !_hasTarget ? State.NoTarget : _armed ? State.Armed : State.Idle;
            if (_armed) Poll();
            Invalidate();
        }

        private void Poll()
        {
            if (!_hasTarget) return;
            Color c;
            bool ok = NativeMethods.TryReadScreenPixel(_target.X, _target.Y, out c);
            _live = ok ? (Color?)c : null;

            if (_armed)
            {
                if (!ok)
                {
                    // Can't see the screen (locked, secure desktop...). Never treat that as a change.
                    if (_state != State.Changed) _state = State.NoReading;
                }
                else if (c.ToArgb() != _baseline.ToArgb())
                {
                    if (_state != State.Changed)
                    {
                        _state = State.Changed;
                        SetTone(true);
                    }
                }
                else
                {
                    if (_state == State.Changed) SetTone(false);
                    _state = State.Armed;
                }
            }

            _pulse += 0.35;
            Invalidate(Sc(GaugePanel));
            Invalidate(Sc(StatusPanel));
        }

        private void SetTone(bool on)
        {
            if (on == _tone.IsPlaying) return;
            if (on) _tone.Start(); else _tone.Stop();
            Invalidate(Sc(SpeakerRect));
        }

        private void TestTone()
        {
            if (_state == State.Changed) return;
            SetTone(true);
            _testTimer.Stop();
            _testTimer.Start();
        }

        // ---------------------------------------------------------------- painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // title plate
            Rectangle title = Sc(TitleRect);
            Theme.RivetedPanel(g, title);
            int mid = title.Y + title.Height / 2;
            DrawCentered(g, "\"TELL ME WHEN YOU", Theme.TitleFont, Theme.BrassHi, new Rectangle(title.X, mid - S(16), title.Width, S(16)));
            DrawCentered(g, "SEE THIS CHANGE\"", Theme.TitleFont, Theme.BrassHi, new Rectangle(title.X, mid, title.Width, S(16)));

            // gauges
            Theme.RivetedPanel(g, Sc(GaugePanel));
            Theme.Gauge(g, Sc(TargetGauge), _hasTarget ? (Color?)_baseline : null);
            Theme.Gauge(g, Sc(LiveGauge), _hasTarget ? _live : null);

            Rectangle tCol = Sc(new Rectangle(TargetGauge.X - 20, TargetGauge.Bottom + 4, TargetGauge.Width + 40, 16));
            Rectangle lCol = Sc(new Rectangle(LiveGauge.X - 30, TargetGauge.Bottom + 4, LiveGauge.Width + 60, 16));
            DrawCentered(g, "target", Theme.LabelFont, Theme.InkDim, tCol);
            DrawCentered(g, "live", Theme.LabelFont, Theme.InkDim, lCol);
            tCol.Offset(0, S(17)); lCol.Offset(0, S(17));
            DrawCentered(g, _hasTarget ? Theme.Hex(_baseline) : "#------", Theme.MonoFont, Theme.Ink, tCol);
            DrawCentered(g, _hasTarget && _live.HasValue ? Theme.Hex(_live.Value) : "#------", Theme.MonoFont,
                _state == State.Changed ? Theme.LampRed : Theme.Ink, lCol);
            tCol.Offset(0, S(17));
            DrawCentered(g, _hasTarget ? string.Format("x {0}  y {1}", _target.X, _target.Y) : "no pixel chosen",
                Theme.MonoSmall, Theme.Brass, tCol);

            Theme.Speaker(g, Sc(SpeakerRect), _tone.IsPlaying);

            // status
            Rectangle status = Sc(StatusPanel);
            Theme.RivetedPanel(g, status);
            Color lamp; bool lit; string text; float glow = 0.6f;
            switch (_state)
            {
                case State.Armed: lamp = Theme.LampGreen; lit = true; text = "ARMED — WATCHING"; break;
                case State.Changed:
                    lamp = Theme.LampRed; lit = true; text = "CHANGE DETECTED";
                    glow = (float)(0.55 + 0.45 * Math.Sin(_pulse)); break;
                case State.NoReading: lamp = Color.FromArgb(0xFF, 0xB3, 0x3B); lit = true; text = "NO READING"; break;
                case State.Idle: lamp = Theme.LampOff; lit = false; text = "DISARMED"; break;
                default: lamp = Theme.LampOff; lit = false; text = "SELECT A PIXEL"; break;
            }
            Rectangle lampRect = Sc(LampRect);
            Theme.Lamp(g, lampRect, lamp, lit, glow);
            Rectangle textRect = new Rectangle(lampRect.Right + S(16), status.Y, status.Right - lampRect.Right - S(28), status.Height);
            TextRenderer.DrawText(g, text, Theme.ButtonFont, textRect, lit ? Theme.Ink : Theme.InkDim,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }

        private static void DrawCentered(Graphics g, string text, Font font, Color color, Rectangle r)
        {
            TextRenderer.DrawText(g, text, font, r, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        private static Icon BuildIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    Theme.Gauge(g, new Rectangle(1, 1, 30, 30), Theme.LampRed);
                    using (Pen p = new Pen(Theme.Ink, 1.5f))
                    {
                        g.DrawLine(p, 16, 9, 16, 23);
                        g.DrawLine(p, 9, 16, 23, 16);
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
