using System.Collections.Generic;
using UnityEngine;

public static class EnemyDecks
{
    private static readonly Dictionary<int, List<int>> FloorDecks =
        new Dictionary<int, List<int>>
        {
            { 1, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck.
                // Core card preview: Easy Encounter (packable).
                25, 25, 26, 26, 54, 54, 120, 120, 0, 0,
                40, 40, 91, 91, 147, 147, 148, 148, 1, 1,
                16, 16, 19, 19, 22, 22, 71, 71, 72, 72
                
                }
            },
            { 2, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck.
                // Core card preview: Slingshot (packable).
                119, 119, 90, 90, 3, 3, 6, 6, 72, 72,
                175, 175, 4, 4, 5, 5, 34, 34, 153, 153,
                165, 165, 171, 171, 173, 173, 174, 174, 186, 186
                
                }
            },
            { 3, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck.
                // Core card preview: Lasagna Lunch (packable).
                44, 44, 60, 60, 61, 61, 64, 64, 147, 147,
                8, 8, 47, 47, 48, 48, 53, 53, 63, 63,
                71, 71, 109, 109, 112, 112, 146, 146, 162, 162
                
                }
            },
            { 4, new List<int>
                {
                // Pure theme: Pure Pokemon ladder deck.
                // Core card preview: Easy Encounter (packable).
                25, 25, 26, 26, 54, 54, 120, 120, 0, 0,
                40, 40, 147, 147, 148, 148, 16, 16, 19, 19,
                22, 22, 112, 112, 133, 133, 146, 146, 149, 149
                
                }
            },
            { 5, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck.
                // Core card preview: Lion Turtle's Blessing (packable).
                84, 84, 53, 53, 137, 137, 201, 201, 230, 230,
                80, 80, 82, 82, 134, 134, 193, 193, 81, 81,
                184, 184, 194, 194, 196, 196, 197, 197, 83, 83
                
                }
            },
            { 6, new List<int>
                {
                // Mixed theme: Mixed Chaos-Gunner synergy deck.
                // Core card preview: Jour De Fete (packable).
                158, 158, 119, 119, 120, 120, 90, 90, 126, 126,
                159, 159, 160, 160, 129, 129, 130, 130, 132, 132,
                133, 133, 164, 164, 51, 51, 99, 99, 143, 143
                
                }
            },
            { 7, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Healer synergy deck.
                // Core card preview: Leftovers (packable).
                147, 147, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 44, 44, 148, 148, 8, 8,
                16, 16, 112, 112, 146, 146, 52, 52, 210, 210
                
                }
            },
            { 8, new List<int>
                {
                // Pure theme: Pure MonsterHunter ladder deck.
                // Core card preview: Slingshot (packable).
                119, 119, 3, 3, 6, 6, 4, 4, 5, 5,
                34, 34, 153, 153, 36, 36, 37, 37, 38, 38,
                9, 9, 30, 30, 32, 32, 103, 103, 104, 104
                
                }
            },
            { 9, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Avatar synergy deck.
                // Core card preview: Slingshot (packable).
                119, 119, 84, 84, 3, 3, 6, 6, 53, 53,
                137, 137, 4, 4, 5, 5, 34, 34, 80, 80,
                82, 82, 134, 134, 153, 153, 36, 36, 37, 37
                
                }
            },
            { 10, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck.
                // Core card preview: Yoru Clone (unpackable).
                96, 96, 119, 119, 90, 90, 91, 91, 159, 159,
                160, 160, 1, 1, 71, 71, 72, 72, 132, 132,
                164, 164, 51, 51, 69, 69, 73, 73, 95, 95
                
                }
            },
            { 11, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck.
                // Core card preview: Easy Encounter (packable).
                25, 25, 26, 26, 54, 54, 62, 62, 120, 120,
                0, 0, 40, 40, 60, 60, 61, 61, 64, 64,
                147, 147, 148, 148, 16, 16, 19, 19, 22, 22,
                63, 63, 109, 109, 112, 112, 133, 133, 146, 146,
                149, 149, 162, 162, 175, 175, 202, 202, 216, 216
                
                }
            },
            { 12, new List<int>
                {
                // Pure theme: Pure Faith ladder deck.
                // Core card preview: Duaa (packable).
                60, 60, 113, 113, 119, 119, 120, 120, 61, 61,
                64, 64, 147, 147, 53, 53, 63, 63, 71, 71,
                109, 109, 162, 162, 67, 67, 68, 68, 145, 145,
                58, 58, 65, 65, 66, 66, 107, 107, 108, 108,
                110, 110, 114, 114, 111, 111, 106, 106, 229, 229
                
                }
            },
            { 13, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 59, 59, 119, 119, 120, 120, 88, 88,
                126, 126, 142, 142, 129, 129, 130, 130, 132, 132,
                133, 133, 201, 201, 202, 202, 141, 141, 166, 166,
                208, 208, 85, 85, 89, 89, 157, 157, 191, 191,
                131, 131, 158, 158, 188, 188, 128, 128, 205, 205
                
                }
            },
            { 14, new List<int>
                {
                // Pure theme: Pure Chaos gimmick flood (Feed The Chaos).
                // Core card preview: Feed the chaos (packable).
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 7, 7
                
                }
            },
            { 15, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 44, 44, 84, 84, 147, 147, 8, 8,
                47, 47, 48, 48, 53, 53, 71, 71, 112, 112,
                137, 137, 146, 146, 42, 42, 49, 49, 51, 51,
                52, 52, 80, 80, 82, 82, 99, 99, 134, 134,
                152, 152, 166, 166, 210, 210, 45, 45, 24, 24
                
                }
            },
            { 16, new List<int>
                {
                // Pure theme: Pure Avatar ladder deck.
                // Core card preview: Aang (unpackable).
                74, 74, 119, 119, 120, 120, 84, 84, 147, 147,
                53, 53, 71, 71, 112, 112, 132, 132, 133, 133,
                137, 137, 146, 146, 201, 201, 75, 75, 80, 80,
                82, 82, 134, 134, 81, 81, 83, 83, 176, 176,
                177, 177, 138, 138, 135, 135, 136, 136, 155, 155
                
                }
            },
            { 17, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 59, 59, 119, 119, 88, 88,
                142, 142, 3, 3, 6, 6, 201, 201, 202, 202,
                4, 4, 5, 5, 34, 34, 141, 141, 153, 153,
                208, 208, 36, 36, 37, 37, 38, 38, 89, 89,
                9, 9, 30, 30, 32, 32, 103, 103, 104, 104
                
                }
            },
            { 18, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck.
                // Core card preview: Infernape (unpackable).
                21, 21, 25, 25, 26, 26, 54, 54, 113, 113,
                120, 120, 0, 0, 40, 40, 91, 91, 147, 147,
                148, 148, 1, 1, 16, 16, 19, 19, 22, 22,
                71, 71, 72, 72, 112, 112, 132, 132, 133, 133,
                146, 146, 149, 149, 175, 175, 202, 202, 216, 216
                
                }
            },
            { 19, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 119, 119, 90, 90, 3, 3,
                6, 6, 72, 72, 175, 175, 4, 4, 5, 5,
                34, 34, 153, 153, 165, 165, 171, 171, 173, 173,
                174, 174, 186, 186, 209, 209, 36, 36, 37, 37,
                38, 38, 45, 45, 184, 184, 66, 66, 98, 98
                
                }
            },
            { 20, new List<int>
                {
                // Pure theme: Pure Gunner ladder deck.
                // Core card preview: Sage Wall (unpackable).
                94, 94, 113, 113, 119, 119, 120, 120, 90, 90,
                147, 147, 159, 159, 160, 160, 53, 53, 96, 96,
                164, 164, 51, 51, 99, 99, 143, 143, 151, 151,
                165, 165, 172, 172, 173, 173, 93, 93, 95, 95,
                97, 97, 161, 161, 92, 92, 98, 98, 158, 158
                
                }
            },
            { 21, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 44, 44, 60, 60, 61, 61, 64, 64,
                140, 140, 147, 147, 8, 8, 47, 47, 48, 48,
                50, 50, 53, 53, 63, 63, 71, 71, 109, 109,
                112, 112, 146, 146, 162, 162, 42, 42, 49, 49,
                51, 51, 52, 52, 67, 67, 68, 68, 99, 99
                
                }
            },
            { 22, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck.
                // Core card preview: Aang (unpackable).
                74, 74, 84, 84, 53, 53, 137, 137, 201, 201,
                230, 230, 75, 75, 80, 80, 82, 82, 134, 134,
                193, 193, 81, 81, 184, 184, 194, 194, 196, 196,
                197, 197, 83, 83, 176, 176, 177, 177, 195, 195,
                78, 78, 192, 192, 198, 198, 138, 138, 135, 135
                
                }
            },
            { 23, new List<int>
                {
                // Mixed theme: Mixed Chaos-Gunner synergy deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 119, 119, 120, 120, 90, 90, 126, 126,
                159, 159, 160, 160, 94, 94, 96, 96, 129, 129,
                130, 130, 132, 132, 133, 133, 164, 164, 51, 51,
                99, 99, 143, 143, 151, 151, 165, 165, 166, 166,
                172, 172, 173, 173, 85, 85, 93, 93, 158, 158
                
                }
            },
            { 24, new List<int>
                {
                // Pure theme: Pure Healer ladder deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 119, 119, 44, 44, 140, 140, 147, 147,
                8, 8, 47, 47, 48, 48, 50, 50, 53, 53,
                71, 71, 112, 112, 146, 146, 42, 42, 49, 49,
                51, 51, 52, 52, 99, 99, 152, 152, 166, 166,
                210, 210, 45, 45, 93, 93, 139, 139, 46, 46
                
                }
            },
            { 25, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 44, 44, 140, 140, 147, 147,
                148, 148, 8, 8, 16, 16, 19, 19, 22, 22,
                47, 47, 48, 48, 50, 50, 53, 53, 71, 71,
                112, 112, 146, 146, 52, 52, 210, 210, 18, 18
                
                }
            },
            { 26, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Avatar synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 119, 119, 13, 13, 31, 31,
                84, 84, 3, 3, 6, 6, 53, 53, 137, 137,
                4, 4, 5, 5, 34, 34, 80, 80, 82, 82,
                134, 134, 153, 153, 36, 36, 37, 37, 38, 38,
                81, 81, 83, 83, 176, 176, 177, 177, 9, 9
                
                }
            },
            { 27, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck.
                // Core card preview: Yoru Clone (unpackable).
                96, 96, 119, 119, 90, 90, 91, 91, 159, 159,
                160, 160, 1, 1, 70, 70, 71, 71, 72, 72,
                94, 94, 132, 132, 164, 164, 51, 51, 69, 69,
                73, 73, 99, 99, 143, 143, 151, 151, 165, 165,
                172, 172, 173, 173, 93, 93, 95, 95, 21, 21
                
                }
            },
            { 28, new List<int>
                {
                // Pure theme: Pure Speedster ladder deck.
                // Core card preview: Spin Dash (unpackable).
                70, 70, 113, 113, 119, 119, 120, 120, 91, 91,
                147, 147, 1, 1, 53, 53, 71, 71, 72, 72,
                96, 96, 112, 112, 132, 132, 133, 133, 146, 146,
                201, 201, 202, 202, 230, 230, 51, 51, 52, 52,
                69, 69, 73, 73, 95, 95, 131, 131, 21, 21
                
                }
            },
            { 29, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 60, 60, 61, 61, 64, 64,
                147, 147, 148, 148, 16, 16, 19, 19, 22, 22,
                63, 63, 109, 109, 112, 112, 133, 133, 146, 146,
                149, 149, 162, 162, 41, 41, 199, 199, 203, 203
                
                }
            },
            { 30, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 59, 59, 120, 120, 88, 88, 126, 126,
                142, 142, 167, 167, 129, 129, 130, 130, 132, 132,
                133, 133, 201, 201, 202, 202, 141, 141, 166, 166,
                189, 189, 190, 190, 208, 208, 85, 85, 89, 89,
                157, 157, 191, 191, 203, 203, 131, 131, 158, 158
                
                }
            },
            { 31, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 44, 44, 84, 84, 140, 140, 147, 147,
                8, 8, 47, 47, 48, 48, 50, 50, 53, 53,
                71, 71, 94, 94, 112, 112, 137, 137, 146, 146,
                42, 42, 49, 49, 51, 51, 52, 52, 74, 74,
                80, 80, 82, 82, 99, 99, 134, 134, 152, 152
                
                }
            },
            { 32, new List<int>
                {
                // Pure theme: Pure Chaos ladder deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 113, 113, 119, 119, 120, 120, 126, 126,
                147, 147, 53, 53, 71, 71, 112, 112, 129, 129,
                130, 130, 132, 132, 133, 133, 146, 146, 166, 166,
                189, 189, 190, 190, 85, 85, 157, 157, 191, 191,
                131, 131, 158, 158, 188, 188, 86, 86, 128, 128
                
                }
            },
            { 33, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 59, 59, 119, 119, 13, 13,
                31, 31, 33, 33, 88, 88, 142, 142, 3, 3,
                6, 6, 201, 201, 202, 202, 4, 4, 5, 5,
                34, 34, 141, 141, 153, 153, 208, 208, 36, 36,
                37, 37, 38, 38, 89, 89, 9, 9, 30, 30
                
                }
            },
            { 34, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck.
                // Core card preview: Infernape (unpackable).
                21, 21, 25, 25, 26, 26, 54, 54, 113, 113,
                120, 120, 0, 0, 40, 40, 91, 91, 147, 147,
                148, 148, 1, 1, 16, 16, 19, 19, 22, 22,
                70, 70, 71, 71, 72, 72, 96, 96, 112, 112,
                132, 132, 133, 133, 146, 146, 149, 149, 41, 41
                
                }
            },
            { 35, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 119, 119, 13, 13, 31, 31,
                33, 33, 35, 35, 90, 90, 3, 3, 6, 6,
                72, 72, 175, 175, 4, 4, 5, 5, 34, 34,
                153, 153, 165, 165, 171, 171, 173, 173, 174, 174,
                186, 186, 209, 209, 36, 36, 37, 37, 38, 38
                
                }
            },
            { 36, new List<int>
                {
                // Pure theme: Pure Pokemon ladder deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 147, 147, 148, 148, 16, 16,
                19, 19, 22, 22, 112, 112, 133, 133, 146, 146,
                149, 149, 175, 175, 202, 202, 216, 216, 219, 219,
                41, 41, 199, 199, 203, 203, 217, 217, 220, 220
                
                }
            },
            { 37, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 44, 44, 60, 60, 61, 61, 64, 64,
                140, 140, 147, 147, 8, 8, 47, 47, 48, 48,
                50, 50, 53, 53, 63, 63, 71, 71, 94, 94,
                96, 96, 109, 109, 112, 112, 146, 146, 162, 162,
                42, 42, 49, 49, 51, 51, 52, 52, 18, 18
                
                }
            },
            { 38, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck.
                // Core card preview: Aang (unpackable).
                74, 74, 84, 84, 53, 53, 137, 137, 201, 201,
                230, 230, 75, 75, 80, 80, 82, 82, 134, 134,
                193, 193, 81, 81, 184, 184, 194, 194, 196, 196,
                197, 197, 83, 83, 176, 176, 177, 177, 195, 195,
                78, 78, 79, 79, 178, 178, 179, 179, 192, 192
                
                }
            },
            { 39, new List<int>
                {
                // Mixed theme: Mixed Chaos-Gunner synergy deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 119, 119, 120, 120, 90, 90, 126, 126,
                159, 159, 160, 160, 94, 94, 96, 96, 129, 129,
                130, 130, 132, 132, 133, 133, 164, 164, 51, 51,
                99, 99, 143, 143, 151, 151, 165, 165, 166, 166,
                172, 172, 189, 189, 190, 190, 158, 158, 86, 86
                
                }
            },
            { 40, new List<int>
                {
                // Pure theme: Pure MonsterHunter ladder deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 119, 119, 120, 120, 13, 13,
                31, 31, 33, 33, 35, 35, 121, 121, 3, 3,
                6, 6, 4, 4, 5, 5, 34, 34, 153, 153,
                36, 36, 37, 37, 38, 38, 9, 9, 30, 30,
                32, 32, 103, 103, 104, 104, 181, 181, 154, 154
                
                }
            },
            { 41, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 44, 44, 140, 140, 147, 147,
                148, 148, 8, 8, 16, 16, 19, 19, 22, 22,
                47, 47, 50, 50, 94, 94, 112, 112, 146, 146,
                52, 52, 210, 210, 41, 41, 199, 199, 18, 18
                
                }
            },
            { 42, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Avatar synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 119, 119, 13, 13, 31, 31,
                33, 33, 35, 35, 84, 84, 121, 121, 3, 3,
                6, 6, 53, 53, 137, 137, 4, 4, 5, 5,
                34, 34, 80, 80, 82, 82, 134, 134, 153, 153,
                36, 36, 37, 37, 38, 38, 81, 81, 83, 83
                
                }
            },
            { 43, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck.
                // Core card preview: Yoru Clone (unpackable).
                96, 96, 113, 113, 119, 119, 90, 90, 91, 91,
                159, 159, 160, 160, 1, 1, 70, 70, 71, 71,
                72, 72, 94, 94, 132, 132, 164, 164, 51, 51,
                69, 69, 73, 73, 99, 99, 143, 143, 151, 151,
                165, 165, 95, 95, 203, 203, 18, 18, 21, 21
                
                }
            },
            { 44, new List<int>
                {
                // Pure theme: Pure Faith ladder deck.
                // Core card preview: Duaa (packable).
                60, 60, 113, 113, 61, 61, 64, 64, 63, 63,
                94, 94, 96, 96, 109, 109, 162, 162, 67, 67,
                68, 68, 145, 145, 58, 58, 203, 203, 65, 65,
                66, 66, 107, 107, 108, 108, 18, 18, 21, 21,
                24, 24, 110, 110, 111, 111, 106, 106, 229, 229
                
                }
            },
            { 45, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 25, 25, 26, 26, 54, 54, 120, 120,
                0, 0, 40, 40, 60, 60, 61, 61, 64, 64,
                147, 147, 148, 148, 16, 16, 19, 19, 22, 22,
                63, 63, 109, 109, 112, 112, 41, 41, 199, 199,
                203, 203, 217, 217, 220, 220, 223, 223, 17, 17
                
                }
            },
            { 46, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck.
                // Core card preview: Cheater's Will (unpackable).
                127, 127, 59, 59, 113, 113, 120, 120, 88, 88,
                126, 126, 142, 142, 167, 167, 129, 129, 130, 130,
                132, 132, 133, 133, 201, 201, 202, 202, 141, 141,
                166, 166, 189, 189, 190, 190, 208, 208, 85, 85,
                89, 89, 157, 157, 203, 203, 204, 204, 86, 86
                
                }
            },
            { 47, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck.
                // Core card preview: Fresh Water (unpackable).
                113, 113, 44, 44, 84, 84, 140, 140, 147, 147,
                8, 8, 47, 47, 48, 48, 50, 50, 53, 53,
                71, 71, 94, 94, 112, 112, 137, 137, 146, 146,
                42, 42, 49, 49, 51, 51, 52, 52, 74, 74,
                75, 75, 80, 80, 82, 82, 78, 78, 79, 79
                
                }
            },
            { 48, new List<int>
                {
                // Pure theme: Pure Avatar ladder deck.
                // Core card preview: Aang (unpackable).
                74, 74, 113, 113, 119, 119, 120, 120, 84, 84,
                147, 147, 53, 53, 137, 137, 75, 75, 80, 80,
                82, 82, 134, 134, 81, 81, 83, 83, 176, 176,
                177, 177, 78, 78, 79, 79, 178, 178, 179, 179,
                180, 180, 138, 138, 135, 135, 136, 136, 155, 155
                
                }
            },
            { 49, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck.
                // Core card preview: Odogaron claws (unpackable).
                10, 10, 39, 39, 59, 59, 119, 119, 13, 13,
                31, 31, 33, 33, 35, 35, 88, 88, 121, 121,
                122, 122, 142, 142, 3, 3, 6, 6, 201, 201,
                202, 202, 4, 4, 5, 5, 34, 34, 141, 141,
                153, 153, 208, 208, 36, 36, 37, 37, 38, 38
                
                }
            },
            { 50, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck.
                // Core card preview: Infernape (unpackable).
                21, 21, 25, 25, 26, 26, 54, 54, 113, 113,
                120, 120, 0, 0, 40, 40, 91, 91, 147, 147,
                148, 148, 1, 1, 16, 16, 19, 19, 22, 22,
                70, 70, 71, 71, 72, 72, 96, 96, 112, 112,
                132, 132, 41, 41, 199, 199, 203, 203, 217, 217
                
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
