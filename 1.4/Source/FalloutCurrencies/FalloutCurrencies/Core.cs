using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FalloutCurrencies_NonReplacement
{
    public class FactionCurrency : DefModExtension
    {
        public ThingDef currency;

        public static FactionCurrency Get(Def def)
        {
            return def.GetModExtension<FactionCurrency>();
        }
    }

    [HarmonyPatch(typeof(TradeSession), "SetupWith")]
    public static class SetupWith_Patch
    {
        public static void Postfix(ITrader newTrader, Pawn newPlayerNegotiator, bool giftMode)
        {
            var faction = newTrader.Faction;
            if (faction.TryGetCurrency(out var currency))
            {
                CurrencyManager.SwapCurrency(currency);
            }
            else if (ThingDefOf.Silver != CurrencyManager.defaultCurrencyDef)
            {
                CurrencyManager.SwapCurrency(CurrencyManager.defaultCurrencyDef);
            }
        }
    }

    [HarmonyPatch(typeof(TradeSession), "Close")]
    public static class Close_Patch
    {
        public static void Prefix()
        {
            if (ThingDefOf.Silver != CurrencyManager.defaultCurrencyDef)
            {
                CurrencyManager.SwapCurrency(CurrencyManager.defaultCurrencyDef);
            }
        }
    }

    [HarmonyPatch(typeof(StockGenerator_SingleDef), "HandlesThingDef")]
    public static class HandlesThingDef_Patch
    {
        public static void Postfix(StockGenerator_SingleDef __instance, ref bool __result, ref ThingDef ___thingDef)
        {
            if (___thingDef == CurrencyManager.defaultCurrencyDef && ___thingDef != ThingDefOf.Silver)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(StockGenerator_SingleDef), "GenerateThings")]
    public static class GenerateThings_Patch
    {
        public static void Prefix(StockGenerator_SingleDef __instance, ref ThingDef ___thingDef, out ThingDef __state, int forTile, Faction faction = null)
        {
            if (___thingDef == CurrencyManager.defaultCurrencyDef && faction.TryGetCurrency(out var currency))
            {
                ___thingDef = currency;
            }
            __state = ___thingDef;
        }
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, StockGenerator_SingleDef __instance, ThingDef __state, int forTile, Faction faction = null)
        {
            foreach (var thing in __result)
            {
                if (faction.TryGetCurrency(out var currency) && __state == currency)
                {
                    __state = CurrencyManager.defaultCurrencyDef;
                }
                yield return thing;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class CurrencyManager
    {
        public static ThingDef defaultCurrencyDef;
        static CurrencyManager()
        {
            defaultCurrencyDef = ThingDefOf.Silver;
            new Harmony("FalloutCurrencies.Mod").PatchAll();
        }

        public static bool TryGetCurrency(this Faction faction, out ThingDef currency)
        {
            var extension = faction?.def.GetModExtension<FactionCurrency>();
            if (extension != null)
            {
                currency = extension.currency;
                return true;
            }
            currency = null;
            return false;
        }
        public static void SwapCurrency(ThingDef newDef)
        {
            ThingDefOf.Silver = newDef;
        }
    }
}

