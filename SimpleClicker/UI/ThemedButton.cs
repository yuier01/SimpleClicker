using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleClicker.UI
{
    internal enum ButtonStyle
    {
        Primary,
        Secondary
    }

    internal sealed class ThemedButton : Control
    {
        private bool _hovered;
        private bool _pressed;

        public ThemedButton()
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
            Size = new Size(120, 38);
        }

        public ButtonStyle Style { get; set; }

        public event EventHandler Clicked;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            Theme.Prepare(graphics);
            graphics.Clear(BackColor);

            var scale = Height / 38f;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            var radius = 10f * scale;

            Color background;
            Color border;
            Color foreground;

            if (!Enabled)
            {
                if (Style == ButtonStyle.Primary)
                {
                    background = Color.FromArgb(247, 213, 224);
                    border = Color.FromArgb(247, 213, 224);
                    foreground = Color.White;
                }
                else
                {
                    background = Color.FromArgb(250, 251, 252);
                    border = Theme.FieldBorder;
                    foreground = Color.FromArgb(196, 201, 209);
                }
            }
            else if (Style == ButtonStyle.Primary)
            {
                background = _pressed ? Theme.AccentDeep : (_hovered ? Theme.AccentHover : Theme.Accent);
                border = background;
                foreground = Color.White;
            }
            else
            {
                background = _pressed ? Theme.FieldPressed : (_hovered ? Theme.FieldHover : Theme.FieldBackground);
                border = Theme.FieldBorder;
                foreground = Theme.TextPrimary;
            }

            using (var path = Theme.RoundedRect(bounds, radius))
            using (var brush = new SolidBrush(background))
            using (var pen = new Pen(border, 1f))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            var font = Font ?? Control.DefaultFont;
            using (var textBrush = new SolidBrush(foreground))
            {
                Theme.DrawCentered(graphics, Text, font, textBrush, ClientRectangle, true);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
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
            if (Enabled && e.Button == MouseButtons.Left)
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
                var shouldClick = Enabled && _pressed && ClientRectangle.Contains(e.Location);
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
