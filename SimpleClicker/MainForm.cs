using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleClicker.UI;

namespace SimpleClicker
{
    public partial class MainForm : Form
    {
        private const int CardWidth = 440;
        private const int CardHeight = 376;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int CS_DROPSHADOW = 0x00020000;

        private readonly AppConfig config;

        private bool isRunning;
        private bool isHotkeySetMode;
        private Keys tempHotkey = Keys.None;
        private Thread clickThread;
        private CancellationTokenSource cancellationTokenSource;
        private GlobalKeyboardHook keyboardHook;
        private Region roundedRegion;

        private readonly Font titleFont;
        private readonly Font subtitleFont;
        private readonly Font labelFont;
        private readonly Font smallFont;
        private readonly Font buttonFont;
        private readonly Font compactButtonFont;
        private readonly Font numberFont;
        private readonly Font statusFont;
        private readonly Font heroSubFont;
        private readonly Font sectionFont;
        private readonly Font footerFont;

        private WindowIconButton minimizeButton;
        private WindowIconButton closeButton;
        private ThemedButton startStopButton;
        private IntervalStepper intervalInput;
        private SegmentControl mouseButtons;
        private HotkeyChip hotkeyChip;
        private ThemedButton setHotkeyButton;
        private ThemedButton confirmHotkeyButton;
        private ThemedButton cancelHotkeyButton;
        private ToolTip toolTip;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public MainForm()
        {
            config = ConfigManager.Load();

            titleFont = new Font(Theme.DisplayFontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
            subtitleFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
            labelFont = new Font(Theme.UiFontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);
            smallFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
            buttonFont = new Font(Theme.UiFontFamily, 9F, FontStyle.Bold, GraphicsUnit.Point);
            compactButtonFont = new Font(Theme.UiFontFamily, 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            numberFont = new Font(Theme.DisplayFontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point);
            statusFont = new Font(Theme.UiFontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point);
            heroSubFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);
            sectionFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Bold, GraphicsUnit.Point);
            footerFont = new Font(Theme.UiFontFamily, 8F, FontStyle.Regular, GraphicsUnit.Point);

            InitializeWindow();
            InitializeControls();
            InitializeHook();
        }

        private void InitializeWindow()
        {
            Text = "SimpleClicker - 鼠标连点器";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            ShowIcon = false;
            MaximizeBox = false;
            MinimizeBox = true;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(CardWidth, CardHeight);
            BackColor = Color.White;
            Font = labelFont;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        private void InitializeControls()
        {
            minimizeButton = new WindowIconButton
            {
                Kind = WindowIconKind.Minimize,
                Bounds = new Rectangle(370, 10, 26, 26),
                AccessibleName = "最小化"
            };
            minimizeButton.Clicked += MinimizeButton_Clicked;

            closeButton = new WindowIconButton
            {
                Kind = WindowIconKind.Close,
                Bounds = new Rectangle(398, 10, 26, 26),
                AccessibleName = "关闭"
            };
            closeButton.Clicked += CloseButton_Clicked;

            startStopButton = new ThemedButton
            {
                Bounds = new Rectangle(276, 78, 132, 42),
                Text = "开始连点",
                Style = ButtonStyle.Primary,
                Font = buttonFont,
                BackColor = Theme.HeroFill,
                AccessibleName = "开始或停止连点"
            };
            startStopButton.Clicked += StartStopButton_Clicked;

            intervalInput = new IntervalStepper
            {
                Bounds = new Rectangle(150, 180, 274, 38),
                Minimum = 1,
                Maximum = 99999,
                Value = config.Interval,
                Font = numberFont,
                AccessibleName = "连点间隔，单位毫秒"
            };
            intervalInput.ValueChanged += IntervalInput_ValueChanged;

            mouseButtons = new SegmentControl
            {
                Bounds = new Rectangle(150, 228, 274, 38),
                LeftText = "左键",
                RightText = "右键",
                SelectedIndex = config.ClickButton == MouseButtons.Left ? 0 : 1,
                Font = buttonFont,
                AccessibleName = "连点按键"
            };
            mouseButtons.SelectionChanged += MouseButtons_SelectionChanged;

            hotkeyChip = new HotkeyChip
            {
                Bounds = new Rectangle(150, 276, 170, 38),
                Text = GetHotkeyString(config.Hotkey),
                Font = buttonFont,
                AccessibleName = "当前启停热键"
            };

            setHotkeyButton = new ThemedButton
            {
                Bounds = new Rectangle(332, 276, 92, 38),
                Text = "更改",
                Style = ButtonStyle.Secondary,
                Font = compactButtonFont,
                AccessibleName = "更改启停热键"
            };
            setHotkeyButton.Clicked += SetHotkeyButton_Clicked;

            confirmHotkeyButton = new ThemedButton
            {
                Bounds = new Rectangle(300, 276, 56, 38),
                Text = "确认",
                Style = ButtonStyle.Primary,
                Font = compactButtonFont,
                Enabled = false,
                Visible = false,
                AccessibleName = "确认新的启停热键"
            };
            confirmHotkeyButton.Clicked += ConfirmHotkeyButton_Clicked;

            cancelHotkeyButton = new ThemedButton
            {
                Bounds = new Rectangle(366, 276, 58, 38),
                Text = "取消",
                Style = ButtonStyle.Secondary,
                Font = compactButtonFont,
                Visible = false,
                AccessibleName = "取消设置热键"
            };
            cancelHotkeyButton.Clicked += CancelHotkeyButton_Clicked;

            Controls.Add(startStopButton);
            Controls.Add(intervalInput);
            Controls.Add(mouseButtons);
            Controls.Add(hotkeyChip);
            Controls.Add(setHotkeyButton);
            Controls.Add(confirmHotkeyButton);
            Controls.Add(cancelHotkeyButton);
            Controls.Add(minimizeButton);
            Controls.Add(closeButton);

            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };
            toolTip.SetToolTip(minimizeButton, "最小化");
            toolTip.SetToolTip(closeButton, "关闭 SimpleClicker");
            toolTip.SetToolTip(startStopButton, "开始或停止鼠标连点");
            toolTip.SetToolTip(intervalInput, "直接输入数字，或点击两侧的 − / + 调整");
            toolTip.SetToolTip(mouseButtons, "选择连点时按下的鼠标键");
            toolTip.SetToolTip(hotkeyChip, "当前启停热键");
            toolTip.SetToolTip(setHotkeyButton, "设置新的启停热键");
            toolTip.SetToolTip(confirmHotkeyButton, "保存新的启停热键");
            toolTip.SetToolTip(cancelHotkeyButton, "放弃设置");
        }

