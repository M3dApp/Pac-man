using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;

namespace Pacman
{
    public partial class Main : Form
    {
        public static readonly Random Rnd = new Random();
        string File_Dir;

        Bitmap Grid;
        readonly Pen Wall = new Pen(Color.Blue, 20);
        readonly Pen Dot = new Pen(Color.Orange, 2);
        readonly Pen Big_Dot = new Pen(Color.Honeydew, 4);
        int[] Offset = { (584 - (X_Size * 20)) / 2, (612 - (Y_Size * 20)) / 2 };
        char[,] Map;

        char Direction = 'R';
        char Holding_Key = 'R';
        char Reset_Key = ' ';

        string[] Bot_Progression = { "R", "F", "RR", "FR", "RRR", "FRR", "FMR", "RRRR", "FRRR" , "FMRR" , "FMSR", "FMSS" };
        int Current_Score = 0;
        int Total_Dots = 0;
        int Collected_Dots = 0;
        int Round = 1;

        Player Plr;
        Ghost[] Ghosts = new Ghost[4];

        System.Windows.Forms.Timer Frame;
        float TICK = 0; // ms
        public const int X_Size = 17;
        public const int Y_Size = 23;

        const int Round_Time = 180; // Seconds
        const int Countdown = 3; // Seconds
        const int FPS = 10; // Frames per Second
        const int Show_Top = 12;
        const int Default_Lives = 3;

        public Main()
        {
            InitializeComponent();
            File_Dir = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath) + "\\Leaderboard";
            if (!File.Exists(File_Dir))
            {
                File.Create(File_Dir).Close();
                File.WriteAllLines(File_Dir, new string[Show_Top]);
            }
            Next_Screen(Name_Menu, false);
        }

        private void On_Frame(object sender, EventArgs e)
        {
            TICK += (float) 1000 / FPS;
            if ((TICK - Countdown * 1000) >= Round_Time * 1000)
            {
                Plr.TimesUp = true;
            }
            if (Time_Text.Text != "FINISH" && !Plr.TimesUp && TICK > Countdown * 1000 && Collected_Dots < Total_Dots && !Plr.Hit)
            {
                float Remaining_Time = Round_Time - (TICK - Countdown * 1000) / 1000;
                if (Remaining_Time < 10)
                    Time_Text.Text = "Time: " + Convert.ToString(Remaining_Time);
                else
                    Time_Text.Text = "Time: " + Convert.ToString((int)Remaining_Time);
                int[] XY = Get_XY(Direction);
                if (Plr.AnimationState && Holding_Key != Direction)
                {
                    XY = Get_XY(Holding_Key);
                    if (Map[Plr.Location[0] + XY[0], Plr.Location[1] + XY[1]] != 'X' && Map[Plr.Location[0] + XY[0], Plr.Location[1] + XY[1]] != 'B')
                        Direction = Holding_Key;
                    else
                        XY = Get_XY(Direction);
                }
                if (Map[Plr.Location[0] + XY[0], Plr.Location[1] + XY[1]] != 'X' && Map[Plr.Location[0] + XY[0], Plr.Location[1] + XY[1]] != 'B')
                {
                    if (Plr.AnimationState)
                        Plr.Sprite.Location = new Point((Plr.Location[0] * 20) + (XY[0] * 10) + Offset[0], (Plr.Location[1] * 20) + (XY[1] * 10) + Offset[1]);
                    else
                    {
                        Plr.SetLocation(Plr.Location[0] + XY[0], Plr.Location[1] + XY[1], Offset, Grid);
                        switch (Map[Plr.Location[0], Plr.Location[1]])
                        {
                            case 'o':
                                Plr.PowerUp();
                                Collect_Dot(5);
                                break;
                            case '.':
                                Collect_Dot(1);
                                break;
                            default:
                                break;
                        }
                    }
                    Plr.Next_Animation(Direction);
                }
                for (int i = 0; i < Ghosts.Length; i++)
                {
                    if (Ghosts[i].Sprite.Visible && Ghosts[i].Move(Map, Offset, Plr))
                    {
                        Plr.Hit = true;
                    }
                    if (Ghosts[i].Points > 0)
                    {
                        Add_Score(Ghosts[i].Points);
                        Ghosts[i].Points = 0;
                    }
                }
                if (Reset_Key == Holding_Key)
                {
                    Reset_Key = ' ';
                    Holding_Key = Direction;
                }
                else
                    Reset_Key = ' ';
            }
            else if (Plr.Hit || Plr.TimesUp)
            {
                if (Time_Text.Text != "" && Time_Text.Text != "GAME OVER")
                {
                    Plr.Lives -= 1;
                    if (Plr.Lives == 0)
                        Time_Text.Text = "GAME OVER";
                    else
                        Time_Text.Text = "";
                    TICK = 0;
                    Plr.Sprite.Visible = false;
                    Display_Lives();
                }
                if (TICK > 3000)
                {
                    if (Plr.Lives > 0)
                    {
                        Frame.Dispose();
                        Start_Game(false);
                    }
                    else
                    {
                        Frame.Dispose();
                        Add_Entry();
                        Display_Leaderboard();
                        Round = 1;
                        Current_Score = 0;
                        Next_Screen(Leaderboard_Menu, true);
                    }
                }
            }
            else if (Time_Text.Text != "FINISH" && TICK <= Countdown * 1000)
            {
                if ((int)TICK / 1000 != Countdown)
                    Time_Text.Text = Convert.ToString(Countdown - (int)TICK / 1000);
                else
                    Time_Text.Text = "GO!";
            }
            else
            {
                if (Time_Text.Text != "FINISH")
                {
                    Time_Text.Text = "FINISH";
                    TICK = 0;
                }
                if (TICK > 2000)
                {
                    Frame.Dispose();
                    Plr.Sprite.Visible = false;
                    if (Plr.Lives < Default_Lives)
                        Plr.Lives += 1;
                    Round++;
                    Start_Game(true);
                }
            }
        }

