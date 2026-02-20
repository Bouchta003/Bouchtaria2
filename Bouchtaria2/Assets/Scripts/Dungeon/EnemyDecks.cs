using System.Collections.Generic;
using UnityEngine;

public static class EnemyDecks
{
    private static readonly Dictionary<int, List<int>> FloorDecks =
        new Dictionary<int, List<int>>
        {
            { 1, new List<int>
                {
                // Pokemon tempo starters and utility items
                // Core card preview: Mudkip (packable).
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
                40, 40, 52, 52, 54, 54, 55, 55, 56, 56,
                57, 57, 87, 87, 112, 112, 115, 115, 117, 117
                
                }
            },
            { 2, new List<int>
                {
                // Monster Hunter midrange with hunters and parts
                // Core card preview: Alma (packable).
                5, 5, 6, 6, 9, 9, 30, 30, 32, 32,
                34, 34, 36, 36, 37, 37, 38, 38, 103, 103,
                104, 104, 119, 119, 153, 153, 154, 154, 3, 3
                
                }
            },
             { 3, new List<int>
                {
                // Faith value engine with draw, armor and sustain
                // Core card preview: Hijabi (packable).
                63, 63, 64, 64, 65, 65, 66, 66, 67, 67,
                68, 68, 106, 106, 107, 107, 108, 108, 109, 109,
                110, 110, 111, 111, 145, 145, 162, 162, 58, 58
                
                }
            },
            { 4, new List<int>
                {
                // Avatar elemental tempo with fire and support
                // Core card preview: Appa (packable).
                83, 83, 46, 46, 84, 84, 47, 47, 134, 134,
                48, 48, 135, 135, 49, 49, 136, 136, 51, 51,
                137, 137, 52, 52, 138, 138, 53, 53, 155, 155
                
                }
            },
            { 5, new List<int>
                {
                // Faith core with chaos splash finishers
                // Core card preview: Seeker of Ilm (packable).
                109, 109, 126, 126, 110, 110, 128, 128, 111, 111,
                129, 129, 145, 145, 130, 130, 162, 162, 132, 132,
                58, 58, 133, 133, 60, 60, 157, 157, 88, 88
                
                }
            },
            { 6, new List<int>
                {
                // PURE MonsterHunter midrange that curves hunters into efficient removal and board locks.
                // Core card preview: Seikret (packable) to tutor hunters and stabilize hand quality.
                153, 153, 3, 3, 4, 4, 5, 5, 6, 6,
                9, 9, 30, 30, 32, 32, 34, 34, 36, 36,
                37, 37, 38, 38, 103, 103, 104, 104, 119, 119
                
                }
            },
            { 7, new List<int>
                {
                // Gunner ambush list that recycles summons and never runs out of gas.
                // Core card preview: Rocket Raccoon (packable); extra draw from Frog, Palico and Trapwire.
                161, 161, 95, 95, 51, 51, 131, 131, 90, 90,
                1, 1, 92, 92, 69, 69, 160, 160, 71, 71,
                89, 89, 3, 3, 98, 98, 99, 99, 97, 97
                


                }
            },
            { 8, new List<int>
                {
                // Chaos casino deck with random payoffs
                // Core card preview: Boyd (packable).
                157, 157, 88, 88, 54, 54, 158, 158, 89, 89,
                55, 55, 85, 85, 141, 141, 56, 56, 120, 120,
                142, 142, 57, 57, 126, 126, 114, 114, 128, 128
                
                
                }
            },
            { 9, new List<int>
                {
                // Pokemon control that chains sleep tempo and closes with cosmic legendaries.
                // Core card preview: Darkrai (packable); legendaries Dialga + Palkia are unpackable bosses.
                55, 55, 54, 54, 56, 56, 57, 57, 87, 87,
                112, 112, 115, 115, 117, 117, 120, 120, 133, 133,
                144, 144, 146, 146, 52, 52, 71, 71, 72, 72
                
                }
            },
            { 10, new List<int>
                {
                // Pokemon legends with one cosmic finisher package
                // Core card preview: Wigglytuff (packable).
                56, 56, 27, 27, 57, 57, 87, 87, 112, 112,
                115, 115, 117, 117, 120, 120, 133, 133, 144, 144,
                146, 146, 147, 147, 148, 148, 149, 149, 150, 150
                
                }
            },
            { 11, new List<int>
                {
                // Monster Hunter elder dragon pressure
                // Core card preview: Stygian Zinogre (packable).
                103, 103, 101, 101, 104, 104, 119, 119, 153, 153,
                154, 154, 3, 3, 4, 4, 5, 5, 6, 6,
                9, 9, 30, 30, 32, 32, 34, 34, 36, 36,
                80, 80, 83, 83, 109, 109, 145, 145, 120, 120,
                133, 133, 97, 97, 88, 88, 89, 89, 59, 59
                
                }
            },
            { 12, new List<int>
                {
                // Aggro Gunner deck that keeps pressure with pings, quickstrikes and refill turns.
                // Core card preview: Deadpool (packable) backed by draw engines Palico/Frog/Trapwire.
                97, 97, 74, 74, 3, 3, 89, 89, 98, 98,
                95, 95, 99, 99, 131, 131, 119, 119, 1, 1,
                143, 143, 69, 69, 151, 151, 160, 160, 72, 72,
                159, 159, 83, 83, 103, 103, 109, 109, 145, 145,
                120, 120, 133, 133, 153, 153, 88, 88, 59, 59
                
                }
            },
            { 13, new List<int>
                {
                // Avatar masters with awakened avatars
                // Core card preview: Katara (packable).
                80, 80, 74, 74, 99, 99, 81, 81, 112, 112,
                82, 82, 139, 139, 83, 83, 146, 146, 84, 84,
                147, 147, 134, 134, 152, 152, 135, 135, 8, 8,
                103, 103, 109, 109, 145, 145, 120, 120, 133, 133,
                153, 153, 97, 97, 88, 88, 89, 89, 59, 59
                
                }
            },
            { 14, new List<int>
                {
                // Constand chaos feeding
                // Core card preview: Feed the chaos (packable).
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 127, 127
                
                }
            },
            { 15, new List<int>
                {
                // Faith control with silence windows, hand refill and hard removals.
                // Core card preview: The Old Preacher (packable) with No More Music and Last Hope finish.
                145, 145, 70, 70, 116, 116, 58, 58, 61, 61,
                63, 63, 64, 64, 65, 65, 66, 66, 67, 67,
                68, 68, 106, 106, 107, 107, 108, 108, 109, 109,
                110, 110, 111, 111, 120, 120, 133, 133, 153, 153,
                97, 97, 88, 88, 89, 89, 59, 59, 141, 141
                
                }
            },
            { 16, new List<int>
                {
                // Speedster race deck with evolved ace
                // Core card preview: Twice (packable).
                1, 1, 70, 70, 21, 21, 90, 90, 120, 120,
                69, 69, 92, 92, 133, 133, 71, 71, 93, 93,
                144, 144, 72, 72, 95, 95, 146, 146, 73, 73,
                97, 97, 153, 153, 88, 88, 89, 89, 59, 59,
                141, 141, 142, 142, 51, 51, 52, 52, 53, 53
                
                }
            },
            { 17, new List<int>
                {
                // Pokemon relic toolbox that curves items into heavy legendary inevitability.
                // Core card preview: Abracada-Hoopa (packable); legendaries Reshiram + Zekrom are unpackable.
                133, 133, 116, 116, 118, 118, 144, 144, 146, 146,
                147, 147, 148, 148, 149, 149, 150, 150, 156, 156,
                120, 120, 87, 87, 55, 55, 52, 52, 57, 57,
                153, 153, 97, 97, 88, 88, 89, 89, 59, 59,
                141, 141, 142, 142, 51, 51, 53, 53, 69, 69
                
                }
            },
            { 18, new List<int>
                {
                // Monster gear-control with big bodies
                // Core card preview: Alma (packable).
                5, 5, 100, 100, 102, 102, 93, 93, 6, 6,
                95, 95, 9, 9, 97, 97, 30, 30, 98, 98,
                32, 32, 99, 99, 34, 34, 119, 119, 36, 36,
                143, 143, 153, 153, 88, 88, 89, 89, 59, 59,
                141, 141, 142, 142, 51, 51, 52, 52, 53, 53
                
                }
            },
            { 19, new List<int>
                {
                // Pokemon value-flood deck that abuses gear chains and late legendary resets.
                // Core card preview: Revive (packable); bosses are Ho-Oh + Dialga (both unpackable).
                146, 146, 27, 27, 163, 163, 147, 147, 148, 148,
                149, 149, 150, 150, 156, 156, 0, 0, 16, 16,
                19, 19, 22, 22, 25, 25, 26, 26, 55, 55,
                56, 56, 97, 97, 88, 88, 89, 89, 59, 59,
                141, 141, 142, 142, 51, 51, 52, 52, 53, 53
                
                }
            },
            { 20, new List<int>
                {
                // Monster Hunter expedition list with direct damage support and resilient bodies.
                // Core card preview: Rathalos (packable) plus Seikret tutoring and hunter burn package.
                103, 103, 105, 105, 113, 113, 140, 140, 104, 104,
                153, 153, 154, 154, 3, 3, 4, 4, 5, 5,
                6, 6, 9, 9, 30, 30, 32, 32, 34, 34,
                36, 36, 37, 37, 38, 38, 88, 88, 89, 89,
                59, 59, 141, 141, 142, 142, 51, 51, 184, 184
                
                
                }
            },
            { 21, new List<int>
                {
                // Faith long game with resilient board
                // Core card preview: Hijab (packable).
                64, 64, 50, 50, 127, 127, 113, 113, 44, 44,
                65, 65, 45, 45, 66, 66, 46, 46, 67, 67,
                47, 47, 68, 68, 48, 48, 106, 106, 49, 49,
                107, 107, 51, 51, 108, 108, 52, 52, 109, 109,
                53, 53, 110, 110, 71, 71, 111, 111, 93, 93
                
                }
            },
            { 22, new List<int>
                {
                // Avatar nations war of attrition
                // Core card preview: Iroh (packable).
                138, 138, 75, 75, 78, 78, 140, 140, 45, 45,
                99, 99, 155, 155, 46, 46, 119, 119, 53, 53,
                47, 47, 143, 143, 80, 80, 48, 48, 151, 151,
                81, 81, 49, 49, 158, 158, 82, 82, 51, 51,
                159, 159, 83, 83, 52, 52, 160, 160, 84, 84
                
                }
            },
            { 23, new List<int>
                {
                // Chaos-control with theft and disruption
                // Core card preview: Le Bens (packable).
                128, 128, 127, 127, 163, 163, 178, 178, 134, 134,
                142, 142, 150, 150, 129, 129, 59, 59, 88, 88,
                156, 156, 130, 130, 131, 131, 89, 89, 0, 0,
                132, 132, 141, 141, 16, 16, 133, 133, 19, 19,
                157, 157, 22, 22, 158, 158, 25, 25, 85, 85
                
                }
            },
            { 24, new List<int>
                {
                // Speedster chain attacks and resets
                // Core card preview: Twice (packable).
                1, 1, 70, 70, 96, 96, 178, 178, 143, 143,
                129, 129, 69, 69, 151, 151, 130, 130, 71, 71,
                158, 158, 132, 132, 72, 72, 159, 159, 133, 133,
                73, 73, 160, 160, 157, 157, 91, 91, 161, 161,
                95, 95, 51, 51, 85, 85, 131, 131, 90, 90
                
                }
            },
            { 25, new List<int>
                {
                // Pokemon + neutral utility that catches tempo units and snowballs with legendary stones.
                // Core card preview: Starter Choice (packable); bosses are Reshiram + Zekrom (unpackable).
                0, 0, 116, 116, 118, 118, 179, 179, 180, 180,
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
                40, 40, 52, 52, 54, 54, 55, 55, 56, 56,
                57, 57, 87, 87, 112, 112, 115, 115, 117, 117,
                120, 120, 133, 133, 144, 144, 146, 146, 147, 147
                
                }
            },
            { 26, new List<int>
                {
                // MonsterHunter + Faith defensive crusade
                // Core card preview: Sword'n'Shield Hunter (packable).
                38, 38, 102, 102, 124, 124, 180, 180, 187, 187,
                106, 106, 103, 103, 107, 107, 104, 104, 108, 108,
                119, 119, 109, 109, 153, 153, 110, 110, 154, 154,
                111, 111, 3, 3, 145, 145, 4, 4, 162, 162,
                5, 5, 58, 58, 6, 6, 60, 60, 9, 9
                
                }
            },
            { 27, new List<int>
                {
                // PURE Avatar deck with all nation tools, then neutral removal and sustain glue.
                // Core card preview: Avatar State (unpackable) with Appa and nation blessings as support.
                75, 75, 74, 74, 78, 78, 79, 79, 80, 80,
                81, 81, 82, 82, 83, 83, 84, 84, 134, 134,
                135, 135, 136, 136, 137, 137, 138, 138, 155, 155,
                53, 53, 88, 88, 89, 89, 119, 119, 141, 141,
                142, 142, 143, 143, 151, 151, 158, 158, 69, 69
                
                }
            },
            { 28, new List<int>
                {
                // Gunner control-combo that chips board, draws through traps, then burns face.
                // Core card preview: Trapwire (packable) with Frog/Colonel Whatsapp for sustained draw.
                160, 160, 94, 94, 96, 96, 189, 189, 190, 190,
                161, 161, 158, 158, 51, 51, 90, 90, 92, 92,
                93, 93, 95, 95, 97, 97, 98, 98, 99, 99,
                119, 119, 143, 143, 151, 151, 159, 159, 85, 85,
                89, 89, 3, 3, 141, 141, 142, 142, 157, 157
                
                }
            },
            { 29, new List<int>
                {
                // Avatar fire nation pressure
                // Core card preview: Lion Turtle's Blessing (packable).
                84, 84, 74, 74, 79, 79, 190, 190, 199, 199,
                131, 131, 161, 161, 134, 134, 132, 132, 51, 51,
                135, 135, 90, 90, 136, 136, 59, 59, 92, 92,
                137, 137, 93, 93, 138, 138, 95, 95, 155, 155,
                97, 97, 53, 53, 98, 98, 80, 80, 99, 99
                
                }
            },
            { 30, new List<int>
                {
                // PURE Speedster deck with constant initiative, then neutral tools for reach.
                // Core card preview: Nuke (packable) and backup finishers from speed legends.
                1, 1, 70, 70, 96, 96, 2, 2, 21, 21,
                199, 199, 69, 69, 71, 71, 72, 72, 73, 73,
                91, 91, 95, 95, 131, 131, 88, 88, 89, 89,
                119, 119, 141, 141, 142, 142, 143, 143, 151, 151,
                158, 158, 159, 159, 160, 160, 161, 161, 164, 164
                
                }
            },
            { 31, new List<int>
                {
                // Chaos fiesta with prankster tools
                // Core card preview: Metronome (packable).
                120, 120, 127, 127, 200, 200, 207, 207, 212, 212,
                221, 221, 131, 131, 90, 90, 126, 126, 1, 1,
                92, 92, 128, 128, 69, 69, 93, 93, 129, 129,
                71, 71, 95, 95, 130, 130, 72, 72, 97, 97,
                132, 132, 73, 73, 98, 98, 133, 133, 91, 91
                
                }
            },
            { 32, new List<int>
                {
                // Full Gear expedition deck: equip hunters, protect carriers, then grind value.
                // Core card preview: Seikret (packable) tutoring hunters and wearing key gear pieces.
                153, 153, 207, 207, 212, 212, 221, 221, 228, 228,
                27, 27, 3, 3, 4, 4, 5, 5, 6, 6,
                9, 9, 30, 30, 32, 32, 34, 34, 36, 36,
                37, 37, 38, 38, 103, 103, 104, 104, 119, 119,
                64, 64, 141, 141, 142, 142, 143, 143, 147, 147
                
                }
            },
            { 33, new List<int>
                {
                // Faith-Avatar harmony deck
                // Core card preview: Warrior Gratittude (packable).
                162, 162, 75, 75, 50, 50, 212, 212, 221, 221,
                228, 228, 137, 137, 112, 112, 58, 58, 138, 138,
                139, 139, 60, 60, 155, 155, 146, 146, 61, 61,
                53, 53, 147, 147, 63, 63, 80, 80, 152, 152,
                64, 64, 81, 81, 8, 8, 65, 65, 62, 62
                
                
                }
            },
            { 34, new List<int>
                {
                // Hybrid legends and hunters
                // Core card preview: Darkrai (packable).
                55, 55, 27, 27, 101, 101, 221, 221, 228, 228,
                74, 74, 5, 5, 56, 56, 6, 6, 57, 57,
                9, 9, 87, 87, 30, 30, 112, 112, 32, 32,
                115, 115, 34, 34, 117, 117, 36, 36, 120, 120,
                37, 37, 133, 133, 38, 38, 144, 144, 103, 103
                
                }
            },
            { 35, new List<int>
                {
                // Speedster vs control anti-spell plan
                // Core card preview: Knuckles (packable).
                72, 72, 70, 70, 94, 94, 228, 228, 27, 27,
                101, 101, 74, 74, 134, 134, 97, 97, 73, 73,
                59, 59, 98, 98, 91, 91, 131, 131, 99, 99,
                95, 95, 132, 132, 119, 119, 143, 143, 1, 1,
                151, 151, 69, 69, 158, 158, 71, 71, 159, 159
                
                }
            },
            { 36, new List<int>
                {
                // Healer + Gunner skirmish sustain
                // Core card preview: Leftovers (packable).
                147, 147, 140, 140, 96, 96, 27, 27, 101, 101,
                74, 74, 94, 94, 98, 98, 152, 152, 99, 99,
                8, 8, 119, 119, 42, 42, 143, 143, 44, 44,
                151, 151, 45, 45, 158, 158, 46, 46, 159, 159,
                47, 47, 160, 160, 48, 48, 161, 161, 49, 49
                
                }
            },
            { 37, new List<int>
                {
                // Pokemon rainbow toolbox that rotates utility stones into explosive boss turns.
                // Core card preview: Ditto (packable); bosses are Ho-Oh + Palkia (both unpackable).
                87, 87, 163, 163, 28, 28, 101, 101, 74, 74,
                94, 94, 127, 127, 112, 112, 115, 115, 117, 117,
                120, 120, 133, 133, 144, 144, 146, 146, 147, 147,
                148, 148, 149, 149, 150, 150, 156, 156, 0, 0,
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26
                
                }
            },
            { 38, new List<int>
                {
                // Monster swarm with elder closers
                // Core card preview: Rathian (packable).
                32, 32, 11, 11, 14, 14, 74, 74, 94, 94,
                127, 127, 70, 70, 8, 8, 34, 34, 42, 42,
                36, 36, 44, 44, 37, 37, 45, 45, 38, 38,
                46, 46, 103, 103, 47, 47, 104, 104, 48, 48,
                119, 119, 49, 49, 153, 153, 51, 51, 154, 154
                
                }
            },
            { 39, new List<int>
                {
                // Faith combo chain with chaos spice
                // Core card preview: Armor Clad Faith (packable).
                65, 65, 127, 127, 50, 50, 94, 94, 70, 70,
                116, 116, 118, 118, 158, 158, 66, 66, 85, 85,
                67, 67, 120, 120, 68, 68, 126, 126, 106, 106,
                128, 128, 107, 107, 129, 129, 108, 108, 130, 130,
                109, 109, 132, 132, 110, 110, 133, 133, 111, 111
                
                }
            },
            { 40, new List<int>
                {
                // Avatar guardians and wise elders
                // Core card preview: Appa (packable).
                83, 83, 74, 74, 75, 75, 127, 127, 70, 70,
                116, 116, 118, 118, 100, 100, 44, 44, 84, 84,
                45, 45, 134, 134, 46, 46, 135, 135, 47, 47,
                136, 136, 48, 48, 137, 137, 49, 49, 138, 138,
                51, 51, 155, 155, 52, 52, 53, 53, 80, 80
                
                }
            },
            { 41, new List<int>
                {
                // Gunner tactical operations
                // Core card preview: Jour De Fete (packable).
                158, 158, 94, 94, 96, 96, 70, 70, 116, 116,
                118, 118, 100, 100, 102, 102, 89, 89, 69, 69,
                159, 159, 141, 141, 71, 71, 160, 160, 142, 142,
                72, 72, 161, 161, 88, 88, 73, 73, 51, 51,
                91, 91, 90, 90, 95, 95, 92, 92, 131, 131
                
                }
            },
            { 42, new List<int>
                {
                // Pokemon tempo-control using chaos disruption while legendary dragons close games.
                // Core card preview: Abracada-Hoopa (packable); bosses are Reshiram + Zekrom unpackable.
                133, 133, 116, 116, 118, 118, 100, 100, 102, 102,
                105, 105, 113, 113, 140, 140, 144, 144, 146, 146,
                147, 147, 148, 148, 149, 149, 150, 150, 156, 156,
                120, 120, 0, 0, 16, 16, 19, 19, 22, 22,
                25, 25, 26, 26, 40, 40, 52, 52, 54, 54
                
                }
            },
            { 43, new List<int>
                {
                // MonsterHunter arena duelists
                // Core card preview: Stygian Zinogre (packable).
                103, 103, 100, 100, 105, 105, 118, 118, 102, 102,
                113, 113, 140, 140, 163, 163, 72, 72, 104, 104,
                73, 73, 119, 119, 91, 91, 153, 153, 95, 95,
                154, 154, 131, 131, 3, 3, 1, 1, 4, 4,
                69, 69, 5, 5, 71, 71, 6, 6, 9, 9
                
                }
            },
            { 44, new List<int>
                {
                // Healer feast and leftovers engine
                // Core card preview: Invisible Girl (packable).
                48, 48, 50, 50, 113, 113, 100, 100, 102, 102,
                105, 105, 140, 140, 163, 163, 146, 146, 49, 49,
                147, 147, 51, 51, 148, 148, 52, 52, 149, 149,
                53, 53, 150, 150, 71, 71, 156, 156, 93, 93,
                0, 0, 99, 99, 16, 16, 112, 112, 19, 19
                
                }
            },
            { 45, new List<int>
                {
                // Chaos metronome carnival
                // Core card preview: Confusing Weapon (packable).
                130, 130, 127, 127, 102, 102, 105, 105, 113, 113,
                140, 140, 163, 163, 178, 178, 179, 179, 147, 147,
                132, 132, 148, 148, 133, 133, 149, 149, 157, 157,
                150, 150, 158, 158, 156, 156, 85, 85, 0, 0,
                120, 120, 16, 16, 126, 126, 19, 19, 128, 128
                
                }
            },
            { 46, new List<int>
                {
                // Faith fortress with protected board
                // Core card preview: Seeker of Ilm (packable).
                109, 109, 50, 50, 127, 127, 105, 105, 113, 113,
                140, 140, 163, 163, 178, 178, 179, 179, 141, 141,
                148, 148, 110, 110, 142, 142, 149, 149, 111, 111,
                88, 88, 150, 150, 145, 145, 89, 89, 156, 156,
                162, 162, 0, 0, 58, 58, 16, 16, 60, 60
                
                }
            },
            { 47, new List<int>
                {
                // Avatar and speed blitz
                // Core card preview: I am Melon Lord (packable).
                155, 155, 78, 78, 79, 79, 113, 113, 140, 140,
                163, 163, 178, 178, 179, 179, 180, 180, 131, 131,
                149, 149, 53, 53, 1, 1, 150, 150, 80, 80,
                69, 69, 156, 156, 81, 81, 71, 71, 0, 0,
                82, 82, 72, 72, 16, 16, 83, 83, 73, 73
                
                }
            },
            { 48, new List<int>
                {
                // Gunner suppressive fire
                // Core card preview: Sage (packable).
                93, 93, 94, 94, 96, 96, 140, 140, 163, 163,
                178, 178, 179, 179, 180, 180, 187, 187, 59, 59,
                150, 150, 95, 95, 131, 131, 156, 156, 97, 97,
                132, 132, 0, 0, 98, 98, 134, 134, 16, 16,
                99, 99, 19, 19, 119, 119, 22, 22, 143, 143
                
                }
            },
            { 49, new List<int>
                {
                // Pokemon capture engine that stalls and then wins through dual time-space bosses.
                // Core card preview: Greatball (packable); bosses are Dialga + Palkia unpackable.
                156, 156, 27, 27, 28, 28, 163, 163, 178, 178,
                179, 179, 180, 180, 187, 187, 189, 189, 0, 0,
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
                40, 40, 52, 52, 54, 54, 55, 55, 56, 56,
                57, 57, 87, 87, 112, 112, 115, 115, 117, 117
                
                }
            },
            { 50, new List<int>
                {
                // Monster expedition with carved parts
                // Core card preview: Alma (packable).
                5, 5, 121, 121, 122, 122, 178, 178, 179, 179,
                180, 180, 187, 187, 189, 189, 190, 190, 0, 0,
                6, 6, 16, 16, 9, 9, 19, 19, 30, 30,
                22, 22, 32, 32, 25, 25, 34, 34, 26, 26,
                36, 36, 40, 40, 37, 37, 52, 52, 38, 38
                
                }
            },
        };

    public static List<int> GetFloorDeck(int floor)
    {
        if (FloorDecks.TryGetValue(floor, out List<int> deck))
            return deck;

        int randomFloor = UnityEngine.Random.Range(1, FloorDecks.Count + 1);
        return FloorDecks[randomFloor];
    }

    // Future expansion: add more unique packable Gunner/Speedster and pure Chaos control cards to reduce cross-trait overlap in late-floor 50-card decks.
}
