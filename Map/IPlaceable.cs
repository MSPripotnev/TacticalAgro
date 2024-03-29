﻿using System.Windows;
using System.Windows.Media;

namespace TacticalAgro.Map {
    public interface IPlaceable : System.ComponentModel.INotifyPropertyChanged {
        public UIElement Build();
        public Point Position { get; set; }
        [System.Xml.Serialization.XmlIgnore]
        public Color Color { get; set; }
    }
}