        private void Start_Game(bool NewMap)
        {
            if (Frame != null)
                Frame.Dispose();
            TICK = 0;
            Frame = new System.Windows.Forms.Timer
            {
                Interval = 1000 / FPS
            };
            Frame.Tick += new EventHandler(On_Frame);
            Reset_Key = ' ';
            Direction = 'R';
            Holding_Key = 'R';
            Time_Text.Text = Convert.ToString(Countdown);
            Round_Text.Text = " LVL " + Convert.ToString(Round);
            Add_Score(0); // Update score display
            if (Map == null)
                Map = new char[X_Size, Y_Size];
            int Index = Round;
            if (Index > Bot_Progression.Length)
                Index = Bot_Progression.Length;
            int Total_Ghosts = Bot_Progression[Index - 1].Length;
            Ghosts[0] = new Ghost(Red_Ghost, Bot_Progression[Index - 1][0], X_Size / 2, Y_Size / 2 - 1, Offset);
            Ghosts[1] = new Ghost(Blue_Ghost, Bot_Progression[Index - 1][1 % Total_Ghosts], X_Size / 2 - 1, Y_Size / 2, Offset);
            Ghosts[2] = new Ghost(Orange_Ghost, Bot_Progression[Index - 1][2 % Total_Ghosts], X_Size / 2 + 1, Y_Size / 2, Offset);
            Ghosts[3] = new Ghost(Pink_Ghost, Bot_Progression[Index - 1][3 % Total_Ghosts], X_Size / 2, Y_Size / 2, Offset);
            for (int i = 0; i < Total_Ghosts; i++)
            {
                Ghosts[i].Sprite.Visible = true;
            }
            if (Plr != null)
            {
                if (Plr.Lives == 0)
                    Plr.Lives = Default_Lives;
                Plr = new Player(Pacman_Sprite, Plr.Lives);
            }
            else
                Plr = new Player(Pacman_Sprite, Default_Lives);
            Display_Lives();

            // Spawn Player
            Plr.SetLocation(1, 1, Offset);
            Map[Plr.Location[0], Plr.Location[1]] = ' ';
            Plr.Sprite.Visible = true;

            // Map Generation
            if (NewMap)
            {
                // Blank map with borders
                Map = new char[X_Size, Y_Size];
                for (int y = 0; y < Y_Size; y++)
                {
                    for (int x = 0; x < X_Size; x++)
                    {
                        if (x == 0 || x == X_Size - 1 || y == 0 || y == Y_Size - 1)
                            Map[x, y] = 'X';
                        else
                        {
                            Map[x, y] = '?';
                        }
                    }
                }
                // Centre Box
                Map[X_Size / 2, Y_Size / 2] = ' ';
                Map[X_Size / 2, Y_Size / 2 - 1] = 'B';
                Map[X_Size / 2, Y_Size / 2 + 1] = 'X';
                Map[X_Size / 2 + 1, Y_Size / 2] = ' ';
                Map[X_Size / 2 + 2, Y_Size / 2] = 'X';
                Map[X_Size / 2 + 2, Y_Size / 2 + 1] = 'X';
                Map[X_Size / 2 + 1, Y_Size / 2 + 1] = 'X';
                Map[X_Size / 2 + 2, Y_Size / 2 - 1] = 'X';
                Map[X_Size / 2 + 1, Y_Size / 2 - 1] = 'X';
                Map[X_Size / 2 - 1, Y_Size / 2] = ' ';
                Map[X_Size / 2 - 2, Y_Size / 2] = 'X';
                Map[X_Size / 2 - 2, Y_Size / 2 + 1] = 'X';
                Map[X_Size / 2 - 1, Y_Size / 2 + 1] = 'X';
                Map[X_Size / 2 - 2, Y_Size / 2 - 1] = 'X';
                Map[X_Size / 2 - 1, Y_Size / 2 - 1] = 'X';
                // Create first column
                int Remaining = Y_Size - 4;
                for (int y = 2; y < Y_Size - 2; y++)
                {
                    int Blocks = Rnd.Next(1, 4);
                    if (Remaining - Blocks - 1 < 1)
                        Blocks = Remaining;
                    for (int i = 0; i < Blocks; i++)
                        Map[2, y + i] = 'X';
                    Remaining -= Blocks + 1;
                    y += Blocks;
                }
                // Extend and place more walls
                for (int x = 3; x <= X_Size / 2; x++)
                {
                    for (int y = 2; y < Y_Size - 2; y++)
                    {
                        if (x < X_Size / 2 - 3 || x > X_Size / 2 + 3 || y < Y_Size / 2 - 2 || y > Y_Size / 2 + 2)
                        {
                            int Existing_Blocks = 1;
                            for (int Dir = -1; Dir < 2; Dir += 2)
                            {
                                int Offset = Dir;
                                while (Map[x, y + Offset] == 'X')
                                {
                                    Existing_Blocks++;
                                    Offset += Dir;
                                }
                            }
                            if ((Map[x - 1, y] != 'X' && Map[x - 1, y - 1] != 'X' && Map[x - 1, y + 1] != 'X' && Existing_Blocks < 4) || (Map[x - 1, y] == 'X' && Existing_Blocks == 1 && Rnd.Next(0, 2) == 1))
                            {
                                Existing_Blocks = 1;
                                for (int Dir = -1; Dir < 2; Dir += 2)
                                {
                                    int Offset = Dir;
                                    while (Map[x + Offset, y] == 'X')
                                    {
                                        Existing_Blocks++;
                                        Offset += Dir;
                                    }
                                }
                                if (Existing_Blocks < 4)
                                {
                                    Map[x, y] = 'X';
                                }
                            }
                        }
                    }
                }
                // Connect the single walls together
                for (int y = 2; y < Y_Size - 2; y++)
                {
                    for (int x = 2; x < X_Size - 2; x++)
                    {
                        if (Map[x, y] == 'X' && Check_Space(x, y) == 8)
                        {
                            for (int i = -1; i < 2; i += 2)
                            {
                                if (Map[x + i * 2, y] == 'X' && Check_Space(x + i * 2, y) == 8 && x + i <= X_Size / 2 || (Map[x + i, y] == '?' && Check_Space(x + i, y) == 7))
                                    Map[x + i, y] = 'X';
                            }
                            for (int i = -1; i < 2; i += 2)
                            {
                                if (Map[x, y + i * 2] == 'X' && Check_Space(x, y + i * 2) == 8 || (Map[x, y + i] == '?' && Check_Space(x, y + i) == 7))
                                    Map[x, y + i] = 'X';
                            }
                        }
                    }
                }
                // Mirror left side to right side
                for (int x = 2; x < X_Size / 2; x++)
                {
                    for (int y = 2; y < Y_Size - 2; y++)
                    {
                        Map[X_Size - x - 1, y] = Map[x, y];
                    }
                }
                // Fixing irregularities
                for (int y = 2; y < Y_Size - 2; y++)
                {
                    for (int x = 2; x < X_Size - 2; x++)
                    {
                        if (Map[x, y] == '?' && Check_Space(x, y) <= 1)
                        {
                            if (Rnd.Next(0, 2) == 1)
                            {
                                Map[x, y - 1] = '?';
                                Map[x, y + 1] = '?';
                            }
                            else
                            {
                                Map[x - 1, y] = '?';
                                Map[x + 1, y] = '?';
                            }
                        }
                    }
                }
                // Replace empty space with dots
                Map[1, 1] = ' ';
                Total_Dots = 0;
                Collected_Dots = 0;
                for (int y = 0; y < Y_Size; y++)
                {
                    for (int x = 0; x < X_Size; x++)
                    {
                        if (Map[x, y] == '?')
                        {
                            Map[x, y] = '.';
                            Total_Dots++;
                        }
                    }
                }
                Map[X_Size / 2, Y_Size / 2 + 2] = 'o';
                Map[X_Size / 2, 1] = 'o';
                Map[1, Y_Size / 2] = 'o';
                Map[X_Size - 2, Y_Size / 2] = 'o';
                Map[X_Size / 2, Y_Size - 2] = 'o';
            }
            // Drawing
            Display_Game();
            // Start tick
            Frame.Start();
        }

