using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GameOptimizer.Core.Themes;
using GameOptimizer.Core.Tweaks;
using GameOptimizer.Utils;

namespace GameOptimizer.UI.Controls
{
    /// <summary>
    /// One tweak as a row: risk stripe, name, live state and its own
    /// enable / revert buttons.
    /// </summary>
    public sealed class TweakRow : Control
    {
        public TweakItem Item { get; }
        /// <summary>Last known state — read this instead of re-detecting (detection spawns processes).</summary>
        public TweakState State { get; private set; } = new();
        public event Action<TweakRow> ApplyClicked;
        public event Action<TweakRow> RevertClicked;

        private readonly FlatButton _applyBtn;
        private readonly FlatButton _revertBtn;
        private TweakState _state = new();
        private bool _isHover;
        private bool _busy;

        // wide enough for "Включить" + icon without wrapping
        private const int BtnW = 126;
        private const int BtnH = 28;

        public TweakRow(TweakItem item)
        {
            Item = item;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint             |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Height = 68;
            Width = 600;
            Margin = new Padding(0, 0, 0, 6);
            Font = new Font("Segoe UI", 9f);

            _applyBtn = new FlatButton
            {
                Text = "Включить", IconKind = "bolt", Style = FlatButtonStyle.Soft,
                AccentColor = Theme.Accent, Size = new Size(BtnW, BtnH)
            };
            _revertBtn = new FlatButton
            {
                Text = "Откатить", Style = FlatButtonStyle.Outline,
                AccentColor = Theme.Muted, Size = new Size(BtnW, BtnH)
            };
            _applyBtn.Click  += (_, __) => ApplyClicked?.Invoke(this);
            _revertBtn.Click += (_, __) => RevertClicked?.Invoke(this);
            Controls.Add(_applyBtn);
            Controls.Add(_revertBtn);

            MouseEnter += (_, __) => { _isHover = true;  Invalidate(); };
            MouseLeave += (_, __) => { _isHover = false; Invalidate(); };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // OnResize fires while the constructor still sets Size, i.e. before the
            // buttons exist — without this guard the first row throws.
            if (_revertBtn == null || _applyBtn == null) return;

            _revertBtn.Location = new Point(Width - 14 - BtnW, (Height - BtnH) / 2);
            _applyBtn.Location  = new Point(_revertBtn.Left - 8 - BtnW, (Height - BtnH) / 2);
        }

        public void SetState(TweakState state)
        {
            _state = state ?? new TweakState();
            State = _state;
            _busy = false;
            _applyBtn.Text    = _state.Status == TweakStatus.Applied ? "Включено"
                              : _state.Status == TweakStatus.OneShot   ? "Выполнить"
                              : "Включить";
            _applyBtn.Enabled = _state.Status != TweakStatus.Unsupported;
            _revertBtn.Enabled = _state.RevertAvailable;
            Invalidate();
        }

        public void SetBusy(string text)
        {
            _busy = true;
            _applyBtn.Text = text;
            _applyBtn.Enabled = false;
            _revertBtn.Enabled = false;
            Invalidate();
        }

        private Color RiskColor => Item.Risk switch
        {
            TweakRisk.Safe    => Theme.Success,
            TweakRisk.Caution => Theme.Warning,
            _                 => Theme.HardColor
        };

        private Color StateColor => _state.Status switch
        {
            TweakStatus.Applied     => Theme.Success,
            TweakStatus.Partial     => Theme.Warning,
            TweakStatus.OneShot     => Theme.AccentAlt,
            TweakStatus.Unsupported => Theme.Muted,
            _                       => Theme.TextSecondary
        };

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(_isHover ? Theme.CardHover : Theme.Card))
                g.FillRoundedRectangle(b, r.X, r.Y, r.Width, r.Height, 10);

            using (var p = new Pen(_isHover ? Theme.Alpha(Theme.Accent, 90) : Theme.Border, 1f))
                g.DrawRoundedRectangle(p, r.X, r.Y, r.Width, r.Height, 10);

            // risk stripe
            using (var b = new SolidBrush(Theme.Alpha(RiskColor, 210)))
                g.FillRoundedRectangle(b, r.X + 1, r.Y + 12, 3, r.Height - 24, 2);

            // title + optional note chip
            int x = 16;
            using (var f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var b = new SolidBrush(Theme.Text))
            {
                g.DrawString(Item.Title, f, b, x, 9);
                x += (int)g.MeasureString(Item.Title, f).Width + 10;
            }

            if (!string.IsNullOrEmpty(Item.Note))
            {
                using var f = new Font("Segoe UI", 7.6f);
                var sz = g.MeasureString(Item.Note, f);
                var noteRect = new RectangleF(x, 10, sz.Width + 12, 17);
                using (var b = new SolidBrush(Theme.Alpha(Theme.AccentAlt, 32)))
                    g.FillRoundedRectangle(b, noteRect.X, noteRect.Y, noteRect.Width, noteRect.Height, 7);
                using (var b = new SolidBrush(Theme.AccentAlt))
                    g.DrawString(Item.Note, f, b, noteRect.X + 6, noteRect.Y + 1);
                x += (int)noteRect.Width + 8;
            }

            if (Item.RequiresReboot)
            {
                using var f = new Font("Segoe UI", 7.6f);
                var sz = g.MeasureString("нужна перезагрузка", f);
                var rr = new RectangleF(x, 10, sz.Width + 12, 17);
                using (var b = new SolidBrush(Theme.Alpha(Theme.Warning, 32)))
                    g.FillRoundedRectangle(b, rr.X, rr.Y, rr.Width, rr.Height, 7);
                using (var b = new SolidBrush(Theme.Warning))
                    g.DrawString("нужна перезагрузка", f, b, rr.X + 6, rr.Y + 1);
            }

            // description + live detail
            int textWidth = Math.Max(60, _applyBtn.Left - 26 - 96);
            using (var f = new Font("Segoe UI", 8.2f))
            using (var b = new SolidBrush(Theme.TextSecondary))
            using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(Item.Desc, f, b, new RectangleF(16, 28, textWidth, 15), fmt);

            using (var f = new Font("Consolas", 7.8f))
            using (var b = new SolidBrush(Theme.Muted))
            using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(string.IsNullOrEmpty(_state.Detail) ? "…" : _state.Detail, f, b,
                             new RectangleF(16, 45, textWidth, 14), fmt);

            // state chip
            string label = _busy ? "работаю…" : _state.StatusText;
            var chipRect = LayoutChip(label);
            using (var b = new SolidBrush(Theme.Alpha(StateColor, 30)))
                g.FillRoundedRectangle(b, chipRect.X, chipRect.Y, chipRect.Width, chipRect.Height, 8);
            using (var p = new Pen(Theme.Alpha(StateColor, 120)))
                g.DrawRoundedRectangle(p, chipRect.X, chipRect.Y, chipRect.Width, chipRect.Height, 8);
            using (var f = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var b = new SolidBrush(StateColor))
            using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(label, f, b, chipRect, fmt);
        }

        private RectangleF LayoutChip(string label)
        {
            using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
            int w = Math.Max(84, (int)System.Windows.Forms.TextRenderer.MeasureText(label, f).Width + 18);
            float left = _applyBtn.Left - 10 - w;
            if (left < 120) left = 120;
            return new RectangleF(left, (Height - 22) / 2f, w, 22);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { Font?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
