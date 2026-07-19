using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GameOptimizer.Utils
{
    public static class AnimationEngine
    {
        public static void SlideIn(Control newControl, Panel container, int durationMs = 180)
        {
            if (newControl == null || container == null) return;

            EnableDoubleBuffer(container);
            EnableDoubleBuffer(newControl);

            int startX = container.Width;
            int endX = 0;

            newControl.Location = new Point(startX, 0);
            newControl.Size = container.ClientSize;
            newControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            if (!container.Controls.Contains(newControl))
            {
                container.Controls.Add(newControl);
            }
            newControl.BringToFront();

            Timer timer = new Timer { Interval = 15 };
            int steps = Math.Max(1, durationMs / timer.Interval);
            int currentStep = 0;

            timer.Tick += (s, e) =>
            {
                currentStep++;
                if (currentStep >= steps)
                {
                    newControl.Location = new Point(endX, 0);
                    newControl.Dock = DockStyle.Fill;
                    timer.Stop();
                    timer.Dispose();

                    for (int i = container.Controls.Count - 1; i >= 0; i--)
                    {
                        var oldControl = container.Controls[i];
                        if (oldControl != newControl)
                        {
                            container.Controls.Remove(oldControl);
                            oldControl.Dispose();
                        }
                    }
                }
                else
                {
                    float progress = (float)currentStep / steps;
                    float ease = 1f - (float)Math.Pow(1f - progress, 2);
                    int currentX = startX - (int)(startX * ease);
                    newControl.Location = new Point(currentX, 0);
                }
            };

            timer.Start();
        }

        private static void EnableDoubleBuffer(Control control)
        {
            try
            {
                PropertyInfo doubleBufferProperty = typeof(Control).GetProperty("DoubleBuffered", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                doubleBufferProperty?.SetValue(control, true, null);
            }
            catch { }
        }
    }
}