        private int[] Get_XY(char Dir)
        {
            int[] XY = { 0, 0 };
            switch (Dir)
            {
                case 'U':
                    XY[1] = -1;
                    break;
                case 'D':
                    XY[1] = 1;
                    break;
                case 'L':
                    XY[0] = -1;
                    break;
                case 'R':
                    XY[0] = 1;
                    break;
                default:
                    break;
            }
            return XY;
        }

        private void Add_Entry()
        {
            string[] Prev_Scores = File.ReadAllLines(File_Dir);
            string[] Updated_LB = new string[Show_Top];
            int Index = 0;
            bool Added = false;
            for (int i = 0; i < Prev_Scores.Length && i < Updated_LB.Length && Index < Updated_LB.Length; i++)
            {
                string[] Data = Prev_Scores[i].Split(' ');
                int SCORE = 0;
                try { SCORE = Convert.ToInt32(Data[1]); }
                catch { }
                if (!Added && Current_Score > SCORE)
                {
                    Added = true;
                    Updated_LB[Index] = Name_Display.Text + " " + Convert.ToString(Current_Score);
                    Index++;
                }
                if (Index < Updated_LB.Length)
                {
                    Updated_LB[Index] = Prev_Scores[i];
                    Index++;
                }
            }
            if (!Added && Index < Updated_LB.Length)
            {
                Updated_LB[Index] = Name_Display.Text + " " + Convert.ToString(Current_Score);
            }
            File.WriteAllLines(File_Dir, Updated_LB);
        }

