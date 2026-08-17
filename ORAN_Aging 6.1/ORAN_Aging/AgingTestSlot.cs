using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ORAN_Aging
{
    public class AgingTestSlot : IDisposable
    {
        public int ColumnIndex { get; }
        private readonly DataGridView Grid;
        private readonly Stopwatch stopwatch;
        private readonly System.Windows.Forms.Timer uiTimer;
        private bool isRunning;

        public AgingTestSlot(int columnIndex, DataGridView grid) {
            ColumnIndex = columnIndex;
            Grid = grid;
            stopwatch = new Stopwatch();

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000; // Update UI every second
            uiTimer.Tick += (s, e) => UpdateGridTime();
        }

        public void Start() {
            if (isRunning) return;
            stopwatch.Restart();
            isRunning = true;
            uiTimer.Start();
            UpdateGridTime();
        }

        public void Stop() {
            if (!isRunning) return;
            stopwatch.Stop();
            uiTimer.Stop();
            isRunning = false;
        }

        public void Reset() {
            Stop();
            stopwatch.Reset();
            UpdateGridTime();
        }

        private void UpdateGridTime() {
            if (Grid.IsDisposed) return;

            if (Grid.InvokeRequired) {
                if (!Grid.Disposing && !Grid.IsDisposed)
                    Grid.BeginInvoke(new Action(UpdateGridTime));
                return;
            }

            TimeSpan elapsed = stopwatch.Elapsed;
            Grid.Rows[(int)Form1.AgingDataRow.Timer]
                .Cells[ColumnIndex].Value = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        public void Dispose() {
            uiTimer.Stop();
            uiTimer.Dispose();
            stopwatch.Stop();
        }
    }
}
