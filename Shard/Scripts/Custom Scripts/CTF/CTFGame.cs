using Server;
using System;
using System.Collections;
using Server.Items;
using Server.Network;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using Server.Commands;
using System.Collections.Generic;
using Server.Misc;

using System.Net;

namespace Server.Items
{
	public class CTFGame : Item
	{
		private CTFGame m_Game2; 
		private static int m_GameReg = 1;
		private static ArrayList m_Registry = new ArrayList();

		//public static ArrayList Registry{ get{ return m_Registry; } }

		public static void Initialize()
		{
			EventSink.PlayerDeath += new PlayerDeathEventHandler( OnPlayerDeath );
        	        EventSink.Logout += new LogoutEventHandler( OnLogout );

			CommandSystem.Register( "endgame", AccessLevel.GameMaster, new CommandEventHandler( EndGame_Command ) );
			CommandSystem.Register( "startgame", AccessLevel.GameMaster, new CommandEventHandler( StartGame_Command ) );
			CommandSystem.Register( "Team", AccessLevel.Player, new CommandEventHandler( TeamMessage_Command ) );
			CommandSystem.Register( "t",  AccessLevel.Player, new CommandEventHandler( TeamMessage_Command ) );
		}

		private static void TeamMessage_Command( CommandEventArgs e )
		{
			string msg = e.ArgString;
			if ( msg == null )
				return;
			msg = msg.Trim();
			if ( msg.Length <= 0 )
				return;
			
			CTFTeam team = FindTeamFor( e.Mobile );
			if ( team != null )
			{
				msg = String.Format( "Team [{0}]: {1}", e.Mobile.Name, msg );
				for(int m=0;m<team.Members.Count;m++)
					((Mobile)team.Members[m]).SendMessage( msg );
			}
		}

		private static void EndGame_Command( CommandEventArgs e )
		{
			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( EndGame_Target ) );
			e.Mobile.SendMessage( "Target the game control stone to END a game." );
		}
		private static void EndGame_Target( Mobile from, object o )
		{
			if ( o is CTFGame )
			{
				CTFGame game = (CTFGame)o;
				game.EndGame();
				from.SendMessage( "The game has been ended." );
			}
			else
			{
				from.BeginTarget( -1, false, TargetFlags.None, new TargetCallback( EndGame_Target ) );
				from.SendMessage( "Target the game stone." );
			}
		}

		private static void StartGame_Command( CommandEventArgs e )
		{
			if ( e.Arguments.Length < 1 )
			{
				e.Mobile.SendMessage( "Usage: startgame <ResetTeams>" );
				e.Mobile.SendMessage( "So, if you want to start the game and force everyone to choose a new team, do [startgame true" );
			}
			
			string str = e.GetString( 0 ).ToUpper().Trim();
			bool reset;
			if ( str == "YES" || str == "TRUE" || str == "Y" || str == "T" || str == "1" )
				reset = true;
			else
				reset = false;

			e.Mobile.BeginTarget( -1, false, TargetFlags.None, new TargetStateCallback( StartGame_Target ), reset );
			e.Mobile.SendMessage( "Target the game control stone to START a game." );
		}

		private static void StartGame_Target( Mobile from, object o, object state )
		{
			bool reset = state is bool ? (bool)state : false;

			if ( o is CTFGame )
			{
				CTFGame game = (CTFGame)o;
				game.StartGame( reset );
				from.SendMessage( "The game has been started." );
			}
			else
			{
				from.BeginTarget( -1, false, TargetFlags.None, new TargetStateCallback( StartGame_Target ), reset );
				from.SendMessage( "Target the game stone." );
			}
		}

		private static void OnPlayerDeath( PlayerDeathEventArgs e )
		{
			CTFTeam team = FindTeamFor( e.Mobile );
			if ( team != null )
				new DeathTimer( e.Mobile, team ).Start();

		}

    		  public static void OnLogout( LogoutEventArgs e )
    		  {
			Mobile m = e.Mobile;
			PlayerMobile pm = (PlayerMobile)m;
			for(int i=0;i<m_Registry.Count;i++)
			{
			CTFGame game = (CTFGame)m_Registry[i];
			CTFTeam team = game.GetTeam( pm );
			//pm.Profession = 666;
				if ( team != null )
		        new LogoutTimer(pm,game).Start();
			}
        	  }

