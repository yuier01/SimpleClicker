using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleClicker.UI
{
    internal sealed class SegmentControl : Control
    {
        private int _selectedIndex;
        private int _hoverIndex = -1;
        private bool _pressed;

        public SegmentControl()
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
            AccessibleRole = AccessibleRole.Grouping;
            Size = new Size(274, 38);
        }

        public string LeftText { get; set; } = "左键";

        public string RightText { get; set; } = "右键";

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var normalized = value == 1 ? 1 : 0;
                if (_selectedIndex != normalized)
                {
                    _selectedIndex = normalized;
                    Invalidate();
                }
            }
        }

        public event EventHandler SelectionChanged;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            Theme.Prepare(graphics);
            graphics.Clear(Color.White);

            var scale = Height / 38f;
            var track = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var trackPath = Theme.RoundedRect(track, 10f * scale))
            using (var trackBrush = new SolidBrush(Theme.FieldBackground))
            using (var trackPen = new Pen(Theme.FieldBorder, 1f))
            {
                graphics.FillPath(trackBrush, trackPath);
                graphics.DrawPath(trackPen, trackPath);
            }

            var inset = 3.5f * scale;
            var gap = 4f * scale;
            var segmentWidth = (Width - inset * 2f - gap) / 2f;
            var segmentHeight = Height - inset * 2f;

            for (var index = 0; index < 2; index++)
            {
                var segment = new RectangleF(
                    inset + index * (segmentWidth + gap),
                    inset,
                    segmentWidth,
                    segmentHeight);

                if (index == _selectedIndex)
                {
                    using (var path = Theme.RoundedRect(segment, 7.5f * scale))
                    using (var brush = new SolidBrush(Theme.AccentSoft))
                    using (var pen = new Pen(Theme.AccentBorder, 1f))
                    {
                        graphics.FillPath(brush, path);
                        graphics.DrawPath(pen, path);
                    }
                }
                else if (index == _hoverIndex && !_pressed)
                {
                    using (var path = Theme.RoundedRect(segment, 7.5f * scale))
                    using (var brush = new SolidBrush(Theme.FieldHover))
                    {
                        graphics.FillPath(brush, path);
                    }
                }

                var font = index == _selectedIndex
                    ? new Font(Font, FontStyle.Bold)
                    : Font;
                try
                {
                    using (var brush = new SolidBrush(
                        index == _selectedIndex ? Theme.AccentDeep : Theme.TextSecondary))
                    {
                        Theme.DrawCentered(
                            graphics,
                            index == 0 ? LeftText : RightText,
                            font,
                            brush,
                            segment,
                            true);
                    }
                }
                finally
                {
                    if (index == _selectedIndex)
                    {
                        font.Dispose();
                    }
                }
            }
        }

        private int GetIndexAt(int x)
        {
            var inset = 3.5f;
            var gap = 4f;
            var segmentWidth = (Width - inset * 2f - gap) / 2f;
            var boundary = inset + segmentWidth + gap / 2f;
            return x < boundary ? 0 : 1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var index = GetIndexAt(e.X);
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
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
                var index = GetIndexAt(e.X);
                var shouldSelect = _pressed && ClientRectangle.Contains(e.Location);
                _pressed = false;
                Capture = false;
                Invalidate();

                if (shouldSelect && index != _selectedIndex)
                {
                    _selectedIndex = index;
                    Invalidate();
                    var handler = SelectionChanged;
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
