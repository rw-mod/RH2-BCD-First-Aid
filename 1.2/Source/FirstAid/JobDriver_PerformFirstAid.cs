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
using Verse.Sound;

namespace FirstAid
{
    public class JobDriver_PerformFirstAid : JobDriver
    {
        private bool usesMedicine;

        private const int BaseTendDuration = 600;

        private const float TendSpeedMultiplier = 0.25f;

        private const int TicksBetweenSelfTendMotes = 100;
        protected Thing MedicineUsed => job.targetB.Thing;

        protected Pawn Deliveree => (Pawn)job.targetA.Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref usesMedicine, "usesMedicine", defaultValue: false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            usesMedicine = (MedicineUsed != null);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Deliveree != pawn && !pawn.Reserve(Deliveree, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            if (usesMedicine)
            {
                int num = pawn.Map.reservationManager.CanReserveStack(pawn, MedicineUsed, 10);
                if (num <= 0 || !pawn.Reserve(MedicineUsed, job, 10, Mathf.Min(num, Medicine.GetMedicineCountToFullyHeal(Deliveree)), null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(delegate
            {
                if (Deliveree.GetPosture() == PawnPosture.Standing)
                {
                    return true;
                }
                if (MedicineUsed != null && pawn.Faction == Faction.OfPlayer)
                {
                    if (Deliveree.playerSettings != null && !Deliveree.playerSettings.medCare.AllowsMedicine(MedicineUsed.def))
                    {
                        return true;
                    }
                }
                return (pawn == Deliveree && pawn.Faction == Faction.OfPlayer && !pawn.playerSettings.selfTend) ? true : false;
            });

            AddEndCondition(delegate
            {
                if (pawn.Faction == Faction.OfPlayer && Deliveree.health.HasHediffsNeedingTend())
                {
                    return JobCondition.Ongoing;
                }
                var condition = (pawn.Faction != Faction.OfPlayer && Deliveree.health.HasHediffsNeedingTend()) ? JobCondition.Ongoing : JobCondition.Succeeded;
                return condition;
            });
            this.FailOnAggroMentalState(TargetIndex.A);
            Toil reserveMedicine = null;
            Log.Message("usesMedicine: " + usesMedicine + " - "  + TargetB);
            if (usesMedicine)
            {
                reserveMedicine = Toils_Tend.ReserveMedicine(TargetIndex.B, Deliveree).FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return reserveMedicine;
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
                yield return Toils_Tend.PickupMedicine(TargetIndex.B, Deliveree).FailOnDestroyedOrNull(TargetIndex.B);
                yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveMedicine, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            }
            PathEndMode interactionCell = (Deliveree == pawn) ? PathEndMode.OnCell : PathEndMode.InteractionCell;
            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, interactionCell);
            yield return gotoToil;
            var waitPeriod = (int)((1f / pawn.GetStatValue(StatDefOf.MedicalTendSpeed) * BaseTendDuration) * TendSpeedMultiplier);
            Toil toil = Toils_General.Wait(waitPeriod)
                    .FailOnCannotTouch(TargetIndex.A, interactionCell).WithProgressBarToilDelay(TargetIndex.A)
                    .PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            toil.activeSkill = (() => SkillDefOf.Medicine);
            if (pawn == Deliveree && pawn.Faction != Faction.OfPlayer)
            {
                toil.tickAction = delegate
                {
                    if (pawn.IsHashIntervalTick(TicksBetweenSelfTendMotes) && !pawn.Position.Fogged(pawn.Map))
                    {
                        MoteMaker.ThrowMetaIcon(pawn.Position, pawn.Map, ThingDefOf.Mote_HealingCross);
                    }
                };
            }
            yield return toil;
            yield return Toils_Tend.FinalizeTend(Deliveree);
            if (usesMedicine)
            {
                Toil toil2 = new Toil();
                toil2.initAction = delegate
                {
                    if (MedicineUsed.DestroyedOrNull())
                    {
                        Thing thing = FloatMenuMakerCarryAdder.FindBestMedicine(pawn, Deliveree);
                        if (thing != null)
                        {
                            job.targetB = thing;
                            JumpToToil(reserveMedicine);
                        }
                    }
                };
                yield return toil2;
            }
            yield return Toils_Jump.Jump(gotoToil);
        }
    }
}