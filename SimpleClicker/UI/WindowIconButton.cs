using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleClicker.UI
{
    internal enum WindowIconKind
    {
        Minimize,
        Close
    }

    internal sealed class WindowIconButton : Control
    {
        private bool _hovered;
        private bool _pressed;

        public WindowIconButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.White;
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
            Size = new Size(26, 26);
        }

        public WindowIconKind Kind { get; set; }

        public event EventHandler Clicked;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            Theme.Prepare(graphics);
            graphics.Clear(Color.White);

            var scale = Math.Min(Width, Height) / 26f;
            var surface = new RectangleF(1f * scale, 1f * scale, Width - 2f * scale, Height - 2f * scale);
            Color background;
            if (_pressed)
            {
                background = Theme.HeaderPressed;
            }
            else if (_hovered)
            {
                background = Theme.HeaderHover;
            }
            else
            {
                background = Color.White;
            }

            if (background != Color.White)
            {
                using (var path = Theme.RoundedRect(surface, 7f * scale))
                using (var brush = new SolidBrush(background))
                {
                    graphics.FillPath(brush, path);
                }
            }

            if (Kind == WindowIconKind.Close)
            {
                DrawClose(graphics, scale);
            }
            else
            {
                DrawMinimize(graphics, scale);
            }
        }

        private static void DrawMinimize(Graphics graphics, float scale)
        {
            using (var pen = new Pen(Theme.HeaderIcon, 1.6f * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                var half = 5f * scale;
                var y = 13f * scale;
                graphics.DrawLine(pen, 13f * scale - half, y, 13f * scale + half, y);
            }
        }

        private static void DrawClose(Graphics graphics, float scale)
        {
            using (var pen = new Pen(Theme.HeaderIcon, 1.6f * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                var half = 4.5f * scale;
                graphics.DrawLine(pen, 13f * scale - half, 13f * scale - half, 13f * scale + half, 13f * scale + half);
                graphics.DrawLine(pen, 13f * scale + half, 13f * scale - half, 13f * scale - half, 13f * scale + half);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Capture = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var shouldClick = _pressed && ClientRectangle.Contains(e.Location);
                _pressed = false;
                Capture = false;
                Invalidate();

                if (shouldClick)
                {
                    var handler = Clicked;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }

            base.OnMouseUp(e);
        }
    }
}
