using RimWorld;
using Verse;

namespace GCRD
{
    internal class SmartMine : Building
    {
        private int _counter;
        private int _delay = 30;
        private CompExplosive _explosive;
        private Graphic_Single _graphicSingle;
        private bool _isArmed;
        //public override Graphic Graphic
        //{
        //    get { return _graphicSingle; }
        //}

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            _explosive = GetComp<CompExplosive>();

            //_graphicSingle = new Graphic_Single(MaterialPool.MatFrom("Things/Weapons/SmartMine"), false);
        }

        public override void Tick()
        {
            base.Tick();

            ++_counter;

            if (_counter == 15)
            {
                if (!_isArmed) _isArmed = true;
                foreach (var thing in Find.ThingGrid.ThingsAt(Position))
                {
                    if (thing is Pawn)
                    {
                        var pawn = thing as Pawn;
                        foreach (var hpawn in Find.ListerPawns.PawnsHostileToColony)
                        {
                            if (hpawn != null && hpawn == pawn) Detonate();
                        }
                    }
                }
                _counter = 0;
            }
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
        }

        private void Detonate()
        {
            var damageInfo = new BodyPartDamageInfo(null, BodyPartDepth.Outside);
            var explosionInfo = default(ExplosionInfo);

            explosionInfo.center = Position;
            explosionInfo.radius = _explosive.props.explosiveRadius;
            explosionInfo.dinfo = new DamageInfo(_explosive.props.explosiveDamageType, 30, this, damageInfo, null);
            explosionInfo.Explode();
        }
    }
}