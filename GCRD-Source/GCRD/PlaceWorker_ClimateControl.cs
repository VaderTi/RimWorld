using System.Linq;
using UnityEngine;
using Verse;

namespace GCRD
{
    internal class PlaceWorker_ClimateControl : PlaceWorker
    {
        private ClimateControl _indoorUnit;

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            var climatecontrols = Find.ListerBuildings.AllBuildingsColonistOfClass<ClimateControl>();

            foreach (var unit in climatecontrols)
            {
                if (unit.Position != center) continue;
                _indoorUnit = unit;
                break;
            }
            var room = RoomQuery.RoomAt(center);
            if (room == null || room.UsesOutdoorTemperature) return;

            if (_indoorUnit == null) return;
            var status = _indoorUnit.WorkStatus;

            var color = new Color(1f, 0.7f, 0.0f, 0.5f);
            switch (status)
            {
                case Status.Heating:
                    color = GenTemperature.ColorRoomHot;
                    break;
                case Status.Cooling:
                    color = GenTemperature.ColorRoomCold;
                    break;
            }

            GenDraw.DrawFieldEdges(room.Cells.ToList(), color);
        }
    }
}