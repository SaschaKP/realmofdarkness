using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests
{
	public abstract class MondainQuester : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		public override bool IsActiveVendor{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		public override bool DisallowAllMoves{ get{ return false; } }
		public override bool ClickTitle{ get { return false; } }
		public override bool CanTeach{ get{ return true; } }
		
		public virtual int AutoTalkRange{ get{ return -1; } }
		public virtual int AutoSpeakRange{ get{ return 10; } }		
		public virtual TimeSpan SpeakDelay{ get{ return TimeSpan.FromMinutes( 1 ); } }
		
		public abstract Type[] Quests{ get; }
		
		private DateTime m_Spoken;
		
		public override void InitSBInfo()
		{		
		}
		
		public MondainQuester() : base( null )
		{
			SpeechHue = 0x3B2;
		}
		
		public MondainQuester( string name ) : this( name, null )
		{
		}

		public MondainQuester( string name, string title ) : base( title )
		{
			Name = name;
			SpeechHue = 0x3B2;
		}
		
		public MondainQuester( Serial serial ) : base( serial )
		{
		}
		
		public virtual void OnTalk( PlayerMobile player )
		{				
			if ( QuestHelper.DeliveryArrived( player, this ) )
				return;
			
			if ( QuestHelper.InProgress( player, this ) )
				return;
		
			if ( QuestHelper.QuestLimitReached( player ) )
				return;
			
			// check if this quester can offer any quest chain (already started)
			foreach( KeyValuePair<QuestChain,BaseChain> pair in player.Chains )
			{
				BaseChain chain = pair.Value;
																			
				if ( chain != null && chain.Quester != null && chain.Quester == GetType() )
				{
					BaseQuest quest = QuestHelper.RandomQuest( player, new Type[] { chain.CurrentQuest }, this );
					
					if ( quest != null )
					{
						player.CloseGump( typeof( MondainQuestGump ) );
						player.SendGump( new MondainQuestGump( quest ) );
						return;
					}
				}
			}
					
			BaseQuest questt = QuestHelper.RandomQuest( player, Quests, this );
						
			if ( questt != null )
			{
				player.CloseGump( typeof( MondainQuestGump ) );
				player.SendGump( new MondainQuestGump( questt ) );
			}
		}
		
		public virtual void OnOfferFailed()
		{			
			Say( 1075575 ); // I'm sorry, but I don't have anything else for you right now. Could you check back with me in a few minutes?
		}
		
		public virtual void Advertise()
		{
			Say( Utility.RandomMinMax( 1074183, 1074223 ) );
		}
		
		public override bool CanBeDamaged()
		{
			return false;
		}
		
		public override void InitBody()
		{
			if ( Race != null )
			{
				HairItemID = Race.RandomHair( Female );
				HairHue = Race.RandomHairHue();
				FacialHairItemID = Race.RandomFacialHair( Female );
				FacialHairHue = Race.RandomHairHue();
				Hue = Race.RandomSkinHue();
			}
		}
		
		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m.Alive && !m.Hidden && m is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)m;

				int range = AutoTalkRange;

				if ( range >= 0 && InRange( m, range ) && !InRange( oldLocation, range ) )
					OnTalk( pm );
					
				range = AutoSpeakRange;
				
				if ( range >= 0 && InRange( m, range ) && !InRange( oldLocation, range ) && DateTime.Now >= m_Spoken + SpeakDelay )
				{
					if ( Utility.Random( 100 ) < 50 )
						Advertise();
					
					m_Spoken = DateTime.Now;
				}
			}
		}
		
		public override void OnDoubleClick( Mobile m )
		{
			if ( m.Alive && m is PlayerMobile )
				OnTalk( (PlayerMobile) m );				
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1072269 ); // Quest Giver
		}
		
		public void FocusTo( Mobile to )
		{
			QuestSystem.FocusTo( this, to );
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
			
			if ( CantWalk )
				Frozen = true;	
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();		
			
			m_Spoken = DateTime.Now;
			
			if ( CantWalk )
				Frozen = true;	
		}
	}
}