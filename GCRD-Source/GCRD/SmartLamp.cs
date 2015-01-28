using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace GCRD
{
    public class SmartLamp : Building
    {
        private ColorInt _alertNormal;
        private int _counter;
        private int _delayCounter;
        private CompGlower _glower;
        private bool _isAlert;
        private bool _isConnectedToThreatSensor;
        private bool _isDelayCounter;
        private bool _isThreatSensorLoaded;
        private float _maxPowerСonsumption;
        private CompPowerTrader _powerTrader;
        private Room _room;
        private string _txtPowerConnectedRateStored = "Мощность сети/аккумулировано: {0} Вт / {1} Вт*дней";
        private string _txtPowerNeeded = "Потребляемая мощность";
        private string _txtPowerNotConnected = "Не подключено к сети.";
        private string _txtThreatSensor = "Детектор угроз";
        private string _txtTsConnected = "Соеденен";
        private string _txtTsDisconnected = "Не найден";
        private string _txtWatt = "Вт";
        public ThreatSensor Sensor;
        private readonly ColorInt _alertIntruders = new ColorInt(222, 0, 0);
        private readonly ColorInt _alertWarning = new ColorInt(210, 210, 0);

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

            _powerTrader = GetComp<CompPowerTrader>();
            _maxPowerСonsumption = _powerTrader.powerOutput;
            _glower = GetComp<CompGlower>();
            _alertNormal = _glower.props.glowColor;
            _room = RoomQuery.RoomAt(Position);

            Sensor = null;
            _isAlert = false;
            _isConnectedToThreatSensor = false;
            _isThreatSensorLoaded = false;

            var mod = from m in LoadedModManager.LoadedMods where m.name == "GCRD-Threat Sensor" select m;

            if (!mod.Any()) return;

            _isThreatSensorLoaded = true;
            TryConnectToThreatSensor();
        }

        public override void Tick()
        {
            if (_isDelayCounter) ++_delayCounter;
            ++_counter;
            if (_counter == 60)
            {
                if (Sensor != null)
                {
                    _isConnectedToThreatSensor = true;
                    switch (Sensor.Alert)
                    {
                        case AlertStatus.Intruders:
                            _isAlert = true;
                            _glower.props.glowColor = _alertIntruders;
                            break;

                        case AlertStatus.Warning:
                            _isAlert = true;
                            _glower.props.glowColor = _alertWarning;
                            break;

                        case AlertStatus.Normal:
                            _isAlert = false;
                            _glower.props.glowColor = _alertNormal;
                            break;

                        default:
                            _isAlert = false;
                            _glower.props.glowColor = _alertNormal;
                            break;
                    }
                }
                else
                {
                    _isConnectedToThreatSensor = false;
                    _isAlert = false;
                    _glower.props.glowColor = _alertNormal;
                }

                _room = RoomQuery.RoomAt(Position);

                var pawns =
                    Find.ListerPawns.AllPawns.Where(p => !p.InContainer && (_room == RoomQuery.RoomAt(p.Position)));

                if (!pawns.Any() && !_isAlert)
                {
                    if (_isDelayCounter && _delayCounter > 120)
                    {
                        _powerTrader.powerOutput = -1.0f;
                        _glower.Lit = false;
                        _delayCounter = 0;
                    }
                    _isDelayCounter = true;
                }
                else
                {
                    _powerTrader.powerOutput = _maxPowerСonsumption;
                    _glower.Lit = true;
                    _delayCounter = 0;
                    _isDelayCounter = false;
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

            sensor.FirstOrDefault().RegisterLamp(this);
            _isConnectedToThreatSensor = true;

        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();

            if (_powerTrader == null) return stringBuilder.ToString();

            if (_powerTrader.PowerNet.hasPowerSource)
            {
                var pcrs = (int) (_powerTrader.PowerNet.CurrentEnergyGainRate()/CompPower.WattsToWattDaysPerTick);
                stringBuilder.AppendLine(_txtPowerNeeded + ": " + -_powerTrader.powerOutput + " " + _txtWatt);
                stringBuilder.AppendLine(string.Format(_txtPowerConnectedRateStored, pcrs,
                    _powerTrader.PowerNet.CurrentStoredEnergy().ToString("F0")));
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