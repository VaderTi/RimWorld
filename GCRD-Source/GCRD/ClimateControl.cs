using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace GCRD
{
    public enum Status
    {
        Waiting,
        Heating,
        Cooling,
        Outdoor
    }

    internal class ClimateControl : Building
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
            _txUiMinTempMinus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MinMinus", true);
            _txUiMinTempPlus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MinPlus", true);
            _txUiMaxTempMinus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MaxMinus", true);
            _txUiMaxTempPlus = ContentFinder<Texture2D>.Get("UI/Commands/UI_MaxPlus", true);
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            GetTextures();

            _txtMinus = (Prefs.TemperatureMode == TemperatureDisplayMode.Celsius) ? "-1" : "-2";
            _txtPlus = (Prefs.TemperatureMode == TemperatureDisplayMode.Celsius) ? "1" : "2";

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

            _room = RoomQuery.RoomAt(Position);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref _minComfortTemp, "minComfortTemp");
            Scribe_Values.LookValue(ref _maxComfortTemp, "maxComfortTemp");
        }

        public override void TickRare()
        {
            base.TickRare();

            _room = RoomQuery.RoomAt(Position);

            if (!_powerTrader.PowerOn || _room == null || _room.UsesOutdoorTemperature)
            {
                if (_room.UsesOutdoorTemperature) WorkStatus = Status.Outdoor;
                _powerTrader.powerOutput = 0;
                return;
            }

            var currentTemp = _room.Temperature;

            if (currentTemp >= _minComfortTemp && currentTemp <= _maxComfortTemp)
            {
                WorkStatus = Status.Waiting;
                _powerTrader.powerOutput = -1.0f;
                return;
            }

            var needPower = 0.0f;
            var diffTemp = 0.0f;
            var powerMod = 1.1f;
            var efficiencyLoss = 0.0f;
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

            efficiencyLoss = EfficiencyLossPerDegreeDifference*Mathf.Abs(diffTemp);
            var enegyLimit = diffTemp*_room.CellCount*energyMul*0.3333f;
            needPower = Mathf.Abs(enegyLimit*(powerMod + efficiencyLoss));

            _powerTrader.powerOutput = -needPower;
            ChangeTemp(enegyLimit);
            //Log.Warning("Tick Rare");
        }

        private void ChangeTemp(float energy)
        {
            _room.PushHeat(energy);
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

        public override IEnumerable<Command> GetCommands()
        {
            var commandList = new List<Command>();
            var baseCommandList = _powerTrader.CompGetCommandsExtra();
            commandList = baseCommandList.ToList();

            var actionMinTempMinus = new Command_Action();
            actionMinTempMinus.icon = _txUiMinTempMinus;
            actionMinTempMinus.defaultLabel = _txtMinus;
            actionMinTempMinus.defaultDesc = _txtMinTempMinus;
            actionMinTempMinus.activateSound = SoundDef.Named("Click");
            actionMinTempMinus.action = MinTempMinus;
            commandList.Add(actionMinTempMinus);


            var actionMinTempPlus = new Command_Action();
            actionMinTempPlus.icon = _txUiMinTempPlus;
            actionMinTempPlus.defaultLabel = _txtPlus;
            actionMinTempPlus.defaultDesc = _txtMinTempPlus;
            actionMinTempPlus.activateSound = SoundDef.Named("Click");
            actionMinTempPlus.action = MinTempPlus;
            commandList.Add(actionMinTempPlus);


            var actionMaxTempMinus = new Command_Action();
            actionMaxTempMinus.icon = _txUiMaxTempMinus;
            actionMaxTempMinus.defaultLabel = _txtMinus;
            actionMaxTempMinus.defaultDesc = _txtMaxTempMinus;
            actionMaxTempMinus.activateSound = SoundDef.Named("Click");
            actionMaxTempMinus.action = MaxTempMinus;
            commandList.Add(actionMaxTempMinus);


            var actionMaxTempPlus = new Command_Action();
            actionMaxTempPlus.icon = _txUiMaxTempPlus;
            actionMaxTempPlus.defaultLabel = _txtPlus;
            actionMaxTempPlus.defaultDesc = _txtMaxTempPlus;
            actionMaxTempPlus.activateSound = SoundDef.Named("Click");
            actionMaxTempPlus.action = MaxTempPlus;
            commandList.Add(actionMaxTempPlus);

            return commandList.AsEnumerable();
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            if (_powerTrader == null) return stringBuilder.ToString();

            if (_powerTrader.PowerNet.hasPowerSource)
            {
                var statusstring = _txtStatusWaiting;
                switch (WorkStatus)
                {
                    case Status.Heating:
                        statusstring = _txtStatusHeating;
                        break;
                    case Status.Cooling:
                        statusstring = _txtStatusCooling;
                        break;
                    case Status.Outdoor:
                        statusstring = _txtStatusOutdoor;
                        break;
                }
                var pcrs = (int) (_powerTrader.PowerNet.CurrentEnergyGainRate()/CompPower.WattsToWattDaysPerTick);
                stringBuilder.AppendLine(_txtStatus + ": " + statusstring);
                stringBuilder.AppendLine(_txtPowerNeeded + ": " + -(int) _powerTrader.powerOutput + " " + _txtWatt);
                stringBuilder.AppendLine(_txtMinComfortTemp + ": " + _minComfortTemp.ToStringTemperature("F0"));
                stringBuilder.AppendLine(_txtMaxComfortTemp + ": " + _maxComfortTemp.ToStringTemperature("F0"));
                stringBuilder.AppendLine(string.Format(_txtPowerConnectedRateStored, pcrs,
                    _powerTrader.PowerNet.CurrentStoredEnergy().ToString("F0")));
            }
            else
            {
                stringBuilder.AppendLine(_txtPowerNotConnected);
            }
            return stringBuilder.ToString();
        }
    }
}