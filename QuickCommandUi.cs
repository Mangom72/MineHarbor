using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

internal static partial class Launcher
{
	private sealed partial class LauncherForm
	{
		private const int QuickCommandPanelWidth = 410;
		private RoundedPanel quickCommandPanel;
		private Label quickCommandTitle;
		private Label quickCommandStatus;
		private Label quickCommandSyntax;
		private QuickCommandTokenInput quickCommandBox;
		private Button quickCommandMenuButton;
		private Button quickCommandManageButton;
		private Button quickCommandSendButton;
		private ListBox quickCommandSuggestionList;
		private System.Windows.Forms.Timer quickCommandDebounceTimer;
		private readonly List<string> quickCommandHistory = new List<string>();
		private readonly Dictionary<string, QuickCommandBuilderState> quickCommandDrafts = new Dictionary<string, QuickCommandBuilderState>(StringComparer.OrdinalIgnoreCase);
		private List<QuickCommandSuggestion> quickCommandSuggestions = new List<QuickCommandSuggestion>();
		private List<QuickCommandDefinition> quickCommandUsers = new List<QuickCommandDefinition>();
		private QuickCommandDefinition activeQuickCommandDefinition;
		private int quickCommandHistoryIndex = -1;
		private int quickCommandSuggestionGeneration;
		private string quickCommandServerType = "paper";
		private string quickCommandMinecraftVersion = "26.2";

		private static string QuickText(string korean, string english)
		{
			return Localization.CurrentLanguage == Localization.Korean ? korean : english;
		}

		private void InitializeQuickCommandPanel(TableLayoutPanel workspace)
		{
			quickCommandPanel = new RoundedPanel();
			quickCommandPanel.Dock = DockStyle.Fill;
			quickCommandPanel.Margin = Padding.Empty;
			quickCommandPanel.Padding = new Padding(16, 12, 16, 12);
			quickCommandPanel.CornerRadius = 20;
			quickCommandPanel.Tag = "main-card";
			workspace.Controls.Add(quickCommandPanel, 1, 0);

			quickCommandTitle = new Label();
			quickCommandTitle.AutoSize = true;
			quickCommandTitle.Font = new Font("Pretendard", 11F, FontStyle.Bold);
			quickCommandTitle.Location = new Point(16, 12);
			quickCommandPanel.Controls.Add(quickCommandTitle);

			quickCommandStatus = new Label();
			quickCommandStatus.AutoEllipsis = false;
			quickCommandStatus.Tag = "muted";
			quickCommandStatus.Location = new Point(16, 39);
			quickCommandStatus.Size = new Size(378, 32);
			quickCommandStatus.TextAlign = ContentAlignment.MiddleLeft;
			quickCommandPanel.Controls.Add(quickCommandStatus);

			quickCommandMenuButton = CreateButton(string.Empty, 174);
			quickCommandMenuButton.Tag = "secondary";
			quickCommandMenuButton.Location = new Point(16, 76);
			quickCommandMenuButton.Size = new Size(174, 40);
			quickCommandMenuButton.Click += delegate { ShowQuickCommandMenu(); };
			quickCommandPanel.Controls.Add(quickCommandMenuButton);

			quickCommandManageButton = CreateButton(string.Empty, 174);
			quickCommandManageButton.Tag = "secondary";
			quickCommandManageButton.Location = new Point(202, 76);
			quickCommandManageButton.Size = new Size(192, 40);
			quickCommandManageButton.Click += delegate { OpenQuickCommandManager(); };
			quickCommandPanel.Controls.Add(quickCommandManageButton);

			quickCommandSyntax = new Label();
			quickCommandSyntax.AutoEllipsis = false;
			quickCommandSyntax.Tag = "muted";
			quickCommandSyntax.Location = new Point(16, 122);
			quickCommandSyntax.Size = new Size(378, 22);
			quickCommandPanel.Controls.Add(quickCommandSyntax);

			Panel inputPanel = new Panel();
			inputPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
			inputPanel.Location = new Point(16, quickCommandPanel.Height - 52);
			inputPanel.Size = new Size(378, 40);
			quickCommandPanel.Controls.Add(inputPanel);

			quickCommandBox = new QuickCommandTokenInput();
			quickCommandBox.Dock = DockStyle.Fill;
			quickCommandBox.Enabled = false;
			quickCommandBox.Font = new Font("Pretendard", 11F);
			quickCommandBox.TextChanged += delegate
			{
				quickCommandHistoryIndex = -1;
				UpdateQuickCommandSendState();
				UpdateQuickCommandBuilderStatus();
				quickCommandDebounceTimer.Stop();
				quickCommandDebounceTimer.Start();
			};
			quickCommandBox.KeyDown += QuickCommandBoxKeyDown;
			inputPanel.Controls.Add(quickCommandBox);

			quickCommandSendButton = CreateButton(string.Empty, 86);
			quickCommandSendButton.Dock = DockStyle.Right;
			quickCommandSendButton.Tag = "primary";
			quickCommandSendButton.Enabled = false;
			quickCommandSendButton.Click += delegate { SendQuickCommand(); };
			inputPanel.Controls.Add(quickCommandSendButton);

			quickCommandSuggestionList = new ListBox();
			quickCommandSuggestionList.Visible = false;
			quickCommandSuggestionList.DrawMode = DrawMode.OwnerDrawFixed;
			quickCommandSuggestionList.ItemHeight = 46;
			quickCommandSuggestionList.IntegralHeight = false;
			quickCommandSuggestionList.BorderStyle = BorderStyle.FixedSingle;
			quickCommandSuggestionList.Anchor = AnchorStyles.None;
			quickCommandSuggestionList.DrawItem += DrawQuickCommandSuggestion;
			quickCommandSuggestionList.SelectedIndexChanged += delegate { UpdateQuickCommandSyntax(); };
			quickCommandSuggestionList.DoubleClick += delegate { ApplySelectedQuickCommandSuggestion(); };
			Controls.Add(quickCommandSuggestionList);
			quickCommandSuggestionList.BringToFront();

			quickCommandDebounceTimer = new System.Windows.Forms.Timer();
			quickCommandDebounceTimer.Interval = 125;
			quickCommandDebounceTimer.Tick += delegate
			{
				quickCommandDebounceTimer.Stop();
				RefreshQuickCommandSuggestions(false);
			};

			quickCommandPanel.Resize += delegate { LayoutQuickCommandPanel(); };
			Resize += delegate { LayoutQuickCommandPanel(); };
			FormClosed += delegate
			{
				if (quickCommandDebounceTimer != null) quickCommandDebounceTimer.Dispose();
			};
			ReloadQuickCommandContext();
			ApplyQuickCommandLocalization();
			EnsureQuickCommandWorkspaceLayout();
			LayoutQuickCommandPanel();
		}

