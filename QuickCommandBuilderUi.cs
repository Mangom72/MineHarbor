using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static partial class Launcher
{
	private sealed class QuickCommandTokenInput : UserControl
	{
		private sealed class TokenHitTarget
		{
			public int PartIndex;
			public Rectangle Bounds;
		}

		private readonly ModernTextBox editor;
		private readonly List<TokenHitTarget> hitTargets = new List<TokenHitTarget>();
		private QuickCommandBuilderState state;
		private ThemePalette palette = ThemePalette.Create(false);
		private bool synchronizing;
		private int horizontalOffset;

		public QuickCommandTokenInput()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
			BackColor = palette.CardSecondary;
			MinimumSize = new Size(180, 40);
			TabStop = true;

			editor = new ModernTextBox();
			editor.BorderStyle = BorderStyle.None;
			editor.Font = new Font(ThemeFonts.Body, 10.5F);
			editor.BackColor = palette.CardSecondary;
			editor.ForeColor = palette.Text;
			editor.TextChanged += EditorTextChanged;
			editor.KeyDown += delegate(object sender, KeyEventArgs eventArgs) { OnKeyDown(eventArgs); };
			editor.GotFocus += delegate { Invalidate(); };
			editor.LostFocus += delegate { Invalidate(); };
			Controls.Add(editor);
		}

		public QuickCommandBuilderState BuilderState
		{
			get { return state; }
		}

		public bool BuilderMode
		{
			get { return state != null; }
		}

		public bool BuilderComplete
		{
			get { return state == null || state.IsComplete; }
		}

		public string ActiveArgumentName
		{
			get { return state == null || state.ActivePart == null ? string.Empty : state.ActivePart.Name; }
		}

		public bool ActiveArgumentOptional
		{
			get { return state != null && state.ActivePart != null && !state.ActivePart.Required; }
		}

		public bool ActiveArgumentHasValue
		{
			get { return state != null && state.ActivePart != null && state.ActivePart.HasValue; }
		}

		public int SelectionStart
		{
			get { return editor.SelectionStart; }
			set { editor.SelectionStart = Math.Max(0, Math.Min(value, editor.TextLength)); }
		}

		public int TextLength
		{
			get { return Text.Length; }
		}

		public override string Text
		{
			get { return state == null ? editor.Text : state.BuildCommand(); }
			set
			{
				state = null;
				horizontalOffset = 0;
				SetEditorText(value ?? string.Empty);
				UpdateEditorPresentation();
				OnTextChanged(EventArgs.Empty);
				Invalidate();
			}
		}

		public void SetBuilderState(QuickCommandBuilderState value)
		{
			state = value;
			horizontalOffset = 0;
			SyncEditorFromActivePart();
			UpdateEditorPresentation();
			OnTextChanged(EventArgs.Empty);
			Invalidate();
		}

		public void Clear()
		{
			state = null;
			horizontalOffset = 0;
			SetEditorText(string.Empty);
			UpdateEditorPresentation();
			OnTextChanged(EventArgs.Empty);
			Invalidate();
		}

		public bool ActivateArgument(int partIndex)
		{
			if (state == null || !state.Activate(partIndex)) return false;
			SyncEditorFromActivePart();
			UpdateEditorPresentation();
			OnTextChanged(EventArgs.Empty);
			FocusEditor();
			Invalidate();
			return true;
		}

		public bool ActivateFirstIncompleteArgument()
		{
			if (state == null) return false;
			int partIndex = state.FindFirstIncompleteRequired();
			if (partIndex < 0) partIndex = state.FindFirstInvalidArgument();
			return partIndex >= 0 && ActivateArgument(partIndex);
		}

		public bool MoveNextArgument(bool includeOptional)
		{
			if (state == null || !state.MoveNext(includeOptional))
			{
				SyncEditorFromActivePart();
				UpdateEditorPresentation();
				OnTextChanged(EventArgs.Empty);
				Invalidate();
				return false;
			}
			SyncEditorFromActivePart();
			UpdateEditorPresentation();
			OnTextChanged(EventArgs.Empty);
			FocusEditor();
			Invalidate();
			return true;
		}

		public bool MovePreviousArgument()
		{
			if (state == null || !state.MovePrevious()) return false;
			SyncEditorFromActivePart();
			UpdateEditorPresentation();
			OnTextChanged(EventArgs.Empty);
			FocusEditor();
			Invalidate();
			return true;
		}

		public void ApplyActiveValue(string value)
		{
			if (state == null || state.ActivePart == null) return;
			SetEditorText(value ?? string.Empty);
			state.SetValue(state.ActivePartIndex, editor.Text);
			OnTextChanged(EventArgs.Empty);
			Invalidate();
		}

		public bool FocusEditor()
		{
			if (!editor.Visible) Focus();
			else editor.Focus();
			return Focused || editor.Focused;
		}

		public void ApplyPalette(ThemePalette value)
		{
			if (value == null) return;
			palette = value;
			BackColor = palette.CardSecondary;
			editor.BackColor = state != null && state.ActivePart != null ? palette.AccentSoft : palette.CardSecondary;
			editor.ForeColor = palette.Text;
			NativeControlTheme.Apply(editor, palette.Window.GetBrightness() < 0.45F);
			Invalidate();
		}

		private void EditorTextChanged(object sender, EventArgs eventArgs)
		{
			if (synchronizing) return;
			if (state != null && state.ActivePart != null) state.SetValue(state.ActivePartIndex, editor.Text);
			OnTextChanged(EventArgs.Empty);
			UpdateAccessiblePreview();
			Invalidate();
		}

		private void SetEditorText(string value)
		{
			synchronizing = true;
			try
			{
				editor.Text = value ?? string.Empty;
				editor.SelectionStart = editor.TextLength;
			}
			finally { synchronizing = false; }
		}

		private void SyncEditorFromActivePart()
		{
			SetEditorText(state == null || state.ActivePart == null ? string.Empty : state.ActivePart.Value);
			UpdateAccessiblePreview();
		}

		private void UpdateEditorPresentation()
		{
			if (editor == null) return;
			if (state == null)
			{
				editor.Visible = true;
				editor.CueText = LauncherUiText("명령을 입력하세요", "Enter a command");
				editor.BackColor = palette.CardSecondary;
				editor.Bounds = new Rectangle(10, Math.Max(6, (Height - editor.PreferredHeight) / 2), Math.Max(20, Width - 20), editor.PreferredHeight);
				return;
			}
			editor.Visible = state.ActivePart != null;
			if (state.ActivePart != null)
			{
				string label = GetQuickCommandArgumentDisplayName(state.ActivePart.Name, !state.ActivePart.Required);
				if (!string.IsNullOrWhiteSpace(state.ActivePart.DefaultValue)) label += LauncherUiText(" · 기본 ", " · default ") + state.ActivePart.DefaultValue;
				editor.CueText = label;
				editor.BackColor = palette.AccentSoft;
			}
			UpdateAccessiblePreview();
		}

		private void UpdateAccessiblePreview()
		{
			if (state == null) return;
			AccessibleDescription = LauncherUiText("작성 중인 명령: ", "Command in progress: ") + state.BuildAccessiblePreview();
		}

		protected override void OnEnter(EventArgs eventArgs)
		{
			base.OnEnter(eventArgs);
			if (editor.Visible && !editor.Focused) editor.Focus();
		}

		protected override void OnResize(EventArgs eventArgs)
		{
			base.OnResize(eventArgs);
			UpdateEditorPresentation();
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs eventArgs)
		{
			base.OnMouseDown(eventArgs);
			for (int i = 0; i < hitTargets.Count; i++)
			{
				if (!hitTargets[i].Bounds.Contains(eventArgs.Location)) continue;
				ActivateArgument(hitTargets[i].PartIndex);
				return;
			}
			FocusEditor();
		}

		protected override void OnPaint(PaintEventArgs eventArgs)
		{
			base.OnPaint(eventArgs);
			eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle backgroundBounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
			using (GraphicsPath backgroundPath = RoundedPanel.CreateRoundedRectangle(backgroundBounds, 10))
			using (SolidBrush backgroundBrush = new SolidBrush(palette.CardSecondary))
			using (Pen borderPen = new Pen(palette.Border))
			{
				eventArgs.Graphics.FillPath(backgroundBrush, backgroundPath);
				eventArgs.Graphics.DrawPath(borderPen, backgroundPath);
			}
			if (state == null)
			{
				UpdateEditorPresentation();
				return;
			}
			DrawBuilderParts(eventArgs.Graphics);
		}

		private void DrawBuilderParts(Graphics graphics)
		{
			hitTargets.Clear();
			List<Rectangle> partBounds = CalculatePartBounds(graphics);
			for (int i = 0; i < state.Parts.Count; i++)
			{
				QuickCommandTemplatePart part = state.Parts[i];
				Rectangle bounds = partBounds[i];
				if (bounds.Right < 0 || bounds.Left > Width) continue;
				if (!part.Argument)
				{
					TextRenderer.DrawText(graphics, part.Literal, Font, bounds, palette.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
					continue;
				}
				bool active = state.ActivePartIndex == i;
				bool valid = !part.HasValue || ValidateQuickCommandArgumentValue(part.Name, part.Value);
				if (!part.HasValue || active || !valid)
				{
					Color fill = active ? palette.AccentSoft : palette.Card;
					Color border = !valid ? palette.Danger : active ? palette.Accent : palette.Border;
					using (GraphicsPath chipPath = RoundedPanel.CreateRoundedRectangle(bounds, Math.Min(10, bounds.Height / 2)))
					using (SolidBrush chipBrush = new SolidBrush(fill))
					using (Pen chipPen = new Pen(border, active ? 2F : 1F))
					{
						graphics.FillPath(chipBrush, chipPath);
						graphics.DrawPath(chipPen, chipPath);
					}
					if (!active)
					{
						string display = part.HasValue ? part.Value : GetQuickCommandArgumentDisplayName(part.Name, !part.Required);
						if (!part.HasValue && !string.IsNullOrWhiteSpace(part.DefaultValue)) display += LauncherUiText(" · 기본 ", " · default ") + part.DefaultValue;
						TextRenderer.DrawText(graphics, display, Font, Rectangle.Inflate(bounds, -10, 0), valid ? palette.Muted : palette.Danger, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
					}
				}
				else
				{
					TextRenderer.DrawText(graphics, part.Value, Font, bounds, palette.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
				}
				hitTargets.Add(new TokenHitTarget { PartIndex = i, Bounds = bounds });
				if (active)
				{
					editor.Bounds = Rectangle.Inflate(bounds, -9, -5);
					editor.Visible = true;
					editor.BringToFront();
				}
			}
		}

		private List<Rectangle> CalculatePartBounds(Graphics graphics)
		{
			List<Rectangle> result = new List<Rectangle>();
			int x = 10;
			int y = 6;
			int height = Math.Max(28, Height - 12);
			for (int i = 0; i < state.Parts.Count; i++)
			{
				QuickCommandTemplatePart part = state.Parts[i];
				string text = part.Argument
					? (part.HasValue ? part.Value : GetQuickCommandArgumentDisplayName(part.Name, !part.Required))
					: part.Literal;
				if (part.Argument && !string.IsNullOrWhiteSpace(part.DefaultValue) && !part.HasValue) text += LauncherUiText(" · 기본 ", " · default ") + part.DefaultValue;
				int measured = TextRenderer.MeasureText(graphics, text ?? string.Empty, Font, new Size(1000, height), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
				int width = part.Argument ? Math.Max(72, Math.Min(190, measured + 22)) : Math.Max(10, measured + 2);
				result.Add(new Rectangle(x, y, width, height));
				x += width + 7;
			}
			int activeIndex = state.ActivePartIndex;
			if (activeIndex >= 0 && activeIndex < result.Count)
			{
				Rectangle active = result[activeIndex];
				if (active.Right - horizontalOffset > Width - 8) horizontalOffset = active.Right - (Width - 8);
				if (active.Left - horizontalOffset < 8) horizontalOffset = Math.Max(0, active.Left - 8);
			}
			else horizontalOffset = 0;
			if (horizontalOffset > 0)
			{
				for (int i = 0; i < result.Count; i++)
				{
					Rectangle shifted = result[i];
					shifted.X -= horizontalOffset;
					result[i] = shifted;
				}
			}
			return result;
		}
	}
}
