using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace battleship
{
    public partial class Form1 : Form
    {
        // ====== КОНСТАНТЫ ДЛЯ РАЗМЕТКИ ======
        const int CellSize = 30;
        const int CellGap = 1;
        const int FieldMarginX = 20;
        const int FieldMarginY = 30;

        // ====== ВНУТРЕННИЕ ТИПЫ ======

        // Состояние клетки
        enum CellState
        {
            Empty,
            Ship,
            Hit,
            Miss
        }

        // Игрок
        enum Player
        {
            Player1,
            Player2
        }

        // Фаза игры
        enum GamePhase
        {
            SetupP1,
            SetupP2,
            Battle
        }

        // Переключения, требующие подтверждения "Готово"
        enum PendingSwitchMode
        {
            None,
            ToSetupP2,
            ToBattleP1, 
            NextTurnBattle 
        }

        // Режим игры (для меню)
        enum GameMode
        {
            None,
            Custom,     // Своя игра (ручная расстановка)
            Standard    // Стандартная игра (авторасстановка)
        }

        // Игровое поле
        class Board
        {
            public const int Size = 10;
            public CellState[,] Cells = new CellState[Size, Size];
            public int TotalShipCells;     // сколько клеток занято кораблями
            public int HitCount;           // сколько клеток уже подбито

            public bool AllShipsDestroyed => HitCount >= TotalShipCells;
        }

        // ====== ПОЛЯ СОСТОЯНИЯ И ИНТЕРФЕЙСА ======

        // Игровое состояние
        Board board1 = new Board();   // поле игрока 1
        Board board2 = new Board();   // поле игрока 2
        Player currentPlayer = Player.Player1;
        GamePhase phase = GamePhase.SetupP1;
        PendingSwitchMode pendingSwitch = PendingSwitchMode.None;
        GameMode gameMode = GameMode.None;
        Random rng = new Random();
        bool gameOver = false;

        // Был ли уже ПРОМАХ в текущем ходу
        bool shotMadeThisTurn = false;

        // Кнопки для отображения полей
        Button[,] buttonsP1 = new Button[Board.Size, Board.Size];
        Button[,] buttonsP2 = new Button[Board.Size, Board.Size];

        // Элементы UI
        Label lblCurrentPlayer;
        Label lblInfo;
        GroupBox groupP1;
        GroupBox groupP2;

        // Кнопка "Завершить расстановку" / "Конец хода"
        Button btnFinishSetup;

        // Кнопка "Закончить бой" (возврат в меню)
        Button btnEndBattle;

        // Панель "Передайте ход другому игроку"
        Panel panelSwitch;
        Label lblSwitchMessage;
        Button btnSwitchOk;

        // Главное меню
        Panel panelMenu;
        Label lblMenuTitle;
        Button btnMenuStandard;
        Button btnMenuCustom;
        Button btnMenuExit;

        public Form1()
        {
            InitializeComponent();

            this.Text = "Морской бой - человек против человека";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            CreateUi();
            CreateMainMenu();
            ShowMainMenu();
        }

        // ====== СОЗДАНИЕ ИНТЕРФЕЙСА И МЕНЮ ======

        private void CreateUi()
        {
            // Метка "Чей ход"
            lblCurrentPlayer = new Label
            {
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold),
                Location = new Point(20, 10)
            };
            this.Controls.Add(lblCurrentPlayer);

            // Информационная строка (сообщения об ошибках и т.п.)
            lblInfo = new Label
            {
                AutoSize = false,
                Width = 850,
                Height = 30,
                Location = new Point(20, 40),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblInfo);

            // Размеры рамки поля в зависимости от сетки
            int fieldWidth = FieldMarginX * 2 + Board.Size * CellSize + (Board.Size - 1) * CellGap;
            int fieldHeight = FieldMarginY + Board.Size * CellSize + (Board.Size - 1) * CellGap + 20;

            // Группа для поля Игрока 1
            groupP1 = new GroupBox
            {
                Text = "Поле игрока 1",
                Location = new Point(20, 80),
                Size = new Size(fieldWidth, fieldHeight)
            };
            this.Controls.Add(groupP1);

            // Группа для поля Игрока 2
            groupP2 = new GroupBox
            {
                Text = "Поле игрока 2",
                Location = new Point(450, 80),
                Size = new Size(fieldWidth, fieldHeight)
            };
            this.Controls.Add(groupP2);

            // Кнопка "Завершить расстановку" / "Конец хода"
            btnFinishSetup = new Button
            {
                Text = "Завершить расстановку",
                Location = new Point(360, 10),
                AutoSize = true
            };
            btnFinishSetup.Click += BtnFinishSetup_Click;
            this.Controls.Add(btnFinishSetup);

            // Кнопка "Закончить бой"
            btnEndBattle = new Button
            {
                Text = "Закончить бой",
                AutoSize = true
            };
            int endBtnX = groupP2.Location.X + groupP2.Width - btnEndBattle.PreferredSize.Width;
            btnEndBattle.Location = new Point(endBtnX, 10);

            btnEndBattle.Click += BtnEndBattle_Click;
            this.Controls.Add(btnEndBattle);

            // Создаём сетки кнопок 10x10
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    int bx = FieldMarginX + x * (CellSize + CellGap);
                    int by = FieldMarginY + y * (CellSize + CellGap);

                    // Кнопки для поля игрока 1
                    var btn1 = new Button
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Location = new Point(bx, by),
                        Tag = new Point(x, y) 
                    };
                    btn1.Click += Player1BoardClick;
                    groupP1.Controls.Add(btn1);
                    buttonsP1[x, y] = btn1;

                    // Кнопки для поля игрока 2
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

            // Панель "Передайте ход"
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
                Text = "Готово",
                Width = 80,
                Height = 30,
                Location = new Point((panelSwitch.Width - 80) / 2, panelSwitch.Height - 50)
            };
            btnSwitchOk.Click += BtnSwitchOk_Click;
            panelSwitch.Controls.Add(btnSwitchOk);

            this.Controls.Add(panelSwitch);
            panelSwitch.BringToFront();
        }

        private void CreateMainMenu()
        {
            panelMenu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = this.BackColor,
                Visible = false
            };

            lblMenuTitle = new Label
            {
                AutoSize = false,
                Text = "Морской бой",
                ForeColor = Color.Black,
                Font = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };
            panelMenu.Controls.Add(lblMenuTitle);

            btnMenuStandard = new Button
            {
                Text = "Стандартная игра",
                Width = 200,
                Height = 40,
                Location = new Point(320, 130)
            };
            btnMenuStandard.Click += BtnMenuStandard_Click;
            panelMenu.Controls.Add(btnMenuStandard);

            btnMenuCustom = new Button
            {
                Text = "Своя игра",
                Width = 200,
                Height = 40,
                Location = new Point(320, 190)
            };
            btnMenuCustom.Click += BtnMenuCustom_Click;
            panelMenu.Controls.Add(btnMenuCustom);

            btnMenuExit = new Button
            {
                Text = "Выйти",
                Width = 200,
                Height = 40,
                Location = new Point(320, 250)
            };
            btnMenuExit.Click += BtnMenuExit_Click;
            panelMenu.Controls.Add(btnMenuExit);

            this.Controls.Add(panelMenu);
            panelMenu.BringToFront();
        }

        private void ShowMainMenu()
        {
            gameMode = GameMode.None;
            phase = GamePhase.SetupP1;
            pendingSwitch = PendingSwitchMode.None;
            gameOver = false;
            shotMadeThisTurn = false;

            panelMenu.Visible = true;
            panelMenu.BringToFront();
            panelSwitch.Visible = false;

            btnFinishSetup.Enabled = false;
            btnFinishSetup.BackColor = Color.LightGray;
            btnEndBattle.Enabled = false;
            btnEndBattle.BackColor = Color.LightGray;

            MaskBoardsVisual();
            lblCurrentPlayer.Text = "Морской бой";
            ShowInfo("Выберите режим: стандартная игра или своя игра.");
        }

        // ====== ИНИЦИАЛИЗАЦИЯ ИГРЫ ======

        // Своя игра - ручная расстановка
        private void InitBoards()
        {
            ClearBoard(board1);
            ClearBoard(board2);

            phase = GamePhase.SetupP1;
            currentPlayer = Player.Player1;
            gameOver = false;
            shotMadeThisTurn = false;

            btnFinishSetup.Enabled = true;
            btnFinishSetup.Text = "Завершить расстановку";
            btnFinishSetup.BackColor = Color.LightYellow;

            btnEndBattle.Enabled = true;
            btnEndBattle.BackColor = SystemColors.Control;

            UpdateCurrentPlayerLabel();
            UpdateGroupTitles();
            RefreshBoardsVisual();
            ShowInfo("Игрок 1: расставьте свои корабли на левом поле. " +
                     "Клик – поставить/убрать палубу. Затем нажмите \"Завершить расстановку\".");
            panelSwitch.Visible = false;
        }

        // Стандартная игра - авторасстановка
        private void InitBoardsStandard()
        {
            ClearBoard(board1);
            ClearBoard(board2);

            PlaceStandardFleet(board1);
            PlaceStandardFleet(board2);

            phase = GamePhase.Battle;
            currentPlayer = Player.Player1;
            gameOver = false;
            shotMadeThisTurn = false;

            btnFinishSetup.Enabled = false;
            btnFinishSetup.Text = "Конец хода";
            btnFinishSetup.BackColor = Color.LightGray;

            btnEndBattle.Enabled = true;
            btnEndBattle.BackColor = SystemColors.Control;

            UpdateCurrentPlayerLabel();
            UpdateGroupTitles();
            RefreshBoardsVisual();

            ShowInfo("Стандартная игра. Игрок 1 стреляет по вражескому полю (справа).");
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

        private void PlaceStandardFleet(Board b)
        {
            // Стандартный набор кораблей: 1x4, 2x3, 3x2, 4x1
            int[] shipLengths = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            // Локальная функция: проверка, можно ли поставить корабль
            bool CanPlace(int startX, int startY, int length, bool horizontal)
            {
                int dx = horizontal ? 1 : 0;
                int dy = horizontal ? 0 : 1;

                // Проверяем, не вылезает ли за границы
                int endX = startX + dx * (length - 1);
                int endY = startY + dy * (length - 1);
                if (endX < 0 || endX >= Board.Size || endY < 0 || endY >= Board.Size)
                    return false;

                // Проверяем клетки корабля и соседние (чтобы не соприкасались)
                for (int i = 0; i < length; i++)
                {
                    int x = startX + dx * i;
                    int y = startY + dy * i;

                    for (int ny = y - 1; ny <= y + 1; ny++)
                    {
                        for (int nx = x - 1; nx <= x + 1; nx++)
                        {
                            if (nx < 0 || nx >= Board.Size || ny < 0 || ny >= Board.Size)
                                continue;

                            if (b.Cells[nx, ny] == CellState.Ship)
                                return false;
                        }
                    }
                }

                return true;
            }

            // Локальная функция: фактически поставить корабль
            void PutShipRandom(int length)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed)
                {
                    attempts++;
                    if (attempts > 1000)
                        throw new Exception("Не удалось расставить корабли случайно. Попробуйте ещё раз.");

                    bool horizontal = rng.Next(2) == 0;
                    int startX = rng.Next(Board.Size);
                    int startY = rng.Next(Board.Size);

                    if (!CanPlace(startX, startY, length, horizontal))
                        continue;

                    int dx = horizontal ? 1 : 0;
                    int dy = horizontal ? 0 : 1;

                    for (int i = 0; i < length; i++)
                    {
                        int x = startX + dx * i;
                        int y = startY + dy * i;
                        b.Cells[x, y] = CellState.Ship;
                        b.TotalShipCells++;
                    }

                    placed = true;
                }
            }

            // Сбрасываем счётчик палуб
            b.TotalShipCells = 0;

            foreach (int len in shipLengths)
                PutShipRandom(len);
        }

        // Переключение отображения полей
        private void RefreshBoardsVisual()
        {
            // Поле Игрока 1 (левое)
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

            // Поле Игрока 2 (правое)
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

        // Маскируем полей
        private void MaskBoardsVisual()
        {
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    buttonsP1[x, y].BackColor = SystemColors.ControlDark;
                    buttonsP2[x, y].BackColor = SystemColors.ControlDark;
                }
            }
        }

        private void UpdateCurrentPlayerLabel()
        {
            string playerText = currentPlayer == Player.Player1 ? "Игрок 1" : "Игрок 2";
            lblCurrentPlayer.Text = $"Ход: {playerText}";
        }

        private void UpdateGroupTitles()
        {
            if (phase == GamePhase.SetupP1)
            {
                groupP1.Text = "Поле игрока 1 (расстановка)";
                groupP2.Text = "Поле игрока 2 (ожидание)";
            }
            else if (phase == GamePhase.SetupP2)
            {
                groupP1.Text = "Поле игрока 1 (ожидание)";
                groupP2.Text = "Поле игрока 2 (расстановка)";
            }
            else // Battle
            {
                if (currentPlayer == Player.Player1)
                {
                    groupP1.Text = "Своё поле";
                    groupP2.Text = "Поле противника";
                }
                else
                {
                    groupP1.Text = "Поле противника";
                    groupP2.Text = "Своё поле";
                }
            }
        }

        private void ShowInfo(string message)
        {
            lblInfo.Text = message;
        }

        // ====== РАССТАНОВКА КОРАБЛЕЙ ======

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
            if (panelMenu != null && panelMenu.Visible) return;

            if (phase == GamePhase.SetupP1)
            {
                if (board1.TotalShipCells == 0)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("У Игрока 1 нет ни одного корабля. Расставьте хотя бы один.");
                    return;
                }

                pendingSwitch = PendingSwitchMode.ToSetupP2;

                ShowSwitchPanel("Передайте управление Игроку 2.\n" +
                                "Нажмите \"Готово\", когда Игрок 2 будет готов к расстановке.");
            }
            else if (phase == GamePhase.SetupP2)
            {
                if (board2.TotalShipCells == 0)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("У Игрока 2 нет ни одного корабля. Расставьте хотя бы один.");
                    return;
                }

                pendingSwitch = PendingSwitchMode.ToBattleP1;

                ShowSwitchPanel("Передайте управление Игроку 1.\n" +
                                "Нажмите \"Готово\", когда Игрок 1 будет готов начать бой.");
            }
            else if (phase == GamePhase.Battle)
            {
                if (gameOver)
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                // shotMadeThisTurn == true означает, что уже был ПРОМАХ
                if (!shotMadeThisTurn)
                {
                    SystemSounds.Beep.Play();
                    ShowInfo("Ход ещё не завершён. При промахе нажмите \"Конец хода\".");
                    return;
                }

                SwitchPlayer();
            }
        }

        private void BtnEndBattle_Click(object? sender, EventArgs e)
        {
            ShowMainMenu();
        }

        // ====== ПАНЕЛЬ ПЕРЕДАЧИ ХОДА ======

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

            // Выполняем отложенное переключение
            if (pendingSwitch == PendingSwitchMode.ToSetupP2)
            {
                phase = GamePhase.SetupP2;
                currentPlayer = Player.Player2;
                shotMadeThisTurn = false;

                btnFinishSetup.Text = "Завершить расстановку";
                btnFinishSetup.Enabled = true;
                btnFinishSetup.BackColor = Color.LightYellow;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                ShowInfo("Игрок 2: расставьте свои корабли на правом поле. " +
                         "Клик – поставить/убрать палубу. Затем нажмите \"Завершить расстановку\".");
            }
            else if (pendingSwitch == PendingSwitchMode.ToBattleP1)
            {
                phase = GamePhase.Battle;
                currentPlayer = Player.Player1;
                gameOver = false;
                shotMadeThisTurn = false;

                btnFinishSetup.Text = "Конец хода";
                btnFinishSetup.Enabled = false;
                btnFinishSetup.BackColor = Color.LightGray;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                ShowInfo("Бой начался. Игрок 1 стреляет по вражескому полю (справа).");
            }
            else if (pendingSwitch == PendingSwitchMode.NextTurnBattle)
            {
                currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
                shotMadeThisTurn = false;

                btnFinishSetup.Enabled = false;
                btnFinishSetup.BackColor = Color.LightGray;

                UpdateCurrentPlayerLabel();
                UpdateGroupTitles();
                RefreshBoardsVisual();

                if (currentPlayer == Player.Player1)
                    ShowInfo("Игрок 1: сделайте выстрел по вражескому полю (справа).");
                else
                    ShowInfo("Игрок 2: сделайте выстрел по вражескому полю (слева).");
            }

            pendingSwitch = PendingSwitchMode.None;
        }

        // ====== ОБРАБОТКА КЛИКОВ ПО ПОЛЯМ ======

        private void Player1BoardClick(object? sender, EventArgs e)
        {
            if (panelSwitch.Visible || (panelMenu != null && panelMenu.Visible)) return;

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

            // В бою по полю Игрока 1 может стрелять только Игрок 2
            if (currentPlayer != Player.Player2)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Сейчас ход Игрока 1 – он стреляет по вражескому полю справа.");
                return;
            }

            ProcessShot(board1, buttonsP1, x, y, shooter: Player.Player2);
        }

        private void Player2BoardClick(object? sender, EventArgs e)
        {
            if (panelSwitch.Visible || (panelMenu != null && panelMenu.Visible)) return;

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

            if (currentPlayer != Player.Player1)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Сейчас ход Игрока 2 – он стреляет по вражескому полю слева.");
                return;
            }

            ProcessShot(board2, buttonsP2, x, y, shooter: Player.Player1);
        }

        // ====== ЛОГИКА ВЫСТРЕЛА ======
        private void ProcessShot(Board targetBoard, Button[,] targetButtons, int x, int y, Player shooter)
        {
            if (phase != GamePhase.Battle) return;

            // shotMadeThisTurn == true → уже был промах, ждём только "Конец хода"
            if (shotMadeThisTurn)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Вы уже промахнулись в этом ходу. Нажмите \"Конец хода\", чтобы передать ход.");
                return;
            }

            var state = targetBoard.Cells[x, y];

            if (state == CellState.Hit || state == CellState.Miss)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Сюда уже стреляли. Выберите другую клетку.");
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

                    string winner = shooter == Player.Player1 ? "Игрок 1" : "Игрок 2";
                    ShowInfo($"Все корабли противника уничтожены. Победил {winner}!");
                    MessageBox.Show(this, $"Победил {winner}!", "Игра окончена",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShowInfo("Попадание! Стреляйте ещё по вражескому полю.");
            }
            else if (state == CellState.Empty)
            {
                targetBoard.Cells[x, y] = CellState.Miss;
                RefreshBoardsVisual();

                shotMadeThisTurn = true;
                btnFinishSetup.Enabled = true;
                btnFinishSetup.BackColor = Color.LightGreen;

                ShowInfo("Мимо. Нажмите \"Конец хода\", чтобы передать ход сопернику.");
            }
        }

        private void SwitchPlayer()
        {
            if (gameOver) return;

            pendingSwitch = PendingSwitchMode.NextTurnBattle;

            btnFinishSetup.Enabled = false;
            btnFinishSetup.BackColor = Color.LightGray;

            string nextPlayerText = currentPlayer == Player.Player1 ? "Игрок 2" : "Игрок 1";

            ShowSwitchPanel(
                $"Ход переходит к: {nextPlayerText}.\n" +
                "Передайте управление этому игроку.\n" +
                "Нажмите \"Готово\", когда он будет готов к ходу.");
        }

        // ====== ОБРАБОТЧИКИ КНОПОК МЕНЮ ======

        private void BtnMenuStandard_Click(object? sender, EventArgs e)
        {
            panelMenu.Visible = false;
            gameMode = GameMode.Standard;
            InitBoardsStandard();
        }

        private void BtnMenuCustom_Click(object? sender, EventArgs e)
        {
            panelMenu.Visible = false;
            gameMode = GameMode.Custom;
            InitBoards();
        }

        private void BtnMenuExit_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