        private void Display_Leaderboard()
        {
            string[] LB = File.ReadAllLines(File_Dir);
            Leaderboard_Text.Text = "";
            for (int i = 0; i < LB.Length && i < Show_Top; i++)
            {
                if (i < 9)
                    Leaderboard_Text.Text = Leaderboard_Text.Text + "0" + Convert.ToString(i + 1) + " | " + LB[i] + "\n";
                else
                    Leaderboard_Text.Text = Leaderboard_Text.Text + Convert.ToString(i + 1) + " | " + LB[i] + "\n";
            }
        }

        private void Display_Lives()
        {
            Lives_Text.Text = "";
            for (int i = 0; i < Plr.Lives; i++)
                Lives_Text.Text += "❤";
        }

        private void Add_Score(int Amount)
        {
            Current_Score += Amount;
            Score_Text.Text = "Score: " + Convert.ToString(Current_Score);
        }

        private void Collect_Dot(int Amount)
        {
            Map[Plr.Location[0], Plr.Location[1]] = ' ';
            Collected_Dots++;
            Add_Score(Amount);
        }

        private void Next_Screen(Panel Screen, bool Back_Button)
        {
            Game.Visible = false;
            Name_Menu.Visible = false;
            this.Back_Button.Visible = false;
            Main_Menu.Visible = false;
            Help_Menu.Visible = false;
            Leaderboard_Menu.Visible = false;
            Screen.Visible = true;
            this.Back_Button.Visible = Back_Button;
            this.Back_Button.BringToFront();
        }