        private void InitializeHook()
        {
            keyboardHook = new GlobalKeyboardHook(
                config,
                OnHotkeyToggle,
                UpdateHotkeyDisplay,
                () => isHotkeySetMode,
                OnKeyDetected);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ClassStyle |= CS_DROPSHADOW;
                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRoundedRegion();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BeginInvoke((MethodInvoker)delegate
            {
                ActiveControl = null;
            });
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRoundedRegion();
        }

        private void UpdateRoundedRegion()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            using (var path = Theme.RoundedRect(
                new RectangleF(0f, 0f, ClientSize.Width, ClientSize.Height),
                20f))
            {
                var newRegion = new Region(path);
                var oldRegion = roundedRegion;
                roundedRegion = newRegion;
                Region = newRegion;
                if (oldRegion != null)
                {
                    oldRegion.Dispose();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            Theme.Prepare(graphics);
            graphics.Clear(Color.White);

            using (var borderPath = Theme.RoundedRect(
                new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f),
                20f))
            using (var borderPen = new Pen(Theme.CardBorder, 1.2f))
            {
                graphics.DrawPath(borderPen, borderPath);
            }

            using (var titleBrush = new SolidBrush(Theme.TextPrimary))
            using (var subtitleBrush = new SolidBrush(Theme.TextMuted))
            {
                Theme.DrawLeft(graphics, "SimpleClicker", titleFont, titleBrush, new RectangleF(16f, 5f, 300f, 24f));
                Theme.DrawLeft(graphics, "鼠标连点器", subtitleFont, subtitleBrush, new RectangleF(16f, 27f, 300f, 16f));
            }

            DrawSettingsSection(graphics);
            DrawHero(graphics);
            DrawFooter(graphics);
        }

        private void DrawSettingsSection(Graphics graphics)
        {
            using (var sectionBrush = new SolidBrush(Theme.SectionText))
            {
                Theme.DrawLeft(graphics, "设置", sectionFont, sectionBrush, new RectangleF(16f, 154f, 40f, 16f));
            }

            using (var dividerPen = new Pen(Theme.Divider, 1f))
            {
                graphics.DrawLine(dividerPen, 52f, 162f, 424f, 162f);
            }

            DrawRowLabel(graphics, "点击间隔", "ms", 180);
            DrawRowLabel(graphics, "连点按键", null, 228);
            DrawRowLabel(graphics, "启停热键", null, 276);
        }