		private void EnsureQuickCommandWorkspaceLayout()
		{
			if (quickCommandPanel == null || quickCommandPanel.Parent == null) return;
			TableLayoutPanel workspace = quickCommandPanel.Parent as TableLayoutPanel;
			if (workspace == null || workspace.ColumnStyles.Count < 2) return;
			quickCommandPanel.SuspendLayout();
			// 콘솔 표시 여부와 관계없이 빠른 명령을 구버전의 오른쪽 고정 위치에 유지합니다.
			workspace.ColumnStyles[1].SizeType = SizeType.Absolute;
			workspace.ColumnStyles[1].Width = QuickCommandPanelWidth;
			workspace.SetColumn(quickCommandPanel, 1);
			workspace.SetRow(quickCommandPanel, 0);
			quickCommandPanel.Dock = DockStyle.Fill;
			quickCommandPanel.ResumeLayout(true);
			LayoutQuickCommandPanel();
		}

		private void LayoutQuickCommandPanel()
		{
			if (quickCommandPanel == null || quickCommandBox == null) return;
			int width = Math.Max(260, quickCommandPanel.ClientSize.Width - 32);
			quickCommandStatus.Width = width;
			quickCommandSyntax.Width = width;
			int gap = 12;
			int menuWidth = Math.Max(174, MeasureQuickCommandButtonWidth(quickCommandMenuButton));
			int manageWidth = Math.Max(192, MeasureQuickCommandButtonWidth(quickCommandManageButton));
			if (menuWidth + gap + manageWidth > width)
			{
				// 좁은 동반 패널에서는 문구를 줄이지 않고 버튼을 세로로 배치한다.
				quickCommandMenuButton.Bounds = new Rectangle(16, 76, width, 40);
				quickCommandManageButton.Bounds = new Rectangle(16, 122, width, 40);
				quickCommandSyntax.Location = new Point(16, 168);
			}
			else
			{
				quickCommandMenuButton.Bounds = new Rectangle(16, 76, menuWidth, 40);
				quickCommandManageButton.Bounds = new Rectangle(16 + menuWidth + gap, 76, manageWidth, 40);
				quickCommandSyntax.Location = new Point(16, 122);
			}
			Control input = quickCommandBox.Parent;
			input.Location = new Point(16, Math.Max(144, quickCommandPanel.ClientSize.Height - 52));
			input.Size = new Size(width, 40);
			if (quickCommandSuggestionList == null) return;
			Point inputTop = Point.Empty;
			Control current = input;
			while (current != null && current != this)
			{
				inputTop.Offset(current.Left, current.Top);
				current = current.Parent;
			}
			if (current != this) return;
			int desiredHeight = Math.Max(1, quickCommandSuggestionList.Items.Count) * quickCommandSuggestionList.ItemHeight + 2;
			int availableAbove = Math.Max(0, inputTop.Y - 12);
			int availableBelow = Math.Max(0, ClientSize.Height - inputTop.Y - input.Height - 12);
			bool placeAbove = availableAbove >= Math.Min(desiredHeight, 430) || availableAbove >= availableBelow;
			int availableHeight = placeAbove ? availableAbove : availableBelow;
			int maximumHeight = Math.Min(430, Math.Max(1, availableHeight));
			int popupHeight = Math.Min(desiredHeight, maximumHeight);
			int popupWidth = Math.Min(Math.Max(260, input.Width), Math.Max(1, ClientSize.Width - 16));
			int popupX = Math.Max(8, Math.Min(inputTop.X, ClientSize.Width - popupWidth - 8));
			int popupY = placeAbove ? Math.Max(8, inputTop.Y - popupHeight - 4) : inputTop.Y + input.Height + 4;
			quickCommandSuggestionList.Bounds = new Rectangle(popupX, popupY, popupWidth, popupHeight);
		}

		private static int MeasureQuickCommandButtonWidth(Button button)
		{
			if (button == null) return 0;
			Size measured = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font, new Size(4096, Math.Max(1, button.Height)), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
			return measured.Width + 24;
		}

		private void ApplyQuickCommandLocalization()
		{
			if (quickCommandPanel == null) return;
			quickCommandTitle.Text = QuickText("빠른 명령", "Quick commands");
			quickCommandMenuButton.Text = QuickText("명령 선택", "Choose command");
			quickCommandManageButton.Text = QuickText("명령·브리지 관리", "Commands and bridge");
			quickCommandSendButton.Text = QuickText("전송", "Send");
			quickCommandSyntax.Text = QuickText("명령을 입력하거나 목록에서 선택하세요.", "Type a command or choose one from the list.");
			ConfigureAccessibleField(quickCommandBox, QuickText("단계형 빠른 서버 명령", "Step-by-step quick server command"), QuickText("회색 인수는 아직 설정되지 않았습니다. 탭 또는 엔터로 값을 확정하고 다음 인수로 이동하며, Shift+Tab으로 이전 인수로 돌아갑니다.", "Gray arguments are not set yet. Use Tab or Enter to confirm a value and advance, or Shift+Tab to return to the previous argument."));
			ConfigureAccessibleField(quickCommandSuggestionList, QuickText("현재 인수 자동완성 후보", "Current argument suggestions"), QuickText("위아래 방향키로 후보를 이동하고 Tab으로 확정합니다. 모든 필수 인수가 완료된 뒤 Enter를 누르면 전송합니다.", "Use Up and Down to move through candidates and Tab to confirm. Press Enter to send only after all required arguments are complete."));
			ReloadQuickCommandContext();
			UpdateQuickCommandBridgeStatus();
			LayoutQuickCommandPanel();
		}

		private void ApplyQuickCommandTheme()
		{
			if (quickCommandBox == null) return;
			ThemePalette palette = ThemePalette.Create(darkTheme);
			quickCommandBox.ApplyPalette(palette);
			quickCommandSuggestionList.BackColor = palette.Card;
			quickCommandSuggestionList.ForeColor = palette.Text;
		}

		private void ReloadQuickCommandContext()
		{
			try
			{
				string root;
				string directory;
				LauncherOptions options = ReadActiveLauncherOptions(out root, out directory);
				quickCommandServerType = options.ServerType;
				quickCommandMinecraftVersion = options.MinecraftVersion;
				quickCommandUsers = LoadUserQuickCommands(root);
			}
			catch
			{
				string root = GetServersRootDirectory(AppDomain.CurrentDomain.BaseDirectory);
				quickCommandUsers = LoadUserQuickCommands(root);
			}
		}

		public void UpdateQuickCommandControls()
		{
			if (quickCommandBox == null || IsDisposed) return;
			if (InvokeRequired)
			{
				TryPostToUi(this, (MethodInvoker)UpdateQuickCommandControls);
				return;
			}
			quickCommandBox.Enabled = serverRunning;
			UpdateQuickCommandSendState();
			quickCommandManageButton.Enabled = !workflowRunning && !serverRunning;
			if (!serverRunning) HideQuickCommandSuggestions();
			UpdateQuickCommandBridgeStatus();
		}

