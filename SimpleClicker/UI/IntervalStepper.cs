using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleClicker.UI
{
    internal sealed class IntervalStepper : Control
    {
        private readonly TextBox _textBox;
        private int _value = 100;
        private int _minimum = 1;
        private int _maximum = 99999;
        private bool _suppressTextChange;
        private int _hoverZone;

        public IntervalStepper()
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
            AccessibleRole = AccessibleRole.SpinButton;
            Size = new Size(274, 38);
            Step = 10;

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                BackColor = Theme.FieldBackground,
                ForeColor = Theme.TextPrimary,
                MaxLength = 5,
                Font = Font,
                TabStop = false
            };
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyPress += TextBox_KeyPress;
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.Enter += TextBox_Enter;
            _textBox.Leave += TextBox_Leave;
            Controls.Add(_textBox);

            UpdateTextBoxBounds();
            SetTextSilently(_value.ToString());
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = Math.Max(1, value);
                if (_maximum < _minimum)
                {
                    _maximum = _minimum;
                }

                Value = _value;
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(_minimum, value);
                Value = _value;
            }
        }

        public int Step { get; set; }

        public int Value
        {
            get { return _value; }
            set { SetValueCore(value, false); }
        }

        public event EventHandler ValueChanged;

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_textBox != null)
            {
                _textBox.Font = Font;
                UpdateTextBoxBounds();
            }

            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextBoxBounds();
        }

        private void UpdateTextBoxBounds()
        {
            if (_textBox == null)
            {
                return;
            }

            var width = Math.Min(110, Math.Max(60, Width - 140));
            var height = 24;
            _textBox.Bounds = new Rectangle(
                (Width - width) / 2,
                (Height - height) / 2,
                width,
                height);
        }

        private void SetTextSilently(string text)
        {
            _suppressTextChange = true;
            _textBox.Text = text;
            _suppressTextChange = false;
        }

        private void SetValueCore(int value, bool raiseEvent)
        {
            var clamped = Math.Max(_minimum, Math.Min(_maximum, value));
            var changed = clamped != _value;
            _value = clamped;
            SetTextSilently(_value.ToString());
            Invalidate();

            if (changed && raiseEvent)
            {
                var handler = ValueChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private int ReadCurrentValue()
        {
            int parsed;
            if (int.TryParse(_textBox.Text, out parsed))
            {
                return Math.Max(_minimum, Math.Min(_maximum, parsed));
            }

            return _value;
        }

        private void CommitText()
        {
            if (_suppressTextChange)
            {
                return;
            }

            var parsed = ReadCurrentValue();
            var changed = parsed != _value;
            _value = parsed;
            SetTextSilently(_value.ToString());
            Invalidate();

            if (changed)
            {
                var handler = ValueChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private void Adjust(int direction)
        {
            var next = ReadCurrentValue() + direction * Math.Max(1, Step);
            SetValueCore(next, true);
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!_suppressTextChange)
            {
                Invalidate();
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitText();
                _textBox.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                SetTextSilently(_value.ToString());
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            CommitText();
            Invalidate();
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
            using (var brush = new SolidBrush(Theme.FieldBackground))
            using (var pen = new Pen(ContainsFocus ? Theme.AccentBorder : Theme.FieldBorder, 1f))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            using (var pen = new Pen(
                _hoverZone == -1 || _hoverZone == 1 ? Theme.AccentDeep : Theme.TextSecondary,
                1.6f * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                var centerY = Height / 2f;
                var length = 4.5f * scale;
                var leftX = 22f * scale;
                var rightX = Width - 22f * scale;

                graphics.DrawLine(pen, leftX - length, centerY, leftX + length, centerY);
                graphics.DrawLine(pen, rightX - length, centerY, rightX + length, centerY);
                graphics.DrawLine(pen, rightX, centerY - length, rightX, centerY + length);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var zone = e.X < 44 ? -1 : (e.X > Width - 44 ? 1 : 0);
            if (zone != _hoverZone)
            {
                _hoverZone = zone;
                Cursor = zone == 0 ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverZone = 0;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.X < 44)
                {
                    Adjust(-1);
                }
                else if (e.X > Width - 44)
                {
                    Adjust(1);
                }
            }

            base.OnMouseDown(e);
        }
    }
}
