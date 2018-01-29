using System;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace GCRD
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    internal class PlaceWorker_ClimateControl : PlaceWorker
    {
        private Building_ClimateControl _climateControl;

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            foreach (
                var unit in
                Find.VisibleMap.listerBuildings.AllBuildingsColonistOfClass<Building_ClimateControl>()
                    .Where(unit => unit.Position == center))
            {
                _climateControl = unit;
                break;
            }

            var roomGroup = center.GetRoomGroup(Find.VisibleMap);
            if (roomGroup == null || roomGroup.UsesOutdoorTemperature) return;

            if (_climateControl == null) return;
            var status = _climateControl.WorkStatus;

            Color color;
            switch (status)
            {
                case Status.Waiting:
                    color = new Color(1f, 0.7f, 0.0f, 0.5f);
                    break;

                case Status.Heating:
                    color = new Color(1f, 0.0f, 0.0f, 0.3f);
                    break;

                case Status.Cooling:
                    color = new Color(0.0f, 0.0f, 1f, 0.3f);
                    break;

                case Status.Outdoor:
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            GenDraw.DrawFieldEdges(roomGroup.Cells.ToList(), color);
        }
    }
}