        private void DrawRowLabel(Graphics graphics, string text, string suffix, int top)
        {
            using (var labelBrush = new SolidBrush(Theme.TextPrimary))
            using (var suffixBrush = new SolidBrush(Theme.TextMuted))
            {
                var bounds = new RectangleF(16f, top, 128f, 38f);
                Theme.DrawLeft(graphics, text, labelFont, labelBrush, bounds);

                if (!string.IsNullOrEmpty(suffix))
                {
                    var labelSize = graphics.MeasureString(text, labelFont);
                    var suffixBounds = new RectangleF(16f + labelSize.Width + 2f, top, 36f, 38f);
                    Theme.DrawLeft(graphics, suffix, smallFont, suffixBrush, suffixBounds);
                }
            }
        }

        private void DrawHero(Graphics graphics)
        {
            var hero = new RectangleF(16f, 56f, 408f, 86f);
            using (var heroPath = Theme.RoundedRect(hero, 14f))
            using (var heroBrush = new SolidBrush(Theme.HeroFill))
            using (var heroPen = new Pen(Theme.HeroBorder, 1f))
            {
                graphics.FillPath(heroBrush, heroPath);
                graphics.DrawPath(heroPen, heroPath);
            }

            var statusColor = isRunning ? Theme.StatusRunning : Theme.StatusIdle;
            using (var haloBrush = new SolidBrush(Color.FromArgb(36, statusColor)))
            using (var dotBrush = new SolidBrush(statusColor))
            {
                graphics.FillEllipse(haloBrush, 26f, 66f, 20f, 20f);
                graphics.FillEllipse(dotBrush, 32f, 72f, 8f, 8f);
            }

            using (var statusBrush = new SolidBrush(Theme.TextPrimary))
            using (var subtitleBrush = new SolidBrush(Theme.TextMuted))
            {
                Theme.DrawLeft(
                    graphics,
                    isRunning ? "运行中" : "已停止",
                    statusFont,
                    statusBrush,
                    new RectangleF(46f, 63f, 210f, 26f));
                Theme.DrawLeft(
                    graphics,
                    GetHeroSubtitle(),
                    heroSubFont,
                    subtitleBrush,
                    new RectangleF(46f, 88f, 210f, 18f));
            }
        }

        private void DrawFooter(Graphics graphics)
        {
            using (var dividerPen = new Pen(Theme.Divider, 1f))
            {
                graphics.DrawLine(dividerPen, 16f, 330f, 424f, 330f);
            }

            using (var footerBrush = new SolidBrush(Theme.TextMuted))
            {
                Theme.DrawCentered(
                    graphics,
                    "热键可随时启停 · 更改自动保存",
                    footerFont,
                    footerBrush,
                    new RectangleF(16f, 340f, 408f, 18f));
            }
        }

        private string GetHeroSubtitle()
        {
            string hotkey = GetHotkeyString(config.Hotkey);
            string button = config.ClickButton == MouseButtons.Left ? "左键" : "右键";

            if (isRunning)
            {
                return "每 " + config.Interval + " ms 点击" + button + " · 按 " + hotkey + " 停止";
            }

            return "按 " + hotkey + " 或点击右侧开始";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < 48)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
            }

