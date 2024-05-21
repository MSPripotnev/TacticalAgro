﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TacticalAgro.Map.Stations {
	internal class AntiIceStation : Station {
		public AntiIceStation() : base() {
			Color = Colors.CadetBlue;
		}
		public AntiIceStation(System.Windows.Point pos) : base(pos) { }
	}
}