        private void Display_Game()
        {
            if (Grid != null)
                Grid.Dispose();
            Grid = new Bitmap(X_Size * 20, Y_Size * 20);
            Grid.MakeTransparent(Color.Black);
            using (Graphics G = Graphics.FromImage(Grid))
            {
                for (int y = 0; y < Y_Size; y++)
                {
                    for (int x = 0; x < X_Size; x++)
                    {
                        if (Map[x, y] == 'X')
                            G.DrawLine(Wall, x * 20, y * 20 + 10, (x + 1) * 20, y * 20 + 10);
                        else if (Map[x, y] == '.')
                            G.DrawLine(Dot, x * 20 + 9, y * 20 + 10, (x + 1) * 20 - 9, y * 20 + 10);
                        else if (Map[x, y] == 'o')
                            G.DrawLine(Big_Dot, x * 20 + 8, y * 20 + 10, (x + 1) * 20 - 8, y * 20 + 10);
                    }
                }
            }
            Game.Invalidate();
        }

        private int Check_Space(int X, int Y)
        {
            int Count = 0;
            if (X > 0 && X < X_Size - 1 && Y > 0 && Y < Y_Size - 1)
            {
                if (Map[X + 1, Y] != 'X')
                    Count++;
                if (Map[X - 1, Y] != 'X')
                    Count++;
                if (Map[X, Y + 1] != 'X')
                    Count++;
                if (Map[X, Y - 1] != 'X')
                    Count++;
                if (Map[X + 1, Y + 1] != 'X')
                    Count++;
                if (Map[X + 1, Y - 1] != 'X')
                    Count++;
                if (Map[X - 1, Y + 1] != 'X')
                    Count++;
                if (Map[X - 1, Y - 1] != 'X')
                    Count++;
            }
            return Count;
        }

        private void Start_Click(object sender, EventArgs e)
        {
            Next_Screen(Game, false);
            Start_Game(true);
        }

        private void Help_Click(object sender, EventArgs e)
        {
            Next_Screen(Help_Menu, true);
        }

        private void Leaderboard_Click(object sender, EventArgs e)
        {
            Display_Leaderboard();
            Next_Screen(Leaderboard_Menu, true);
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Back_Click(object sender, EventArgs e)
        {
            Next_Screen(Main_Menu, false);
        }

        private void Name_Done_Click(object sender, EventArgs e)
        {
            bool Pass = true;
            Name_TxtBox.Text = Name_TxtBox.Text.ToUpper();
            for (int i = 0; i < Name_TxtBox.Text.Length; i++)
                if (Name_TxtBox.Text[i] < 65 || Name_TxtBox.Text[i] > 90)
                    Pass = false;
            if (Pass && Name_TxtBox.Text.Length > 0)
            {
                Name_Display.Text = Name_TxtBox.Text;
                Next_Screen(Main_Menu, false);
            }
            else
                Name_TxtBox.Text = "Invalid name.";
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    Holding_Key = 'U';
                    break;
                case Keys.Down:
                case Keys.S:
                    Holding_Key = 'D';
                    break;
                case Keys.Left:
                case Keys.A:
                    Holding_Key = 'L';
                    break;
                case Keys.Right:
                case Keys.D:
                    Holding_Key = 'R';
                    break;
                default:
                    break;
            }
        }