            base.OnMouseDown(e);
        }

        private void MinimizeButton_Clicked(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void CloseButton_Clicked(object sender, EventArgs e)
        {
            Close();
        }

        private void StartStopButton_Clicked(object sender, EventArgs e)
        {
            if (isRunning)
            {
                StopClicking();
            }
            else
            {
                StartClicking();
            }
        }

        private void IntervalInput_ValueChanged(object sender, EventArgs e)
        {
            config.Interval = intervalInput.Value;
            ConfigManager.Save(config);
            Invalidate();
        }

        private void MouseButtons_SelectionChanged(object sender, EventArgs e)
        {
            config.ClickButton = mouseButtons.SelectedIndex == 0 ? MouseButtons.Left : MouseButtons.Right;
            ConfigManager.Save(config);
            Invalidate();
        }

        private void SetHotkeyButton_Clicked(object sender, EventArgs e)
        {
            BeginHotkeyCapture();
        }

        private void ConfirmHotkeyButton_Clicked(object sender, EventArgs e)
        {
            if (tempHotkey == Keys.None)
            {
                return;
            }

            config.Hotkey = tempHotkey;
            ConfigManager.Save(config);
            EndHotkeyCapture();
        }

        private void CancelHotkeyButton_Clicked(object sender, EventArgs e)
        {
            EndHotkeyCapture();
        }

        private void BeginHotkeyCapture()
        {
            if (isHotkeySetMode)
            {
                return;
            }

            isHotkeySetMode = true;
            tempHotkey = Keys.None;
            hotkeyChip.IsCapturing = true;
            hotkeyChip.Text = "按下组合键…";
            setHotkeyButton.Visible = false;
            confirmHotkeyButton.Visible = true;
            confirmHotkeyButton.Enabled = false;
            cancelHotkeyButton.Visible = true;
            ApplyHotkeyLayout();
            Invalidate();
        }

        private void EndHotkeyCapture()
        {
            isHotkeySetMode = false;
            hotkeyChip.IsCapturing = false;
            hotkeyChip.Text = GetHotkeyString(config.Hotkey);
            setHotkeyButton.Visible = true;
            confirmHotkeyButton.Visible = false;
            confirmHotkeyButton.Enabled = false;
            cancelHotkeyButton.Visible = false;
            ApplyHotkeyLayout();
            Invalidate();
        }

        private void ApplyHotkeyLayout()
        {
            if (isHotkeySetMode)
            {
                hotkeyChip.Bounds = new Rectangle(150, 276, 140, 38);
                confirmHotkeyButton.Bounds = new Rectangle(300, 276, 56, 38);
                cancelHotkeyButton.Bounds = new Rectangle(366, 276, 58, 38);
            }
            else
            {
                hotkeyChip.Bounds = new Rectangle(150, 276, 170, 38);
                setHotkeyButton.Bounds = new Rectangle(332, 276, 92, 38);
            }
        }

        private void OnHotkeyToggle(bool state)
        {
            Invoke((MethodInvoker)delegate
            {
                if (isRunning)
                {
                    StopClicking();
                }
                else
                {
                    StartClicking();
                }
            });
        }

        private void UpdateHotkeyDisplay(string text)
        {
            Invoke((MethodInvoker)delegate
            {
                if (hotkeyChip != null && isHotkeySetMode)
                {
                    hotkeyChip.Text = text;
                }
            });
        }

        private void OnKeyDetected(Keys key)
        {
            tempHotkey = key;
            Invoke((MethodInvoker)delegate
            {
                if (hotkeyChip == null)
                {
                    return;
                }

                hotkeyChip.Text = GetHotkeyString(tempHotkey);
                confirmHotkeyButton.Enabled = true;
            });
        }

        private string GetHotkeyString(Keys key)
        {
            if (key == Keys.None)
            {
                return "未设置";
            }

            string result = "";

            if ((key & Keys.Control) == Keys.Control)
            {
                result += "Ctrl + ";
            }

            if ((key & Keys.Shift) == Keys.Shift)
            {
                result += "Shift + ";
            }

            if ((key & Keys.Alt) == Keys.Alt)
            {
                result += "Alt + ";
            }

            Keys actualKey = key & ~Keys.Modifiers;
            result += actualKey.ToString();

            return result;
        }

        private void StartClicking()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            startStopButton.Text = "停止连点";
            Invalidate();

            cancellationTokenSource = new CancellationTokenSource();

            clickThread = new Thread(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested && isRunning)
                {
                    try
                    {
                        await Task.Delay(config.Interval, cancellationTokenSource.Token);

                        if (!isRunning)
                        {
                            break;
                        }

                        MouseController.Click(config.ClickButton);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });
            clickThread.IsBackground = true;
            clickThread.Start();
        }

        private void StopClicking()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            startStopButton.Text = "开始连点";
            Invalidate();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ConfigManager.Save(config);

            if (keyboardHook != null)
            {
                keyboardHook.Dispose();
            }

            StopClicking();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && toolTip != null)
            {
                toolTip.Dispose();
            }

            base.Dispose(disposing);

            if (disposing)
            {
                titleFont.Dispose();
                subtitleFont.Dispose();
                labelFont.Dispose();
                smallFont.Dispose();
                buttonFont.Dispose();
                compactButtonFont.Dispose();
                numberFont.Dispose();
                statusFont.Dispose();
                heroSubFont.Dispose();
                sectionFont.Dispose();
                footerFont.Dispose();
            }
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
