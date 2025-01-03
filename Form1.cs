using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.Runtime.InteropServices;


namespace 잠수함게임
{
    public partial class Form1 : Form
    {
        //상수 정의
        const int SHIP_COUNT = 10;    //적군 배의 수 
        const int ENEMY_BULLET_COUNT = 40;    //적군 총알의 수
        const int PLAYER_BULLET_COUNT = 10;    //플레이어 총알의 수
        const int ENEMY_BULLET_SPEED = 5;   //적군 총알 속도
        const int PLAYER_BULLET_SPEED = 7;   //플레이어 총알 속도
        const int PLAYER_BULLET_GAP = 40;    //플레이어 총알 발사 간격
        const int SUBMARINE_SPEED = 8;   //잠수함 이동 속도

        // 크기 정의
        const int SUBMARINE_WIDTH = 60; //잠수함 너비
        const int SUBMARINE_HEIGHT = 35; //잠수함 높이
        const int ENEMY_SHIP_WIDTH = 52; //적군 배 너비
        const int ENEMY_SHIP_HEIGHT = 25; //적군 배 높이
        const int BULLET_WIDTH = 6; //총알 너비
        const int BULLET_HEIGHT = 16; //총알 높이

        //구조체 정의

        struct SHIP
        {
            public bool exist;  //존재여부 : true/false
            public int x, y;    //좌표 : x, y
            public int speed;   //이동 속도 
            public int direction;   //이동 방향
        }

        SHIP[] ships = new SHIP[SHIP_COUNT];
        //구조체 배열을 선언하면 배열 크기만큼의 요소가 생성되고, 각 요소는 구조체의 기본값을 가지고 있다.

        struct Bullet
        {
            public bool exist;
            public int x, y;
        }

        Bullet[] enemyBullets = new Bullet[ENEMY_BULLET_COUNT];
        Bullet[] playerBullets = new Bullet[PLAYER_BULLET_COUNT];

        // 잠수함 초기 좌표
        int submarineX = 600;
        int submarineY = 700;

        int score = 0;
        static int record_score = 0;    //최고 점수

        SoundPlayer sndBomb = null; // 폭발음 사운드 플레이어
        Random random = new Random();

        //이미지 리소스
        Bitmap submarineImage, enemyShipImage, playerBulletImage, enemyBulletImage, seaImage;
        Bitmap drawingArea = new Bitmap(1200, 800);   // 그리기 영역


        [DllImport("User32.dll")]
        // 윈도우 OS에서 제공하는 키 상태를 가져오는 외부 라이브러리
        private static extern short GetKeyState(int nVirtKey);


        [DllImport("winmm.dll")]
        // MCI 라이브러리를 사용하여 미디어 제어
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        //폼의 기본 배경 그리기를 비활성화하여 게임 화면의 부드러운 업데이트를 위해 깜빡임을 줄이는 역할
        //기본적으로 OnPaintBackground 메서드는 폼의 배경을 지우고 다시 그리는 작업을 합니다.
        //그런데 이 메서드를 재정의하고 base.OnPaintBackground(e); 호출을 생략하게 되면,
        //배경이 자동으로 지워지지 않으므로 배경 깜빡임이 줄어들게 됩니다.
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 배경 그리기를 비활성화하여 깜빡임을 줄임
            //base.OnPaintBackground(e);

        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 폼 사이즈 설정
            Size = new Size(1200, 800);

            LoadResources();

            //Start Game
            StartGame();

        }
        private void LoadResources()
        {
            seaImage = Properties.Resources.sea; //[솔루션탐색기]-[Properties]-[Resources.resx]-[리소스추가]
            submarineImage = Properties.Resources.jamsuham;
            enemyShipImage = Properties.Resources.ship;
            enemyBulletImage = Properties.Resources.egun;
            playerBulletImage = Properties.Resources.jgun;
        }
        private void StartGame()
        {
            //게임 초기화

            //적군배
            InitializeShips();

            //총알
            InitializeBullets();

            //배경음악 로드 및 재생
            PlayBackgroundMusic();

            score = 0; // 점수 초기화

            timer1.Start(); // 타이머 시작
        }

