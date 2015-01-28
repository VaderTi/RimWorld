using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace GCRD
{
    public class SmartTurret : Building_TurretGun
    {
        private const float PowerConsumptionPerTile = 13.51f;
        private float _basePowerConsumption;
        private int _counter;
        private bool _isConnectedToThreatSensor;
        private bool _isThreatSensorLoaded;
        private float _range;
        private string _txtPowerConnectedRateStored = "Мощность сети/аккумулировано: {0} Вт / {1} Вт*дней";
        private string _txtPowerNeeded = "Потребляемая мощность";
        private string _txtPowerNotConnected = "Не подключено к сети.";
        private string _txtThreatSensor = "Детектор угроз";
        private string _txtTsConnected = "Соеденен";
        private string _txtTsDisconnected = "Не найден";
        private string _txtWatt = "Вт";
        public ThreatSensor Sensor;

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            _txtWatt = "Watt".Translate();
            _txtPowerNotConnected = "PowerNotConnected".Translate();
            _txtPowerNeeded = "PowerNeeded".Translate();
            _txtPowerConnectedRateStored = "PowerConnectedRateStored".Translate();

            _txtThreatSensor = "ThreatSensor".Translate();
            _txtTsConnected = "TSConnected".Translate();
            _txtTsDisconnected = "TSDisconnected".Translate();

            _basePowerConsumption = powerComp.powerOutput;

            _range = gun.PrimaryVerb.verbProps.range;

            Sensor = null;
            _isConnectedToThreatSensor = false;
            _isThreatSensorLoaded = false;

            var mod = from m in LoadedModManager.LoadedMods where m.name == "GCRD-Threat Sensor" select m;

            if (!mod.Any()) return;

            _isThreatSensorLoaded = true;
            TryConnectToThreatSensor();

            Log.Warning("Display radius: " + def.specialDisplayRadius);
            Log.Warning("Fire radius: " + gun.PrimaryVerb.verbProps.range);
        }

        public override void Tick()
        {
            ++_counter;
            if (_counter == 60)
            {
                if (Sensor != null)
                {
                    _isConnectedToThreatSensor = true;
                    switch (Sensor.Alert)
                    {
                        case AlertStatus.Normal:
                            powerComp.powerOutput = -10.0f;
                            gun.PrimaryVerb.verbProps.range = def.specialDisplayRadius = 10.0f/PowerConsumptionPerTile;

                            break;

                        case AlertStatus.Intruders:
                            powerComp.powerOutput = _basePowerConsumption;
                            gun.PrimaryVerb.verbProps.range = def.specialDisplayRadius = _range;
                            break;
                    }
                }
                else
                {
                    _isConnectedToThreatSensor = false;
                    powerComp.powerOutput = _basePowerConsumption;
                    gun.PrimaryVerb.verbProps.range = def.specialDisplayRadius = _range;
                }
            }


            if (_counter == 120)
            {
                _counter = 1;
                TryConnectToThreatSensor();
            }

            base.Tick();
        }

        private void TryConnectToThreatSensor()
        {
            var sensor = Find.ListerBuildings.AllBuildingsColonistOfClass<ThreatSensor>();
            if (!sensor.Any()) return;

            sensor.FirstOrDefault().RegisterTurret(this);
            _isConnectedToThreatSensor = true;
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            if (powerComp == null) return stringBuilder.ToString();

            if (powerComp.PowerNet.hasPowerSource)
            {
                var pcrs = (int) (powerComp.PowerNet.CurrentEnergyGainRate()/CompPower.WattsToWattDaysPerTick);
                stringBuilder.AppendLine(_txtPowerNeeded + ": " + -powerComp.powerOutput + " " + _txtWatt);
                stringBuilder.AppendLine(string.Format(_txtPowerConnectedRateStored, pcrs,
                    powerComp.PowerNet.CurrentStoredEnergy().ToString("F0")));
                if (_isThreatSensorLoaded)
                {
                    stringBuilder.AppendLine(_txtThreatSensor + ": " +
                                             (_isConnectedToThreatSensor ? _txtTsConnected : _txtTsDisconnected));
                }
            }
            else
            {
                stringBuilder.AppendLine(_txtPowerNotConnected);
            }

            return stringBuilder.ToString();
        }
    }
}