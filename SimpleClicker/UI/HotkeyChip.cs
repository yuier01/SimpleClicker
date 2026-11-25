using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleClicker.UI
{
    internal sealed class HotkeyChip : Control
    {
        private bool _isCapturing;

        public HotkeyChip()
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
            AccessibleRole = AccessibleRole.StaticText;
            Size = new Size(170, 38);
        }

        public bool IsCapturing
        {
            get { return _isCapturing; }
            set
            {
                if (_isCapturing != value)
                {
                    _isCapturing = value;
                    Invalidate();
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            Theme.Prepare(graphics);
            graphics.Clear(Color.White);

            var scale = Height / 38f;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Theme.RoundedRect(bounds, 10f * scale))
            using (var brush = new SolidBrush(_isCapturing ? Theme.AccentSoft : Theme.FieldBackground))
            using (var pen = new Pen(_isCapturing ? Theme.AccentBorder : Theme.FieldBorder, 1f))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            var font = Font ?? Control.DefaultFont;
            using (var brush = new SolidBrush(_isCapturing ? Theme.AccentDeep : Theme.TextPrimary))
            {
                Theme.DrawCentered(graphics, Text, font, brush, ClientRectangle, true);
            }
        }
    }
}
