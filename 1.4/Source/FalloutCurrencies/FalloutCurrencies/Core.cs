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
    /// <summary>
    /// Swaps the currency the trade uses
    /// Without this the trade caravan will still generate with their custom currency,
    /// however the actual trade will still use silver
    /// </summary>
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
    /// <summary>
    /// Doesn't seem to actually ever be run
    /// </summary>
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
    /// <summary>
    /// defaultCurrencyDef is always Silver
    /// The if statement checks for if ___thingDef both is and isn't Silver, which will always fail
    /// </summary>
    /*
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
    */
    /// <summary>
    /// Replaces all instances of Silver with the factions specified currency
    /// Obviously raises the issue that traders can never actually sell silver, 
    /// as it will always be converted into their own currency
    /// </summary>
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
            Log.Message("Swapping silver to: " + newDef.label);
            ThingDefOf.Silver = newDef;
            Log.Message("Silver is now: " + ThingDefOf.Silver.label);
        }
    }
}

