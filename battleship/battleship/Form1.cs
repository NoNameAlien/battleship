using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace battleship
{
    public partial class Form1 : Form
    {
        // ====== ÊÎÍÑÒÀÍÒÛ ÄËß ÐÀÇÌÅÒÊÈ ======
        const int CellSize = 30;
        const int CellGap = 1;
        const int FieldMarginX = 20;
        const int FieldMarginY = 30;

        // ====== ÂÍÓÒÐÅÍÍÈÅ ÒÈÏÛ ======

        // Ñîñòîÿíèå êëåòêè
        enum CellState
        {
            Empty,
            Ship,
            Hit,
            Miss
        }

        // Ð˜Ð³Ñ€Ð¾Ðº
        enum Player
        {
            Player1,
            Player2
        }

        // Ôàçà èãðû
        enum GamePhase
        {
            SetupP1,   // Èãðîê 1 ðàññòàâëÿåò êîðàáëè
            SetupP2,   // Èãðîê 2 ðàññòàâëÿåò êîðàáëè
            Battle     // Áîé
        }

        // Ôîðìà ïåðåêëþ÷åíèÿ õîäà/ôàçû, êîòîðàÿ æä¸ò íàæàòèÿ "Ãîòîâî"
        enum PendingSwitchMode
        {
            None,
            ToSetupP2,      // ïîñëå ðàññòàíîâêè Èãðîêà 1 – ïåðåéòè ê ðàññòàíîâêå Èãðîêà 2
            ToBattleP1,     // ïîñëå ðàññòàíîâêè Èãðîêà 2 – ïåðåéòè ê áîþ, õîä Èãðîêà 1
            NextTurnBattle  // ïîñëå "Êîíåö õîäà" – ïåðåéòè ê ñëåäóþùåìó èãðîêó â áîþ
        }

        // Èãðîâîå ïîëå
        class Board
        {
            public const int Size = 10;
            public CellState[,] Cells = new CellState[Size, Size];
            public int TotalShipCells;     // ÑÐºÐ¾Ð»ÑŒÐºÐ¾ ÐºÐ»ÐµÑ‚Ð¾Ðº Ð·Ð°Ð½ÑÑ‚Ð¾ ÐºÐ¾Ñ€Ð°Ð±Ð»ÑÐ¼Ð¸
            public int HitCount;           // ÑÐºÐ¾Ð»ÑŒÐºÐ¾ ÐºÐ»ÐµÑ‚Ð¾Ðº ÑƒÐ¶Ðµ Ð¿Ð¾Ð´Ð±Ð¸Ñ‚Ð¾

            public bool AllShipsDestroyed => HitCount >= TotalShipCells;
        }

        // ====== ÏÎËß ÑÎÑÒÎßÍÈß È ÈÍÒÅÐÔÅÉÑÀ ======

        // Èãðîâîå ñîñòîÿíèå
        Board board1 = new Board();   // ïîëå èãðîêà 1
        Board board2 = new Board();   // ïîëå èãðîêà 2
        Player currentPlayer = Player.Player1;
        GamePhase phase = GamePhase.SetupP1;
        PendingSwitchMode pendingSwitch = PendingSwitchMode.None;
        bool gameOver = false;

        // Áûë ëè óæå ñäåëàí âûñòðåë â òåêóùåì õîäó
        bool shotMadeThisTurn = false;

        // Êíîïêè äëÿ îòîáðàæåíèÿ ïîëåé
        Button[,] buttonsP1 = new Button[Board.Size, Board.Size];
        Button[,] buttonsP2 = new Button[Board.Size, Board.Size];

        // Ýëåìåíòû UI
        Label lblCurrentPlayer;
        Label lblInfo;
        GroupBox groupP1;
        GroupBox groupP2;

        // Êíîïêà "Çàâåðøèòü ðàññòàíîâêó" / "Êîíåö õîäà"
        Button btnFinishSetup;

        // Ïàíåëü "Ïåðåäàéòå õîä äðóãîìó èãðîêó"
        Panel panelSwitch;
        Label lblSwitchMessage;
        Button btnSwitchOk;

        public Form1()
        {
            InitializeComponent(); // êîä èç Form1.Designer.cs

            this.Text = "ÐœÐ¾Ñ€ÑÐºÐ¾Ð¹ Ð±Ð¾Ð¹ - Ñ‡ÐµÐ»Ð¾Ð²ÐµÐº Ð¿Ñ€Ð¾Ñ‚Ð¸Ð² Ñ‡ÐµÐ»Ð¾Ð²ÐµÐºÐ°";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            CreateUi();
            CreateMainMenu();
            ShowMainMenu();
        }

        // ====== ÑÎÇÄÀÍÈÅ ÈÍÒÅÐÔÅÉÑÀ ======

        private void CreateUi()
        {
            // ÐœÐµÑ‚ÐºÐ° "Ð§ÐµÐ¹ Ñ…Ð¾Ð´"
            lblCurrentPlayer = new Label
            {
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold),
                Location = new Point(20, 10)
            };
            this.Controls.Add(lblCurrentPlayer);

            // Ð˜Ð½Ñ„Ð¾Ñ€Ð¼Ð°Ñ†Ð¸Ð¾Ð½Ð½Ð°Ñ ÑÑ‚Ñ€Ð¾ÐºÐ° (ÑÐ¾Ð¾Ð±Ñ‰ÐµÐ½Ð¸Ñ Ð¾Ð± Ð¾ÑˆÐ¸Ð±ÐºÐ°Ñ… Ð¸ Ñ‚.Ð¿.)
            lblInfo = new Label
            {
                AutoSize = false,
                Width = 850,
                Height = 30,
                Location = new Point(20, 40),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblInfo);

            // Ðàçìåðû ðàìêè ïîëÿ â çàâèñèìîñòè îò ñåòêè
            int fieldWidth = FieldMarginX * 2 + Board.Size * CellSize + (Board.Size - 1) * CellGap;
            int fieldHeight = FieldMarginY + Board.Size * CellSize + (Board.Size - 1) * CellGap + 20;

            // Ãðóïïà äëÿ ïîëÿ Èãðîêà 1
            groupP1 = new GroupBox
            {
                Text = "Ïîëå èãðîêà 1",
                Location = new Point(20, 80),
                Size = new Size(fieldWidth, fieldHeight)
            };
            this.Controls.Add(groupP1);

            // Ãðóïïà äëÿ ïîëÿ Èãðîêà 2
            groupP2 = new GroupBox
            {
                Text = "Ïîëå èãðîêà 2",
                Location = new Point(450, 80),
                Size = new Size(fieldWidth, fieldHeight)
            };
            this.Controls.Add(groupP2);

            // Êíîïêà "Çàâåðøèòü ðàññòàíîâêó" äëÿ ôàç ðàññòàíîâêè è "Êîíåö õîäà" â áîþ
            btnFinishSetup = new Button
            {
                Text = "Çàâåðøèòü ðàññòàíîâêó",
                Location = new Point(360, 10),
                AutoSize = true
            };
            btnFinishSetup.Click += BtnFinishSetup_Click;
            this.Controls.Add(btnFinishSetup);

            // Ñîçäà¸ì ñåòêè êíîïîê 10x10
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    int bx = FieldMarginX + x * (CellSize + CellGap);
                    int by = FieldMarginY + y * (CellSize + CellGap);

                    // Êíîïêè äëÿ ïîëÿ èãðîêà 1
                    var btn1 = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Location = new Point(bx, by),
                        Tag = new Point(x, y) // çàïîìèíàåì êîîðäèíàòû
                    };
                    btn1.Click += Player1BoardClick;
                    groupP1.Controls.Add(btn1);
                    buttonsP1[x, y] = btn1;

                    // ÐšÐ½Ð¾Ð¿ÐºÐ¸ Ð´Ð»Ñ Ð¿Ð¾Ð»Ñ Ð¸Ð³Ñ€Ð¾ÐºÐ° 2
                    var btn2 = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Location = new Point(bx, by),
                        Tag = new Point(x, y)
                    };
                    btn2.Click += Player2BoardClick;
                    groupP2.Controls.Add(btn2);
                    buttonsP2[x, y] = btn2;
                }
            }

            // Ïàíåëü "Ïåðåäàéòå õîä"
            panelSwitch = new Panel
            {
                Size = new Size(400, 150),
                BackColor = Color.FromArgb(230, 230, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            panelSwitch.Location = new Point(
                (this.ClientSize.Width - panelSwitch.Width) / 2,
                (this.ClientSize.Height - panelSwitch.Height) / 2);

            lblSwitchMessage = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
            };
            panelSwitch.Controls.Add(lblSwitchMessage);

            btnSwitchOk = new Button
            {
                Text = "Ãîòîâî",
                Width = 80,
                Height = 30,
                Location = new Point((panelSwitch.Width - 80) / 2, panelSwitch.Height - 50)
            };
            btnSwitchOk.Click += BtnSwitchOk_Click;
            panelSwitch.Controls.Add(btnSwitchOk);

            this.Controls.Add(panelSwitch);
            panelSwitch.BringToFront();
        }

        // ====== ÈÍÈÖÈÀËÈÇÀÖÈß ÈÃÐÛ ======

        private void InitBoards()
        {
            ClearBoard(board1);
            ClearBoard(board2);

            // Ñòàðòóåì ñ ðàññòàíîâêè Èãðîêà 1
            phase = GamePhase.SetupP1;
            currentPlayer = Player.Player1;
            gameOver = false;
            shotMadeThisTurn = false;

            btnFinishSetup.Enabled = true;
            btnFinishSetup.Text = "Çàâåðøèòü ðàññòàíîâêó";
            btnFinishSetup.BackColor = Color.LightYellow;

            UpdateCurrentPlayerLabel();
            UpdateGroupTitles();
            RefreshBoardsVisual();
            ShowInfo("Èãðîê 1: ðàññòàâüòå ñâîè êîðàáëè íà ëåâîì ïîëå. " +
                     "Êëèê – ïîñòàâèòü/óáðàòü ïàëóáó. Çàòåì íàæìèòå \"Çàâåðøèòü ðàññòàíîâêó\".");
            panelSwitch.Visible = false;
        }

        private void ClearBoard(Board b)
        {
            for (int y = 0; y < Board.Size; y++)
                for (int x = 0; x < Board.Size; x++)
                    b.Cells[x, y] = CellState.Empty;

            b.TotalShipCells = 0;
            b.HitCount = 0;
        }

        // Ïåðåêëþ÷åíèå îòîáðàæåíèÿ ïîëåé
        private void RefreshBoardsVisual()
        {
            // Ïîëå Èãðîêà 1 (ëåâîå)
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    var state = board1.Cells[x, y];
                    Color c = SystemColors.Control;

                    if (state == CellState.Hit)
                        c = Color.Red;
                    else if (state == CellState.Miss)
                        c = Color.LightGray;
                    else if (state == CellState.Ship)
                    {
                        if (phase == GamePhase.SetupP1 ||
                            (phase == GamePhase.Battle && currentPlayer == Player.Player1))
                            c = Color.LightGreen;
                        else
                            c = SystemColors.Control;
                    }

                    buttonsP1[x, y].BackColor = c;
                }
            }

            // Ïîëå Èãðîêà 2 (ïðàâîå)
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    var state = board2.Cells[x, y];
                    Color c = SystemColors.Control;

                    if (state == CellState.Hit)
                        c = Color.Red;
                    else if (state == CellState.Miss)
                        c = Color.LightGray;
                    else if (state == CellState.Ship)
                    {
                        if (phase == GamePhase.SetupP2 ||
                            (phase == GamePhase.Battle && currentPlayer == Player.Player2))
                            c = Color.LightGreen;
                        else
                            c = SystemColors.Control;
                    }

                    buttonsP2[x, y].BackColor = c;
                }
            }
        }

        // Ìàñêèðóåì îáà ïîëÿ, ÷òîáû ïðè ïåðåäà÷å õîäà íåëüçÿ áûëî íè÷åãî ïîäñìàòðèâàòü
        private void MaskBoardsVisual()
        {
            // Ëåâîå ïîëå (Èãðîê 1)
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    buttonsP1[x, y].BackColor = SystemColors.ControlDark;
                }
            }

            // Ïðàâîå ïîëå (Èãðîê 2)
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    buttonsP2[x, y].BackColor = SystemColors.ControlDark;
                }
            }
        }

        private void UpdateCurrentPlayerLabel()
        {
            string playerText = currentPlayer == Player.Player1 ? "Ð˜Ð³Ñ€Ð¾Ðº 1" : "Ð˜Ð³Ñ€Ð¾Ðº 2";
            lblCurrentPlayer.Text = $"Ð¥Ð¾Ð´: {playerText}";
        }

        private void UpdateGroupTitles()
        {
            if (phase == GamePhase.SetupP1)
            {
                groupP1.Text = "ÐŸÐ¾Ð»Ðµ Ð¸Ð³Ñ€Ð¾ÐºÐ° 1 (Ñ€Ð°ÑÑÑ‚Ð°Ð½Ð¾Ð²ÐºÐ°)";
                groupP2.Text = "ÐŸÐ¾Ð»Ðµ Ð¸Ð³Ñ€Ð¾ÐºÐ° 2 (Ð¾Ð¶Ð¸Ð´Ð°Ð½Ð¸Ðµ)";
            }
            else if (phase == GamePhase.SetupP2)
            {
                groupP1.Text = "ÐŸÐ¾Ð»Ðµ Ð¸Ð³Ñ€Ð¾ÐºÐ° 1 (Ð¾Ð¶Ð¸Ð´Ð°Ð½Ð¸Ðµ)";
                groupP2.Text = "ÐŸÐ¾Ð»Ðµ Ð¸Ð³Ñ€Ð¾ÐºÐ° 2 (Ñ€Ð°ÑÑÑ‚Ð°Ð½Ð¾Ð²ÐºÐ°)";
            }
            else // Battle
            {
                if (currentPlayer == Player.Player1)
                {
                    groupP1.Text = "Ð¡Ð²Ð¾Ñ‘ Ð¿Ð¾Ð»Ðµ";
                    groupP2.Text = "ÐŸÐ¾Ð»Ðµ Ð¿Ñ€Ð¾Ñ‚Ð¸Ð²Ð½Ð¸ÐºÐ°";
                }
                else
                {
                    groupP1.Text = "ÐŸÐ¾Ð»Ðµ Ð¿Ñ€Ð¾Ñ‚Ð¸Ð²Ð½Ð¸ÐºÐ°";
                    groupP2.Text = "Ð¡Ð²Ð¾Ñ‘ Ð¿Ð¾Ð»Ðµ";
                }
            }
        }

        private void UpdateGroupTitles()
        {
            if (phase == GamePhase.SetupP1)
            {
                groupP1.Text = "Ïîëå èãðîêà 1 (ðàññòàíîâêà)";
                groupP2.Text = "Ïîëå èãðîêà 2 (îæèäàíèå)";
            }
            else if (phase == GamePhase.SetupP2)
            {
                groupP1.Text = "Ïîëå èãðîêà 1 (îæèäàíèå)";
                groupP2.Text = "Ïîëå èãðîêà 2 (ðàññòàíîâêà)";
            }
            else // Battle
            {
                if (currentPlayer == Player.Player1)
                {
                    groupP1.Text = "Ñâî¸ ïîëå";
                    groupP2.Text = "Ïîëå ïðîòèâíèêà";
                }
                else
                {
                    groupP1.Text = "Ïîëå ïðîòèâíèêà";
                    groupP2.Text = "Ñâî¸ ïîëå";
                }
            }
        }

        private void ShowInfo(string message)
        {
            lblInfo.Text = message;
        }

        // ====== ÐÀÑÑÒÀÍÎÂÊÀ ÊÎÐÀÁËÅÉ ======

        private void ToggleShip(Board b, int x, int y)
        {
            if (b.Cells[x, y] == CellState.Ship)
            {
                b.Cells[x, y] = CellState.Empty;
                b.TotalShipCells--;
                if (b.TotalShipCells < 0) b.TotalShipCells = 0;
            }
            else if (b.Cells[x, y] == CellState.Empty)
            {
                b.Cells[x, y] = CellState.Ship;
                b.TotalShipCells++;
            }
        }

        private void BtnFinishSetup_Click(object? sender, EventArgs e)
        {
            if (phase == GamePhase.SetupP1)
            {
                if (board1.TotalShipCells == 0)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("Ó Èãðîêà 1 íåò íè îäíîãî êîðàáëÿ. Ðàññòàâüòå õîòÿ áû îäèí.");
                    return;
                }

                // Íå ïåðåêëþ÷àåì ôàçó è èãðîêà ñðàçó – òîëüêî ñòàâèì â î÷åðåäü
                pendingSwitch = PendingSwitchMode.ToSetupP2;

                ShowSwitchPanel("Ïåðåäàéòå óïðàâëåíèå Èãðîêó 2.\n" +
                                "Íàæìèòå \"Ãîòîâî\", êîãäà Èãðîê 2 áóäåò ãîòîâ ê ðàññòàíîâêå.");
            }
            else if (phase == GamePhase.SetupP2)
            {
                if (board2.TotalShipCells == 0)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("Ó Èãðîêà 2 íåò íè îäíîãî êîðàáëÿ. Ðàññòàâüòå õîòÿ áû îäèí.");
                    return;
                }

                // Ïåðåõîä ê áîþ – òîæå îòêëàäûâàåì äî íàæàòèÿ "Ãîòîâî"
                pendingSwitch = PendingSwitchMode.ToBattleP1;

                ShowSwitchPanel("Ïåðåäàéòå óïðàâëåíèå Èãðîêó 1.\n" +
                                "Íàæìèòå \"Ãîòîâî\", êîãäà Èãðîê 1 áóäåò ãîòîâ íà÷àòü áîé.");
            }
            else if (phase == GamePhase.Battle)
            {
                if (gameOver)
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                // shotMadeThisTurn == true òåïåðü îçíà÷àåò, ÷òî óæå áûë ÏÐÎÌÀÕ
                if (!shotMadeThisTurn)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("Õîä åù¸ íå çàâåðø¸í. Ñäåëàéòå âûñòðåë è ïðè ïðîìàõå íàæìèòå \"Êîíåö õîäà\".");
                    return;
                }

                // Çàâåðøåíèå õîäà âðó÷íóþ – òîëüêî ñòàâèì â î÷åðåäü ïåðåêëþ÷åíèå
                SwitchPlayer();
            }
        }

        // ====== ÏÀÍÅËÜ ÏÅÐÅÄÀ×È ÕÎÄÀ ======

        private void ShowSwitchPanel(string message)
        {
            MaskBoardsVisual();

            lblSwitchMessage.Text = message;
            panelSwitch.Location = new Point(
                (this.ClientSize.Width - panelSwitch.Width) / 2,
                (this.ClientSize.Height - panelSwitch.Height) / 2);
            panelSwitch.Visible = true;
            panelSwitch.BringToFront();
        }

        private void BtnSwitchOk_Click(object? sender, EventArgs e)
        {
            panelSwitch.Visible = false;

            // Âûïîëíÿåì îòëîæåííîå ïåðåêëþ÷åíèå
            if (pendingSwitch == PendingSwitchMode.ToSetupP2)
            {
                phase = GamePhase.SetupP2;
                currentPlayer = Player.Player2;
                shotMadeThisTurn = false;

                btnFinishSetup.Text = "Çàâåðøèòü ðàññòàíîâêó";
                btnFinishSetup.Enabled = true;
                btnFinishSetup.BackColor = Color.LightYellow;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                ShowInfo("Èãðîê 2: ðàññòàâüòå ñâîè êîðàáëè íà ïðàâîì ïîëå. " +
                         "Êëèê – ïîñòàâèòü/óáðàòü ïàëóáó. Çàòåì íàæìèòå \"Çàâåðøèòü ðàññòàíîâêó\".");
            }
            else if (pendingSwitch == PendingSwitchMode.ToBattleP1)
            {
                phase = GamePhase.Battle;
                currentPlayer = Player.Player1;
                gameOver = false;
                shotMadeThisTurn = false;

                btnFinishSetup.Text = "Êîíåö õîäà";
                btnFinishSetup.Enabled = false;
                btnFinishSetup.BackColor = Color.LightGray;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                ShowInfo("Áîé íà÷àëñÿ. Èãðîê 1 ñòðåëÿåò ïî âðàæåñêîìó ïîëþ (ñïðàâà).");
            }
            else if (pendingSwitch == PendingSwitchMode.NextTurnBattle)
            {
                // Òîëüêî òåïåðü ðåàëüíî ìåíÿåì èãðîêà
                currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
                shotMadeThisTurn = false;

                btnFinishSetup.Enabled = false;
                btnFinishSetup.BackColor = Color.LightGray;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                if (currentPlayer == Player.Player1)
                    ShowInfo("Èãðîê 1: ñäåëàéòå îäèí âûñòðåë ïî âðàæåñêîìó ïîëþ (ñïðàâà).");
                else
                    ShowInfo("Èãðîê 2: ñäåëàéòå îäèí âûñòðåë ïî âðàæåñêîìó ïîëþ (ñëåâà).");
            }

            pendingSwitch = PendingSwitchMode.None;
        }

        // ====== ÎÁÐÀÁÎÒÊÀ ÊËÈÊÎÂ ÏÎ ÏÎËßÌ ======

        private void Player1BoardClick(object? sender, EventArgs e)
        {
            if (panelSwitch.Visible) return;

            var btn = (Button)sender!;
            var pt = (Point)btn.Tag;
            int x = pt.X;
            int y = pt.Y;

            if (phase == GamePhase.SetupP1)
            {
                if (gameOver) return;
                ToggleShip(board1, x, y);
                RefreshBoardsVisual();
                return;
            }

            if (phase != GamePhase.Battle) return;
            if (gameOver)
            {
                SystemSounds.Beep.Play();
                return;
            }

            // Â áîþ ïî ïîëþ Èãðîêà 1 ìîæåò ñòðåëÿòü òîëüêî Èãðîê 2
            if (currentPlayer != Player.Player2)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Ñåé÷àñ õîä Èãðîêà 1 – îí ñòðåëÿåò ïî âðàæåñêîìó ïîëþ ñïðàâà.");
                return;
            }

            ProcessShot(board1, buttonsP1, x, y, shooter: Player.Player2);
        }

        private void Player2BoardClick(object? sender, EventArgs e)
        {
            if (panelSwitch.Visible) return;

            var btn = (Button)sender!;
            var pt = (Point)btn.Tag;
            int x = pt.X;
            int y = pt.Y;

            if (phase == GamePhase.SetupP2)
            {
                if (gameOver) return;
                ToggleShip(board2, x, y);
                RefreshBoardsVisual();
                return;
            }

            if (phase != GamePhase.Battle) return;
            if (gameOver)
            {
                SystemSounds.Beep.Play();
                return;
            }

            // Â áîþ ïî ïîëþ Èãðîêà 2 ìîæåò ñòðåëÿòü òîëüêî Èãðîê 1
            if (currentPlayer != Player.Player1)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Ñåé÷àñ õîä Èãðîêà 2 – îí ñòðåëÿåò ïî âðàæåñêîìó ïîëþ ñëåâà.");
                return;
            }

            ProcessShot(board2, buttonsP2, x, y, shooter: Player.Player1);
        }

        // ====== ËÎÃÈÊÀ ÂÛÑÒÐÅËÀ ======
        private void ProcessShot(Board targetBoard, Button[,] targetButtons, int x, int y, Player shooter)
        {
            if (phase != GamePhase.Battle) return;

            // shotMadeThisTurn == true òåïåðü çíà÷èò, ÷òî â ýòîì õîäó óæå áûë ÏÐÎÌÀÕ,
            // è íàäî òîëüêî íàæàòü "Êîíåö õîäà"
            if (shotMadeThisTurn)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Âû óæå ïðîìàõíóëèñü â ýòîì õîäó. Íàæìèòå \"Êîíåö õîäà\", ÷òîáû ïåðåäàòü õîä.");
                return;
            }

            var state = targetBoard.Cells[x, y];

            if (state == CellState.Hit || state == CellState.Miss)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Ð¡ÑŽÐ´Ð° ÑƒÐ¶Ðµ ÑÑ‚Ñ€ÐµÐ»ÑÐ»Ð¸. Ð’Ñ‹Ð±ÐµÑ€Ð¸Ñ‚Ðµ Ð´Ñ€ÑƒÐ³ÑƒÑŽ ÐºÐ»ÐµÑ‚ÐºÑƒ.");
                return;
            }

            if (state == CellState.Ship)
            {
                targetBoard.Cells[x, y] = CellState.Hit;
                targetBoard.HitCount++;
                RefreshBoardsVisual();

                if (targetBoard.AllShipsDestroyed)
                {
                    gameOver = true;
                    btnFinishSetup.Enabled = false;
                    btnFinishSetup.BackColor = Color.LightGray;

                    string winner = shooter == Player.Player1 ? "Èãðîê 1" : "Èãðîê 2";
                    ShowInfo($"Âñå êîðàáëè ïðîòèâíèêà óíè÷òîæåíû. Ïîáåäèë {winner}!");
                    MessageBox.Show(this, $"Ïîáåäèë {winner}!", "Èãðà îêîí÷åíà",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ÏÎÏÀÄÀÍÈÅ: õîä ïðîäîëæàåòñÿ, êíîïêó "Êîíåö õîäà" íå òðîãàåì
                ShowInfo("Ïîïàäàíèå! Ñòðåëÿéòå åù¸ ïî âðàæåñêîìó ïîëþ.");
            }
            else if (state == CellState.Empty)
            {
                targetBoard.Cells[x, y] = CellState.Miss;
                RefreshBoardsVisual();

                // ÏÐÎÌÀÕ: òåïåðü õîä ìîæíî çàâåðøàòü
                shotMadeThisTurn = true;
                btnFinishSetup.Enabled = true;
                btnFinishSetup.BackColor = Color.LightGreen;

                ShowInfo("Ìèìî. Íàæìèòå \"Êîíåö õîäà\", ÷òîáû ïåðåäàòü õîä ñîïåðíèêó.");
            }
        }

        private void SwitchPlayer()
        {
            if (gameOver) return;

            // Ãîòîâèì ïåðåêëþ÷åíèå õîäà
            pendingSwitch = PendingSwitchMode.NextTurnBattle;

            // Êíîïêà "Êîíåö õîäà" ñòàíîâèòñÿ íåàêòèâíîé äî âûñòðåëà â íîâîì õîäó
            btnFinishSetup.Enabled = false;
            btnFinishSetup.BackColor = Color.LightGray;

            string nextPlayerText = currentPlayer == Player.Player1 ? "Èãðîê 2" : "Èãðîê 1";

            ShowSwitchPanel(
                $"Õîä ïåðåõîäèò ê: {nextPlayerText}.\n" +
                "Ïåðåäàéòå óïðàâëåíèå ýòîìó èãðîêó.\n" +
                "Íàæìèòå \"Ãîòîâî\", êîãäà îí áóäåò ãîòîâ ê õîäó.");
        }
    }
}

