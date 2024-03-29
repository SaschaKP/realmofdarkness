using System;
using Server;
using Server.Network;
using System.Collections;
using Server.Items;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;

namespace Server.Misc
{
    public class VirtueArtifactSystem
    {
        private static bool m_Enabled = (Core.Expansion >= Expansion.ML);
        public static bool Enabled { get { return m_Enabled; } }

        private static Type[] m_VirtueArtifacts = new Type[]
			{
				typeof( KatrinasCrook ), typeof( JaanasStaff ), 
                typeof( DragonsEnd ), typeof( SentinelsGuard ),
				typeof( LordBlackthornsExemplar ), typeof(JusticeBreastplate), 
                typeof(HonorLegs), typeof(SpiritualityHelm), 
                typeof(CompassionArms), typeof(SacrificeSollerets),
                typeof(ValorGauntlets), typeof(HonestyGorget), typeof( MapofKnownWorld )/*, typeof( AnkhPendant ), typeof (10thAnniversarsySculpture)*/ // Need to Finish.
			};

        public static Type[] VirtueArtifacts { get { return m_VirtueArtifacts; } }

        private static bool CheckLocation(Mobile m)
        {
            Region r = m.Region;

            if (r.IsPartOf(typeof(Server.Regions.HouseRegion)) || Server.Multis.BaseBoat.FindBoatAt(m, m.Map) != null)
                return false;

            if (r.IsPartOf("Deceit") || r.IsPartOf("Despise") || r.IsPartOf("Destard") || r.IsPartOf("Hythloth") || r.IsPartOf("Shame") || r.IsPartOf("Covetous") || r.IsPartOf("Wrong"))
                return true;

            return (r.IsPartOf("Deceit") || r.IsPartOf("Despise") || r.IsPartOf("Destard") || r.IsPartOf("Hythloth") || r.IsPartOf("Shame") || r.IsPartOf("Covetous") || r.IsPartOf("Wrong"));
        }

        public static void HandleKill(Mobile victim, Mobile killer)
        {
            PlayerMobile pm = killer as PlayerMobile;
            BaseCreature bc = victim as BaseCreature;

            if (!Enabled || pm == null || bc == null || !CheckLocation(bc) || !CheckLocation(pm) || !killer.InRange(victim, 18))
                return;

            if (bc.Controlled || bc.Owners.Count > 0 || bc.Fame <= 0)
                return;

            //25000 for 1/100 chance, 10 hyrus
            //1500, 1/1000 chance, 20 lizard men for that chance.

            pm.VASTotalMonsterFame += (int)(bc.Fame * (1 + Math.Sqrt(pm.Luck) / 100));

            //This is the Exponentional regression with only 2 datapoints.
            //A log. func would also work, but it didn't make as much sense.
            //This function isn't OSI exact beign that I don't know OSI's func they used ;p
            int x = pm.VASTotalMonsterFame;

            //const double A = 8.63316841 * Math.Pow( 10, -4 );
            const double A = 0.000000316841;
            //const double B = 4.25531915 * Math.Pow( 10, -6 );
            const double B = 0.00000000531915;

            double chance = A * Math.Pow(10, B * x);

            if (chance > Utility.RandomDouble())
            {
                Item i = null;

                try
                {
                    i = Activator.CreateInstance(m_VirtueArtifacts[Utility.Random(m_VirtueArtifacts.Length)]) as Item;
                }
                catch
                { }

                if (i != null)
                {
                    if (pm.AddToBackpack(i))
                    {
                        pm.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                        pm.VASTotalMonsterFame = 0;
                    }
                    else if (pm.BankBox != null && pm.BankBox.TryDropItem(pm, i, false))
                    {
                        pm.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
                        pm.SendLocalizedMessage(1072224); // An item has been placed in your bank box.
                        pm.VASTotalMonsterFame = 0;
                    }
                    else
                    {
                        i.MoveToWorld(pm.Location, pm.Map);
                        pm.SendLocalizedMessage(1072523); // You find an artifact, but your backpack and bank are too full to hold it.
                        pm.VASTotalMonsterFame = 0;
                    }
                }
            }
        }
    }
}