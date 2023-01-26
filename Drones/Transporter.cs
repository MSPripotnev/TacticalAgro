﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using TacticalAgro.Map;

namespace TacticalAgro.Drones {
    public enum RobotState {
        Disable,
        Ready,
        Thinking,
        Going,
        Carrying,
        Broken
    }
    public class Transporter : IPlaceable, IDrone {

        #region Properties

        #region Map
        private Point position;
        public Point Position {
            get => position;
            set {
                position = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Position)));
            }
        }
        [XmlIgnore]
        public Color Color { get; set; } = Colors.Red;
        [XmlIgnore]
        public float Speed { get; set; }
        public UIElement Build() {
            Ellipse el = new Ellipse();
            el.Width = 20;
            el.Height = 20;
            el.Fill = new SolidColorBrush(Color);
            el.Stroke = Brushes.Black;
            el.StrokeThickness = 2;
            el.Margin = new Thickness(-10, -10, 0, 0);
            System.Windows.Controls.Panel.SetZIndex(el, 3);

            Binding binding = new Binding(nameof(Position) + ".X");
            binding.Source = this;
            el.SetBinding(System.Windows.Controls.Canvas.LeftProperty, binding);
            binding = new Binding(nameof(Position) + ".Y");
            binding.Source = this;
            el.SetBinding(System.Windows.Controls.Canvas.TopProperty, binding);

            return el;
        }
        public UIElement BuildTrajectory() {
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(Colors.Gray);
            polyline.StrokeThickness = 2;
            polyline.StrokeDashArray = new DoubleCollection(new double[] { 4.0, 2.0 });
            polyline.StrokeDashCap = PenLineCap.Round;
            polyline.Margin = new Thickness(0, 0, 0, 0);

            Binding b = new Binding(nameof(Trajectory));
            b.Source = this;
            b.Converter = new TrajectoryConverter();
            polyline.SetBinding(Polyline.PointsProperty, b);
            return polyline;
        }
        public UIElement PointsAnalyzed(bool opened) {
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(opened ? Colors.LightGreen : Colors.DarkRed);
            polyline.StrokeThickness = 5.0;
            /*polyline.StrokeDashArray = new DoubleCollection(new double[] { MaxStraightRange });
            polyline.StrokeStartLineCap = PenLineCap.Round;
            polyline.StrokeDashCap = PenLineCap.Round;
            polyline.StrokeEndLineCap = PenLineCap.Round;*/
            polyline.Margin = new Thickness(-5, -5, 0, 0);

            Binding b = new Binding(opened ? nameof(OpenedPoints) : nameof(ClosedPoints));
            b.Source = this;
            b.Converter = new TrajectoryConverter();
            polyline.SetBinding(Polyline.PointsProperty, b);
            return polyline;
        }

        public List<Point> OpenedPoints {
            get {
                var vs = new List<Point>();
                if (Pathfinder.ActiveExplorer == null) return vs;
                for (int i = 0; i < Pathfinder.ActiveExplorer.OpenedPoints.Count; i++)
                    vs.Add(Pathfinder.ActiveExplorer.OpenedPoints[i].Position);
                return vs;
            }
            set {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(OpenedPoints)));
            }
        }
        public List<Point> ClosedPoints {
            get {
                var vs = new List<Point>();
                if (Pathfinder.ActiveExplorer == null) return vs;
                for (int i = 0; i < Pathfinder.ActiveExplorer.ClosedPoints.Count; i++)
                    vs.Add(Pathfinder.ActiveExplorer.ClosedPoints[i].Position);
                return vs;
            }
            set {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ClosedPoints)));
            }
        }
        #endregion

        #region Brain
        [XmlIgnore]
        private RobotState state;
        [XmlIgnore]
        public RobotState CurrentState {
            get {
                return state;
            }
            set {
                switch (value) {
                    case RobotState.Disable:
                        break;
                    case RobotState.Broken:
                        break;
                    case RobotState.Ready:
                        //объект взят
                        if (CurrentState == RobotState.Carrying) {
                            AttachedObj.Finished = true;
                            AttachedObj.ReservedTransporter = null;
                            AttachedObj = null;
                            //робот сломался/выключился
                        } else if (CurrentState == RobotState.Disable || CurrentState == RobotState.Broken) {
                            BlockedTargets.Clear();
                        }
                        break;
                    //нужно рассчитать траекторию
                    case RobotState.Thinking:
                        //инициализация модуля прокладывания пути
                        Pathfinder.SelectExplorer(TargetPosition, Position, CurrentState == RobotState.Ready ? InteractDistance : 5);
                        break;
                    case RobotState.Going:
                        break;
                    case RobotState.Carrying:
                        if (CurrentState == RobotState.Thinking)
                            Trajectory.Add(Trajectory[^1]);
                        break;
                    default:
                        break;
                }
                state = value;
            }
        }

        #region Trajectory
        [XmlIgnore]
        public const int DistanceCalculationTimeout = 20000;
        [XmlIgnore]
        private List<Point> trajectory = new List<Point>();
        [XmlIgnore]
        public PathFinder Pathfinder { get; set; }
        [XmlIgnore]
        public List<Point> Trajectory {
            get { return trajectory; }
            set {
                trajectory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Trajectory)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TraversedWay)));
            }
        }
        [XmlIgnore]
        public Point TargetPosition {
            get {
                return Trajectory.Count > 0 ? Trajectory[^1] : Position; //последняя точка пути
            }
            set {
                Trajectory.Clear();
                Trajectory.Add(value);
                CurrentState = RobotState.Thinking;
            }
        }
        #endregion

        #region Interact
        [XmlIgnore]
        public int InteractDistance { get; init; } = 30;
        [XmlIgnore]
        public int ViewingDistance { get; init; } = 2;
        [XmlIgnore]
        public Target? AttachedObj { get; set; } = null;
        [XmlIgnore]
        public List<Target> BlockedTargets { get; set; } = new List<Target>();
        #endregion

        #region Debug Info
        [XmlIgnore]
        public long ThinkingIterations { get; private set; } = 0;
        [XmlIgnore]
        public long WayIterations { get; private set; } = 0;
        [XmlIgnore]
        public double TraversedWay { get; set; } = 0;
        [XmlIgnore]
        public double DistanceToTarget {
            get {
                if (Trajectory.Count < 1 || AttachedObj == null) return -1;
                if (Trajectory.Count == 1) return PathFinder.Distance(Position, Trajectory[0]);

                double s = PathFinder.Distance(Position, Trajectory[0]);
                for (int i = 0; i < Trajectory.Count - 1; i++)
                    s += PathFinder.Distance(Trajectory[i], Trajectory[i + 1]);
                s += PathFinder.Distance(trajectory[^1], AttachedObj.Position);
                return s;
            }
        }
        #endregion

        #endregion

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        #region Constructors
        public Transporter(Point pos) : this() {
            Position = pos;
        }
        public Transporter(int X, int Y) : this(new Point(X, Y)) { }
        public Transporter() {
            Color = Colors.Red;
            CurrentState = RobotState.Ready;
            AttachedObj = null;
            Speed = 5F;
            InteractDistance = 30;
            BlockedTargets = new List<Target>();
            MaxStraightRange = 2 * Speed;
        }
        #endregion

        #region Func
        public void Simulate() {
            switch (CurrentState) {
                case RobotState.Disable:
                case RobotState.Broken:
                    return;
                case RobotState.Ready:
                    break;
                case RobotState.Thinking:
                    Trajectory = Pathfinder.Result;
                    OpenedPoints = ClosedPoints = null;
                    //ошибка при расчётах
                    if (Pathfinder.Result == null) {
                        CurrentState = RobotState.Broken;
                        //путь найден
                    } else if (Pathfinder.IsCompleted) {
                        Pathfinder.IsCompleted = false;
                        //робот едет к объекту
                        if (AttachedObj != null && AttachedObj.ReservedTransporter != this)
                            CurrentState = RobotState.Going;
                        //робот доставляет объект
                        else if (AttachedObj.ReservedTransporter == this) {
                            CurrentState = RobotState.Carrying;
                        }
                        //переключение на другую задачу
                        else
                            CurrentState = RobotState.Ready;
                        ThinkingIterations += Pathfinder.Iterations;
                    } else
                        Pathfinder.NextStep(); //продолжение расчёта
                    break;
                case RobotState.Going:
                    //есть куда двигаться
                    if (Trajectory.Count > 0) {
                        //двигаемся
                        Move();
                        //дошли до нужной точки
                        if (PathFinder.Distance(Position, TargetPosition) <= InteractDistance)
                            //цель = объект
                            if (AttachedObj != null)
                                //захватываем объект
                                CurrentState = RobotState.Carrying;
                    }
                    break;
                case RobotState.Carrying:
                    if (Trajectory.Count > 0) //есть куда ехать
                        Move(); //ехать
                    if (AttachedObj != null) //если объект захвачен
                        AttachedObj.Position = new Point(Position.X, Position.Y); //переместить захваченный объект)
                    if (!Trajectory.Any()) //доехал
                        CurrentState = RobotState.Ready; //сброс состояния на стандартное
                    break;
                default:
                    break;
            }
        }
        public double MaxStraightRange { get; init; }
        private void Move() {
            Point nextPoint = Trajectory[0];

            if (PathFinder.Distance(Position, nextPoint) < MaxStraightRange) {
                List<Point> pc = new(Trajectory.Skip(1));
                if (pc.Any()) {
                    TraversedWay += PathFinder.Distance(nextPoint, pc[0]);
                    nextPoint = pc[0];
                }
                Trajectory = pc;
            }
            Vector V = nextPoint - Position;
            if (V.Length > 0)
                V.Normalize();
            //новое значение
            Position = new Point(Position.X + V.X * Speed, Position.Y + V.Y * Speed);
            WayIterations++;
        }
        public override string ToString() {
            return Enum.GetName(typeof(RobotState), state) + "_" +
                new Point(Math.Round(position.X, 2), Math.Round(position.Y, 2)).ToString();
        }
        #endregion

    }
}
