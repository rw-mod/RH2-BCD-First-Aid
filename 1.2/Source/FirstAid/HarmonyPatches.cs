using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FirstAid
{
	[StaticConstructorOnStartup]
	internal static class HarmonyInit
	{
		static HarmonyInit()
		{
			new Harmony("CP.FirstAid").PatchAll();
		}
	}
	[HarmonyPatch(typeof(TendUtility), "CalculateBaseTendQuality", new Type[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef)})]
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

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class FloatMenuMakerCarryAdder
	{
		public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
			{
				foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt_NewTemp(clickPos, TargetingParameters.ForRescue(pawn), true, null))
				{
					Pawn target = (Pawn)localTargetInfo.Thing;
					if (target != null && (target.health?.HasHediffsNeedingTend(false) ?? false))
					{
						if (pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true))
						{
							Action action = delegate ()
							{
								var medicine = FindBestMedicine(pawn, target);
								Job job = new Job(CPDefOf.CP_FirstAid, target, medicine);
								pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
							};
							string label = "RH.PerformFirstAid".Translate(target.Named("PAWN"));
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action, MenuOptionPriority.RescueOrCapture, null, target, 0f, null, null), pawn, target, "ReservedBy"));
						}
					}
				}
			}
		}

		public static Thing FindBestMedicine(Pawn healer, Pawn patient)
		{
			if (patient.playerSettings != null && patient.playerSettings.medCare <= MedicalCareCategory.NoMeds)
			{
				return null;
			}
			if (Medicine.GetMedicineCountToFullyHeal(patient) <= 0)
			{
				return null;
			}
			Predicate<Thing> validator = (Thing m) => (!m.IsForbidden(healer) && (patient.playerSettings is null || patient.playerSettings.medCare.AllowsMedicine(m.def)) 
			&& healer.CanReserve(m, 10, 1)) ? true : false;
			Func<Thing, float> priorityGetter = (Thing t) => t.def.GetStatValueAbstract(StatDefOf.MedicalPotency);
			float radius = 10f;
			Thing medicine = null;
			var candidates = new List<Thing>();
			for (int j = 0; j < healer.inventory.innerContainer.Count; j++)
			{
				Thing item = healer.inventory.innerContainer[j];
				if (validator(item))
                {
					candidates.Add(item);
				}
			}
			if (candidates.Any() && candidates.TryRandomElementByWeight(priorityGetter, out medicine))
            {
				return medicine;
            }
			medicine = GenClosest.ClosestThing_Global_Reachable(healer.Position, patient.Map, patient.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch,
				TraverseParms.For(healer), radius, validator, priorityGetter);
			if (medicine is null)
            {
				medicine = GenClosest.ClosestThing_Global_Reachable(patient.Position, patient.Map, patient.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch,
					TraverseParms.For(healer), radius, validator, priorityGetter);
			}
			return medicine;
		}
	}
}