		public void UpdateQuickCommandBridgeStatus()
		{
			if (quickCommandStatus == null || IsDisposed) return;
			if (InvokeRequired)
			{
				TryPostToUi(this, (MethodInvoker)UpdateQuickCommandBridgeStatus);
				return;
			}
			CommandBridgeSession bridge = GetActiveCommandBridge();
			if (bridge != null && bridge.Connected)
			{
				quickCommandStatus.Text = QuickText("● 실시간 명령 연동됨 · 명령 " + bridge.CommandCount, "● Live command bridge · " + bridge.CommandCount + " commands");
				quickCommandStatus.ForeColor = ThemePalette.Create(darkTheme).Success;
			}
			else if (bridge != null)
			{
				quickCommandStatus.Text = QuickText("○ 브리지 연결 중 · 기본 명령 사용 가능", "○ Connecting bridge · local commands available");
				quickCommandStatus.ForeColor = ThemePalette.Create(darkTheme).Warning;
			}
			else
			{
				string root;
				string directory;
				try
				{
					LauncherOptions options = ReadActiveLauncherOptions(out root, out directory);
					BridgeManagedInfo managed = ReadBridgeManagedInfo(directory);
					if ((NormalizeServerType(options.ServerType) == "paper" || NormalizeServerType(options.ServerType) == "purpur") && !IsCommandBridgeSupported(options.ServerType, options.MinecraftVersion))
					{
						quickCommandStatus.Text = QuickText("○ 브리지 호환되지 않음 · 기본 명령 사용", "○ Bridge incompatible · using local commands");
						quickCommandStatus.ForeColor = ThemePalette.Create(darkTheme).Warning;
						return;
					}
					if (managed != null && !string.Equals(managed.Version, BuildVersionInfo.ProductVersion, StringComparison.OrdinalIgnoreCase))
					{
						quickCommandStatus.Text = QuickText("○ 브리지 업데이트 필요 · 기본 명령 사용", "○ Bridge update needed · using local commands");
						quickCommandStatus.ForeColor = ThemePalette.Create(darkTheme).Warning;
						return;
					}
				}
				catch { }
				quickCommandStatus.Text = QuickText("○ 기본 명령 목록 사용 중", "○ Using local command list");
				quickCommandStatus.ForeColor = ThemePalette.Create(darkTheme).Muted;
			}
		}

		private void UpdateQuickCommandSendState()
		{
			if (quickCommandSendButton == null || quickCommandBox == null) return;
			quickCommandSendButton.Enabled = CanSendQuickCommand(serverRunning, quickCommandBox.Text) && quickCommandBox.BuilderComplete;
		}

		private void UpdateQuickCommandBuilderStatus()
		{
			if (quickCommandSyntax == null || quickCommandBox == null || !quickCommandBox.BuilderMode) return;
			QuickCommandBuilderState state = quickCommandBox.BuilderState;
			if (state == null) return;
			int missing = state.FindFirstIncompleteRequired();
			int invalid = state.FindFirstInvalidArgument();
			if (missing >= 0)
			{
				QuickCommandTemplatePart part = state.Parts[missing];
				quickCommandSyntax.Text = QuickText("다음 필수 인수 · ", "Next required · ") + GetQuickCommandArgumentDisplayName(part.Name, false) + "  ·  " + state.BuildAccessiblePreview();
			}
			else if (invalid >= 0)
			{
				QuickCommandTemplatePart part = state.Parts[invalid];
				quickCommandSyntax.Text = QuickText("값을 다시 확인하세요 · ", "Check this value · ") + GetQuickCommandArgumentDisplayName(part.Name, !part.Required) + "  ·  " + state.BuildAccessiblePreview();
			}
			else if (state.ActivePart != null && !state.ActivePart.Required)
			{
				quickCommandSyntax.Text = QuickText("필수 인수 완료 · 선택 인수를 입력하거나 Enter로 전송 · ", "Required arguments complete · fill the optional argument or press Enter to send · ") + state.BuildAccessiblePreview();
			}
			else quickCommandSyntax.Text = QuickText("전송 준비 · ", "Ready to send · ") + state.BuildCommand();
		}

		private void CancelQuickCommandBuilder()
		{
			if (activeQuickCommandDefinition != null) quickCommandDrafts.Remove(GetQuickCommandBuilderKey(activeQuickCommandDefinition));
			activeQuickCommandDefinition = null;
			quickCommandBox.Clear();
			quickCommandBox.FocusEditor();
			quickCommandSyntax.Text = QuickText("명령 작성을 취소했습니다.", "Command draft cancelled.");
			UpdateQuickCommandSendState();
		}

