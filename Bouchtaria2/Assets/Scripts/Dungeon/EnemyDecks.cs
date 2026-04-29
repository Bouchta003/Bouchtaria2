using System.Collections.Generic;
using UnityEngine;

public static class EnemyDecks
{
    private static readonly Dictionary<int, List<int>> FloorDecks =
        new Dictionary<int, List<int>>
        {
            { 1, new List<int>
                {
                // Trait focus: MonsterHunter early hunt deck with straightforward board pressure.
                3, 3, 4, 4, 5, 5, 6, 6, 9, 9,
                30, 30, 32, 32, 34, 34, 36, 36, 37, 37,
                38, 38, 103, 103, 104, 104, 119, 119, 153, 153
                }
            },
            { 2, new List<int>
                {
                // Theme focus: Speed tempo and quick pivots with evasive pressure tools.
                1, 1, 69, 69, 71, 71, 72, 72, 73, 73,
                91, 91, 95, 95, 131, 131, 132, 132, 160, 160,
                25, 25, 59, 59, 88, 88, 89, 89, 142, 142
                }
            },
            { 3, new List<int>
                {
                // Trait focus: Faith value deck built around scaling blessings and sustain.
                58, 58, 60, 60, 61, 61, 63, 63, 64, 64,
                65, 65, 67, 67, 68, 68, 106, 106, 107, 107,
                108, 108, 109, 109, 145, 145, 162, 162, 229, 229
                }
            },
            { 4, new List<int>
                {
                // Theme focus: Gunner control shell using repeated chip damage and removal.
                92, 92, 93, 93, 97, 97, 98, 98, 99, 99,
                119, 119, 143, 143, 151, 151, 159, 159, 160, 160,
                161, 161, 164, 164, 172, 172, 173, 173, 174, 174
                }
            },
            { 5, new List<int>
                {
                // Trait focus: Avatar midrange deck leveraging elemental units and finishers.
                80, 80, 81, 81, 82, 82, 83, 83, 84, 84,
                134, 134, 135, 135, 136, 136, 137, 137, 138, 138,
                155, 155, 176, 176, 177, 177, 53, 53, 71, 71
                }
            },
            { 6, new List<int>
                {
                // Theme focus: Neutral toolbox with disruptive tricks and flexible answers.
                59, 59, 88, 88, 89, 89, 142, 142, 201, 201,
                202, 202, 205, 205, 208, 208, 141, 141, 126, 126,
                129, 129, 130, 130, 131, 131, 132, 132, 166, 166
                }
            },
            { 7, new List<int>
                {
                // Trait focus: Healer sustain deck that outlasts through recovery chains.
                8, 8, 42, 42, 44, 44, 47, 47, 48, 48,
                49, 49, 51, 51, 52, 52, 71, 71, 93, 93,
                99, 99, 146, 146, 147, 147, 152, 152, 210, 210
                }
            },
            { 8, new List<int>
                {
                // Theme focus: Combo economy deck that snowballs resources into big turns.
                193, 193, 194, 194, 196, 196, 197, 197, 201, 201,
                184, 184, 230, 230, 83, 83, 84, 84, 135, 135,
                137, 137, 138, 138, 155, 155, 176, 176, 177, 177
                }
            },
            { 9, new List<int>
                {
                // Trait focus: Chaos deck centered on volatility, random generation, and pressure.
                85, 85, 126, 126, 128, 128, 129, 129, 130, 130,
                131, 131, 132, 132, 133, 133, 157, 157, 158, 158,
                166, 166, 188, 188, 191, 191, 120, 120, 59, 59
                }
            },
            { 10, new List<int>
                {
                // Theme focus: Pokemon evolution curve with two non-packable ace threats.
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
                40, 40, 54, 54, 120, 120, 146, 146, 147, 147,
                148, 148, 149, 149, 202, 202, 208, 208, 7, 21
                }
            },
            { 11, new List<int>
                {
                // Trait focus: Swordsman weapon specialists with two non-packable power spikes.
                36, 36, 37, 37, 38, 38, 97, 97, 137, 137,
                141, 141, 143, 143, 171, 171, 174, 174, 184, 184,
                186, 186, 90, 90, 98, 98, 119, 119, 10, 39
                }
            },
            { 12, new List<int>
                {
                // Theme focus: Fighter brawl deck tuned for board trades and burst finishers.
                45, 45, 66, 66, 72, 72, 90, 90, 98, 98,
                165, 165, 171, 171, 173, 173, 174, 174, 175, 175,
                184, 184, 186, 186, 209, 209, 119, 119, 2, 96
                }
            },
            { 13, new List<int>
                {
                // Trait focus: MonsterHunter advanced hunt list with premium non-packable gear.
                3, 3, 4, 4, 5, 5, 6, 6, 9, 9,
                30, 30, 32, 32, 34, 34, 103, 103, 104, 104,
                119, 119, 153, 153, 36, 37, 10, 31, 141, 142
                }
            },
            { 14, new List<int>
                {
                // Theme focus: Faith fortress deck balancing defense, buffs, and late inevitability.
                58, 58, 60, 60, 61, 61, 63, 63, 64, 64,
                65, 65, 67, 67, 68, 68, 106, 106, 107, 107,
                108, 108, 109, 109, 145, 145, 162, 162, 62, 113
                }
            },
            { 15, new List<int>
                {
                // Trait focus: Avatar masters list with four non-packable incarnations as bosses.
                80, 80, 81, 81, 82, 82, 83, 83, 84, 84,
                134, 134, 135, 135, 136, 136, 137, 137, 138, 138,
                155, 155, 176, 176, 177, 177, 74, 74, 75, 75
                }
            },
            { 16, new List<int>
                {
                // Theme focus: Gunner kill-zone deck built on traps, walls, and ranged burst.
                92, 92, 93, 93, 97, 97, 98, 98, 99, 99,
                119, 119, 143, 143, 151, 151, 159, 159, 160, 160,
                164, 164, 172, 172, 173, 173, 94, 94,
                96, 96
                }
            },
            { 17, new List<int>
                {
                // Trait focus: Pokemon legendary lineup with four non-packable endgame bombs.
                16, 16, 19, 19, 22, 22, 25, 25, 26, 26,
                40, 40, 54, 54, 120, 120, 146, 146, 147, 147,
                148, 148, 149, 149, 208, 208, 7, 21,
                27, 28
                }
            },
            { 18, new List<int>
                {
                // Theme focus: Chaos trickster deck with cursed non-packable swing cards.
                85, 85, 126, 126, 128, 128, 129, 129, 130, 130,
                131, 131, 132, 132, 133, 133, 158, 158,
                166, 166, 188, 188, 191, 191, 120, 120, 127, 86,
                189, 190
                }
            },
            { 19, new List<int>
                {
                // Trait focus: Speedster-Fighter assault deck with four non-packable elite tools.
                1, 1, 69, 69, 71, 71, 72, 72, 73, 73,
                91, 91, 95, 95, 131, 131, 132, 132,
                90, 90, 98, 98, 171, 171, 173, 173, 2, 96,
                94, 113
                }
            },
            { 20, new List<int>
                {
                // Theme focus: Final gauntlet of cross-trait legends with six non-packable bosses.
                3, 3, 5, 5, 6, 6, 16, 16, 22, 22,
                25, 25, 30, 30, 34, 34, 58, 58, 60, 60,
                71, 71, 80, 80, 82, 82, 84, 84, 90, 90,
                92, 92, 97, 97, 103, 103, 119, 119, 126, 126,
                134, 134, 162, 162, 11, 14, 27, 29,
                212, 239
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
                //Omri : (30)
                192,192,
                196,196,
                197,197,
                194,194,
                143,143,//Failnaught bows
                158,158,//jour de fete
                93,93,
                302,302,//Gate Discard
                309,309,//Omri complaint
                47,47,//BBQ
                355,355,//discarder
                356,356,//samurai jack
                357,357,//kogmaw
                85,85,
                85,85//Whatsapp
                }
            },
            { 2, new List<int>
                {
                // Omar : (30)
                128,128,
                166,166,
                286,286,
                305,305,//Omar DrBens and Qurupeco bensalmiserable
                193,51,
                38,246,
                193,51,
                38,246,
                181,181,
                346,346,
                105,105,
                104,104,//Deviljho Glavenus Arkveld and ReyDau
                316,316,//Nago
                332,243,//Slifer+gible
                246,246,//Elder Dragon
                }
            },
            { 3, new List<int>
                {
                // Othmane :(30 cards)
                188,188,
                189,69,
                191,191,
                71,72,//Othmane cards and sonic core
                214,214,//Luigi
                307,307,//Doukha Mania
                317,317,//Mukla
                287,287,//Soccer monster
                354,354,//WeBallin
                354,19,//Chimchar
                68,68,
                68,68,
                58,58,//Faith and no more music
                111,111,
                60,60,//Tawakkul and duaa
                }
            },
            { 4, new List<int>
                {
                // Amine : . (30)
                168,168,//Agni
                183,183,
                184,184,
                185,185,
                186,186,
                187,187,
                209,209,//Amine Mains
                299,299,
                300,300,//Vikingrr and Max
                165,165,//Holly
                170,170,//Bursts
                171,171,//Plata
                36,319,//Greatsword+Lich king
                88,88,//rainbow card
                141,141,//lostvayne

            }
            },
            { 5, new List<int>
                {
                //Adam :  (30)
                275,275,//football
                275,275,//football
                287,287,
                271,271,
                271,271,
                298,298,
                298,298,
                312,312,//short rose
                358,358,//Alfonso
                359,359,//Adam
                353,353,//Chbakiya
                354,354,//Weballin
                266,266,//Daisuke
                257,257,//Gouenji
                259,259,//Fubuki
                }
            },
            { 6, new List<int>
                {
                // Reda :  30
                269,269, //Reda
                270,270, //Redox
                269,270, //Reda redox en plus
                227,227, //Bowser
                111,111, //Tawakkul
                162,162, //Gratitude
                60,60, //duaa
                63,63, //hijabi
                67,67, //Awrah
                106,106,
                107,107,
                108,108,//Faith warriors
                145,145,//Adhan
                58,58,//no more music
                68,68,//dans ldin

                }
            },
            { 7, new List<int>
                {//o : Count 30
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,// o: Gunner+Fighter 
                90,90,
                90,90,
                90,91,//Plata o pLomo
                91,91,//troll bouisk
                158,158,//Jour de fete
                1,1,//La légende de twice
                232,232,
                234,234,//Poly et super poly
                }
            },
            { 8, new List<int>
                {
                // Thibauld : (30)
                272,272,//Thib
                272,274,
                274,274, //Maxime M
                352,352,
                352,352,
                353,353,
                353,353,//Chbakiya and briwat
                214,215,//Mario and luigi
                44,44,
                47,47,//BouchtaBBQ and lasagna
                51,51,
                45,46,//Faust en rapport à la chanson
                175,175,//Macho gym + fighter
                169,169,//Smash ball for buffs
                233,233,//Bissara
                }
            },
            { 9, new List<int>
                {
                // Madre : 30
                284,284, //madre card
                284,284, //madre card
                110,110, //voice of dhikr
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                276,276, //Rhita
                63,63,//Hijabi
                64,64,//Hijabi
                363,363,//Inchaallah
                229,229,//Ramadan
                61,61,//Sadaqa
                44,44,//Lasagna
                }
            },
            { 10, new List<int>
                {
                // Sara : 30
                283,283,
                54,54,//Dormis
                282,282, //Sarito and Cinamoncops
                304,306,
                306,304, //Sunday both forms
                175,213, //Machop and buzzwole
                57,57,   //Snorlax
                207,207, //Lucario
                360,360, //Sara
                44,44,   //Lasagna
                365,365,//Tinkatink
                231,231,//ultra ball
                367,367,//Tinkaton
                369,369,//Yazio
                156,157,//Boyd and greatball
                }
            },
            { 11, new List<int>
                {
                //Rhita : 30
                276,281,
                276,281, //Rhita and rhita team
                280,280, //RhitaGAteau
                277,277,
                278,278,
                279,279,
                320,320, //Poukoupia
                56,56,  //Wigglytuff
                54,54,  //Dormis
                44,44,   //Lasagna
                47,47, //BBQ
                8,8, //choupitout
                324,324, //pneuma
                109,109,
                361,361, //Rhitout
                }
            },
            { 12, new List<int>
                {
                // Padre : 30.
                285,285, //Padre card
                285,362,//Padre prime *1
                270,270, //Redox
                241,241, //Great rod
                241,241,//Great rod
                241,241,//Great rod
                241,241,//Great rod
                241,241,//Great rod
                233,233,//Bissara
                363,363,//Inchaallah
                106,106,//Sabr
                145,145,//Adhan
                107,107,//Protector of the ummah
                328,328,//Huge banana
                61,61,//Sadaqa
                }
            },
            { 13, new List<int>
                {
                //Bouchta : SoulForceDeck with knights and free cards
                374,374,//BouchtaSpell
                375,375,//KnightsSpell
                285,361,//PapaRhita
                360,284,//SaraMama
                343,343,//Vulcan
                332,368,//Slifer+Aegislash
                292,292,//Soul Eater
                291,291,
                293,293,
                289,289,
                335,335,
                337,337,
                333,333,
                345,345,//Soul Eater end
                }
            },
            { 14, new List<int>
                {
                // ???: Prime curse.
                27,28,
                29,163,//Dialga Palkia GIratina Hooh
                189,189,
                189,85,//Triple Le D + colonel
                120,120,//metronome
                126,126,//feed the chaos
                127,127,//Cheater will
                127,127,
                128,129,//Le Bens + gmpves
                239,239,//GonPrime
                167,167,
                308,308,//suave
                128,129,
                158,158,
                75,75,    
            }
            }
        };
    // Hard mode uses the same fight IDs as adventure mode.
    // Populate this dictionary with hard-mode decklists per ID.
    private static readonly Dictionary<int, List<int>> AdventureHardDecks =
        new Dictionary<int, List<int>>(){
            { 1, new List<int>
                {
                //Omri : 
                192,192,
                196,196,
                197,197,
                194,194,
                143,143,//Failnaught bows
                158,158,//jour de fete
                93,93,
                302,302,//Gate Discard
                309,309,//Omri complaint
                47,47,//BBQ
                355,355,//discarder
                356,356,//samurai jack
                357,357,//kogmaw
                85,85,
                85,85//Whatsapp
                }
            },
            { 2, new List<int>
                {
                // Omar : (30) Extinction dragon deck with Rods + Revives + Inchaallah
                246,246,//Extinction dragon
                210,210,//Max Revive
                146,146,//Revive
                241,241,//Great Rod
                241,241,//Great Rod
                32,32,//Rathian
                181,181,//Deviljho
                9,9,//Odogaron
                103,103,//Zinogre
                363,363,//Inchaallah
                145,145,//Adhan
                229,6,//Ramadan + own order
                353,353,//Chbakiya
                169,169,//SmashBall
                44,44,//Lasagna
                }
            },
            { 3, new List<int>
                {
                // Othmane :(30 cards) chaos deck full randomness.
                120,120,//Metronome
                120,120,//Metronome
                120,120,//Metronome
                120,120,//Metronome
                120,120,//Metronome
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                126,126,//Feed The chaos
                189,189,//Le D
                128,128,//Le Bens
                127,127,//Cheater's Will
                }
            },
            { 4, new List<int>
                {
                // Amine : buff deck with berserk logic ?
                168,168,//Agni
                183,183,
                184,184,
                185,185,
                186,186,
                187,187,
                209,209,//Amine Mains
                299,299,
                300,300,//Vikingrr and Max
                165,165,//Holly
                170,170,//Bursts
                171,171,//Plata
                36,319,//Greatsword+Lich king
                88,88,//rainbow card
                141,141,//lostvayne

            }
            },
            { 5, new List<int>
                {
                //Adam :  (18)
                275,275,//football
                275,275,//football
                287,287,//Soccer monster
                271,271,//Siuu
                271,271,//Siuu
                359,359,//Adam
                354,354,//Weballin
                354,354,//Weballin
                354,354,//Weballin
                }
            },
            { 6, new List<int>
                {
                // Reda :  30 - full discover highlander deck with Reda and Sara Jackson
                326,310, //Sara Jackson+Tawhid
                269,270, //Reda redox en plus
                60,68,//Duaa and dans ldin
                88,90,//Rainbow and PlataoPlomo
                162,209,//Gratitude and Amine Mains
                58,145,//No more music and Adhan
                229,111,//Ramadan and Tawakkul
                308,157,//Suave and Boyd
                64,65,//Hijabi and Armor Clad Faith
                106,107,//Bearer of Sabr and Protector of the Ummah
                171,172,//Plata and plomo
                173,193,//o + market crasher
                377,85,//Coran Reader + Colonel
                378,109,//Muslim avengers+ilm seeker
                146,284,//Revive+La Mama
                }
            },
            { 7, new List<int>
                {//o :
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,
                173,173,// o: Gunner+Fighter 
                90,90,
                90,90,
                90,91,//Plata o pLomo
                91,91,//troll bouisk
                158,158,//Jour de fete
                1,1,//La légende de twice
                232,232,
                234,234,//Poly et super poly
                }
            },
            { 8, new List<int>
                {
                // Thibauld :
                272,272,//Thib
                272,274,
                274,274, //Maxime M
                352,352,
                352,352,
                353,353,
                353,353,//Chbakiya and briwat
                214,215,//Mario and luigi
                44,44,
                47,47,//BouchtaBBQ and lasagna
                51,51,
                45,46,//Faust en rapport à la chanson
                175,175,//Macho gym + fighter
                169,169,//Smash ball for buffs
                233,233,//Bissara
                }
            },
            { 9, new List<int>
                {
                // Madre : 22
                284,284, //madre card
                284,284, //madre card
                110,110, //voice of dhikr
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                60,60,   //Duaa
                363,363,//Inchaallah
                229,229,//Ramadan
                61,61,//Sadaqa
                //Add card to gain passive EOT +2 HP to all units for the rest of the game.
                }
            },
            { 10, new List<int>
                {
                // Sara : 22 : Pokemon killer deck (raichu buzzwole balls tinkaton and garchomp)+core heal with Sarito
                306,306, //Soundays
                213,213, //buzzwole
                57,57,   //Snorlax
                360,360, //Sara
                360,360, //Sara
                44,44,   //Lasagna
                365,365,//Tinkatink
                243,243,//Gible
                231,231,//ultra ball
                367,367,//Tinkaton
                227,227,//Bowser
                }
            },
            { 11, new List<int>
                {
                //Rhita : 30 : Heal deck with some killers. 
                276,281,
                276,281, //Rhita and rhita team
                280,280, //RhitaGAteau
                277,277,
                278,278,
                279,279,
                320,320, //Poukoupia
                56,56,  //Wigglytuff
                54,54,  //Dormis
                44,44,   //Lasagna
                47,47, //BBQ
                8,8, //choupitout
                324,324, //pneuma
                109,109,
                361,361, //Rhitout
                //Add a card that will deal damage to enemy core for each heal this game (not overheal)
                }
            },
            { 12, new List<int>
                {
                // Padre : 28.
                285,285, //Padre card
                285,285,//Padre card
                362,362,//Padre prime *2
                270,270, //Redox
                241,241, //Great rod
                241,241,//Great rod
                241,241,//Great rod
                241,241,//Great rod
                241,241,//Great rod
                241,241,//Great rod
                363,363,//Inchaallah
                363,363,//Inchaallah
                106,106,//Sabr
                145,145,//Adhan
                //Add unique padre draw card.
                }
            },
            { 13, new List<int>
                {
                //Bouchta : SoulForceDeck with knights and free cards
                374,374,//BouchtaSpell
                375,375,//KnightsSpell
                285,361,//PapaRhita
                360,284,//SaraMama
                343,343,//Vulcan
                332,368,//Slifer+Aegislash
                292,292,//Soul Eater
                291,291,
                293,293,
                289,289,
                335,335,
                337,337,
                333,333,
                345,345,//Soul Eater end
                }
            },
            { 14, new List<int>
                {
                // ???: Prime curse.
                27,28,
                29,163,//Dialga Palkia GIratina Hooh
                189,189,
                189,85,//Triple Le D + colonel
                120,120,//metronome
                126,126,//feed the chaos
                127,127,//Cheater will
                127,127,
                128,129,//Le Bens + gmpves
                239,239,//GonPrime
                167,167,
                308,308,//suave
                128,129,
                158,158,
                75,75,
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
    public static List<int> GetAdventureDeck(int id)
    {
        return GetAdventureDeck(id, false);
    }

    public static List<int> GetAdventureDeck(int id, bool isHardMode)
    {
        if (isHardMode && AdventureHardDecks.TryGetValue(id, out List<int> hardDeck))
            return hardDeck;

        if (AdventureDecks.TryGetValue(id, out List<int> deck))
            return deck;
        else return null;//Add default deck ? 
    }

}