        private void Main_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    if (Holding_Key == 'U')
                        Reset_Key = Holding_Key;
                    break;
                case Keys.Down:
                case Keys.S:
                    if (Holding_Key == 'D')
                        Reset_Key = Holding_Key;
                    break;
                case Keys.Left:
                case Keys.A:
                    if (Holding_Key == 'L')
                        Reset_Key = Holding_Key;
                    break;
                case Keys.Right:
                case Keys.D:
                    if (Holding_Key == 'R')
                        Reset_Key = Holding_Key;
                    break;
                default:
                    break;
            }
        }

        private void On_Paint(object sender, PaintEventArgs e)
        {
            if (Grid != null)
            {
                e.Graphics.DrawImage(Grid, Offset[0], Offset[1], X_Size * 20, Y_Size * 20);
            }
        }
    }

    class Entity
    {
        public Label Sprite;
        public int[] Location = {1, 1};

        public Entity(Label Sprite)
        {
            this.Sprite = Sprite;
        }

        public void SetLocation(int X, int Y, int[] Offset)
        {
            Location[0] = X;
            Location[1] = Y;
            Sprite.Location = new Point(X * 20 + Offset[0], Y * 20 + Offset[1]);
        }
    }

    class Player : Entity
    {
        public bool AnimationState = true; // true = Mouth Open | false = Mouth Close
        public int Lives = 3;
        public bool Hit = false;
        public bool TimesUp = false;

        private bool PowerUp_Again = false;
        private bool Power_Up = false;

        public Player(Label Sprite, int Lives) : base(Sprite)
        {
            this.Lives = Lives;
            Sprite.Image = Properties.Resources.Pacman_R;
        }

        public void PowerUp()
        {
            if (Power_Up)
                PowerUp_Again = true;
            Power_Up = true;
            Task.Run(() => {
                Thread.Sleep(10000);
                if (PowerUp_Again)
                    PowerUp_Again = false;
                else
                    Power_Up = false;
            });
        }

        public bool IsPowerUp()
        {
            return Power_Up;
        }

        public void Next_Animation(char Direction)
        {
            if (AnimationState)
                Sprite.Image = (Bitmap) Properties.Resources.ResourceManager.GetObject("Pacman_" + Direction);
            else
                Sprite.Image = Properties.Resources.Pacman_Close;
            AnimationState = !AnimationState;
        }

        public void SetLocation(int X, int Y, int[] Offset, Bitmap Grid)
        {
            SetLocation(X, Y, Offset);
            Pen BG = new Pen(Color.Black, 20);
            using (Graphics G = Graphics.FromImage(Grid))
            {
                G.DrawLine(BG, X * 20, Y * 20 + 10, (X + 1) * 20, Y * 20 + 10);
            }
        }
    }

    class Ghost : Entity
    {
        public bool Eaten = false;
        public bool InBetween = false;
        public Queue<int[]> Path;
        public char PathMode;
        public int Points = 0;

        private int[] Spawn;
        private Image NormalImage;
        private bool CanMove = true;

        public Ghost(Label Sprite, char PathMode, int X, int Y, int[] Offset) : base(Sprite)
        {
            SetLocation(X, Y, Offset);
            Spawn = new int[] { X, Y };
            NormalImage = (Bitmap) Properties.Resources.ResourceManager.GetObject(Sprite.Name);
            this.PathMode = PathMode;
            this.Sprite.Visible = false;
            Sprite.Image = NormalImage;
        }

        private void Pathfind(char[,] Map, int[] Start, int[] Goal)
        {
            // Breadth-First Search
            Queue<int[]> QUEUE = new Queue<int[]>();
            Hashtable Visited = new Hashtable();
            QUEUE.Enqueue(new int[] { Start[0], Start[1], -1, -1 });
            while (QUEUE.Count > 0)
            {
                int[] Current = QUEUE.Dequeue();
                if (!Visited.Contains(Current[0] + "," + Current[1]))
                {
                    Visited[Current[0] + "," + Current[1]] = new int[] { Current[2], Current[3] };
                    if (Map[Current[0], Current[1]] != 'X')
                    {
                        if (Current[0] == Goal[0] && Current[1] == Goal[1])
                        {
                            break;
                        }
                        QUEUE.Enqueue(new int[] { Current[0] + 1, Current[1], Current[0], Current[1] });
                        QUEUE.Enqueue(new int[] { Current[0] - 1, Current[1], Current[0], Current[1] });
                        QUEUE.Enqueue(new int[] { Current[0], Current[1] + 1, Current[0], Current[1] });
                        QUEUE.Enqueue(new int[] { Current[0], Current[1] - 1, Current[0], Current[1] });
                    }
                }
            }
            List<int[]> Path = new List<int[]>();
            int[] Now = Goal;
            while (Visited.Contains(Now[0] + "," + Now[1]) && ((int[])Visited[Now[0] + "," + Now[1]])[0] != -1)
            {
                Path.Add((int[])Visited[Now[0] + "," + Now[1]]);
                Now = (int[])Visited[Now[0] + "," + Now[1]];
            }
            Path.Reverse();
            Path.Add(new int[] { Goal[0], Goal[1] });
            this.Path = new Queue<int[]>(Path);
            this.Path.Dequeue();
        }

        private int[] Rnd_Pos(char[,] Map)
        {
            int[] Pos = { Main.Rnd.Next(1, Main.X_Size - 1), Main.Rnd.Next(1, Main.Y_Size - 1) };
            if (Map[Pos[0], Pos[1]] == 'X' || Map[Pos[0], Pos[1]] == 'B')
            {
                if (Map[Pos[0] + 1, Pos[1]] != 'X' && Map[Pos[0] + 1, Pos[1]] != 'B')
                    return new int[] { Pos[0] + 1, Pos[1] };
                else if (Map[Pos[0] - 1, Pos[1]] != 'X' && Map[Pos[0] - 1, Pos[1]] != 'B')
                    return new int[] { Pos[0] - 1, Pos[1] };
                else if (Map[Pos[0], Pos[1] + 1] != 'X' && Map[Pos[0], Pos[1] + 1] != 'B')
                    return new int[] { Pos[0], Pos[1] + 1 };
                else if (Map[Pos[0], Pos[1] - 1] != 'X' && Map[Pos[0], Pos[1] - 1] != 'B')
                    return new int[] { Pos[0], Pos[1] - 1 };
                else
                    return new int[] { Main.X_Size / 2, Main.Y_Size / 2 - 2 };
            }
            else
                return Pos;
        }

        public bool Move(char[,] Map, int[] Offset, Player Plr)
        {
            if (CanMove)
            {
                if (!InBetween)
                {
                    if (Path != null && Path.Count > 0)
                    {
                        InBetween = true;
                        int[] Next = Path.Dequeue();
                        int[] Change = { Next[0] - Location[0], Next[1] - Location[1] };
                        Sprite.Location = new Point(Location[0] * 20 + Offset[0] + (10 * Change[0]), Location[1] * 20 + Offset[1] + (10 * Change[1]));
                        Location = Next;
                    }
                    else
                    {
                        int[] Goal;
                        switch (PathMode)
                        {
                            case 'M':
                                Goal = new int[] { Map.GetLength(0) - Plr.Location[0] - 1, Plr.Location[1] }; // Mirror
                                break;
                            case 'F':
                                Goal = Plr.Location; // Follow
                                break;
                            case 'S':
                                if (Main.Rnd.Next(0, 4) == 1) // Smart Random
                                    Goal = Plr.Location;
                                else
                                    Goal = Rnd_Pos(Map);
                                break;
                            default:
                                Goal = Rnd_Pos(Map); // Random
                                break;
                        }
                        Task.Run(() => { Pathfind(Map, Location, Goal); });
                    }
                }
                else
                {
                    InBetween = false;
                    Sprite.Location = new Point(Location[0] * 20 + Offset[0], Location[1] * 20 + Offset[1]);
                    if (Eaten && Path.Count == 0)
                    {
                        CanMove = false;
                        Task.Run(() => {
                            Thread.Sleep(10000);
                            if (!Plr.IsPowerUp())
                                Sprite.Image = NormalImage;
                            else
                                Sprite.Image = Properties.Resources.Scared_Ghost;
                            Eaten = false;
                            CanMove = true;
                        });
                    }
                }
                if (!Eaten && Plr.IsPowerUp())
                    Sprite.Image = Properties.Resources.Scared_Ghost;
                else if (!Eaten)
                    Sprite.Image = NormalImage;
                if (!Eaten && Sprite.Visible && Math.Abs(Sprite.Location.X - Plr.Sprite.Location.X) < 20 && Math.Abs(Sprite.Location.Y - Plr.Sprite.Location.Y) < 20)
                {
                    if (Plr.IsPowerUp())
                    {
                        Points = 10;
                        Eaten = true;
                        Sprite.Image = Properties.Resources.Dead_Ghost;
                        Task.Run(() => { Pathfind(Map, Location, Spawn); });
                    }
                    else if (!Eaten)
                        return true;
                }
            }
            return false;
        }
    }
}
