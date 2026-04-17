using System.Collections.Generic;
using UnityEngine;

public static class EnemyDecks
{
    private static readonly Dictionary<int, List<int>> FloorDecks =
        new Dictionary<int, List<int>>
        {
            { 1, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                119, 119, 6, 6, 90, 90, 3, 3, 173, 173,
                174, 174, 165, 165, 171, 171, 153, 153, 4, 4,
                5, 5, 186, 186, 175, 175, 34, 34, 72, 72
                }
            },
            { 2, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                25, 25, 147, 147, 0, 0, 19, 19, 40, 40,
                120, 120, 16, 16, 72, 72, 54, 54, 71, 71,
                91, 91, 22, 22, 26, 26, 148, 148, 1, 1
                }
            },
            { 3, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                84, 84, 184, 184, 194, 194, 53, 53, 201, 201,
                197, 197, 193, 193, 134, 134, 81, 81, 230, 230,
                196, 196, 137, 137, 80, 80, 82, 82, 83, 83
                }
            },
            { 4, new List<int>
                {
                // Mixed theme: Mixed Chaos-Gunner synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                158, 158, 126, 126, 159, 159, 164, 164, 119, 119,
                51, 51, 99, 99, 90, 90, 120, 120, 133, 133,
                132, 132, 130, 130, 160, 160, 129, 129, 143, 143
                }
            },
            { 5, new List<int>
                {
                // Pure theme: Pure MonsterHunter ladder deck.
                // Core card preview: first card in deck list.
                119, 119, 104, 104, 6, 6, 5, 5, 153, 153,
                38, 38, 4, 4, 3, 3, 103, 103, 9, 9,
                34, 34, 32, 32, 37, 37, 36, 36, 30, 30
                }
            },
            { 6, new List<int>
                {
                // Pure theme: Pure Pokemon ladder deck.
                // Core card preview: first card in deck list.
                25, 25, 40, 40, 149, 149, 148, 148, 22, 22,
                146, 146, 0, 0, 54, 54, 26, 26, 112, 112,
                133, 133, 19, 19, 16, 16, 120, 120, 147, 147
                }
            },
            { 7, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                44, 44, 109, 109, 64, 64, 63, 63, 60, 60,
                146, 146, 47, 47, 8, 8, 53, 53, 71, 71,
                112, 112, 147, 147, 61, 61, 162, 162, 48, 48
                }
            },
            { 8, new List<int>
                {
                // Mixed theme: Mixed Avatar-MonsterHunter synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                119, 119, 153, 153, 4, 4, 5, 5, 134, 134,
                3, 3, 34, 34, 53, 53, 137, 137, 36, 36,
                6, 6, 84, 84, 80, 80, 37, 37, 82, 82
                }
            },
            { 9, new List<int>
                {
                // Mixed theme: Mixed Healer-Pokemon synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                147, 147, 148, 148, 146, 146, 26, 26, 25, 25,
                112, 112, 210, 210, 16, 16, 120, 120, 8, 8,
                52, 52, 0, 0, 44, 44, 40, 40, 54, 54
                }
            },
            { 10, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                96, 96, 119, 119, 90, 90, 160, 160, 69, 69,
                91, 91, 72, 72, 1, 1, 71, 71, 132, 132,
                95, 95, 51, 51, 159, 159, 73, 73, 164, 164
                }
            },
            { 11, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 103, 103, 39, 39, 5, 5, 202, 202,
                9, 9, 153, 153, 142, 142, 6, 6, 208, 208,
                3, 3, 38, 38, 34, 34, 201, 201, 59, 59,
                32, 32, 104, 104, 36, 36, 89, 89, 37, 37,
                88, 88, 119, 119, 4, 4, 141, 141, 30, 30
                }
            },
            { 12, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 71, 71, 49, 49, 47, 47, 8, 8,
                44, 44, 99, 99, 152, 152, 82, 82, 146, 146,
                147, 147, 84, 84, 45, 45, 52, 52, 210, 210,
                53, 53, 80, 80, 137, 137, 51, 51, 134, 134,
                24, 24, 42, 42, 166, 166, 48, 48, 10, 10
                }
            },
            { 13, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                127, 127, 120, 120, 142, 142, 85, 85, 131, 131,
                205, 205, 208, 208, 201, 201, 128, 128, 188, 188,
                157, 157, 132, 132, 126, 126, 129, 129, 166, 166,
                89, 89, 202, 202, 119, 119, 88, 88, 158, 158,
                133, 133, 141, 141, 59, 59, 191, 191, 130, 130
                }
            },
            { 14, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                25, 25, 60, 60, 64, 64, 0, 0, 19, 19,
                112, 112, 62, 62, 22, 22, 148, 148, 147, 147,
                109, 109, 40, 40, 120, 120, 63, 63, 162, 162,
                54, 54, 202, 202, 26, 26, 16, 16, 133, 133,
                149, 149, 61, 61, 216, 216, 146, 146, 10, 10
                }
            },
            { 15, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 184, 184, 72, 72, 165, 165, 39, 39,
                36, 36, 5, 5, 37, 37, 45, 45, 66, 66,
                175, 175, 90, 90, 153, 153, 3, 3, 119, 119,
                34, 34, 173, 173, 38, 38, 6, 6, 209, 209,
                174, 174, 171, 171, 186, 186, 4, 4, 98, 98
                }
            },
            { 16, new List<int>
                {
                // Pure theme: Pure Avatar ladder deck.
                // Core card preview: first card in deck list.
                74, 74, 112, 112, 53, 53, 81, 81, 138, 138,
                84, 84, 120, 120, 119, 119, 75, 75, 132, 132,
                136, 136, 134, 134, 177, 177, 82, 82, 71, 71,
                147, 147, 135, 135, 137, 137, 146, 146, 176, 176,
                133, 133, 80, 80, 155, 155, 201, 201, 10, 10
                }
            },
            { 17, new List<int>
                {
                // Pure theme: Pure Chaos gimmick flood (Feed The Chaos).
                // Core card preview: first card in deck list.
                126, 126, 7, 7, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 126, 126, 126, 126,
                126, 126, 126, 126, 126, 126, 21, 21, 10, 10
                }
            },
            { 18, new List<int>
                {
                // Pure theme: Pure Faith ladder deck.
                // Core card preview: first card in deck list.
                60, 60, 145, 145, 107, 107, 71, 71, 65, 65,
                109, 109, 53, 53, 64, 64, 61, 61, 120, 120,
                58, 58, 68, 68, 106, 106, 147, 147, 108, 108,
                113, 113, 63, 63, 162, 162, 229, 229, 111, 111,
                110, 110, 119, 119, 66, 66, 67, 67, 10, 10
                }
            },
            { 19, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Avatar synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 84, 84, 53, 53, 119, 119, 34, 34,
                177, 177, 82, 82, 80, 80, 5, 5, 9, 9,
                176, 176, 3, 3, 81, 81, 31, 31, 153, 153,
                134, 134, 38, 38, 6, 6, 36, 36, 39, 39,
                4, 4, 137, 137, 13, 13, 74, 74, 21, 21
                }
            },
            { 20, new List<int>
                {
                // Pure theme: Pure Gunner ladder deck.
                // Core card preview: first card in deck list.
                90, 90, 93, 93, 97, 97, 98, 98, 99, 99,
                143, 143, 151, 151, 159, 159, 160, 160, 161, 161,
                164, 164, 165, 165, 172, 172, 173, 173, 174, 174,
                92, 92, 95, 95, 94, 94, 119, 119, 51, 51,
                10, 10, 21, 21, 39, 39, 74, 74, 96, 96
                }
            },
            { 21, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                74, 74, 198, 198, 82, 82, 197, 197, 83, 83,
                194, 194, 201, 201, 195, 195, 192, 192, 135, 135,
                196, 196, 53, 53, 176, 176, 81, 81, 84, 84,
                137, 137, 138, 138, 177, 177, 184, 184, 80, 80,
                134, 134, 75, 75, 230, 230, 21, 21, 10, 10
                }
            },
            { 22, new List<int>
                {
                // Mixed theme: Mixed Chaos-Gunner synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                127, 127, 165, 165, 166, 166, 164, 164, 119, 119,
                90, 90, 158, 158, 126, 126, 51, 51, 96, 96,
                132, 132, 129, 129, 130, 130, 143, 143, 173, 173,
                159, 159, 93, 93, 133, 133, 85, 85, 94, 94,
                120, 120, 160, 160, 99, 99, 151, 151, 172, 172
                }
            },
            { 23, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 49, 49, 67, 67, 52, 52, 50, 50,
                48, 48, 63, 63, 140, 140, 60, 60, 42, 42,
                53, 53, 51, 51, 68, 68, 8, 8, 147, 147,
                162, 162, 112, 112, 71, 71, 146, 146, 64, 64,
                99, 99, 109, 109, 47, 47, 21, 21, 10, 10
                }
            },
            { 24, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 141, 141, 89, 89, 142, 142, 38, 38,
                36, 36, 39, 39, 9, 9, 5, 5, 59, 59,
                6, 6, 208, 208, 34, 34, 37, 37, 30, 30,
                4, 4, 153, 153, 13, 13, 119, 119, 202, 202,
                33, 33, 201, 201, 94, 94, 74, 74, 21, 21
                }
            },
            { 25, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 40, 40, 47, 47, 53, 53, 26, 26,
                0, 0, 112, 112, 52, 52, 210, 210, 22, 22,
                146, 146, 147, 147, 25, 25, 140, 140, 50, 50,
                19, 19, 16, 16, 148, 148, 120, 120, 44, 44,
                18, 18, 71, 71, 39, 39, 21, 21, 10, 10
                }
            },
            { 26, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 71, 71, 147, 147, 8, 8, 52, 52,
                99, 99, 152, 152, 42, 42, 47, 47, 44, 44,
                146, 146, 112, 112, 80, 80, 134, 134, 74, 74,
                140, 140, 51, 51, 94, 94, 48, 48, 82, 82,
                84, 84, 137, 137, 50, 50, 21, 21, 10, 10
                }
            },
            { 27, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                127, 127, 189, 189, 126, 126, 131, 131, 190, 190,
                85, 85, 208, 208, 130, 130, 133, 133, 141, 141,
                89, 89, 88, 88, 202, 202, 203, 203, 129, 129,
                142, 142, 191, 191, 158, 158, 120, 120, 201, 201,
                132, 132, 74, 74, 39, 39, 21, 21, 10, 10
                }
            },
            { 28, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 22, 22, 26, 26, 60, 60, 61, 61,
                64, 64, 199, 199, 203, 203, 19, 19, 133, 133,
                162, 162, 109, 109, 146, 146, 147, 147, 112, 112,
                63, 63, 120, 120, 16, 16, 148, 148, 54, 54,
                41, 41, 40, 40, 39, 39, 21, 21, 10, 10
                }
            },
            { 29, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Fighter synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 35, 35, 38, 38, 90, 90, 186, 186,
                175, 175, 36, 36, 174, 174, 34, 34, 37, 37,
                3, 3, 209, 209, 33, 33, 153, 153, 5, 5,
                119, 119, 31, 31, 13, 13, 171, 171, 173, 173,
                39, 39, 96, 96, 94, 94, 74, 74, 21, 21
                }
            },
            { 30, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                21, 21, 120, 120, 132, 132, 40, 40, 16, 16,
                1, 1, 22, 22, 113, 113, 96, 96, 54, 54,
                72, 72, 112, 112, 91, 91, 0, 0, 26, 26,
                70, 70, 146, 146, 19, 19, 147, 147, 25, 25,
                71, 71, 41, 41, 133, 133, 39, 39, 10, 10
                }
            },
            { 31, new List<int>
                {
                // Mixed theme: Mixed Avatar-Combo synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                74, 74, 176, 176, 179, 179, 177, 177, 82, 82,
                75, 75, 83, 83, 134, 134, 53, 53, 194, 194,
                80, 80, 196, 196, 84, 84, 195, 195, 78, 78,
                201, 201, 230, 230, 197, 197, 81, 81, 137, 137,
                96, 96, 94, 94, 39, 39, 21, 21, 10, 10
                }
            },
            { 32, new List<int>
                {
                // Pure theme: Pure Inazuma ladder deck.
                // Core card preview: first card in deck list.
                256, 256, 257, 257, 258, 258, 259, 259, 260, 260,
                261, 261, 262, 262, 263, 263, 264, 264, 265, 265,
                266, 266, 267, 267, 268, 268, 248, 248, 249, 249,
                250, 250, 251, 251, 252, 252, 253, 253, 254, 254,
                10, 10, 21, 21, 39, 39, 74, 74, 94, 94
                }
            },
            { 33, new List<int>
                {
                // Mixed theme: Mixed Faith-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 147, 147, 96, 96, 51, 51, 52, 52,
                53, 53, 71, 71, 44, 44, 140, 140, 49, 49,
                94, 94, 162, 162, 48, 48, 146, 146, 50, 50,
                64, 64, 109, 109, 112, 112, 8, 8, 18, 18,
                63, 63, 42, 42, 39, 39, 21, 21, 10, 10
                }
            },
            { 34, new List<int>
                {
                // Pure theme: Pure MonsterHunter ladder deck.
                // Core card preview: first card in deck list.
                10, 10, 181, 181, 34, 34, 39, 39, 6, 6,
                120, 120, 30, 30, 36, 36, 153, 153, 3, 3,
                35, 35, 121, 121, 154, 154, 32, 32, 31, 31,
                37, 37, 38, 38, 33, 33, 4, 4, 103, 103,
                113, 113, 96, 96, 94, 94, 74, 74, 21, 21
                }
            },
            { 35, new List<int>
                {
                // Pure theme: Pure Pokemon ladder deck.
                // Core card preview: first card in deck list.
                113, 113, 54, 54, 19, 19, 22, 22, 199, 199,
                146, 146, 0, 0, 217, 217, 147, 147, 175, 175,
                26, 26, 202, 202, 16, 16, 40, 40, 219, 219,
                216, 216, 203, 203, 148, 148, 112, 112, 41, 41,
                94, 94, 74, 74, 39, 39, 21, 21, 10, 10
                }
            },
            { 36, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                96, 96, 165, 165, 90, 90, 160, 160, 71, 71,
                132, 132, 91, 91, 73, 73, 1, 1, 70, 70,
                72, 72, 21, 21, 95, 95, 173, 173, 143, 143,
                69, 69, 51, 51, 119, 119, 93, 93, 172, 172,
                159, 159, 74, 74, 39, 39, 10, 10, 94, 94
                }
            },
            { 37, new List<int>
                {
                // Mixed theme: Mixed Avatar-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 44, 44, 8, 8, 48, 48, 146, 146,
                137, 137, 140, 140, 112, 112, 147, 147, 52, 52,
                74, 74, 42, 42, 94, 94, 53, 53, 84, 84,
                80, 80, 47, 47, 127, 127, 96, 96, 79, 79,
                10, 10, 21, 21, 39, 39, 94, 94, 178, 178
                }
            },
            { 38, new List<int>
                {
                // Pure theme: Pure Combo gimmick ladder deck.
                // Core card preview: first card in deck list.
                184, 184, 192, 192, 193, 193, 194, 194, 195, 195,
                196, 196, 197, 197, 198, 198, 201, 201, 225, 225,
                230, 230, 184, 184, 192, 192, 193, 193, 194, 194,
                195, 195, 196, 196, 197, 197, 198, 198, 10, 10,
                21, 21, 39, 39, 74, 74, 94, 94, 96, 96
                }
            },
            { 39, new List<int>
                {
                // Pure theme: Pure Faith ladder deck.
                // Core card preview: first card in deck list.
                60, 60, 67, 67, 66, 66, 65, 65, 96, 96,
                58, 58, 145, 145, 68, 68, 113, 113, 111, 111,
                108, 108, 63, 63, 24, 24, 94, 94, 203, 203,
                106, 106, 107, 107, 18, 18, 109, 109, 61, 61,
                64, 64, 74, 74, 39, 39, 21, 21, 10, 10
                }
            },
            { 40, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Avatar synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 35, 35, 53, 53, 83, 83, 137, 137,
                37, 37, 36, 36, 84, 84, 134, 134, 82, 82,
                13, 13, 39, 39, 119, 119, 80, 80, 81, 81,
                33, 33, 38, 38, 31, 31, 6, 6, 34, 34,
                113, 113, 96, 96, 94, 94, 74, 74, 21, 21
                }
            },
            { 41, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Healer synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 18, 18, 0, 0, 120, 120, 94, 94,
                16, 16, 210, 210, 112, 112, 147, 147, 54, 54,
                146, 146, 47, 47, 199, 199, 25, 25, 26, 26,
                41, 41, 50, 50, 140, 140, 19, 19, 44, 44,
                96, 96, 74, 74, 39, 39, 21, 21, 10, 10
                }
            },
            { 42, new List<int>
                {
                // Pure theme: Pure Speedster ladder deck.
                // Core card preview: first card in deck list.
                70, 70, 1, 1, 53, 53, 52, 52, 147, 147,
                51, 51, 119, 119, 73, 73, 71, 71, 201, 201,
                133, 133, 21, 21, 112, 112, 72, 72, 132, 132,
                91, 91, 120, 120, 96, 96, 230, 230, 69, 69,
                113, 113, 94, 94, 74, 74, 39, 39, 10, 10
                }
            },
            { 43, new List<int>
                {
                // Mixed theme: Mixed Inazuma-Speedster synergy deck (Speedster+Inazuma).
                // Core card preview: first card in deck list.
                259, 259, 1, 1, 69, 69, 70, 70, 71, 71,
                72, 72, 73, 73, 91, 91, 95, 95, 132, 132,
                133, 133, 248, 248, 249, 249, 250, 250, 256, 256,
                257, 257, 258, 258, 260, 260, 10, 10, 21, 21,
                39, 39, 74, 74, 94, 94, 96, 96, 113, 113
                }
            },
            { 44, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                127, 127, 166, 166, 208, 208, 59, 59, 202, 202,
                132, 132, 201, 201, 113, 113, 204, 204, 89, 89,
                190, 190, 203, 203, 88, 88, 133, 133, 126, 126,
                189, 189, 96, 96, 157, 157, 120, 120, 74, 74,
                10, 10, 21, 21, 39, 39, 94, 94, 122, 122
                }
            },
            { 45, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                113, 113, 25, 25, 112, 112, 26, 26, 120, 120,
                0, 0, 64, 64, 220, 220, 22, 22, 40, 40,
                203, 203, 223, 223, 19, 19, 54, 54, 199, 199,
                61, 61, 96, 96, 147, 147, 127, 127, 74, 74,
                10, 10, 21, 21, 39, 39, 94, 94, 17, 17
                }
            },
            { 46, new List<int>
                {
                // Mixed theme: Mixed Combo-Inazuma synergy deck.
                // Core card preview: first card in deck list.
                184, 184, 192, 192, 193, 193, 194, 194, 195, 195,
                196, 196, 197, 197, 198, 198, 201, 201, 225, 225,
                230, 230, 248, 248, 249, 249, 250, 250, 256, 256,
                257, 257, 259, 259, 10, 10, 21, 21, 39, 39,
                74, 74, 94, 94, 96, 96, 113, 113, 127, 127
                }
            },
            { 47, new List<int>
                {
                // Pure theme: Pure Healer ladder deck.
                // Core card preview: first card in deck list.
                113, 113, 152, 152, 50, 50, 47, 47, 49, 49,
                71, 71, 42, 42, 119, 119, 46, 46, 52, 52,
                112, 112, 210, 210, 8, 8, 146, 146, 48, 48,
                44, 44, 147, 147, 140, 140, 127, 127, 96, 96,
                94, 94, 74, 74, 39, 39, 21, 21, 10, 10
                }
            },
            { 48, new List<int>
                {
                // Mixed theme: Mixed MonsterHunter-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                10, 10, 141, 141, 35, 35, 38, 38, 3, 3,
                121, 121, 13, 13, 202, 202, 142, 142, 34, 34,
                208, 208, 88, 88, 5, 5, 153, 153, 33, 33,
                37, 37, 36, 36, 31, 31, 127, 127, 113, 113,
                96, 96, 94, 94, 39, 39, 74, 74, 21, 21
                }
            },
            { 49, new List<int>
                {
                // Mixed theme: Mixed Pokemon-Speedster synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                21, 21, 120, 120, 96, 96, 199, 199, 147, 147,
                54, 54, 41, 41, 203, 203, 217, 217, 40, 40,
                19, 19, 112, 112, 26, 26, 1, 1, 70, 70,
                0, 0, 25, 25, 16, 16, 167, 167, 127, 127,
                94, 94, 113, 113, 74, 74, 39, 39, 10, 10
                }
            },
            { 50, new List<int>
                {
                // Mixed theme: Mixed Speedster-Gunner synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                96, 96, 99, 99, 90, 90, 151, 151, 91, 91,
                72, 72, 94, 94, 21, 21, 70, 70, 113, 113,
                160, 160, 18, 18, 71, 71, 119, 119, 73, 73,
                95, 95, 51, 51, 132, 132, 165, 165, 143, 143,
                69, 69, 127, 127, 74, 74, 39, 39, 10, 10
                }
            },
        };
    private static readonly Dictionary<int, List<int>> AdventureDecks =
        new Dictionary<int, List<int>>
        {
            { 1, new List<int>
                {
                //Omri deck : Combo + add discard cards and omri that scales with discards. a lot of draw
                192,192,196,196,197,197,194,194,93,93,
                302,302,//Gate Discard
                }
            },
            { 2, new List<int>
                {
                // Omar : Chaos central + add Omar staple + MH base (qurupeco to combine both worlds)
                128,128,166,166,286,286//Omar DrBens and Qurupeco
                ,193,51,38,246,193,51,38,246,
                181,181,/*Deviljho*/

                }
            },
            { 3, new List<int>
                {
                // Othmane : Le D and friends + inazuma othmane mains.
                188,188,189,189,190,190,191,191,69,69,71,71,72,72,//Othmane cards and sonic core
                214,214,//Luigi
                307,307,//Doukha Mania
                }
            },
            { 4, new List<int>
                {
                // Amine : Fighter based deck with a berserk logic and charges.
                168,168,//Agni
                183,183,184,184,185,185,186,186,187,187,209,209,//Amine Mains
                299,299,300,300,//Vikingrr and Max


                }
            },
            { 5, new List<int>
                {
                //Adam : Bring back footballs in inazuma trait
                275,275,275,275,287,287,271,271,271,271,298,298,298,298,
                }
            },
            { 6, new List<int>
                {
                // Reda : Bring back redox and reda cards, combine faith and neutral for the rest of the archetype. Also add bowser and smash for more buffs.
                269,269,270,270 //Reda/redox
                }
            },
            { 7, new List<int>
                {//Count 20
                173,173,173,173,173,173,173,173,173,173,173,173,// o: Gunner+Fighter 
                90,90,90,90,90,
                91,91,91//troll bouisk
                }
            },
            { 8, new List<int>
                {
                // Thibauld : Healer and food deck + thib cards with maxime medard and moroccan food.
                272,272 //Thib
                ,274,274 //Maxime M
                }
            },
            { 9, new List<int>
                {
                // Madre : Faith with big stats and taunts + resurrect. Give her her unique spell that will summon random taunts that cost a total of 25 for 10 mana.
                //  La madre card (6 2 5 protect blessed) has +1+1 for each taunt in graveyard.
                284,284,//madre card
                }
            },
            { 10, new List<int>
                {
                // Sara : Strong card that self damag her hero but for big payout. in addition to healing and cinnamon cards.
                283,283,282,282, //Sarito and Cinamoncops
                //Add tinkaton evolves when killing with rocks/boulders.
                }
            },
            { 11, new List<int>
                {
                //Rhita : Combo + Pokemon cards with the payoff of her signature card.
                276,281, 276,281, //Rhita and rhita team
                277,277,278,278,279,279,
                304,306,306,304//Sunday both forms
                }
            },
            { 12, new List<int>
                {
                // Padre : Big monsters some faith cards, a lot of charges and his signature card, a huge 10  15 15 charge.
                285,285 //Padre card
                }
            },
            { 13, new List<int>
                {
                // Mixed theme: Mixed Chaos-Neutral synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                127, 127, 120, 120, 142, 142, 85, 85, 131, 131,
                205, 205, 208, 208, 201, 201, 128, 128, 188, 188,
                157, 157, 132, 132, 126, 126, 129, 129, 166, 166,
                89, 89, 202, 202, 119, 119, 88, 88, 158, 158,
                133, 133, 141, 141, 59, 59, 191, 191, 130, 130
                }
            },
            { 14, new List<int>
                {
                // Mixed theme: Mixed Faith-Pokemon synergy deck (refreshed order).
                // Core card preview: first card in deck list.
                25, 25, 60, 60, 64, 64, 0, 0, 19, 19,
                112, 112, 62, 62, 22, 22, 148, 148, 147, 147,
                109, 109, 40, 40, 120, 120, 63, 63, 162, 162,
                54, 54, 202, 202, 26, 26, 16, 16, 133, 133,
                149, 149, 61, 61, 216, 216, 146, 146, 10, 10
                }
            }
        };
    public static List<int> GetFloorDeck(int floor)
    {
        if (FloorDecks.TryGetValue(floor, out List<int> deck))
            return deck;

        int randomFloor = UnityEngine.Random.Range(1, FloorDecks.Count + 1);
        return FloorDecks[randomFloor];
    }
    public static List<int> GetAdventureDeck(int id)
    {
        if (AdventureDecks.TryGetValue(id, out List<int> deck))
            return deck;
        else return null;//Add default deck ? 
    }

    // Future expansion: add more unique packable Gunner/Speedster and pure Chaos control cards to reduce cross-trait overlap in late-floor 50-card decks.
}
