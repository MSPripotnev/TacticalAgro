﻿using System.ComponentModel;

using TacticalAgro.Drones;
using TacticalAgro.Map;

namespace TacticalAgro {
    public partial class Director {

        #region Distribute
        public void DistributeTask() {
            DistributeTaskForFreeTransporters();
            DistributeTaskForCarryingTransporters();
        }

        //readonly Dictionary<Transporter, Task<Point[]>> trajectoryTasks = new Dictionary<Transporter, Task<Point[]>>();
        private void DistributeTaskForFreeTransporters() {
            var freeTransport = new List<Transporter>(FreeTransporters).ToArray();
            if (freeTransport.Length > 0 && FreeTargets.Length > 0) {
                CalculateTrajectoryForFreeTransporters(freeTransport);
                SelectNearestTransporterWithTrajectoryForTarget();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FreeTargets)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CollectedTargets)));
        }
        private void CalculateTrajectoryForFreeTransporters(Transporter[] freeTransport) {
            //распределение ближайших целей по роботам
            for (int i = 0; i < freeTransport.Length; i++) {
                Transporter transporter = freeTransport[i];
                IPlaceable targetPos;
                targetPos = FreeTargets.Where(p => !transporter.BlockedTargets.Contains(p) && !Transporters.Any(t => t.AttachedObj == p))
                    .MinBy(t => PathFinder.Distance(t.Position, transporter.Position));
                if (targetPos == null)
                    continue;

                LinkTargetToTransporter(transporter, targetPos as Target);

                if (!transporter.Trajectory.Any()) {
                    transporter.AttachedObj = null;
                    transporter.BlockedTargets.Add(targetPos as Target);
                }
            }
        }
        private void SelectNearestTransporterWithTrajectoryForTarget() {
            for (int i = 0; i < FreeTargets.Length && FreeTransporters.Any(); i++) {
                Target t = FreeTargets[i];
                var AttachedTransporters = FreeTransporters.Where(p => p.AttachedObj == t).ToArray();
                if (AttachedTransporters != null && AttachedTransporters.Length > 0) {
                    t.ReservedTransporter = AttachedTransporters.MinBy(p => p.DistanceToTarget);
                    for (int j = 0; j < AttachedTransporters.Length; j++) {
                        if (AttachedTransporters[j] != t.ReservedTransporter) {
                            UnlinkTargetFromTransporter(AttachedTransporters[j]);
                        } else {
                            AttachedTransporters[j].CurrentState = RobotState.Going;
                        }
                    }
                }
            }
        }
        private void DistributeTaskForCarryingTransporters() {
            var CarryingTransporters = Transporters.Where(
                p => p.CurrentState == RobotState.Carrying &&
                Map.Bases.All(b => PathFinder.Distance(b.Position, p.TargetPosition) > p.InteractDistance)
                ).ToList();
            if (CarryingTransporters.Count > 0) {
                for (int i = 0; i < CarryingTransporters.Count; i++) {
                    Transporter transporter = CarryingTransporters[i];
                    Base? nearBase = Map.Bases.MinBy(p => PathFinder.Distance(p.Position, transporter.Position));
                    if ((nearBase.Position - transporter.BackTrajectory[^1]).Length < transporter.InteractDistance/2) {
                        transporter.Trajectory = transporter.BackTrajectory.ToList();
                        if (transporter.Trajectory[^1] != nearBase.Position)
                            transporter.Trajectory[^1] = (nearBase.Position);
                        transporter.BackTrajectory = null;
                        transporter.AttachedObj.ReservedTransporter = transporter;
                    }
                    else if (PathFinder.Distance(transporter.TargetPosition, nearBase.Position) > transporter.InteractDistance) {
                        transporter.TargetPosition = nearBase.Position;
                        transporter.AttachedObj.ReservedTransporter = transporter;
                    } else {
                        transporter.CurrentState = RobotState.Ready;
                    }
                }
            }
        }

        #region Main
        private void LinkTargetToTransporter(Transporter transporter, Target target) {
            if (target == null)
                return;
            transporter.AttachedObj = target;
            transporter.TargetPosition = target.Position;
        }
        private void UnlinkTargetFromTransporter(Transporter transporter) {
            transporter.AttachedObj = null;
            transporter.Trajectory.Clear();
            transporter.CurrentState = RobotState.Ready;
        }
        #endregion

        #endregion
    }
}
