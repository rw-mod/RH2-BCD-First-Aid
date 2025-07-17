using RimWorld;
using Verse;
using Verse.AI;

namespace FirstAid;

[StaticConstructorOnStartup]
public class FloatMenuOptionProvider_FirstAid : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        return context.FirstSelectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !context.FirstSelectedPawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);
    }

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!clickedPawn.health.HasHediffsNeedingTend() || clickedPawn.GetPosture() == PawnPosture.Standing || !clickedPawn.RaceProps.IsFlesh)
        {
            return null;
        }

        if (!context.FirstSelectedPawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, ignoreOtherReservations: true))
        {
            return null;
        }

        FloatMenuOption floatMenuOption = new FloatMenuOption( "RH.PerformFirstAid".Translate(clickedPawn.Named("PAWN")),
            delegate
            {
                if (clickedPawn.Drafted)
                {
                    clickedPawn.drafter.Drafted = false;
                }
                Thing medicine = Utility.FindBestMedicine(context.FirstSelectedPawn, clickedPawn);
                Job job = new Job(CPDefOf.CP_FirstAid, clickedPawn, medicine);
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            },
            MenuOptionPriority.High, null, clickedPawn);

        return FloatMenuUtility.DecoratePrioritizedTask(floatMenuOption, context.FirstSelectedPawn, clickedPawn);
    }
}