﻿using System.Drawing;

namespace TacticalAgro {
    public interface IExplorer {
        public AnalyzedPoint Result { get; set; }
        public long Iterations { get; set; }
        public List<AnalyzedPoint> OpenedPoints { get; set; }
        public List<AnalyzedPoint> ClosedPoints { get; set; }
        public event EventHandler<AnalyzedPoint> PathCompleted;
        public event EventHandler PathFailed;

        public void NextStep();
    }
}
