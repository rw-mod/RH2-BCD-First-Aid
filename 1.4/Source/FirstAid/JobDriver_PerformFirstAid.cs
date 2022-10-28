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

        protected Pawn Patient => (Pawn)job.targetA.Thing;

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
            if (Patient != pawn && !pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            if (usesMedicine)
            {
                int num = pawn.Map.reservationManager.CanReserveStack(pawn, MedicineUsed, 10);
                if (num <= 0 || !pawn.Reserve(MedicineUsed, job, 10, Mathf.Min(num, Medicine.GetMedicineCountToFullyHeal(Patient)), null, errorOnFailed))
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
                if (Patient.GetPosture() == PawnPosture.Standing)
                {
                    return true;
                }
                if (MedicineUsed != null && pawn.Faction == Faction.OfPlayer)
                {
                    if (Patient.playerSettings != null && !Patient.playerSettings.medCare.AllowsMedicine(MedicineUsed.def))
                    {
                        return true;
                    }
                }
                return pawn == Patient && pawn.Faction == Faction.OfPlayer 
                && (pawn.playerSettings is null || !pawn.playerSettings.selfTend);
            });
            this.FailOnAggroMentalState(TargetIndex.A);

            AddEndCondition(delegate
            {
                if (pawn.Faction == Faction.OfPlayer && Patient.health.HasHediffsNeedingTend())
                {
                    return JobCondition.Ongoing;
                }
                var condition = (pawn.Faction != Faction.OfPlayer && Patient.health.HasHediffsNeedingTend()) ? JobCondition.Ongoing : JobCondition.Succeeded;
                return condition;
            });
            Toil reserveMedicine = null;
            if (usesMedicine)
            {
                if (pawn.inventory.Contains(TargetB.Thing))
                {
                    yield return new Toil
                    {
                        initAction = delegate
                        {
                            int num = Medicine.GetMedicineCountToFullyHeal(Patient);
                            pawn.inventory.innerContainer.TryTransferToContainer(TargetB.Thing, pawn.carryTracker.innerContainer, num);
                            job.SetTarget(TargetIndex.B, pawn.carryTracker.CarriedThing);
                        }
                    };
                }
                else
                {
                    reserveMedicine = Toils_Tend.ReserveMedicine(TargetIndex.B, Patient).FailOnDespawnedNullOrForbidden(TargetIndex.B);
                    yield return reserveMedicine;
                    yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
                    yield return Toils_Tend.PickupMedicine(TargetIndex.B, Patient).FailOnDestroyedOrNull(TargetIndex.B);
                    yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveMedicine, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
                }

            }
            PathEndMode interactionCell = (Patient == pawn) ? PathEndMode.OnCell : PathEndMode.InteractionCell;
            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, interactionCell);
            yield return gotoToil;
            var waitPeriod = (int)((1f / pawn.GetStatValue(StatDefOf.MedicalTendSpeed) * BaseTendDuration) * TendSpeedMultiplier);
            Toil toil = Toils_General.Wait(waitPeriod)
                    .FailOnCannotTouch(TargetIndex.A, interactionCell).WithProgressBarToilDelay(TargetIndex.A)
                    .PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            toil.activeSkill = (() => SkillDefOf.Medicine);
            if (pawn == Patient && pawn.Faction != Faction.OfPlayer)
            {
                toil.tickAction = delegate
                {
                    if (pawn.IsHashIntervalTick(TicksBetweenSelfTendMotes) && !pawn.Position.Fogged(pawn.Map))
                    {
                        FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
                    }
                };
            }
            yield return toil;
            yield return Toils_Tend.FinalizeTend(Patient);
            if (usesMedicine)
            {
                Toil toil2 = new Toil();
                toil2.initAction = delegate
                {
                    if (MedicineUsed.DestroyedOrNull())
                    {
                        Thing thing = FloatMenuMakerCarryAdder.FindBestMedicine(pawn, Patient);
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