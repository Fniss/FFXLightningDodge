using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using WindowsInput;

namespace ffxLightningDodger
{
    class Program
    {
        static int slowTimer = 20;
        static Process proc = null;
        static int i = 0;
        static DateTime lastflash = DateTime.MinValue;
        static int ax = 0;
        static int ay = 0;
        static int bx = 0;
        static int by = 0;
        //Mouse events
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        static Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        // Imports some functions.
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);
        [DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("User32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        [DllImport("User32.dll")]
        static extern UInt32 SendInput(UInt32 nInputs,
          [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs,
          Int32 cbSize);
        [DllImport("User32.dll")]
        public static extern long SetCursorPos(int x, int y);

        [STAThread]
        static void Main(string[] args)
        {

            bool lastInc = false;
            bool lastdec = false;


            Console.WriteLine("Place yourself somewhere in the thunderplains where you cannot leave the area by doding.");
            Console.WriteLine("This application loses connection with the game after faking a button press. To work around that it switches between this console app and the game.");
            Console.WriteLine("Please do the following setup:");
            Console.WriteLine("Hold mouse over the console window and press F9.");
            Console.WriteLine("Hold mouse halfway between the main character and the top left corner on the game window and press F10.");
            while (ax== 0||ay== 0||bx== 0||by== 0)
            {
                if (Keyboard.IsKeyDown(Key.F9) && !lastInc)
                {
                    Point pos1 = Point.Empty;
                    Console.WriteLine("positions 1 is set");
                    GetCursorPos(ref pos1);
                    ax = pos1.X;
                    ay = pos1.Y;
                    lastInc = true;
                }

                if (Keyboard.IsKeyDown(Key.F10) && !lastdec)
                {
                    Point pos1 = Point.Empty;
                    Console.WriteLine("positions 2 is set");
                    GetCursorPos(ref pos1);
                    bx = pos1.X;
                    by = pos1.Y;
                    lastdec = true;
                }

            }
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += bw_DoWork;

            bw.RunWorkerAsync();

            Console.WriteLine("Press F7 to quit.");
            Console.WriteLine("Press F9 to decrease delay between a lightning bolt and the dodge.");
            Console.WriteLine("Press F10 to increase delay between a lightning bolt and the dodge.");
            lastInc = false;
            lastdec = false;
            getProcess("FFX");
            while (!Keyboard.IsKeyDown(Key.F7))
            {
                if (Keyboard.IsKeyDown(Key.F10) && !lastInc)
                {
                    Console.WriteLine("Increase delay by 5");
                    lastInc = true;
                    slowTimer += 5;
                }
                else if (Keyboard.IsKeyUp(Key.F10))
                    lastInc = false;

                if (Keyboard.IsKeyDown(Key.F9) && !lastdec)
                {
                    Console.WriteLine("Decrease delay by 5");
                    slowTimer -= 5;
                    lastdec = true;
                }
                else if (Keyboard.IsKeyUp(Key.F9))
                    lastdec = false;

            }
        }

        public static void bw_DoWork(object sender, DoWorkEventArgs e)
        { 
            Point mousePoint = new Point();
        
            while (true)
            {

                GetCursorPos(ref mousePoint);
                PollPixel(new Point(15 + mousePoint.X, 15 + mousePoint.Y), Color.DarkGray);
            }
        }

        public static void getProcess(string application)
        {

            proc = Process.GetProcessesByName(application).FirstOrDefault();
            if (proc != null)
            {
                IntPtr h = proc.MainWindowHandle;
                SetForegroundWindow(h);
            }
        }

        /// <summary>
        /// Gets color at position on screen.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Color GetColorAt(Point location)
        {
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        /// <summary>
        /// Checks if pixel has a brighter color than the referenced color.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="color"></param>
        private static void PollPixel(Point location, Color color)
        {
            var c = GetColorAt(location);

            if (c.R >= color.R && c.G >= color.G && c.B >= color.B)
            {
                DodgeLightning();
                return;
                    
            }
        }

        /// <summary>
        /// Sends a input to dodge the lightning, then switches between programs to reset the limitation with automated input.
        /// </summary>
        private static void DodgeLightning()
        {
            Console.WriteLine("Light found "+i + " at " + slowTimer + "delay");
            System.Threading.Thread.Sleep(slowTimer);
            //Randomly set to be N.
            SendScanCode(0x2E);
            i++;
            System.Threading.Thread.Sleep(150);
            DoMouseClick(ax,ay);
            System.Threading.Thread.Sleep(150);
            DoMouseClick(bx,by);
        }

        struct INPUT
        {
            public UInt32 type;
            public ushort wVk;
            public ushort wScan;
            public UInt32 dwFlags;
            public UInt32 time;
            public UIntPtr dwExtraInfo;
            public UInt32 uMsg;
            public ushort wParamL;
            public ushort wParamH;

        }

        enum SendInputFlags
        {
            KeyDown = 0x0000,
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_UNICODE = 0x0004,
            KEYEVENTF_SCANCODE = 0x0008,
        }

        /// <summary>
        /// Sends a scancode to the system.
        /// </summary>
        /// <param name="ScanCode"></param>
        public static void SendScanCode(ushort ScanCode)
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = 1; //INPUT_KEYBOARD
            InputData[0].wScan = (ushort)ScanCode;
            InputData[0].dwFlags = (uint)SendInputFlags.KEYEVENTF_SCANCODE;


            // Sends key flag and scancode flag to the system
            SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));

            System.Threading.Thread.Sleep(25);

            InputData[0].type = 1; //INPUT_KEYBOARD
            InputData[0].wScan = (ushort)ScanCode;
            InputData[0].dwFlags = (uint)(SendInputFlags.KEYEVENTF_KEYUP
                                          | SendInputFlags.KEYEVENTF_UNICODE);

            // Sends key flag and scancode flag to the system
            SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Does a mouse click at the specified position.
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        public static void DoMouseClick(int pX, int pY)
        {
            uint X = (uint)pX;
            uint Y = (uint)pY;
            SetCursorPos(pX, pY);
            mouse_event(MOUSEEVENTF_LEFTDOWN , X, Y, 0, 0);

            System.Threading.Thread.Sleep(20);
            mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }
    }
}