		private void QuickCommandBoxKeyDown(object sender, KeyEventArgs eventArgs)
		{
			if (eventArgs.Control && eventArgs.KeyCode == Keys.Space)
			{
				RefreshQuickCommandSuggestions(true);
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (!quickCommandBox.BuilderMode && eventArgs.Control && (eventArgs.KeyCode == Keys.Up || eventArgs.KeyCode == Keys.Down))
			{
				NavigateQuickCommandHistory(eventArgs.KeyCode == Keys.Up ? -1 : 1);
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (quickCommandBox.BuilderMode && eventArgs.KeyCode == Keys.Tab && eventArgs.Shift)
			{
				quickCommandBox.MovePreviousArgument();
				RefreshQuickCommandSuggestions(true);
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (quickCommandSuggestionList.Visible && (eventArgs.KeyCode == Keys.Up || eventArgs.KeyCode == Keys.Down))
			{
				int delta = eventArgs.KeyCode == Keys.Up ? -1 : 1;
				int next = Math.Max(0, Math.Min(quickCommandSuggestionList.Items.Count - 1, quickCommandSuggestionList.SelectedIndex + delta));
				quickCommandSuggestionList.SelectedIndex = next;
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (eventArgs.KeyCode == Keys.Tab)
			{
				if (quickCommandSuggestionList.Visible && quickCommandSuggestionList.SelectedIndex >= 0) ApplySelectedQuickCommandSuggestion();
				else if (quickCommandBox.BuilderMode)
				{
					QuickCommandBuilderState state = quickCommandBox.BuilderState;
					if (state != null && state.ActivePart != null && state.ActivePart.Required && !state.ActivePart.HasValue) RefreshQuickCommandSuggestions(true);
					else
					{
						quickCommandBox.MoveNextArgument(true);
						RefreshQuickCommandSuggestions(true);
					}
				}
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (eventArgs.KeyCode == Keys.Enter)
			{
				if (quickCommandBox.BuilderMode)
				{
					if (!quickCommandBox.BuilderComplete)
					{
						if (quickCommandSuggestionList.Visible && quickCommandSuggestionList.SelectedIndex >= 0) ApplySelectedQuickCommandSuggestion();
						else
						{
							quickCommandBox.ActivateFirstIncompleteArgument();
							RefreshQuickCommandSuggestions(true);
						}
					}
					else SendQuickCommand();
				}
				else if (quickCommandSuggestionList.Visible && quickCommandSuggestionList.SelectedIndex >= 0) ApplySelectedQuickCommandSuggestion();
				else SendQuickCommand();
				eventArgs.SuppressKeyPress = true;
				return;
			}
			if (eventArgs.KeyCode == Keys.Escape)
			{
				if (quickCommandSuggestionList.Visible) HideQuickCommandSuggestions();
				else if (quickCommandBox.BuilderMode) CancelQuickCommandBuilder();
				eventArgs.SuppressKeyPress = true;
			}
		}

		private void RefreshQuickCommandSuggestions(bool force)
		{
			if (!serverRunning || quickCommandBox == null) return;
			if (quickCommandBox.BuilderMode)
			{
				int builderGeneration = ++quickCommandSuggestionGeneration;
				CommandBridgeSession builderBridge = GetActiveCommandBridge();
				string[] builderPlayers = builderBridge == null ? new string[0] : builderBridge.Players;
				ShowQuickCommandSuggestions(GetQuickCommandBuilderSuggestions(quickCommandBox.BuilderState, builderPlayers), builderGeneration);
				return;
			}
			string input = quickCommandBox.Text;
			if (!force && string.IsNullOrWhiteSpace(input))
			{
				HideQuickCommandSuggestions();
				return;
			}
			int generation = ++quickCommandSuggestionGeneration;
			CommandBridgeSession bridge = GetActiveCommandBridge();
			string[] players = bridge == null ? new string[0] : bridge.Players;
			List<QuickCommandSuggestion> local = GetLocalQuickCommandSuggestions(input, quickCommandBox.SelectionStart, quickCommandServerType, quickCommandMinecraftVersion, quickCommandUsers, players, quickCommandHistory);
			ShowQuickCommandSuggestions(local, generation);
			if (bridge == null || !bridge.Connected) return;
			int cursor = quickCommandBox.SelectionStart;
			CommandParseResult parsed = ParseCommandInput(input, cursor);
			bridge.RequestSuggestions(input, delegate(List<QuickCommandSuggestion> live)
			{
				for (int i = 0; i < live.Count; i++)
				{
					live[i].ReplaceStart = parsed.ReplaceStart + (input.StartsWith("/", StringComparison.Ordinal) ? 1 : 0);
					live[i].ReplaceLength = parsed.ReplaceLength;
				}
				TryPostToUi(this, (MethodInvoker)delegate
				{
					if (!IsSuggestionGenerationCurrent(generation, quickCommandSuggestionGeneration)) return;
					ShowQuickCommandSuggestions(MergeQuickCommandSuggestions(live, local, 40), generation);
				});
			});
		}

		private void ShowQuickCommandSuggestions(List<QuickCommandSuggestion> values, int generation)
		{
			if (!IsSuggestionGenerationCurrent(generation, quickCommandSuggestionGeneration) || values == null || values.Count == 0)
			{
				HideQuickCommandSuggestions();
				return;
			}
			quickCommandSuggestions = values.Take(40).ToList();
			quickCommandSuggestionList.BeginUpdate();
			quickCommandSuggestionList.Items.Clear();
			for (int i = 0; i < quickCommandSuggestions.Count; i++) quickCommandSuggestionList.Items.Add(quickCommandSuggestions[i]);
			quickCommandSuggestionList.SelectedIndex = 0;
			quickCommandSuggestionList.EndUpdate();
			quickCommandSuggestionList.Visible = true;
			LayoutQuickCommandPanel();
			quickCommandSuggestionList.BringToFront();
		}

		private void HideQuickCommandSuggestions()
		{
			if (quickCommandSuggestionList != null) quickCommandSuggestionList.Visible = false;
			UpdateQuickCommandBuilderStatus();
		}

		private void UpdateQuickCommandSyntax()
		{
			QuickCommandSuggestion selected = quickCommandSuggestionList.SelectedItem as QuickCommandSuggestion;
			if (selected == null) return;
			string source = selected.Source == "bridge" ? QuickText("실시간", "live") : selected.Source == "user" ? QuickText("사용자", "user") : selected.Source == "history" ? QuickText("기록", "history") : QuickText("기본", "built-in");
			quickCommandSyntax.Text = (selected.Dangerous ? QuickText("주의 · ", "Caution · ") : string.Empty) + selected.Syntax + " · " + source + (string.IsNullOrWhiteSpace(selected.Description) ? string.Empty : " · " + selected.Description);
		}

		private void DrawQuickCommandSuggestion(object sender, DrawItemEventArgs eventArgs)
		{
			if (eventArgs.Index < 0 || eventArgs.Index >= quickCommandSuggestionList.Items.Count) return;
			QuickCommandSuggestion item = quickCommandSuggestionList.Items[eventArgs.Index] as QuickCommandSuggestion;
			ThemePalette palette = ThemePalette.Create(darkTheme);
			bool selected = (eventArgs.State & DrawItemState.Selected) != 0;
			using (SolidBrush background = new SolidBrush(selected ? palette.AccentSoft : palette.Card)) eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);
			QuickCommandRisk risk = GetSuggestionRisk(item);
			Color titleColor = risk == QuickCommandRisk.Dangerous ? palette.Danger : risk == QuickCommandRisk.Confirm ? palette.Warning : palette.Text;
			using (Font titleFont = new Font(Font, FontStyle.Bold))
			using (SolidBrush titleBrush = new SolidBrush(titleColor))
			using (SolidBrush detailBrush = new SolidBrush(palette.Muted))
			{
				string title = item == null ? string.Empty : item.Value;
				string source = item == null ? string.Empty : item.Source == "bridge" ? QuickText("실시간", "live") : item.Source == "user" ? QuickText("사용자", "user") : item.Source == "history" ? QuickText("기록", "history") : QuickText("기본", "built-in");
				string detail = item == null ? string.Empty : source + "  ·  " + item.Description;
				eventArgs.Graphics.DrawString(title, titleFont, titleBrush, eventArgs.Bounds.Left + 10, eventArgs.Bounds.Top + 4);
				eventArgs.Graphics.DrawString(detail, Font, detailBrush, eventArgs.Bounds.Left + 10, eventArgs.Bounds.Top + 24);
			}
			eventArgs.DrawFocusRectangle();
		}

		private void ApplySelectedQuickCommandSuggestion()
		{
			QuickCommandSuggestion selected = quickCommandSuggestionList.SelectedItem as QuickCommandSuggestion;
			if (selected == null) return;
			if (quickCommandBox.BuilderMode)
			{
				quickCommandBox.ApplyActiveValue(selected.Value);
				bool moved = quickCommandBox.MoveNextArgument(true);
				if (moved) RefreshQuickCommandSuggestions(true);
				else
				{
					HideQuickCommandSuggestions();
					quickCommandBox.FocusEditor();
				}
				UpdateQuickCommandSendState();
				UpdateQuickCommandBuilderStatus();
				return;
			}
			quickCommandBox.Text = ApplyQuickCommandSuggestion(quickCommandBox.Text, selected);
			quickCommandBox.SelectionStart = Math.Min(quickCommandBox.TextLength, selected.ReplaceStart + selected.Value.Length);
			quickCommandBox.FocusEditor();
			HideQuickCommandSuggestions();
		}

		private void NavigateQuickCommandHistory(int direction)
		{
			int nextIndex;
			string value = GetQuickCommandHistoryValue(quickCommandHistory, quickCommandHistoryIndex, direction, out nextIndex);
			if (nextIndex < 0) return;
			quickCommandHistoryIndex = nextIndex;
			quickCommandBox.Text = value;
			quickCommandBox.SelectionStart = quickCommandBox.TextLength;
		}

		private void SendQuickCommand()
		{
			if (quickCommandBox.BuilderMode && !quickCommandBox.BuilderComplete)
			{
				quickCommandBox.ActivateFirstIncompleteArgument();
				UpdateQuickCommandBuilderStatus();
				RefreshQuickCommandSuggestions(true);
				return;
			}
			string command = NormalizeCommandForSend(quickCommandBox.Text);
			if (!CanSendQuickCommand(serverRunning, command))
			{
				ShowNoticeKey("Notice.NoServer", true);
				return;
			}
			List<QuickCommandDefinition> riskDefinitions = GetBuiltInQuickCommands();
			riskDefinitions.AddRange(quickCommandUsers);
			QuickCommandRisk risk = GetQuickCommandRisk(command, riskDefinitions);
			if (risk != QuickCommandRisk.Normal)
			{
				bool dangerous = risk == QuickCommandRisk.Dangerous;
				string message = dangerous
					? QuickText("위험 명령입니다. 서버 상태나 넓은 범위의 데이터에 영향을 줄 수 있습니다. 영향 범위와 인수를 다시 확인한 뒤 실행하시겠습니까?\r\n\r\n", "This is a dangerous command. It can affect server state or a broad range of data. Review its scope and arguments before running it.\r\n\r\n")
					: QuickText("다음 명령은 서버 상태를 변경합니다. 실행하시겠습니까?\r\n\r\n", "This command changes server state. Run it?\r\n\r\n");
				DialogResult result = ShowMineHarborDialog(this, message + command, dangerous ? QuickText("위험 명령 확인", "Confirm dangerous command") : QuickText("명령 실행 확인", "Confirm command"), MessageBoxButtons.YesNo, dangerous ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
				if (result != DialogResult.Yes) return;
			}
			if (!SendServerCommand(command))
			{
				ShowNoticeKey("Notice.NoServer", true);
				return;
			}
			if (quickCommandHistory.Count == 0 || !string.Equals(quickCommandHistory[quickCommandHistory.Count - 1], command, StringComparison.Ordinal)) quickCommandHistory.Add(command);
			if (quickCommandHistory.Count > 100) quickCommandHistory.RemoveAt(0);
			quickCommandHistoryIndex = -1;
			if (activeQuickCommandDefinition != null) quickCommandDrafts.Remove(GetQuickCommandBuilderKey(activeQuickCommandDefinition));
			activeQuickCommandDefinition = null;
			quickCommandBox.Clear();
			HideQuickCommandSuggestions();
		}

		private void ShowQuickCommandMenu()
		{
			ReloadQuickCommandContext();
			List<QuickCommandDefinition> definitions = GetBuiltInQuickCommands();
			definitions.AddRange(quickCommandUsers);
			CommandBridgeSession bridge = GetActiveCommandBridge();
			if (bridge != null && bridge.Connected) definitions.AddRange(BuildBridgeQuickCommandDefinitions(bridge.Commands));
			using (QuickCommandPickerForm picker = new QuickCommandPickerForm(definitions, quickCommandServerType, quickCommandMinecraftVersion))
			{
				if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedCommand != null)
				{
					InsertQuickCommandTemplate(picker.SelectedCommand);
				}
			}
		}

		private void InsertQuickCommandTemplate(QuickCommandDefinition definition)
		{
			if (definition == null) return;
			activeQuickCommandDefinition = definition;
			string key = GetQuickCommandBuilderKey(definition);
			QuickCommandBuilderState state;
			if (!quickCommandDrafts.TryGetValue(key, out state) || !string.Equals(state.Template, NormalizeCommandForSend(definition.Template), StringComparison.Ordinal))
			{
				state = CreateQuickCommandBuilderState(definition);
				quickCommandDrafts[key] = state;
			}
			quickCommandBox.SetBuilderState(state);
			quickCommandBox.FocusEditor();
			RefreshQuickCommandSuggestions(true);
			UpdateQuickCommandSendState();
			UpdateQuickCommandBuilderStatus();
		}

		private void OpenQuickCommandManager()
		{
			if (!RequireStoppedServer()) return;
			string root;
			string directory;
			LauncherOptions options = ReadActiveLauncherOptions(out root, out directory);
			ShowModelessToolWindow("quick-command-manager", delegate { return new QuickCommandManagerForm(root, directory, options); }, true, delegate
			{
				ReloadQuickCommandContext();
				UpdateQuickCommandBridgeStatus();
			});
		}
	}

	private static int GetNextQuickCommandSuggestionIndex(int selectedIndex, int itemCount, bool backwards)
	{
		if (itemCount <= 0) return -1;
		if (selectedIndex < 0 || selectedIndex >= itemCount) return backwards ? itemCount - 1 : 0;
		return backwards ? (selectedIndex + itemCount - 1) % itemCount : (selectedIndex + 1) % itemCount;
	}

	private static bool CanSendQuickCommand(bool serverRunning, string command)
	{
		return serverRunning && !string.IsNullOrWhiteSpace(NormalizeCommandForSend(command));
	}

	private sealed class QuickCommandManagerForm : Form
	{
		private readonly string serversRoot;
		private readonly string serverDirectory;
		private readonly LauncherOptions options;
		private readonly ListBox commandList;
		private readonly Label bridgeStatus;
		private List<QuickCommandDefinition> commands;

		public QuickCommandManagerForm(string root, string directory, LauncherOptions launcherOptions)
		{
			serversRoot = root;
			serverDirectory = directory;
			options = launcherOptions;
			commands = LoadUserQuickCommands(root);
			Text = LauncherUiText("명령·브리지 관리", "Commands & bridge");
			ApplyLauncherWindowIcon(this);
			StartPosition = FormStartPosition.CenterParent;
			MinimumSize = new Size(720, 500);
			Size = new Size(820, 580);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font("Pretendard", 11F);

			TableLayoutPanel rootPanel = new TableLayoutPanel();
			rootPanel.Dock = DockStyle.Fill;
			rootPanel.Padding = new Padding(24);
			rootPanel.ColumnCount = 2;
			rootPanel.RowCount = 2;
			rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
			rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
			rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			rootPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
			Controls.Add(rootPanel);

			GroupBox userGroup = new ModernGroupBox();
			userGroup.Text = LauncherUiText("사용자 명령", "User commands");
			userGroup.Dock = DockStyle.Fill;
			userGroup.Padding = new Padding(14, 28, 14, 14);
			rootPanel.Controls.Add(userGroup, 0, 0);
			commandList = new ListBox();
			commandList.Dock = DockStyle.Fill;
			commandList.BorderStyle = BorderStyle.None;
			ConfigureAccessibleField(commandList, LauncherUiText("사용자 명령 목록", "User command list"), LauncherUiText("추가하거나 수정할 사용자 명령을 선택합니다.", "Select a user command to add or edit."));
			userGroup.Controls.Add(commandList);
			Panel commandButtons = new Panel();
			commandButtons.Dock = DockStyle.Bottom;
			commandButtons.Height = 48;
			userGroup.Controls.Add(commandButtons);
			AddManagerButton(commandButtons, LauncherUiText("추가", "Add"), 0, delegate { EditCommand(null); });
			AddManagerButton(commandButtons, LauncherUiText("수정", "Edit"), 94, delegate { EditCommand(commandList.SelectedItem as QuickCommandDefinition); });
			AddManagerButton(commandButtons, LauncherUiText("삭제", "Delete"), 188, DeleteSelectedCommand);

			GroupBox bridgeGroup = new ModernGroupBox();
			bridgeGroup.Text = LauncherUiText("Paper/Purpur 실시간 연동", "Paper/Purpur live bridge");
			bridgeGroup.Dock = DockStyle.Fill;
			bridgeGroup.Padding = new Padding(16, 28, 16, 16);
			rootPanel.Controls.Add(bridgeGroup, 1, 0);
			bridgeStatus = new Label();
			bridgeStatus.Dock = DockStyle.Top;
			bridgeStatus.Height = 190;
			bridgeStatus.AutoEllipsis = true;
			bridgeStatus.AccessibleName = LauncherUiText("명령 브리지 상태", "Command bridge status");
			bridgeStatus.AccessibleDescription = LauncherUiText("지원 여부, 설치 상태, 연결과 호환성 정보를 표시합니다.", "Shows support, installation, connection, and compatibility details.");
			bridgeGroup.Controls.Add(bridgeStatus);
			FlowLayoutPanel bridgeButtons = new FlowLayoutPanel();
			bridgeButtons.Dock = DockStyle.Bottom;
			bridgeButtons.Height = 150;
			bridgeButtons.FlowDirection = FlowDirection.LeftToRight;
			bridgeButtons.WrapContents = true;
			bridgeGroup.Controls.Add(bridgeButtons);
			AddBridgeButton(bridgeButtons, LauncherUiText("설치·업데이트", "Install / update"), InstallBridge);
			AddBridgeButton(bridgeButtons, LauncherUiText("제거", "Remove"), RemoveBridge);
			AddBridgeButton(bridgeButtons, LauncherUiText("설치 폴더 열기", "Open plugin folder"), OpenBridgeFolder);
			AddBridgeButton(bridgeButtons, LauncherUiText("상태 새로고침", "Refresh status"), RefreshBridgeStatus);

			Button close = new RoundedButton();
			close.Text = LauncherUiText("닫기", "Close");
			close.Width = 110;
			close.Height = 40;
			close.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			close.Click += delegate { Close(); };
			rootPanel.Controls.Add(close, 1, 1);
			RefreshCommands();
			RefreshBridgeStatus();
			ApplySimpleDialogTheme(this);
		}

		private static void AddManagerButton(Control parent, string text, int left, EventHandler handler)
		{
			Button button = new RoundedButton();
			button.Text = text;
			button.Size = new Size(86, 36);
			button.Location = new Point(left, 6);
			button.Click += handler;
			parent.Controls.Add(button);
		}

		private static void AddBridgeButton(Control parent, string text, EventHandler handler)
		{
			Button button = new RoundedButton();
			button.Text = text;
			button.Size = new Size(142, 42);
			button.Margin = new Padding(4);
			button.Click += handler;
			EnsureButtonContentFits(button);
			parent.Controls.Add(button);
		}

		private void RefreshCommands()
		{
			commandList.DataSource = null;
			commandList.DataSource = commands.ToList();
		}

		private void EditCommand(QuickCommandDefinition existing)
		{
			QuickCommandDefinition editable = existing == null ? null : CloneQuickCommand(existing);
			using (QuickCommandEditorForm editor = new QuickCommandEditorForm(editable))
			{
				if (editor.ShowDialog(this) != DialogResult.OK) return;
				QuickCommandDefinition value = editor.Value;
				if (existing == null) commands.Add(value);
				else
				{
					int index = commands.FindIndex(delegate(QuickCommandDefinition item) { return string.Equals(item.Id, existing.Id, StringComparison.Ordinal); });
					if (index >= 0) commands[index] = value;
				}
				SaveUserQuickCommands(serversRoot, commands);
				RefreshCommands();
			}
		}

		private void DeleteSelectedCommand(object sender, EventArgs eventArgs)
		{
			QuickCommandDefinition selected = commandList.SelectedItem as QuickCommandDefinition;
			if (selected == null) return;
			if (ShowMineHarborDialog(this, LauncherUiText("선택한 사용자 명령을 삭제하시겠습니까?", "Delete the selected user command?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
			commands.RemoveAll(delegate(QuickCommandDefinition item) { return string.Equals(item.Id, selected.Id, StringComparison.Ordinal); });
			SaveUserQuickCommands(serversRoot, commands);
			RefreshCommands();
		}

		private void InstallBridge(object sender, EventArgs eventArgs)
		{
			try
			{
				InstallOrUpdateCommandBridge(serverDirectory, options.ServerType, options.MinecraftVersion);
				WriteBridgeChoice(serverDirectory, "install");
				RefreshBridgeStatus();
				ShowMineHarborDialog(this, LauncherUiText("브리지를 설치했습니다. 다음 서버 시작부터 실시간 자동완성을 사용할 수 있습니다.", "Bridge installed. Live suggestions will be available on the next server start."), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception exception) { ShowMineHarborDialog(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}

		private void RemoveBridge(object sender, EventArgs eventArgs)
		{
			try
			{
				DialogResult choice = ShowMineHarborDialog(this, LauncherUiText("브리지 설정과 캐시도 함께 삭제하시겠습니까?\r\n아니요를 선택하면 사용자 데이터는 보존됩니다.", "Also remove bridge settings and cache?\r\nChoose No to preserve user data."), Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (choice == DialogResult.Cancel) return;
				bool removeData = choice == DialogResult.Yes;
				RemoveManagedCommandBridge(serverDirectory, removeData);
				WriteBridgeChoice(serverDirectory, "skip");
				RefreshBridgeStatus();
			}
			catch (Exception exception) { ShowMineHarborDialog(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}

		private void OpenBridgeFolder(object sender, EventArgs eventArgs)
		{
			string folder = Path.Combine(serverDirectory, "plugins");
			Directory.CreateDirectory(folder);
			Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
		}

		private void RefreshBridgeStatus(object sender, EventArgs eventArgs) { RefreshBridgeStatus(); }

		private void RefreshBridgeStatus()
		{
			BridgeManagedInfo managed = ReadBridgeManagedInfo(serverDirectory);
			CommandBridgeSession active = GetActiveCommandBridge();
			bool supported = IsCommandBridgeSupported(options.ServerType, options.MinecraftVersion);
			bridgeStatus.Text = LauncherUiText("지원: ", "Supported: ") + (supported ? LauncherUiText("예", "Yes") : LauncherUiText("아니요", "No")) +
				"\r\n" + LauncherUiText("설치: ", "Installed: ") + (managed == null ? LauncherUiText("아니요", "No") : LauncherUiText("예", "Yes")) +
				"\r\n" + LauncherUiText("연결: ", "Connected: ") + (active != null && active.Connected ? LauncherUiText("예", "Yes") : LauncherUiText("아니요", "No")) +
				"\r\n" + LauncherUiText("브리지 버전: ", "Bridge version: ") + (managed == null ? "-" : managed.Version) +
				"\r\n" + LauncherUiText("프로토콜: ", "Protocol: ") + (managed == null ? "-" : managed.Protocol.ToString()) +
				"\r\n" + LauncherUiText("발견된 명령: ", "Discovered commands: ") + (active == null ? "0" : active.CommandCount.ToString()) +
				"\r\n" + LauncherUiText("마지막 연결: ", "Last connected: ") + (active == null || active.LastConnectedUtc == DateTime.MinValue ? "-" : active.LastConnectedUtc.ToLocalTime().ToString("g")) +
				"\r\n" + LauncherUiText("호환성 상태: ", "Compatibility: ") + (!supported ? LauncherUiText("호환되지 않음", "Incompatible") : managed != null && !string.Equals(managed.Version, BuildVersionInfo.ProductVersion, StringComparison.OrdinalIgnoreCase) ? LauncherUiText("업데이트 필요", "Update required") : LauncherUiText("호환됨", "Compatible")) +
				"\r\n\r\n" + LauncherUiText("브리지는 127.0.0.1만 사용하며 외부 포트를 열지 않습니다.", "The bridge only uses 127.0.0.1 and opens no external port.");
		}
	}

	private sealed class QuickCommandEditorForm : Form
	{
		private readonly TextBox nameBox;
		private readonly TextBox descriptionBox;
		private readonly ComboBox categoryBox;
		private readonly TextBox templateBox;
		private readonly ComboBox riskBox;
		private readonly TextBox minimumVersionBox;
		private readonly TextBox maximumVersionBox;
		private readonly CheckedListBox serverTypes;
		private readonly string originalId;
		public QuickCommandDefinition Value { get; private set; }

		public QuickCommandEditorForm(QuickCommandDefinition value)
		{
			originalId = value == null ? Guid.NewGuid().ToString("N") : value.Id;
			Text = LauncherUiText(value == null ? "사용자 명령 추가" : "사용자 명령 수정", value == null ? "Add user command" : "Edit user command");
			ApplyLauncherWindowIcon(this);
			StartPosition = FormStartPosition.CenterParent;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			ClientSize = new Size(560, 620);
			Font = new Font("Pretendard", 11F);
			AutoScaleMode = AutoScaleMode.Dpi;
			TableLayoutPanel layout = new TableLayoutPanel();
			layout.Dock = DockStyle.Fill;
			layout.Padding = new Padding(22);
			layout.ColumnCount = 1;
			layout.RowCount = 18;
			Controls.Add(layout);
			nameBox = AddEditorText(layout, LauncherUiText("표시 이름", "Display name"), value == null ? string.Empty : value.Name);
			descriptionBox = AddEditorText(layout, LauncherUiText("설명", "Description"), value == null ? string.Empty : value.Description);
			layout.Controls.Add(new Label { Text = LauncherUiText("카테고리", "Category"), AutoSize = true });
			categoryBox = new ModernComboBox(); categoryBox.DropDownStyle = ComboBoxStyle.DropDownList; categoryBox.Items.AddRange(new object[] { "user", "server", "player", "whitelist", "world", "info" }); categoryBox.SelectedItem = value == null ? "user" : value.Category; if (categoryBox.SelectedIndex < 0) categoryBox.SelectedIndex = 0; categoryBox.Dock = DockStyle.Top; ConfigureAccessibleField(categoryBox, LauncherUiText("카테고리", "Category"), LauncherUiText("명령이 표시될 분류를 선택합니다.", "Choose the category where this command appears.")); layout.Controls.Add(categoryBox);
			templateBox = AddEditorText(layout, LauncherUiText("명령 템플릿", "Command template"), value == null ? string.Empty : value.Template);
			layout.Controls.Add(new Label { Text = LauncherUiText("위험도", "Risk level"), AutoSize = true, Margin = new Padding(0, 7, 0, 3) });
			riskBox = new ModernComboBox(); riskBox.DropDownStyle = ComboBoxStyle.DropDownList; riskBox.Items.AddRange(new object[] { LauncherUiText("일반 · 바로 실행", "Normal · run immediately"), LauncherUiText("확인 · 실행 전 질문", "Confirm · ask before running"), LauncherUiText("위험 · 강한 경고", "Dangerous · strong warning") }); riskBox.SelectedIndex = value == null ? 0 : (int)GetDefinitionRisk(value); riskBox.Dock = DockStyle.Top; ConfigureAccessibleField(riskBox, LauncherUiText("명령 위험도", "Command risk level"), LauncherUiText("실행 전 확인과 경고 강도를 선택합니다.", "Choose the confirmation and warning level.")); layout.Controls.Add(riskBox);
			minimumVersionBox = AddEditorText(layout, LauncherUiText("최소 Minecraft 버전 (선택)", "Minimum Minecraft version (optional)"), value == null ? string.Empty : value.MinimumMinecraftVersion);
			maximumVersionBox = AddEditorText(layout, LauncherUiText("최대 Minecraft 버전 (선택)", "Maximum Minecraft version (optional)"), value == null ? string.Empty : value.MaximumMinecraftVersion);
			layout.Controls.Add(new Label { Text = LauncherUiText("지원 서버 종류 (선택하지 않으면 모두)", "Supported server types (none means all)"), AutoSize = true });
			serverTypes = new CheckedListBox(); serverTypes.Height = 74; serverTypes.Items.AddRange(new object[] { "paper", "purpur", "vanilla", "fabric", "forge", "neoforge", "custom" }); ConfigureAccessibleField(serverTypes, LauncherUiText("지원 서버 종류", "Supported server types"), LauncherUiText("선택하지 않으면 모든 서버 종류에서 명령을 표시합니다.", "Leave all items clear to show the command for every server type.")); layout.Controls.Add(serverTypes);
			if (value != null && value.ServerTypes != null) for (int i = 0; i < serverTypes.Items.Count; i++) if (value.ServerTypes.Contains(Convert.ToString(serverTypes.Items[i]), StringComparer.OrdinalIgnoreCase)) serverTypes.SetItemChecked(i, true);
			Label hint = new Label(); hint.Text = LauncherUiText("필수 {player} · 선택 [reason] · 기본값 [count=1]", "Required {player} · optional [reason] · default [count=1]"); hint.AutoSize = true; layout.Controls.Add(hint);
			FlowLayoutPanel buttons = new FlowLayoutPanel(); buttons.FlowDirection = FlowDirection.RightToLeft; buttons.Dock = DockStyle.Fill; layout.Controls.Add(buttons);
			Button save = new RoundedButton(); save.Text = LauncherUiText("저장", "Save"); save.Size = new Size(110, 40); save.Click += Save; buttons.Controls.Add(save);
			Button cancel = new RoundedButton(); cancel.Text = LauncherUiText("취소", "Cancel"); cancel.Size = new Size(100, 40); cancel.DialogResult = DialogResult.Cancel; buttons.Controls.Add(cancel);
			AcceptButton = save; CancelButton = cancel;
			ApplySimpleDialogTheme(this);
		}

		private static TextBox AddEditorText(TableLayoutPanel layout, string label, string value)
		{
			layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 7, 0, 3) });
			TextBox box = new TextBox(); box.Text = value ?? string.Empty; box.Dock = DockStyle.Top; ConfigureAccessibleField(box, label, LauncherUiText("사용자 명령 정보를 입력합니다.", "Enter the user command information.")); layout.Controls.Add(box); return box;
		}

		private void Save(object sender, EventArgs eventArgs)
		{
			QuickCommandDefinition command = new QuickCommandDefinition(); command.Id = originalId; command.Name = nameBox.Text.Trim(); command.Description = descriptionBox.Text.Trim(); command.Category = Convert.ToString(categoryBox.SelectedItem); command.Template = templateBox.Text; command.Risk = (QuickCommandRisk)Math.Max(0, riskBox.SelectedIndex); command.Confirm = command.Risk != QuickCommandRisk.Normal; command.ServerTypes = serverTypes.CheckedItems.Cast<object>().Select(Convert.ToString).ToArray(); command.MinimumMinecraftVersion = minimumVersionBox.Text.Trim(); command.MaximumMinecraftVersion = maximumVersionBox.Text.Trim(); command.Source = "user";
			string error;
			if (!ValidateUserQuickCommand(command, out error)) { ShowMineHarborDialog(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
			Value = command; DialogResult = DialogResult.OK; Close();
		}
	}

	private sealed class CommandBridgeConsentForm : Form
	{
		private readonly CheckBox defaultChoice;
		public BridgeConsentResult Result { get; private set; }

		public CommandBridgeConsentForm()
		{
			Text = LauncherUiText("실시간 명령 자동완성", "Live command suggestions");
			ApplyLauncherWindowIcon(this);
			StartPosition = FormStartPosition.CenterParent;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			ClientSize = new Size(560, 330);
			AutoScaleMode = AutoScaleMode.Dpi;
			Font = new Font("Pretendard", 11F);
			Label title = new Label(); title.Text = LauncherUiText("실시간 명령 자동완성 플러그인을 설치하시겠습니까?", "Install the live command suggestion plugin?"); title.Font = new Font("Pretendard", 16F, FontStyle.Bold); title.AutoSize = false; title.Location = new Point(28, 26); title.Size = new Size(500, 56); Controls.Add(title);
			Label body = new Label(); body.Text = LauncherUiText("설치하면 현재 서버와 설치된 플러그인의 명령 및\r\n자동완성 후보를 런처에서 실시간으로 사용할 수 있습니다.\r\n\r\n브리지는 현재 PC 내부에서만 런처와 통신하며,\r\n별도의 외부 네트워크 포트를 열지 않습니다.", "It provides live commands and suggestions from this server\r\nand its installed plugins.\r\n\r\nThe bridge communicates only inside this PC and does not\r\nopen a separate external network port."); body.Location = new Point(30, 88); body.Size = new Size(500, 124); Controls.Add(body);
			defaultChoice = new ModernCheckBox(); defaultChoice.Text = LauncherUiText("이 선택을 이후 새 Paper/Purpur 서버의 기본값으로 사용", "Use this choice as the default for new Paper/Purpur servers"); defaultChoice.Location = new Point(30, 218); defaultChoice.Size = new Size(500, 30); Controls.Add(defaultChoice);
			Button skip = new RoundedButton(); skip.Text = LauncherUiText("설치하지 않기", "Do not install"); skip.Size = new Size(154, 44); skip.Location = new Point(206, 266); skip.Click += delegate { Complete("skip"); }; Controls.Add(skip);
			Button install = new RoundedButton(); install.Text = LauncherUiText("설치하기", "Install"); install.Size = new Size(154, 44); install.Location = new Point(374, 266); install.Click += delegate { Complete("install"); }; Controls.Add(install);
			CancelButton = skip;
			ApplySimpleDialogTheme(this);
		}

		private void Complete(string choice)
		{
			Result = new BridgeConsentResult(); Result.Choice = choice; Result.UseAsDefault = defaultChoice.Checked; DialogResult = DialogResult.OK; Close();
		}
	}

	private static QuickCommandDefinition CloneQuickCommand(QuickCommandDefinition value)
	{
		QuickCommandDefinition clone = new QuickCommandDefinition(); clone.Id = value.Id; clone.Name = value.Name; clone.Description = value.Description; clone.Category = value.Category; clone.Template = value.Template; clone.Parameters = value.Parameters == null ? new string[0] : (string[])value.Parameters.Clone(); clone.Risk = GetDefinitionRisk(value); clone.Confirm = clone.Risk != QuickCommandRisk.Normal; clone.ServerTypes = value.ServerTypes == null ? new string[0] : (string[])value.ServerTypes.Clone(); clone.MinimumMinecraftVersion = value.MinimumMinecraftVersion; clone.MaximumMinecraftVersion = value.MaximumMinecraftVersion; clone.Source = value.Source; return clone;
	}
}