        private void InitializeShips()
        {
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                ships[i].exist = false;

            }
        }

        private void InitializeBullets()
        {
            for (int i = 0; i < ENEMY_BULLET_COUNT; i++)
            {
                enemyBullets[i].exist = false;
            }

            for (int i = 0; i < PLAYER_BULLET_COUNT; i++)
            {
                playerBullets[i].exist = false;
            }
        }

        private void PlayBackgroundMusic()
        {
            mciSendString("open \"../../resources/bg.mp3\" type mpegvideo alias MediaFile", null, 0, IntPtr.Zero);
            mciSendString("play MediaFile REPEAT", null, 0, IntPtr.Zero);
            //폭발음 초기화
            sndBomb = new SoundPlayer(Properties.Resources.bomb);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 다시시작
            if(e.KeyCode == Keys.Return)
                StartGame();

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if(drawingArea != null)
            {
                e.Graphics.DrawImage(drawingArea, 0, 0);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Rectangle shipRectangle, submarineRectangle, enemyBulletRectangle, playerBulletRectangle, intersectionRectangle;
            int maxY = -1; // 최대 Y 좌표 초기화
            int i, j;

            Graphics g = Graphics.FromImage(drawingArea);
            g.DrawImage(seaImage, 0, 0); // 바다 이미지 그리기
            g.DrawImage(submarineImage, submarineX - SUBMARINE_WIDTH / 2, submarineY); // 잠수함 그리기

            // 잠수함 좌우 이동
            UpdatePlayerPosition();
            

            // 스페이스바 키 입력 처리
            if (GetKeyState((int)Keys.Space) < 0)
            {
                for (i = 0; i < PLAYER_BULLET_COUNT; i++)
                {
                    if (playerBullets[i].exist) 
                    {
                        maxY = Math.Max(playerBullets[i].y, maxY); // 가장 높은 총알 Y 좌표 찾기        
                    }
                }
                for (i = 0; i < PLAYER_BULLET_COUNT; i++)
                {
                    if (!playerBullets[i].exist)
                    {
                        break; // 플레이어 총알이 존재하지 않는 위치 찾기
                    }
                }

                // 총알 발사 조건 확인
                if (i != PLAYER_BULLET_COUNT && submarineY - maxY > PLAYER_BULLET_GAP)
                {
                    playerBullets[i].exist = true;
                    playerBullets[i].x = submarineX;
                    playerBullets[i].y = submarineY - BULLET_HEIGHT;
                }
            }

            // 플레이어 총알 이동 처리
            for (i = 0; i < PLAYER_BULLET_COUNT; i++)
            {
                if (playerBullets[i].exist)
                {
                    // 총알 경계 처리
                    if (playerBullets[i].y > 0)
                    {
                        playerBullets[i].y -= PLAYER_BULLET_SPEED; // 총알 위로 이동
                        g.DrawImage(playerBulletImage, playerBullets[i].x - (BULLET_WIDTH / 2), playerBullets[i].y);
                    }
                    else
                    {
                        playerBullets[i].exist = false; // 총알이 화면을 벗어나면 존재하지 않도록 설정

                    }
                }
            }

            // 적군 배 생성 처리
            if (random.Next(10) == 1)
            {
                for (i = 0; i < SHIP_COUNT; i++)
                {
                    if (!ships[i].exist)
                    {
                        // 적군 배 생성 조건
                        if (random.Next(2) == 1)
                        {
                            ships[i].direction = 1; // 이동 방향
                            ships[i].x = ENEMY_SHIP_WIDTH / 2;

                        }
                        else
                        {
                            ships[i].direction = -1; // 이동 방향
                            ships[i].x = ClientSize.Width - (ENEMY_SHIP_WIDTH / 2);
                        }
                        ships[i].exist = true; // 배 존재 설정
                        ships[i].y = 300; // Y 좌표
                        ships[i].speed = random.Next(5, 10); // 이동 속도
                    }

                }
            }


            for (i = 0; i < SHIP_COUNT; i++)
            {
                if (ships[i].exist)
                {
                    // 적군 배 이동
                    ships[i].x = ships[i].x + ships[i].speed * ships[i].direction;

                    // 적군 배 경계 처리
                    if (ships[i].x < 0 || ships[i].x > ClientSize.Width)
                    {
                        ships[i].exist = false; // 배가 화면을 벗어나면 존재하지 않도록 설정
                    }
                    else
                    {
                        g.DrawImage(enemyShipImage, ships[i].x - (ENEMY_SHIP_WIDTH / 2), ships[i].y);
                    }

                    // 적군 총알 생성 처리
                    if (random.Next(30) == 1)
                    {
                        for (j = 0; j < ENEMY_BULLET_COUNT; j++)
                        {
                            if (!enemyBullets[j].exist)
                            {
                                enemyBullets[j].exist = true; // 총알 존재 설정
                                enemyBullets[j].x = ships[i].x; // 적군 배 X 좌표
                                enemyBullets[j].y = ships[i].y + BULLET_HEIGHT; // 적군 배 Y 좌표
                                break; // 총알 생성 후 종료
                            }
                        }
                    }

                }
            }


            // 적군 총알 이동 처리
            for (i = 0; i < ENEMY_BULLET_COUNT; i++)
            {
                if (enemyBullets[i].exist)
                {

                    // 총알 경계 처리
                    if (enemyBullets[i].y < submarineY)
                    {
                        enemyBullets[i].y = enemyBullets[i].y + ENEMY_BULLET_SPEED; // 총알 아래로 이동
                        enemyBullets[i] = enemyBullets[i]; // 업데이트
                        g.DrawImage(enemyBulletImage, enemyBullets[i].x - 3, enemyBullets[i].y);
                    }
                    else
                    {
                        enemyBullets[i].exist = false; // 총알이 잠수함보다 밑이면 존재하지 않도록 설정
                    }
                }
            }

            Pen pen = new Pen(Color.Red, 1); // 디버깅용

            // 플레이어 총알과 적군 배의 충돌 체크
            for (i = 0; i < PLAYER_BULLET_COUNT; i++)
            {
                if (playerBullets[i].exist)
                {
                    for (j = 0; j < SHIP_COUNT; j++)
                    {
                        if (ships[j].exist)
                        {
                            shipRectangle = new Rectangle(ships[j].x - ENEMY_SHIP_WIDTH / 2, ships[j].y, ENEMY_SHIP_WIDTH, ENEMY_SHIP_HEIGHT);
                            playerBulletRectangle = new Rectangle(playerBullets[i].x - BULLET_WIDTH / 2, playerBullets[i].y, BULLET_WIDTH, BULLET_HEIGHT); // 플레이어 총알 사각형
                            // g.DrawRectangle(pen, shipRectangle);
                            // g.DrawRectangle(pen, playerBulletRectangle);

                            // 충돌 확인
                            if (playerBulletRectangle.IntersectsWith(shipRectangle))
                            {
                                ships[j].exist = false; // 적군 배 파괴
                                playerBullets[i].exist = false; // 플레이어 총알 파괴
                                score += 10; // 점수 증가
                                if (record_score < score)
                                {
                                    record_score = score;
                                }
                                sndBomb.Play(); // 폭발음 재생
                            }
                        }
                    }
                }
            }


            Font myfont = new System.Drawing.Font(new FontFamily("휴먼둥근헤드라인"), 14, FontStyle.Bold);
            g.DrawString("Record : " + score.ToString(), myfont, Brushes.DarkBlue, new PointF(10, 20));
            g.DrawString("New Record : " + record_score.ToString(), myfont, Brushes.DarkBlue, new PointF(500, 20));


            // 적군 총알과 잠수함의 충돌 체크
            for (i = 0; i < ENEMY_BULLET_COUNT; i++)
            {
                if (enemyBullets[i].exist)
                {
                    enemyBulletRectangle = new Rectangle(enemyBullets[i].x - BULLET_WIDTH / 2, enemyBullets[i].y, BULLET_WIDTH, BULLET_HEIGHT); // 적군 총알 사각형
                    submarineRectangle = new Rectangle(submarineX - SUBMARINE_WIDTH / 2, submarineY, SUBMARINE_WIDTH, SUBMARINE_HEIGHT); // 잠수함 사각형
                    // g.DrawRectangle(pen, enemyBulletRectangle);
                    // g.DrawRectangle(pen, submarineRectangle);

                    // 충돌 확인
                    if (enemyBulletRectangle.IntersectsWith(submarineRectangle))
                    {
                        mciSendString("stop MediaFile", null, 0, IntPtr.Zero);
                        enemyBullets[i].exist = false; // 적군 총알 파괴
                        timer1.Stop();
                        g.DrawString("Press Enter to restart the game!!", myfont, Brushes.DarkBlue, new PointF(400, 100));
                        MessageBox.Show("게임 오버"); // 게임 오버 메시지
                    }
                }
            }


           

            Invalidate(); // paint 이벤트를 발생 (update해서 화면에 반영)
        }

        private void UpdatePlayerPosition()
        {
            //키 이벤트 설정
            if (GetKeyState((int)Keys.Left) < 0) 
            {
                submarineX = submarineX - SUBMARINE_SPEED;
                submarineX = Math.Max(SUBMARINE_WIDTH / 2, submarineX); // 왼쪽 벽 경계 설정
            }
            if (GetKeyState((int)Keys.Right) < 0)
            {
                submarineX = submarineX + SUBMARINE_SPEED;
                submarineX = Math.Min(ClientSize.Width - (SUBMARINE_WIDTH / 2), submarineX); // 오른쪽 벽 경계 설정
            }
            //if (GetKeyState((int)Keys.Up) < 0
            //{
            //    submarineY  = submarineY  - SUBMARINE_SPEED;
            //    submarineY  = Math.Max(SUBMARINE_WIDTH / 2, submarineY);
            //}

        }
    }
}
