using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GCRD
{
    public enum AlertStatus
    {
        Normal,
        Warning,
        Intruders
    }

    public class ThreatSensor : Building
    {
        private int _counter;
        private int _numHostiles;
        private CompPowerTrader _powerTrader;
        public AlertStatus Alert;
        private List<SmartLamp> _lamps;
        private List<SmartTurret> _turrets;

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            Alert = AlertStatus.Normal;

            _powerTrader = GetComp<CompPowerTrader>();
            _turrets = new List<SmartTurret>();
            _lamps = new List<SmartLamp>();
        }

        public override void DeSpawn()
        {
            UnregisterAll();
            base.DeSpawn();
        }

        public override void Tick()
        {
            base.Tick();

            ++_counter;
            if (_counter < 60) return;
            _counter = 1;
            UpdateStatus();
        }

        public void RegisterTurret(SmartTurret turret)
        {
            if (turret == null) return;
            turret.Sensor = this;
            _turrets.Add(turret);
        }

        public void RegisterLamp(SmartLamp lamp)
        {
            if (lamp == null) return;
            lamp.Sensor = this;
            _lamps.Add(lamp);
        }

        private void UnregisterAll()
        {
            if (_turrets.Count > 0)
            {
                _turrets.ForEach(t => t.Sensor = null);
            }

            if (_lamps.Count > 0)
            {
                _lamps.ForEach(l => l.Sensor = null);
            }
        }

        private void UpdateStatus()
        {
            if (!_powerTrader.PowerOn)
            {
                Alert = AlertStatus.Warning;
                return;
            }

            _numHostiles = Find.ListerPawns.PawnsHostileToColony.Count();
            Alert = (_numHostiles > 0) ? AlertStatus.Intruders : AlertStatus.Normal;
            Log.Message("Status: " + ((Alert > 0) ? "Invaders" : "Normal"));
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            UnregisterAll();
            base.Destroy(mode);
        }
    }
}