using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace GCRD
{
    internal enum Status
    {
        Waiting,
        Heating,
        Cooling,
        Outdoor
    }

    [StaticConstructorOnStartup]
    internal class Building_ClimateControl : Building
    {
        private const float EfficiencyLossPerDegreeDifference = 0.007692308f;
        private static Texture2D _txUiMinTempMinus;
        private static Texture2D _txUiMinTempPlus;
        private static Texture2D _txUiMaxTempMinus;
        private static Texture2D _txUiMaxTempPlus;
        private float _maxComfortTemp = 22.0f;
        private float _minComfortTemp = 20.0f;
        private CompPowerTrader _powerTrader;
        private Room _room;
        private string _txtMaxComfortTemp = "Максимальная комфортная температура";
        private string _txtMaxTempMinus = "Понижает максимально комфортную температуру.";
        private string _txtMaxTempPlus = "Повышает максимально комфортную температуру.";
        private string _txtMinComfortTemp = "Минимальная комфортная температура";
        private string _txtMinTempMinus = "Понижает минимально комфортную температуру.";
        private string _txtMinTempPlus = "Повышает минимально комфортную температуру.";
        private string _txtMinus;
        private string _txtPlus;
        private string _txtPowerConnectedRateStored = "Мощность сети/аккумулировано: {0} Вт / {1} Вт*дней";
        private string _txtPowerNeeded = "Потребляемая мощность";
        private string _txtPowerNotConnected = "Не подключено к сети.";
        private string _txtStatus = "Статус";
        private string _txtStatusCooling = "Охлаждает";
        private string _txtStatusHeating = "Обогревает";
        private string _txtStatusOutdoor = "Установлен снаружи";
        private string _txtStatusWaiting = "Ожидание";
        private string _txtWatt = "Вт";
        public Status WorkStatus = Status.Waiting;

        private static void GetTextures()
        {
            _txUiMinTempMinus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MinMinus");
            _txUiMinTempPlus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MinPlus");
            _txUiMaxTempMinus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MaxMinus");
            _txUiMaxTempPlus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MaxPlus");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            LongEventHandler.ExecuteWhenFinished(GetTextures);

            _txtMinus = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius ? "-1°C" : "-2°F";
            _txtPlus = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius ? "1°C" : "2°F";

            _txtMinTempMinus = "MinTempMinus".Translate();
            _txtMinTempPlus = "MinTempPlus".Translate();
            _txtMaxTempMinus = "MaxTempMinus".Translate();
            _txtMaxTempPlus = "MaxTempPlus".Translate();

            _txtWatt = "Watt".Translate();
            _txtStatus = "Status".Translate();
            _txtStatusWaiting = "StatusWaiting".Translate();
            _txtStatusHeating = "StatusHeating".Translate();
            _txtStatusCooling = "StatusCooling".Translate();
            _txtStatusOutdoor = "StatusOutdoor".Translate();

            _txtMinComfortTemp = "MinComfortTemp".Translate();
            _txtMaxComfortTemp = "MaxComfortTemp".Translate();


            _txtPowerNotConnected = "PowerNotConnected".Translate();
            _txtPowerNeeded = "PowerNeeded".Translate();
            _txtPowerConnectedRateStored = "PowerConnectedRateStored".Translate();

            _powerTrader = GetComp<CompPowerTrader>();

            _room = RegionAndRoomQuery.RoomAt(Position, Map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _minComfortTemp, "minComfortTemp");
            Scribe_Values.Look(ref _maxComfortTemp, "maxComfortTemp");
        }

        public override void TickRare()
        {
            base.TickRare();

            _txtMinus = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius ? "-1°C" : "-2°F";
            _txtPlus = Prefs.TemperatureMode == TemperatureDisplayMode.Celsius ? "1°C" : "2°F";

            _room = RegionAndRoomQuery.RoomAt(Position, Map);

            if (!_powerTrader.PowerOn || _room == null || _room.UsesOutdoorTemperature)
            {
                if (_room != null && _room.UsesOutdoorTemperature) WorkStatus = Status.Outdoor;
                _powerTrader.PowerOutput = 0;
                return;
            }

            var currentTemp = _room.Temperature;

            if (currentTemp >= _minComfortTemp && currentTemp <= _maxComfortTemp)
            {
                WorkStatus = Status.Waiting;
                _powerTrader.PowerOutput = -1.0f;
                return;
            }

            var diffTemp = 0.0f;
            var powerMod = 1.1f;
            var energyMul = 1.0f;

            if (currentTemp < _minComfortTemp)
            {
                WorkStatus = Status.Heating;
                diffTemp = _minComfortTemp - currentTemp;
            }

            if (currentTemp > _maxComfortTemp)
            {
                WorkStatus = Status.Cooling;
                diffTemp = _maxComfortTemp - currentTemp;
                powerMod = 1.6f;
            }

            if (Mathf.Abs(diffTemp) < 3) energyMul += 0.5f;

            var efficiencyLoss = EfficiencyLossPerDegreeDifference * Mathf.Abs(diffTemp);
            var energyLimit = diffTemp * _room.CellCount * energyMul * 0.3333f;
            var needPower = Mathf.Abs(energyLimit * (powerMod + efficiencyLoss)) + 1.0f;

            _powerTrader.PowerOutput = -needPower * 5;
            ChangeTemp(energyLimit);
        }

        private void ChangeTemp(float energy)
        {
            GenTemperature.PushHeat(Position, Map, energy);
        }

        private void MinTempMinus()
        {
            --_minComfortTemp;
        }

        private void MinTempPlus()
        {
            ++_minComfortTemp;
            if (_minComfortTemp >= _maxComfortTemp) --_minComfortTemp;
        }

        private void MaxTempMinus()
        {
            --_maxComfortTemp;
            if (_maxComfortTemp <= _minComfortTemp) ++_maxComfortTemp;
        }

        private void MaxTempPlus()
        {
            ++_maxComfortTemp;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var baseGizmos = base.GetGizmos().ToList();

            foreach (var t in from t in baseGizmos
                let baseGizmo = t
                where baseGizmo == null || baseGizmo.ToString() != "CommandTogglePowerLabel".Translate()
                select t)
                yield return t;

            var actionMinTempMinus = new Command_Action
            {
                icon = _txUiMinTempMinus,
                defaultLabel = _txtMinus,
                defaultDesc = _txtMinTempMinus,
                activateSound = SoundDef.Named("Click"),
                action = MinTempMinus
            };
            yield return actionMinTempMinus;


            var actionMinTempPlus = new Command_Action
            {
                icon = _txUiMinTempPlus,
                defaultLabel = _txtPlus,
                defaultDesc = _txtMinTempPlus,
                activateSound = SoundDef.Named("Click"),
                action = MinTempPlus
            };
            yield return actionMinTempPlus;


            var actionMaxTempMinus = new Command_Action
            {
                icon = _txUiMaxTempMinus,
                defaultLabel = _txtMinus,
                defaultDesc = _txtMaxTempMinus,
                activateSound = SoundDef.Named("Click"),
                action = MaxTempMinus
            };
            yield return actionMaxTempMinus;


            var actionMaxTempPlus = new Command_Action
            {
                icon = _txUiMaxTempPlus,
                defaultLabel = _txtPlus,
                defaultDesc = _txtMaxTempPlus,
                activateSound = SoundDef.Named("Click"),
                action = MaxTempPlus
            };
            yield return actionMaxTempPlus;
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            if (_powerTrader == null) return stringBuilder.ToString();

            if (_powerTrader.PowerNet.hasPowerSource)
            {
                var statusString = _txtStatusWaiting;
                switch (WorkStatus)
                {
                    case Status.Heating:
                        statusString = _txtStatusHeating;
                        break;
                    case Status.Cooling:
                        statusString = _txtStatusCooling;
                        break;
                    case Status.Outdoor:
                        statusString = _txtStatusOutdoor;
                        break;
                }

                var pcrs = (int) (_powerTrader.PowerNet.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick);
                stringBuilder.AppendLine(_txtStatus + ": " + statusString);
                stringBuilder.AppendLine(_txtPowerNeeded + ": " + -Mathf.FloorToInt(_powerTrader.PowerOutput) + " " +
                                         _txtWatt);
                stringBuilder.AppendLine(_txtMinComfortTemp + ": " + _minComfortTemp.ToStringTemperature("F0"));
                stringBuilder.AppendLine(_txtMaxComfortTemp + ": " + _maxComfortTemp.ToStringTemperature("F0"));
                stringBuilder.AppendLine(string.Format(_txtPowerConnectedRateStored, pcrs,
                    _powerTrader.PowerNet.CurrentStoredEnergy().ToString("F0")));
            }
            else
            {
                stringBuilder.AppendLine(_txtPowerNotConnected);
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}