using System;
using System.Collections;
using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
	[CorpseName( "a mongbat corpse" )]
	public class Mongbat2 : BaseCreature
	{
		[Constructable]
		public Mongbat2() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = "a mongbat";
			Body = 39;
			BaseSoundID = 422;

			SetStr( 6, 10 );
			SetDex( 26, 38 );
			SetInt( 6, 14 );

			SetHits( 4, 6 );
			SetMana( 0 );

			SetDamage( 1, 2 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 5, 10 );

			SetSkill( SkillName.MagicResist, 5.1, 14.0 );
			SetSkill( SkillName.Tactics, 5.1, 10.0 );
			SetSkill( SkillName.Wrestling, 5.1, 10.0 );

			Fame = 150;
			Karma = -150;

			VirtualArmor = 10;

			Tamable = true;
			ControlSlots = 1;
			MinTameSkill = -18.9;
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.Poor );
		}

		public override int Meat{ get{ return 1; } }
		public override FoodType FavoriteFood{ get{ return FoodType.Meat; } }

        /// </summary>
        /// <param name="c"></param>
        public override void OnDeath(Container c) // overrides the OnDeath command in the Scripts>Engines>AI>Creature>BaseCreature.cs
        {

            base.OnDeath(c); //calls to the container
            switch (Utility.Random(70)) // random % rate being in this case you have a 10% chance to get one of these on drop
            {
                case 0: c.DropItem(new NewbieDungeonExchangeTicket());
                    break;

            }


        }

        /// <summary>

		public Mongbat2( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}