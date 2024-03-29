﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;

using TacticalAgro.Drones;
using TacticalAgro.Map;

namespace TacticalAgro {
    public class Target : IPlaceable {
        private Point position;
        [XmlElement(nameof(Point), ElementName = "Position")]
        public Point Position {
            get { return position; }
            set { 
                position = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
            }
        }
        [XmlIgnore]
        public Color Color { get; set; }
        [XmlIgnore]
        public Transporter? ReservedTransporter { get; set; } = null;
        [XmlIgnore]
        public bool Finished { get; set; } = false;
        public Target(Point pos) : this() {
            Position = pos;
        }
        public Target() {
            Finished = false;
            Color = Colors.Green;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public UIElement Build() {
            Ellipse el = new Ellipse();
            el.Width = 15;
            el.Height = 15;
            el.Fill = new SolidColorBrush(Color);
            el.Stroke = Brushes.Black;
            el.StrokeThickness = 1;
            el.Margin = new Thickness(-5, -5, 0, 0);
            System.Windows.Controls.Canvas.SetZIndex(el, 4);

            Binding binding = new Binding(nameof(Position) + ".X");
            binding.Source = this;
            el.SetBinding(System.Windows.Controls.Canvas.LeftProperty, binding);
            binding = new Binding(nameof(Position) + ".Y");
            binding.Source = this;
            el.SetBinding(System.Windows.Controls.Canvas.TopProperty, binding);

            return el;
        }

        public override string ToString() {
            return Position.ToString();
        }
    }
}
