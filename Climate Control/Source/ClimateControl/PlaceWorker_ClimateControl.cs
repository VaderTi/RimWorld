using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ClimateControl
{
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once InconsistentNaming
    internal class PlaceWorker_ClimateControl : PlaceWorker
    {
        private Building_ClimateControl _climateControl;

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            foreach (
                var unit in
                    Find.ListerBuildings.AllBuildingsColonistOfClass<Building_ClimateControl>()
                        .Where(unit => unit.Position == center))
            {
                _climateControl = unit;
                break;
            }
            var room = RoomQuery.RoomAt(center);
            if (room == null || room.UsesOutdoorTemperature) return;

            if (_climateControl == null) return;
            var status = _climateControl.WorkStatus;

            var color = Color.white;
            switch (status)
            {
                case Status.Waiting:
                    color = new Color(1f, 0.7f, 0.0f, 0.5f);
                    break;
                case Status.Heating:
                    color = GenTemperature.ColorRoomHot;
                    break;
                case Status.Cooling:
                    color = GenTemperature.ColorRoomCold;
                    break;
                case Status.Outdoor:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GenDraw.DrawFieldEdges(room.Cells.ToList(), color);
        }
    }
}