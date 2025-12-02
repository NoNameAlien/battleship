using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace battleship
{
    public partial class Form1 : Form
    {
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

        // Игровое поле
        class Board
        {
            public const int Size = 10;
            public CellState[,] Cells = new CellState[Size, Size];
            public int TotalShipCells;     // сколько клеток занято кораблями
            public int HitCount;           // сколько клеток уже подбито

            public bool AllShipsDestroyed => HitCount >= TotalShipCells;
        }

        // Игровое состояние
        Board board1 = new Board();   // поле игрока 1
        Board board2 = new Board();   // поле игрока 2
        Player currentPlayer = Player.Player1;
        bool gameOver = false;

        // Кнопки для отображения полей
        Button[,] buttonsP1 = new Button[Board.Size, Board.Size];
        Button[,] buttonsP2 = new Button[Board.Size, Board.Size];

        Label lblCurrentPlayer;
        Label lblInfo;

        public Form1()
        {
            InitializeComponent(); // вызов метода из Form1.Designer.cs

            this.Text = "Морской бой - человек против человека";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            CreateUi();
            InitBoards();
            UpdateCurrentPlayerLabel();
        }

        // Создание интерфейса (две сетки кнопок + подписи)
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

            // Группа для поля Игрока 1
            GroupBox groupP1 = new GroupBox
            {
                Text = "Поле игрока 1 (свои корабли)",
                Location = new Point(20, 80),
                Size = new Size(400, 380)
            };
            this.Controls.Add(groupP1);

            // Группа для поля Игрока 2
            GroupBox groupP2 = new GroupBox
            {
                Text = "Поле игрока 2 (свои корабли)",
                Location = new Point(450, 80),
                Size = new Size(400, 380)
            };
            this.Controls.Add(groupP2);

            // Создаём сетки кнопок 10x10
            int cellSize = 30;
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    // Кнопки для поля игрока 1
                    var btn1 = new Button
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Location = new Point(20 + x * (cellSize + 1), 30 + y * (cellSize + 1)),
                        Tag = new Point(x, y) // запоминаем координаты
                    };
                    btn1.Click += Player1BoardClick;
                    groupP1.Controls.Add(btn1);
                    buttonsP1[x, y] = btn1;

                    // Кнопки для поля игрока 2
                    var btn2 = new Button
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Location = new Point(20 + x * (cellSize + 1), 30 + y * (cellSize + 1)),
                        Tag = new Point(x, y)
                    };
                    btn2.Click += Player2BoardClick;
                    groupP2.Controls.Add(btn2);
                    buttonsP2[x, y] = btn2;
                }
            }
        }

        // Инициализация полей: расставим тестовые корабли
        private void InitBoards()
        {
            // Всё пусто
            ClearBoard(board1);
            ClearBoard(board2);

            // Расставим корабли (просто фиксированные, для примера)
            PlaceTestShips(board1);
            PlaceTestShips(board2);

            // Отобразим свои корабли игрокам (для наглядности в учебном варианте)
            DrawOwnShips(buttonsP1, board1);
            DrawOwnShips(buttonsP2, board2);
        }

        private void ClearBoard(Board b)
        {
            for (int y = 0; y < Board.Size; y++)
                for (int x = 0; x < Board.Size; x++)
                    b.Cells[x, y] = CellState.Empty;

            b.TotalShipCells = 0;
            b.HitCount = 0;
        }

        // Пример расстановки: один 4-палубник, два 3-палубника и т.п.
        private void PlaceTestShips(Board b)
        {
            void PutShip(int x1, int y1, int x2, int y2)
            {
                if (x1 == x2)
                {
                    // вертикальный
                    int step = y2 > y1 ? 1 : -1;
                    for (int y = y1; y != y2 + step; y += step)
                    {
                        b.Cells[x1, y] = CellState.Ship;
                        b.TotalShipCells++;
                    }
                }
                else if (y1 == y2)
                {
                    // горизонтальный
                    int step = x2 > x1 ? 1 : -1;
                    for (int x = x1; x != x2 + step; x += step)
                    {
                        b.Cells[x, y1] = CellState.Ship;
                        b.TotalShipCells++;
                    }
                }
            }

            // 4-палубник
            PutShip(1, 1, 4, 1);
            // 3-палубники
            PutShip(1, 3, 3, 3);
            PutShip(6, 2, 6, 4);
            // 2-палубники
            PutShip(0, 7, 1, 7);
            PutShip(4, 6, 5, 6);
            PutShip(8, 8, 9, 8);
            // 1-палубники
            PutShip(9, 0, 9, 0);
            PutShip(0, 9, 0, 9);
            PutShip(5, 9, 5, 9);
            PutShip(7, 5, 7, 5);
        }

        // Подсветка своих кораблей (зелёные)
        private void DrawOwnShips(Button[,] buttons, Board b)
        {
            for (int y = 0; y < Board.Size; y++)
            {
                for (int x = 0; x < Board.Size; x++)
                {
                    if (b.Cells[x, y] == CellState.Ship)
                    {
                        buttons[x, y].BackColor = Color.LightGreen;
                    }
                }
            }
        }

        private void UpdateCurrentPlayerLabel()
        {
            string playerText = currentPlayer == Player.Player1 ? "Игрок 1" : "Игрок 2";
            lblCurrentPlayer.Text = $"Ход: {playerText}";
        }

        private void ShowInfo(string message)
        {
            lblInfo.Text = message;
        }

        // Обработка клика по полю игрока 1
        // В эту область ДОЛЖЕН стрелять только Игрок 2
        private void Player1BoardClick(object sender, EventArgs e)
        {
            if (gameOver)
            {
                SystemSounds.Beep.Play();
                return;
            }

            if (currentPlayer != Player.Player2)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Сейчас ход Игрока 1 – он стреляет по полю Игрока 2.");
                return;
            }

            var btn = (Button)sender;
            var pt = (Point)btn.Tag;
            ProcessShot(board1, buttonsP1, pt.X, pt.Y, shooter: Player.Player2);
        }

        // Обработка клика по полю игрока 2
        // В эту область ДОЛЖЕН стрелять только Игрок 1
        private void Player2BoardClick(object sender, EventArgs e)
        {
            if (gameOver)
            {
                SystemSounds.Beep.Play();
                return;
            }

            if (currentPlayer != Player.Player1)
            {
                SystemSounds.Beep.Play();
                ShowInfo("Сейчас ход Игрока 2 – он стреляет по полю Игрока 1.");
                return;
            }

            var btn = (Button)sender;
            var pt = (Point)btn.Tag;
            ProcessShot(board2, buttonsP2, pt.X, pt.Y, shooter: Player.Player1);
        }

        // Обработка выстрела по указанному полю
        private void ProcessShot(Board targetBoard, Button[,] targetButtons, int x, int y, Player shooter)
        {
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
                targetButtons[x, y].BackColor = Color.Red;
                ShowInfo("Попадание!");

                if (targetBoard.AllShipsDestroyed)
                {
                    gameOver = true;
                    string winner = shooter == Player.Player1 ? "Игрок 1" : "Игрок 2";
                    ShowInfo($"Все корабли противника уничтожены. Победил {winner}!");
                    MessageBox.Show(this, $"Победил {winner}!", "Игра окончена",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SwitchPlayer();
            }
            else if (state == CellState.Empty)
            {
                targetBoard.Cells[x, y] = CellState.Miss;
                targetButtons[x, y].BackColor = Color.LightGray;
                ShowInfo("Мимо.");
                SwitchPlayer();
            }
        }

        private void SwitchPlayer()
        {
            currentPlayer = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
            UpdateCurrentPlayerLabel();
        }
    }
}
