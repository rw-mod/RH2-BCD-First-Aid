using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace FirstAid;

[StaticConstructorOnStartup]
public static class Utility
{
    public static Thing FindBestMedicine(Pawn healer, Pawn patient)
    {
        if (patient.playerSettings is { medCare: <= MedicalCareCategory.NoMeds } || Medicine.GetMedicineCountToFullyHeal(patient) <= 0)
        {
            return null;
        }
        
        Predicate<Thing> validator = m => !m.IsForbidden(healer) && 
                                          m.def?.modContentPack?.PackageIdPlayerFacing != "Killathon.MechanicalHumanlikesCore" &&
                                          (patient.playerSettings is null && MedicalCareCategory.HerbalOrWorse.AllowsMedicine(m.def) || (patient.playerSettings?.medCare.AllowsMedicine(m.def) ?? false)) &&
                                          m.def.GetStatValueAbstract(StatDefOf.MedicalPotency) > 0.3f &&
                                          healer.CanReserveAndReach(m, PathEndMode.ClosestTouch, Danger.Deadly);
        
        Func<Thing, float> priorityGetter = t => t.def.GetStatValueAbstract(StatDefOf.MedicalPotency);
        
        const float radius = 10f;
        List<Thing> candidates = new List<Thing>();
        
        for (int i = 0; i < healer.inventory.innerContainer.Count; i++)
        {
            Thing item = healer.inventory.innerContainer[i];
            if (validator(item))
            {
                candidates.Add(item);
            }
        }
        
        if (candidates.Any() && candidates.TryRandomElementByWeight(priorityGetter, out Thing medicine))
        {
            return medicine;
        }

        Map map = patient.Map;
        medicine = GenClosest.ClosestThing_Global_Reachable(healer.Position, map, map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch, TraverseParms.For(healer), radius, validator, priorityGetter) ??
                   GenClosest.ClosestThing_Global_Reachable(patient.Position, map, map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine), PathEndMode.ClosestTouch, TraverseParms.For(healer), radius, validator, priorityGetter);

        return medicine;
    }
}