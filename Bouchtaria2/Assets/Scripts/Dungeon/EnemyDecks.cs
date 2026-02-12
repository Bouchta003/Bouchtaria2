using System.Collections.Generic;
using UnityEngine;

public static class EnemyDecks
{
    private static readonly Dictionary<int, List<int>> FloorDecks =
        new Dictionary<int, List<int>>
        { 
            {1,new List<int>
                {
                0,0,        // Starter Choice
                46,46,      // Faust flower
                19,19,      // Chimchar
                58,58,      // NoMusic

                49,49,      // IO
                120,120,    // Metronome
                40,40,      // Beldum

                54,54,      // Dormis
                55,55,      // Darkrai
                56,56,      // Wigglytuff
                57,57,      // Snorlax

                116,118,    // Reshiram et Zekrom
                88,88,      // Rainbow Card
                89,89,      // Frog
                133,133     // Hoopa portal
                }
            },
            { 2,new List<int>
                {
                3,3,        // Palico
                4,4,        // Gemma
                5,5,        // Alma

                34,34,      // Balahara (packable)
                9,9,        // Odogaron (packable)
                30,30,      // Rathalos (packable)
                32,32,      // Rathian (packable)

                36,36,      // Greatsword Hunter (packable)
                37,37,      // Insectglaive Hunter (packable)
                38,38,      // Sword'n'Shield Hunter (packable)
                103,103,    // Stygian Zinogre (packable)

                104,104,    // Rey Dau (packable)
                129,129,    // Thief's gloves (packable, value/gear)
                101,101,    // Alatreon (non-packable, heavy finisher)
                105,105     // Arkveld (non-packable, strong MonsterPart synergy)
                }
            },{ 3,new List<int>
                {
                // ===== CHAOS CORE (all available Chaos cards) =====
                126,126,      // Colonel Whatsapp (Chaos – draw engine)
                127,127,      // Giratina Origin (Chaos – UNPACKABLE finisher)

                // ===== FAITH DRAW & VALUE =====
                129,129,      // Duaa (Faith – discover Faith)
                130,130,      // Sadaqa (Faith – draw + heal)

                109,109,    // Seeker of Ilm (Faith – repeat draw)
                67,67,      // Awrah Man (Faith – tempo draw)

                // ===== FAITH BOARD CONTROL =====
                58,58,      // No More Music ! (Faith – silence all enemies)
                68,68,      // Dans l'din (Faith – burn + discover)

                // ===== FAITH DEFENSIVE CORE =====
                63,63,      // Hijabi (Faith – Blessed body)
                64,64,      // Hijab (Faith – Blessed gear)

                65,65,      // Armor Clad Faith (Faith – armor + summon)
                66,66,      // Potemslim (Faith – Protect + Blessed)

                // ===== FAITH LATE GAME =====
                108,108,    // Guardian of Niyyah (Faith – sustain engine)
                106,106,    // Bearer of Sabr (Faith – resilient absorber)

                110,110     // Voice Of Dhikr (Faith – scaling finisher)
                }
            }
        };
    public static List<int> GetFloorDeck(int floor)
    {
        if (FloorDecks.TryGetValue(floor, out List<int> deck))
            return deck;

        return FloorDecks.GetValueOrDefault(0);
    }
}
