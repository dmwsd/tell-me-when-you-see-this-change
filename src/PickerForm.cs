using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TellMeWhenYouSeeThisChange
{
    /// <summary>
    /// Full-virtual-screen overlay showing a frozen screenshot with a Snagit-style pixel loupe.
    /// Click / Enter selects, arrows nudge (Shift = 10px), Esc / right-click cancels.
    /// </summary>
    internal sealed class PickerForm : Form
    {
        private const int Cells = 21;   // odd, so there is a centre pixel
        private const int Zoom = 8;
        private const int LoupeSize = Cells * Zoom;
        private const int Border = 4;
        private const int StripHeight = 44;
        private const int Offset = 28;

        private readonly Bitmap _shot;
        private readonly Rectangle _virtual;
        private Point _cursor;          // client coordinates == bitmap coordinates
        private Rectangle _lastLoupe = Rectangle.Empty;

        public Point SelectedScreenPoint { get; private set; }
        public Color SelectedColor { get; private set; }

        public PickerForm(Bitmap screenshot, Rectangle virtualScreen)
        {
            _shot = screenshot;
            _virtual = virtualScreen;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.None;
            Bounds = virtualScreen;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Bounds = _virtual; // re-assert in case Windows adjusted it on show
            Activate();
            MoveTo(PointToClient(Cursor.Position));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MoveTo(e.Location);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { MoveTo(e.Location); Accept(); }
            else if (e.Button == MouseButtons.Right) { DialogResult = DialogResult.Cancel; }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            int step = (keyData & Keys.Shift) == Keys.Shift ? 10 : 1;
            int dx = 0, dy = 0;
            switch (key)
            {
                case Keys.Escape: DialogResult = DialogResult.Cancel; return true;
                case Keys.Enter:
                case Keys.Space: Accept(); return true;
                case Keys.Left: dx = -step; break;
                case Keys.Right: dx = step; break;
                case Keys.Up: dy = -step; break;
                case Keys.Down: dy = step; break;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }
            Point p = new Point(
                Math.Max(0, Math.Min(_shot.Width - 1, _cursor.X + dx)),
                Math.Max(0, Math.Min(_shot.Height - 1, _cursor.Y + dy)));
            Cursor.Position = PointToScreen(p);
            MoveTo(p);
            return true;
        }

        private void Accept()
        {
            SelectedScreenPoint = new Point(_cursor.X + _virtual.X, _cursor.Y + _virtual.Y);
            SelectedColor = _shot.GetPixel(_cursor.X, _cursor.Y);
            DialogResult = DialogResult.OK;
        }

        private void MoveTo(Point p)
        {
            p.X = Math.Max(0, Math.Min(_shot.Width - 1, p.X));
            p.Y = Math.Max(0, Math.Min(_shot.Height - 1, p.Y));
            if (p == _cursor && !_lastLoupe.IsEmpty) return;
            _cursor = p;
            Rectangle next = LoupeBounds(p);
            if (!_lastLoupe.IsEmpty) Invalidate(Rectangle.Inflate(_lastLoupe, 2, 2));
            Invalidate(Rectangle.Inflate(next, 2, 2));
            _lastLoupe = next;
        }

        /// <summary>Whole loupe footprint (bezel + magnified view + readout strip), in client coords.</summary>
        private Rectangle LoupeBounds(Point p)
        {
            int w = LoupeSize + Border * 2;
            int h = LoupeSize + Border * 2 + StripHeight;
            // keep it on the monitor the cursor is on
            Rectangle mon = Screen.FromPoint(new Point(p.X + _virtual.X, p.Y + _virtual.Y)).Bounds;
            mon.Offset(-_virtual.X, -_virtual.Y);

            int x = p.X + Offset;
            int y = p.Y + Offset;
            if (x + w > mon.Right) x = p.X - Offset - w;
            if (y + h > mon.Bottom) y = p.Y - Offset - h;
            if (x < mon.Left) x = mon.Left;
            if (y < mon.Top) y = mon.Top;
            return new Rectangle(x, y, w, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle clip = e.ClipRectangle;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_shot, clip, clip, GraphicsUnit.Pixel);

            if (!_lastLoupe.IsEmpty && clip.IntersectsWith(Rectangle.Inflate(_lastLoupe, 2, 2)))
                DrawLoupe(g, _lastLoupe);
        }

        private void DrawLoupe(Graphics g, Rectangle box)
        {
            Rectangle view = new Rectangle(box.X + Border, box.Y + Border, LoupeSize, LoupeSize);
            Rectangle strip = new Rectangle(box.X, view.Bottom + Border, box.Width, StripHeight);

            // brass bezel
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bezel = new Rectangle(box.X, box.Y, box.Width, LoupeSize + Border * 2);
            using (LinearGradientBrush b = new LinearGradientBrush(bezel, Theme.BrassHi, Theme.BrassLo, 45f))
                g.FillRectangle(b, bezel);
            g.SmoothingMode = SmoothingMode.None;

            // magnified pixels (clip the source to the bitmap so edges don't smear)
            g.FillRectangle(Brushes.Black, view);
            Rectangle src = new Rectangle(_cursor.X - Cells / 2, _cursor.Y - Cells / 2, Cells, Cells);
            Rectangle srcClipped = Rectangle.Intersect(src, new Rectangle(0, 0, _shot.Width, _shot.Height));
            if (!srcClipped.IsEmpty)
            {
                Rectangle dst = new Rectangle(
                    view.X + (srcClipped.X - src.X) * Zoom,
                    view.Y + (srcClipped.Y - src.Y) * Zoom,
                    srcClipped.Width * Zoom, srcClipped.Height * Zoom);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_shot, dst, srcClipped, GraphicsUnit.Pixel);
            }

            // pixel grid
            g.PixelOffsetMode = PixelOffsetMode.Default;
            using (Pen grid = new Pen(Color.FromArgb(38, 0, 0, 0)))
            {
                for (int i = 1; i < Cells; i++)
                {
                    g.DrawLine(grid, view.X + i * Zoom, view.Y, view.X + i * Zoom, view.Bottom - 1);
                    g.DrawLine(grid, view.X, view.Y + i * Zoom, view.Right - 1, view.Y + i * Zoom);
                }
            }

            // crosshair hint lines + centre cell (dual outline so it reads on any colour)
            int c0 = (Cells / 2) * Zoom;
            Rectangle cell = new Rectangle(view.X + c0, view.Y + c0, Zoom, Zoom);
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(50, Theme.BrassHi)))
            {
                g.FillRectangle(hair, view.X, cell.Y, view.Width, Zoom);
                g.FillRectangle(hair, cell.X, view.Y, Zoom, view.Height);
            }
            Color px = _shot.GetPixel(_cursor.X, _cursor.Y);
            using (SolidBrush b = new SolidBrush(px)) g.FillRectangle(b, cell);
            g.DrawRectangle(Pens.Black, cell.X - 2, cell.Y - 2, cell.Width + 3, cell.Height + 3);
            g.DrawRectangle(Pens.White, cell.X - 1, cell.Y - 1, cell.Width + 1, cell.Height + 1);

            // readout strip
            using (SolidBrush b = new SolidBrush(Theme.Iron)) g.FillRectangle(b, strip);
            using (Pen p = new Pen(Theme.BrassLo)) g.DrawRectangle(p, strip.X, strip.Y, strip.Width - 1, strip.Height - 1);
            Rectangle sw = new Rectangle(strip.X + 8, strip.Y + 8, 14, 14);
            using (SolidBrush b = new SolidBrush(px)) g.FillRectangle(b, sw);
            using (Pen p = new Pen(Theme.Brass)) g.DrawRectangle(p, sw);

            int sx = _cursor.X + _virtual.X, sy = _cursor.Y + _virtual.Y;
            TextRenderer.DrawText(g, Theme.Hex(px), Theme.MonoFont, new Point(sw.Right + 6, strip.Y + 5), Theme.Ink, Theme.Iron);
            TextRenderer.DrawText(g, string.Format("{0},{1}", sx, sy), Theme.MonoSmall,
                new Rectangle(strip.X, strip.Y + 6, strip.Width - 8, 18), Theme.BrassHi, Theme.Iron,
                TextFormatFlags.Right | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, "click · arrows nudge · esc", Theme.MonoSmall,
                new Point(strip.X + 8, strip.Y + 25), Theme.InkDim, Theme.Iron);
        }
    }
}
