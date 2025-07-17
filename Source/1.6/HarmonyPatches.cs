using HarmonyLib;
using RimWorld;
using Verse;

namespace FirstAid;

[StaticConstructorOnStartup]
internal static class HarmonyInit
{
	static HarmonyInit()
	{
		Harmony harmony = new Harmony("rw.mod.BCDsFirstAid");
		harmony.PatchAll();
	}
	
	[HarmonyPatch(typeof(TendUtility), "CalculateBaseTendQuality", typeof(Pawn), typeof(Pawn), typeof(ThingDef))]
	public static class CalculateBaseTendQuality_Patch
	{
		public static void Postfix(ref float __result, Pawn doctor, Pawn patient, ThingDef medicine)
		{
			if (doctor != null && doctor.CurJobDef == CPDefOf.CP_FirstAid)
			{
				__result *= 0.75f;
			}
		}
	}
}