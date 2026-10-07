using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TellMeWhenYouSeeThisChange
{
    internal static class Theme
    {
        public static readonly Color Iron = Color.FromArgb(0x1C, 0x1A, 0x17);
        public static readonly Color Plate = Color.FromArgb(0x2A, 0x26, 0x22);
        public static readonly Color PlateEdge = Color.FromArgb(0x12, 0x10, 0x0E);
        public static readonly Color Brass = Color.FromArgb(0xB5, 0x87, 0x3A);
        public static readonly Color BrassHi = Color.FromArgb(0xE0, 0xB5, 0x66);
        public static readonly Color BrassLo = Color.FromArgb(0x6E, 0x50, 0x22);
        public static readonly Color Ink = Color.FromArgb(0xE8, 0xDC, 0xC4);
        public static readonly Color InkDim = Color.FromArgb(0x8A, 0x80, 0x70);
        public static readonly Color LampOff = Color.FromArgb(0x3A, 0x34, 0x2E);
        public static readonly Color LampGreen = Color.FromArgb(0x5C, 0xD6, 0x6A);
        public static readonly Color LampRed = Color.FromArgb(0xFF, 0x3B, 0x2F);

        public static readonly Font TitleFont = new Font("Georgia", 9f, FontStyle.Bold | FontStyle.Italic);
        public static readonly Font LabelFont = new Font("Georgia", 8f, FontStyle.Italic);
        public static readonly Font MonoFont = new Font("Consolas", 10f, FontStyle.Regular);
        public static readonly Font MonoSmall = new Font("Consolas", 8.5f, FontStyle.Regular);
        public static readonly Font ButtonFont = new Font("Georgia", 9f, FontStyle.Bold);

        public static string Hex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Rivet(Graphics g, float cx, float cy, float r)
        {
            RectangleF rc = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            using (LinearGradientBrush b = new LinearGradientBrush(rc, BrassHi, BrassLo, 45f))
                g.FillEllipse(b, rc);
            using (Pen p = new Pen(Color.FromArgb(120, 0, 0, 0)))
                g.DrawEllipse(p, rc);
        }

        /// <summary>Dark plate with a thin brass border and a rivet in each corner.</summary>
        public static void RivetedPanel(Graphics g, Rectangle r)
        {
            using (GraphicsPath path = RoundRect(r, 6))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, Plate, Iron, 90f))
                    g.FillPath(b, path);
                using (Pen p = new Pen(BrassLo, 1.5f))
                    g.DrawPath(p, path);
            }
            const float inset = 9f;
            Rivet(g, r.Left + inset, r.Top + inset, 3f);
            Rivet(g, r.Right - inset, r.Top + inset, 3f);
            Rivet(g, r.Left + inset, r.Bottom - inset, 3f);
            Rivet(g, r.Right - inset, r.Bottom - inset, 3f);
        }

        /// <summary>A round colour "gauge": brass bezel around a swatch, with a glass highlight.</summary>
        public static void Gauge(Graphics g, Rectangle r, Color? fill)
        {
            using (LinearGradientBrush b = new LinearGradientBrush(r, BrassHi, BrassLo, 45f))
                g.FillEllipse(b, r);
            Rectangle inner = Rectangle.Inflate(r, -Math.Max(4, r.Width / 14), -Math.Max(4, r.Width / 14));
            using (SolidBrush b = new SolidBrush(PlateEdge))
                g.FillEllipse(b, inner);
            Rectangle face = Rectangle.Inflate(inner, -2, -2);
            if (fill.HasValue)
            {
                using (SolidBrush b = new SolidBrush(fill.Value))
                    g.FillEllipse(b, face);
            }
            else
            {
                using (HatchBrush b = new HatchBrush(HatchStyle.WideDownwardDiagonal, Plate, Iron))
                    g.FillEllipse(b, face);
            }
            // glass glint
            Rectangle glint = new Rectangle(face.X + face.Width / 6, face.Y + face.Height / 10, face.Width * 2 / 3, face.Height / 3);
            using (LinearGradientBrush b = new LinearGradientBrush(glint, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                g.FillEllipse(b, glint);
        }

        /// <summary>Indicator lamp with an optional glow halo.</summary>
        public static void Lamp(Graphics g, Rectangle r, Color color, bool lit, float glow)
        {
            if (lit && glow > 0)
            {
                Rectangle halo = Rectangle.Inflate(r, r.Width / 2, r.Height / 2);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(halo);
                    using (PathGradientBrush pb = new PathGradientBrush(gp))
                    {
                        pb.CenterColor = Color.FromArgb((int)(140 * glow), color);
                        pb.SurroundColors = new[] { Color.FromArgb(0, color) };
                        g.FillEllipse(pb, halo);
                    }
                }
            }
            Rectangle bezel = Rectangle.Inflate(r, 3, 3);
            using (LinearGradientBrush b = new LinearGradientBrush(bezel, BrassHi, BrassLo, 45f))
                g.FillEllipse(b, bezel);
            Color body = lit ? color : LampOff;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(r);
                using (PathGradientBrush pb = new PathGradientBrush(gp))
                {
                    pb.CenterPoint = new PointF(r.X + r.Width * 0.38f, r.Y + r.Height * 0.35f);
                    pb.CenterColor = lit ? Blend(color, Color.White, 0.55f) : Blend(LampOff, Color.White, 0.12f);
                    pb.SurroundColors = new[] { Blend(body, Color.Black, 0.35f) };
                    g.FillEllipse(pb, r);
                }
            }
        }

        /// <summary>Small speaker glyph; the three sound waves only show while <paramref name="on"/>.</summary>
        public static void Speaker(Graphics g, Rectangle r, bool on)
        {
            float u = r.Height / 22f;
            float x0 = r.X + u, cy = r.Y + r.Height / 2f;
            Color body = on ? BrassHi : BrassLo;

            using (SolidBrush b = new SolidBrush(body))
            {
                g.FillPolygon(b, new[]
                {
                    new PointF(x0, cy - 3 * u), new PointF(x0 + 4 * u, cy - 3 * u),
                    new PointF(x0 + 9 * u, cy - 7 * u), new PointF(x0 + 9 * u, cy + 7 * u),
                    new PointF(x0 + 4 * u, cy + 3 * u), new PointF(x0, cy + 3 * u)
                });
            }
            if (!on) return;

            using (Pen p = new Pen(BrassHi, Math.Max(1.2f, 1.6f * u)))
            {
                p.StartCap = p.EndCap = LineCap.Round;
                float cx = x0 + 9 * u;
                foreach (float radius in new[] { 4f * u, 7.5f * u, 11f * u })
                    g.DrawArc(p, cx - radius, cy - radius, radius * 2, radius * 2, -45, 90);
            }
        }

        public static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    /// <summary>Checkbox with a brass box and a dark, high-contrast check mark.</summary>
    internal sealed class BrassCheckBox : Control
    {
        private bool _checked, _hover;

        public event EventHandler CheckedChanged;

        public BrassCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Theme.Iron;
            Font = Theme.LabelFont;
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) Checked = !Checked;
            base.OnKeyDown(e);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = (int)(Height * 0.6f);
            Rectangle box = new Rectangle(1, (Height - size) / 2, size, size);
            using (GraphicsPath path = Theme.RoundRect(box, 2))
            {
                if (_checked)
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(box, Theme.BrassHi, Theme.Brass, 90f))
                        g.FillPath(b, path);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Theme.PlateEdge)) g.FillPath(b, path);
                }
                using (Pen p = new Pen(_hover ? Theme.BrassHi : Theme.Brass, 1.2f))
                    g.DrawPath(p, path);
            }

            if (_checked)
            {
                using (Pen p = new Pen(Theme.Iron, Math.Max(2f, size / 6f)))
                {
                    p.StartCap = p.EndCap = LineCap.Round;
                    p.LineJoin = LineJoin.Round;
                    g.DrawLines(p, new[]
                    {
                        new PointF(box.X + size * 0.22f, box.Y + size * 0.52f),
                        new PointF(box.X + size * 0.42f, box.Y + size * 0.72f),
                        new PointF(box.X + size * 0.78f, box.Y + size * 0.28f)
                    });
                }
            }

            Rectangle text = new Rectangle(box.Right + 6, 0, Width - box.Right - 6, Height);
            TextRenderer.DrawText(g, Text, Font, text, _checked || _hover ? Theme.Ink : Theme.InkDim,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }
    }

    /// <summary>Flat brass push-button.</summary>
    internal sealed class BrassButton : Control
    {
        private bool _hover, _down;

        public BrassButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.ButtonFont;
            Cursor = Cursors.Hand;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color top = Theme.BrassHi, bottom = Theme.Brass;
            if (!Enabled) { top = Theme.Blend(Theme.Plate, Theme.BrassLo, 0.5f); bottom = Theme.Plate; }
            else if (_down) { top = Theme.Brass; bottom = Theme.BrassLo; }
            else if (_hover) { top = Theme.Blend(Theme.BrassHi, Color.White, 0.2f); bottom = Theme.BrassHi; }

            using (GraphicsPath path = Theme.RoundRect(r, 4))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, top, bottom, 90f))
                    g.FillPath(b, path);
                using (Pen p = new Pen(Enabled ? Theme.BrassLo : Theme.PlateEdge, 1.2f))
                    g.DrawPath(p, path);
            }
            Color text = Enabled ? Theme.Iron : Theme.InkDim;
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
