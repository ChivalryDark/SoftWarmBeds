﻿//using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace SoftWarmBeds
{
    public class JobDriver_MakeBed : JobDriver
    {
		protected Thing Bed
		{
			get
			{
				return this.job.GetTarget(TargetIndex.A).Thing;
			}
		}

		protected CompMakeableBed BedComp
		{
			get
			{
				return this.Bed.TryGetComp<CompMakeableBed>();
			}
		}

        protected Thing Bedding
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.Bed;
			Job job = this.job;
			bool result;
			if (pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
			{
				pawn = this.pawn;
				target = this.Bedding;
				job = this.job;
				result = pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
			}
			else
			{
				result = false;
			}
			return result;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
            Log.Message("Toil start:" + this.pawn +" is taking " + Bedding + " to " + Bed);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			base.AddEndCondition(() => (!this.BedComp.Loaded) ? JobCondition.Ongoing : JobCondition.Succeeded);
            this.job.count = 1;
			Toil reserveBedding = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
			yield return reserveBedding;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveBedding, TargetIndex.B, TargetIndex.None, true, null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(MakingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            Toil makeTheBed = new Toil();
            makeTheBed.initAction = delegate
            {
                Pawn actor = makeTheBed.actor;
                BedComp.LoadBedding(actor.CurJob.targetB.Thing.def, actor.CurJob.targetB.Thing);//, 1);
                Building_SoftWarmBed SoftWarmBed = Bed as Building_SoftWarmBed;
                actor.carryTracker.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            };
            makeTheBed.defaultCompleteMode = ToilCompleteMode.Instant;
            makeTheBed.FailOnDespawnedOrNull(TargetIndex.A);
            yield return makeTheBed;
            yield break;
		}

		private const TargetIndex MakeableInd = TargetIndex.A;

		private const TargetIndex BeddingInd = TargetIndex.B;

		private const int MakingDuration = 200;

    }
}
