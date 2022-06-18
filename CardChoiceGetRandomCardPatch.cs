using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnboundLib;
using UnityEngine;

namespace CardChoiceGetRandomCardPatch
{
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our Mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our Mod Is associated with
    [BepInProcess("Rounds.exe")]
    public class CardChoiceGetRandomCardPatch : BaseUnityPlugin
    {
        private const string ModId = "root.rounds.CardChoiceGetRandomCardPatch";
        private const string ModName = "CardChoice GetRandomCard Patch";
        public const string Version = "1.0.0";

        void Awake()
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
    }


    [Serializable]
    [HarmonyPatch(typeof(CardChoice), "GetRanomCard")]
    public class CardChoicePatchGetRanomCard
    {
        [HarmonyPriority(HarmonyLib.Priority.First)]
        private static bool Prefix(CardChoice __instance, ref GameObject __result)
        {
            __result = null;
            if ((bool)CardChoiceVisuals.instance.GetFieldValue("isShowinig") && PickingPlayer(__instance) != null)
                __result = EfficientGetRanomCard(PickingPlayer(__instance));
            else
                __result = OrignialGetRanomCard();
            return true;

        }

        public static GameObject EfficientGetRanomCard(Player player)
        {
            return EfficientGetRanomCard(player, CardChoice.instance.cards);
        }

        public static GameObject EfficientGetRanomCard(Player player, CardInfo[] cards)
        {
            CardInfo[] validCards = cards.Where(c => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, c)).ToArray();

            try
            {
                if ((bool)CardChoiceVisuals.instance.GetFieldValue("isShowinig"))
                {
                    List<string> spawnedCards = ((List<GameObject>)CardChoice.instance.GetFieldValue("spawnedCards")).Select(obj => obj.GetComponent<CardInfo>().cardName).ToList();
                    validCards = validCards.Where(c => c.categories.Contains(CardChoiceSpawnUniqueCardPatch.CustomCategories.CustomCardCategories.CanDrawMultipleCategory) || !spawnedCards.Contains(c.cardName)).ToArray();
                }
            }
            catch (NullReferenceException)
            {
                validCards = cards.Where(c => ModdingUtils.Utils.Cards.instance.PlayerIsAllowedCard(player, c)).ToArray();
            }
            if (!validCards.Any())
            {
                return ((CardInfo)typeof(CardChoiceSpawnUniqueCardPatch.CardChoiceSpawnUniqueCardPatch).GetField("NullCard", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).gameObject;
            }
            return OrignialGetRanomCard(validCards);
        }

        public static GameObject OrignialGetRanomCard()
        {
            return OrignialGetRanomCard(CardChoice.instance.cards);
        }

        public static GameObject OrignialGetRanomCard(CardInfo[] cards) //Override this to mess with rarity logic
        {
            GameObject result = null;
            float num = 0f;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].rarity == CardInfo.Rarity.Common)
                    num += 10f;
                if (cards[i].rarity == CardInfo.Rarity.Uncommon)
                    num += 4f;
                if (cards[i].rarity == CardInfo.Rarity.Rare)
                    num += 1f;
            }

            float num2 = UnityEngine.Random.Range(0f, num);
            for (int j = 0; j < cards.Length; j++)
            {
                if (cards[j].rarity == CardInfo.Rarity.Common)
                    num2 -= 10f;
                if (cards[j].rarity == CardInfo.Rarity.Uncommon)
                    num2 -= 4f;
                if (cards[j].rarity == CardInfo.Rarity.Rare)
                    num2 -= 1f;
                if (num2 <= 0f)
                {
                    result = cards[j].gameObject;
                    break;
                }
            }
            return result;
        }

        internal static Player PickingPlayer(CardChoice cardChoice)
        {
            Player player = null;
            try
            {
                if ((PickerType)cardChoice.GetFieldValue("pickerType") == PickerType.Team)
                    player = PlayerManager.instance.GetPlayersInTeam(cardChoice.pickrID).FirstOrDefault();
                else
                    player = (cardChoice.pickrID < PlayerManager.instance.players.Count() && cardChoice.pickrID >= 0) ? PlayerManager.instance.players[cardChoice.pickrID] : null;
            }
            catch
            {
                player = null;
            }
            return player;
        }
    }
}
