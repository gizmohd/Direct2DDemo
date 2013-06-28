using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using System.Threading.Tasks;
using System.Drawing;
using System.Timers;
using System.Diagnostics;

namespace DirectXDisplay
{
    public sealed class DisplayScene : Direct2D.AnimatedScene
    {

        private DWrite.TextFormat textFormat;
        private DWrite.DWriteFactory writeFactory;
        private const int PIXEL_SIZE = 3;
        // These are used for tracking an accurate frames per second
        private DateTime time;
        private int frameCount;
        private int fps;
        private Timer someTimer = new Timer();
        public DisplayScene()
            : base(100) // Will probably only be about 67 fps due to the limitations of the timer
        {
            this.writeFactory = DWrite.DWriteFactory.CreateFactory();

            someTimer.Interval = 75;
            someTimer.Elapsed += someTimer_Elapsed;
            someTimer.Enabled = true;

        }

        void someTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            someTimer.Enabled = false;
            GenerateDemoPoints();
            someTimer.Enabled = true;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.writeFactory.Dispose();

            }
            base.Dispose(disposing);
        }

        protected override void OnCreateResources()
        {
            // We don't need to free any resources because the base class will
            // call OnFreeResources if necessary before calling this method.

            this.textFormat = this.writeFactory.CreateTextFormat("Arial", 10);

            base.OnCreateResources(); // Call this last to start the animation
        }

        protected override void OnFreeResources()
        {
            base.OnFreeResources(); // Call this first to stop the animation

        }

        protected override void OnRender()
        {

            try
            {

                // Calculate our actual frame rate
                this.frameCount++;
                if (DateTime.UtcNow.Subtract(this.time).TotalSeconds >= 1)
                {
                    this.fps = this.frameCount;
                    this.frameCount = 0;
                    this.time = DateTime.UtcNow;
                }
                Stopwatch w = Stopwatch.StartNew();

                this.RenderTarget.BeginDraw();
                this.RenderTarget.Clear(new D2D.ColorF(0, 0, 1, 0.5f));

                lock (Points)
                    Points.ToList().ForEach(p =>
                    {
                        var rect = new D2D.RectF();
                        rect.Top = p.Value.Shape.Top;
                        rect.Bottom = p.Value.Shape.Bottom;
                        rect.Left = p.Value.Shape.Left;
                        rect.Right = p.Value.Shape.Right;
                        rect.Height = p.Value.Shape.Height;
                        rect.Width = p.Value.Shape.Width;
                        using (var brush = this.RenderTarget.CreateSolidColorBrush(p.Value.Color.ToColorF()))
                        {
                            this.RenderTarget.FillRectangle(rect, brush);

                        }

                    });

                w.Stop();
                // Draw a little FPS in the top left corner
                string text = string.Format("FPS {0} Points {1} ElapsedMS: {2}", this.fps, Points.Count(), w.ElapsedMilliseconds);
                
                using (var textBrush = this.RenderTarget.CreateSolidColorBrush(Color.White.ToColorF()))
                {
                    this.RenderTarget.DrawText(text, this.textFormat, new D2D.RectF(10, 10, 100, 20), textBrush);
                }
                
                // All done!
                this.RenderTarget.EndDraw();

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
        }


        public ConcurrentDictionary<float, DisplayPoint> Points { get; set; }

        private void GenerateDemoPoints()
        {
            Random r = new Random((int)((double)DateTime.Now.Millisecond * Math.PI));
            if (Points == null) Points = new ConcurrentDictionary<float, DisplayPoint>();

            var size = this.RenderTarget.Size;
            lock (Points)
            {

                for (int i = 1; i < 300 + 1; i++)
                {
                    for (int j = 1; j < 300 + 1; j++)
                    {
                        //if (r.Next(1, 100) > 50)
                        //{
                        //Parallel.For(1, (long)(size.Height + 1), i => {
                        //Parallel.For(1, (long)(size.Width + 1), j => {
                        DisplayPoint p = new DisplayPoint();

                        p = new DisplayPoint()
                      {
                          Shape = new Rectangle()
                          {
                              Height = PIXEL_SIZE,
                              Width = PIXEL_SIZE,
                              X = (int)i * PIXEL_SIZE,
                              Y = (int)j * PIXEL_SIZE,
                          },
                          Identifier = (float)i + ((float)j / 100),
                          Color = System.Drawing.Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255), r.Next(0, 255))
                      };

                        Points[p.Identifier] = p;

                        //}

                        //});
                        //});
                    }
                }
            }
        }

        public struct DisplayPoint
        {
            public Rectangle Shape { get; set; }
            public System.Drawing.Color Color { get; set; }
            public float Identifier { get; set; }
        }


    }
}