		public static CTFTeam FindTeamFor( Mobile m )
		{
			for(int i=0;i<m_Registry.Count;i++)
			{
				CTFGame game = (CTFGame)m_Registry[i];
				CTFTeam team = game.GetTeam( m );
				if ( team != null )
					return team;
			}

			return null;
		}

		private ArrayList m_Teams;
		private int m_Game;

		private bool m_DeathPoint;
		private bool m_GiveRobe;
		private bool m_Open;
		private bool m_Running;
		private bool m_MsgStaff;
		private int m_MaxScore;
		private TimeSpan m_Length;

		private DateTime m_StartTime;

		private Timer m_GameTimer, m_ScoreTimer, m_Timer, m_LogoutTimer;

		[Constructable]
		public CTFGame( int numTeams ) : base( 0xEDC )
		{
			m_Game = m_GameReg++;
			m_Teams = new ArrayList( numTeams );
			m_Length = TimeSpan.FromHours( 1.0 );

			for(int i=0;i<numTeams;i++)
				m_Teams.Add( new CTFTeam( this, i ) );

			Movable = false;
			Name = "Game Control Stone";

			m_Registry.Add( this );

			m_Open = true;

                        m_Running = false;
		}

		public CTFGame( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)1 );//version

			writer.Write( m_MaxScore );

			writer.Write( m_Game );
		
			writer.Write( m_DeathPoint );
			writer.Write( m_GiveRobe );
			writer.Write( m_Open );
			writer.Write( m_MsgStaff );
			writer.Write( m_Length );

			writer.Write( (int)m_Teams.Count );
			for (int i=0;i<m_Teams.Count;i++)
				((CTFTeam)m_Teams[i]).Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_MaxScore = reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					m_Game = reader.ReadInt();

					m_DeathPoint = reader.ReadBool();
					m_GiveRobe = reader.ReadBool();
					m_Open = reader.ReadBool();
					m_MsgStaff = reader.ReadBool();
					m_Length = reader.ReadTimeSpan();

					int len = reader.ReadInt();
					m_Teams = new ArrayList( len );
					for (int i=0;i<len;i++)
						m_Teams.Add( new CTFTeam( reader ) );
					break;
				}
			}

			EventSink.PlayerDeath += new PlayerDeathEventHandler( OnPlayerDeath );
			m_Registry.Add( this );

			m_Running = false;
                        m_Open = true;

		}

		[CommandProperty( AccessLevel.Counselor )]
		public bool Running { get { return m_Running; } }

		public override void OnAfterDelete()
		{
			for(int t=0;t<m_Teams.Count;t++)
			{
				CTFTeam team = (CTFTeam)m_Teams[t];
				while ( team.Members.Count > 0 )
					LeaveGame( (Mobile)team.Members[0] );
			}
					
			m_Registry.Remove( this );
			m_Teams.Clear();

			base.OnAfterDelete();
		}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void LeaveGame( Mobile m )
		{
                        m.MoveToWorld( new Point3D( 1563, 1739, 15 ), Map.Felucca );
			CTFTeam t = GetTeam( m );
			if ( t != null )
				t.RemoveMember( m );
                        m.SolidHueOverride = -1;
			
			Item robe = m.FindItemOnLayer( Layer.OuterTorso );
			if ( robe is CTFRobe )
				robe.Delete();
			if ( m.Backpack != null )
			{
				Item[] robes = m.Backpack.FindItemsByType( typeof( CTFRobe ) );
				for(int i=0;i<robes.Length;i++)
					robes[i].Delete();
			}

			m.Delta( MobileDelta.Noto );

				
		}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void PlayerMessage( string message, params object[] args )
		{
			PlayerMessage( String.Format( message, args ) );
		}

		public void PlayerMessage( string message )
		{
			message = "Game: " + message;
			for (int i=0;i<m_Teams.Count;i++)
			{
				CTFTeam team = (CTFTeam)m_Teams[i];
				for (int j=0;j<team.Members.Count;j++)
					((Mobile)team.Members[j]).SendMessage( 0x489, message );
			}

			if ( m_MsgStaff )
			{
                        }	
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Open )
				LabelTo( from, "Time left: {0:00}:{1:00}:{2:00}", (int)(TimeLeft.TotalSeconds/60/60), (int)(TimeLeft.TotalSeconds/60)%60, (int)(TimeLeft.TotalSeconds)%60 );
			base.OnSingleClick( from );
		}

		public int GameNumber{ get{ return m_Game; } }
		public ArrayList Teams{ get{ return m_Teams; } }
		public TimeSpan TimeLeft{ get{ return m_Length - (DateTime.Now-m_StartTime); } }


		[CommandProperty( AccessLevel.Seer )]
		public bool DeathPoint{ get{ return m_DeathPoint; } set { m_DeathPoint = value; } }

		[CommandProperty( AccessLevel.Seer )]
		public bool GiveRobe{ get{ return m_GiveRobe; } set{ m_GiveRobe = value; } }

		[CommandProperty( AccessLevel.Seer )]
		public bool MessageStaff{ get{ return m_MsgStaff; } set{ m_MsgStaff = value; } }

		[CommandProperty( AccessLevel.Seer )]
		public TimeSpan Length{ get{ return m_Length; } set{ m_Length = value; } }

		[CommandProperty( AccessLevel.Seer )]
		public int MaxScore
		{ 
			get
			{ 
				if ( m_MaxScore <= 0 )
					return int.MaxValue;
				else
					return m_MaxScore; 
			} 
			set{ m_MaxScore = value; } 
		}

		[CommandProperty( AccessLevel.Seer )]
		public int TeamCount
		{
			get { return m_Teams.Count; }
			set
			{
				if ( value > m_Teams.Count )
				{
					for (int i=m_Teams.Count;i<value;i++)
						m_Teams.Add( new CTFTeam( this, i ) );
				}
			}
		}

		[CommandProperty( AccessLevel.Seer )]
		public bool OpenJoin
		{
			get { return m_Open; }
			set { m_Open = value; }
		}

		public bool IsInGame( CTFTeam team )
		{
			return m_Teams != null ? m_Teams.Contains( team ) : false;
		}

		public bool IsInGame( Mobile m )
		{
			return GetTeam( m ) != null;
		}

		public CTFTeam GetTeam( int uid )
		{
			if ( m_Teams != null )
			{
				for (int i=0;i<m_Teams.Count;i++)
				{
					CTFTeam team = (CTFTeam)m_Teams[i];
					if ( team.UId == uid )
						return team;
				}
			}
			return null;
		}

		public CTFTeam GetTeam( Mobile m )
		{
			if ( m_Teams != null )
			{
				for (int i=0;i<m_Teams.Count;i++)
				{
					CTFTeam team = (CTFTeam)m_Teams[i];
					if ( team.Members.Contains( m ) )
						return team;
				}
			}
			return null;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.Seer )
				from.SendGump( new PropertiesGump( from, this ) );
		}

		public void SwitchTeams( Mobile from, CTFTeam team )
		{
			CTFTeam old = GetTeam( from );
			if ( old == team )
				return;
			else if ( old != null )
				old.RemoveMember( from );

			team.AddMember( from );
			from.Delta( MobileDelta.Noto );
                        from.Blessed = true;

			Item robe = from.FindItemOnLayer( Layer.OuterTorso );
			if ( robe is CTFRobe )
			{
				robe.Name = "Game Robe";
				robe.Hue = 0;
			}
			else
			{
				if ( robe != null )
				{
					if ( robe.Movable )
						from.AddToBackpack( robe );
					else
						robe.Delete();
				}
				
				robe = new CTFRobe( team );
				from.EquipItem( robe );
			}
		}

		public int TeamSize
		{
			get
			{
				return 2;

			}
		}


		public void EndGame()
		{
			if ( !m_Running )
				return;

			CTFTeam Winner = null;
			
			if ( m_GameTimer != null )
			{
				m_GameTimer.Stop();
				m_GameTimer = null;
			}

			if ( m_ScoreTimer != null )
			{
				m_ScoreTimer.Stop();
				m_ScoreTimer = null;
			}

			for (int i=0;i<m_Teams.Count;i++)
			{
				CTFTeam team = (CTFTeam)m_Teams[i];
				if ( team.Flag != null )
					team.Flag.ReturnToHome();

				if ( Winner == null || team.Points > Winner.Points )
					Winner = team;
			}

			if ( Winner != null )
			{
				PlayerMessage( "The game is over.  The winner is {0} with {1} points!", Winner.Name, Winner.Points );
				

                                // Uncomment this and mofiy as nessessary to give prize(s) to the winning team members.
				for (int j=0;j<Winner.Members.Count;j++)
				{
					Mobile m = (Mobile)Winner.Members[j];

					//m.AddToBackpack( new Tokens(3) );
				}
///////////////////////////////////////////////////////////////////////////////////////////////////////
			/*for (int h=0;h<m_Teams.Count;h++)
			{
				CTFTeam team = (CTFTeam)m_Teams[h];
				for (int g=0;g<team.Members.Count;g++)
                                {
                                     Mobile m = (Mobile)team.Members[g];
                                     LeaveGame(m);
                                     m.SolidHueOverride = -1;
					m.SendMessage("Game is reset you {0}",g);
                                }
			}*/

			for(int ty=0;ty<m_Teams.Count;ty++)
			{
				CTFTeam team3 = (CTFTeam)m_Teams[ty];
				while ( team3.Members.Count > 0 )
					LeaveGame( (Mobile)team3.Members[0] );
			}
///////////////////////////////////////////////////////////////////////////////////////////////////////
				
			}
			else
			{
				PlayerMessage( "The game is over." );
			}
			
			m_Running = false;
		}

		public void ExEndGame()
		{
			if ( !m_Running )
				return;

			if ( m_GameTimer != null )
			{
				m_GameTimer.Stop();
				m_GameTimer = null;
			}

			if ( m_ScoreTimer != null )
			{
				m_ScoreTimer.Stop();
				m_ScoreTimer = null;
			}

			if ( m_LogoutTimer != null )
			{
				m_LogoutTimer.Stop();
				m_LogoutTimer = null;
			}	

			for (int i2=0;i2<m_Teams.Count;i2++)
			{
				CTFTeam team2 = (CTFTeam)m_Teams[i2];
				if ( team2.Flag != null )
					team2.Flag.ReturnToHome();

				team2.Points = 0;
			}

			for(int tyy=0;tyy<m_Teams.Count;tyy++)
			{
				CTFTeam team33 = (CTFTeam)m_Teams[tyy];
				while ( team33.Members.Count > 0 )
					LeaveGame( (Mobile)team33.Members[0] );
			}		
			m_Running = false;

		}
			



		public List<IPAddress> GetAddresses()
		{
			List<IPAddress> list = new List<IPAddress>();

			for(int i2 = 0; i2 < m_Teams.Count; i2++)
			{
				CTFTeam team = (CTFTeam)m_Teams[i2];

				for(int x = 0; x < team.Members.Count; x++)
				{
				        Mobile m = (Mobile)team.Members[x];
					if(m.NetState != null)
						list.Add(m.NetState.Address);
				}
			}

			return list;
		}










		public void StartGame( bool resetTeams )
		{
			m_Running = false;

			if ( m_GameTimer != null )
				m_GameTimer.Stop();

			if ( m_ScoreTimer != null )
				m_ScoreTimer.Stop();

			if ( resetTeams )
			{
				PlayerMessage( "The game will start in 30 seconds, please select your team." );

				int teamSize = this.TeamSize;
				ArrayList players = new ArrayList();
				for(int i=0;i<m_Teams.Count;i++)
				{
					CTFTeam t = (CTFTeam)m_Teams[i];
					for (int j=0;j<t.Members.Count;j++)
					{
						Mobile m = (Mobile)t.Members[i];
						m.Frozen = true;
						m.SendGump( new GameTeamSelector( this, teamSize ) );
						players.Add( m );
					}

					t.Members.Clear();
				}

				new StartTimer( this, players ).Start();
			}
			else
			{
			 ResetGame();	
			}
		}

		private class StartTimer : Timer
		{
			public static TimeSpan StartDelay = TimeSpan.FromMinutes( 0.5 );
			private CTFGame m_Game;
			private ArrayList m_List;

			public StartTimer( CTFGame game, ArrayList list ) : base( StartDelay )
			{
				m_Game = game;
				m_List = list;
				Priority = TimerPriority.TwoFiftyMS;
			}
			
			protected override void OnTick()
			{
				int sm = -1, ns = -1;
				int[] amc = new int[m_Game.m_Teams.Count];
				for(int i=0;i<m_Game.m_Teams.Count;i++)
				{
					amc[i] = ((CTFTeam)m_Game.m_Teams[i]).ActiveMemberCount;
					if ( sm == -1 || amc[i] < amc[sm] )
					{
						ns = sm;
						sm = i;
					}
					else if ( amc[i] < amc[ns] )
					{
						ns = i;
					}
				}

				for (int i=0;i<m_List.Count;i++)
				{
					Mobile m = (Mobile)m_List[i];

					m.Frozen = false;
					m.CloseGump( typeof( GameTeamSelector ) );

					if ( m_Game.GetTeam( m ) == null )
					{
						int t;
						if ( m.NetState == null )
						{
							t = Utility.Random( amc.Length );
						}
						else
						{
							if ( amc[sm] >= amc[ns] )
								t = Utility.Random( amc.Length );
							else
								t = sm;
							amc[t]++;
						}

						CTFTeam team = (CTFTeam)m_Game.m_Teams[t];

						m_Game.SwitchTeams( m, team );
						m.SendMessage( "You have joined team {0}!", team.Name );
					}
				}
				m_Game.ResetGame();
			}
		}

		public void ResetGame()
		{

			PlayerMessage( "The game has started." );
			
			m_StartTime = DateTime.Now;

			m_GameTimer = Timer.DelayCall( Length, new TimerCallback( EndGame ) );
			m_Running = true;

			if ( m_ScoreTimer == null )
				m_ScoreTimer = new ScoreTimer( this );
			m_ScoreTimer.Start();

			for (int i=0;i<m_Teams.Count;i++)
			{
				CTFTeam team = (CTFTeam)m_Teams[i];

				team.Points = 0;
				if ( team.Flag != null )
					team.Flag.ReturnToHome();

				for(int j=0;j<team.Members.Count;j++)
				{
					Mobile m = (Mobile)team.Members[j];

					m.LogoutLocation = team.Home;
					m.Location = team.Home;
					m.SolidHueOverride = team.Hue;
					m.Map = team.Map;
                                        m.Blessed = false;
	

					m.Hits = m.HitsMax;
					m.Mana = m.ManaMax;
					m.Stam = m.StamMax;

					//m.SendGump(new scoregump(m_Game2,m));

					//m.SendMessage("Game is reset you {0}",j);
				}
			}
		}

		private class DeathTimer : Timer
		{
			public static TimeSpan DeathDelay =  TimeSpan.FromSeconds( 0 );

			private CTFTeam m_Team;
			private Mobile m_Mob;

			public DeathTimer( Mobile m, CTFTeam t ) : base( DeathDelay )
			{
				m_Mob = m;
				m_Team = t;
				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{

				if ( !m_Mob.Alive )
				{
					m_Mob.Location = m_Team.Home;
					m_Mob.Map = m_Team.Map;
					m_Mob.Resurrect();
					m_Mob.Hits = m_Mob.HitsMax;
					m_Mob.Mana = m_Mob.ManaMax;
				}

				if ( m_Mob.Corpse != null && !m_Mob.Corpse.Deleted )
                                        {
					m_Mob.Corpse.Location = m_Team.Home;
                                        m_Mob.Corpse.OnDoubleClick(m_Mob);
					m_Mob.Corpse.Delete();
                                        }

				if ( m_Team.Game.DeathPoint )
					--m_Team.Points;
			}
		}

		private class ScoreTimer : Timer
		{
			private CTFGame m_Game;
			private Mobile m_Mobi;

			public ScoreTimer( CTFGame g ) : base( TimeSpan.FromSeconds( 90 ), TimeSpan.FromSeconds( 90 ) )
			{
				m_Game = g;
			}

			protected override void OnTick()
			{
				m_Game.PlayerMessage( "Time left: {0:0}:{1:00}:{2:00}  <>  Scores:", (int)(m_Game.TimeLeft.TotalSeconds/60/60), (int)(m_Game.TimeLeft.TotalSeconds/60)%60, (int)(m_Game.TimeLeft.TotalSeconds)%60 );
				for (int i=0;i<m_Game.Teams.Count;i++)
				{
					CTFTeam team = (CTFTeam)m_Game.Teams[i];
					m_Game.PlayerMessage( "Team {0}: {1} points", team.Name, team.Points );
				}
			}
		}

		public class LogoutTimer : Timer
		{
			private CTFGame m_Game;
			private Mobile m_Mob;

			public LogoutTimer( Mobile mob, CTFGame gam ) : base( TimeSpan.FromSeconds(0.1) )
			{
				m_Mob = mob;
				m_Game = gam;
			}

			protected override void OnTick()
			{

			 	PlayerMobile pm = (PlayerMobile)m_Mob;

			CTFTeam teamo = (CTFTeam)m_Game.Teams[0];
			CTFTeam teamt = (CTFTeam)m_Game.Teams[1];
			if (teamo.ActiveMemberCount == 0 && teamt.ActiveMemberCount == 0)
			{
			m_Game.ExEndGame();
			}
			else
			{

					m_Game.LeaveGame(pm);
					Stop();
			}

			}
		}		

	}
}
