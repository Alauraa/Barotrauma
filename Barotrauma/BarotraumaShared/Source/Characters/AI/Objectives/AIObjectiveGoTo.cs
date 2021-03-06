﻿using FarseerPhysics;
using Microsoft.Xna.Framework;
using System;

namespace Barotrauma
{
    class AIObjectiveGoTo : AIObjective
    {
        public override string DebugTag => "go to";

        private AIObjectiveFindDivingGear findDivingGear;

        private Vector2 targetPos;

        private bool repeat;
        private bool cannotReach;

        //how long until the path to the target is declared unreachable
        private float waitUntilPathUnreachable;

        private bool getDivingGearIfNeeded;

        public float CloseEnough = 0.5f;

        public bool IgnoreIfTargetDead;

        public bool AllowGoingOutside = false;

        public override float GetPriority(AIObjectiveManager objectiveManager)
        {
            if (FollowControlledCharacter && Character.Controlled == null) { return 0.0f; }
            if (Target != null && Target.Removed) { return 0.0f; }
            if (IgnoreIfTargetDead && Target is Character character && character.IsDead) { return 0.0f; }
                        
            if (objectiveManager.CurrentOrder == this)
            {
                return AIObjectiveManager.OrderPriority;
            }

            return 1.0f;
        }

        public override bool CanBeCompleted
        {
            get
            {
                bool canComplete = !cannotReach && !abandon;
                if (canComplete)
                {
                    if (FollowControlledCharacter && Character.Controlled == null)
                    {
                        canComplete = false;
                    }
                    else if (Target != null && Target.Removed)
                    {
                        canComplete = false;
                    }
                    else if (!repeat && waitUntilPathUnreachable < 0)
                    {
                        if (SteeringManager == PathSteering && PathSteering.CurrentPath != null)
                        {
                            canComplete = !PathSteering.CurrentPath.Unreachable;
                        }
                    }
                }
                if (!canComplete)
                {
#if DEBUG
                    DebugConsole.NewMessage($"{character.Name}: Cannot reach the target.");
#endif
                    character.Speak(TextManager.Get("DialogCannotReach"), identifier: "cannotreach", minDurationBetweenSimilar: 10.0f);
                    character.AIController.SteeringManager.Reset();
                }
                return canComplete;
            }
        }

        public Entity Target { get; private set; }

        public Vector2 TargetPos => Target != null ? Target.SimPosition : targetPos;

        public bool FollowControlledCharacter;

        public AIObjectiveGoTo(Entity target, Character character, bool repeat = false, bool getDivingGearIfNeeded = true)
            : base (character, "")
        {
            this.Target = target;
            this.repeat = repeat;

            waitUntilPathUnreachable = 1.0f;
            this.getDivingGearIfNeeded = getDivingGearIfNeeded;
        }


        public AIObjectiveGoTo(Vector2 simPos, Character character, bool repeat = false, bool getDivingGearIfNeeded = true)
            : base(character, "")
        {
            this.targetPos = simPos;
            this.repeat = repeat;

            waitUntilPathUnreachable = 5.0f;
            this.getDivingGearIfNeeded = getDivingGearIfNeeded;
        }

        protected override void Act(float deltaTime)
        {
            if (FollowControlledCharacter)
            {
                if (Character.Controlled == null) { return; }
                Target = Character.Controlled;
            }

            if (Target == character)
            {
                character.AIController.SteeringManager.Reset();
                return;
            }
            
            waitUntilPathUnreachable -= deltaTime;

            if (!character.IsClimbing)
            {
                character.SelectedConstruction = null;
            }

            if (Target != null) { character.AIController.SelectTarget(Target.AiTarget); }

            Vector2 currTargetPos = Vector2.Zero;

            if (Target == null)
            {
                currTargetPos = targetPos;
            }
            else
            {
                currTargetPos = Target.SimPosition;
                
                //if character is inside the sub and target isn't, transform the position
                if (character.Submarine != null && Target.Submarine == null)
                {
                    currTargetPos -= character.Submarine.SimPosition;
                }
            }

            if (Vector2.DistanceSquared(currTargetPos, character.SimPosition) < CloseEnough * CloseEnough)
            {
                character.AIController.SteeringManager.Reset();
                character.AnimController.TargetDir = currTargetPos.X > character.SimPosition.X ? Direction.Right : Direction.Left;
            }
            else
            {
                bool targetIsOutside = (Target != null && Target.Submarine == null) || 
                    (SteeringManager == PathSteering && PathSteering.CurrentPath != null && PathSteering.CurrentPath.HasOutdoorsNodes);
                if (targetIsOutside && character.CurrentHull != null && !AllowGoingOutside)
                {
                    cannotReach = true;
                }
                else
                {
                    character.AIController.SteeringManager.SteeringSeek(currTargetPos);
                    if (getDivingGearIfNeeded)
                    {
                        if (targetIsOutside ||
                            Target is Hull h && HumanAIController.NeedsDivingGear(h) ||
                            Target is Item i && HumanAIController.NeedsDivingGear(i.CurrentHull) ||
                            Target is Character c && HumanAIController.NeedsDivingGear(c.CurrentHull))
                        {
                            if (findDivingGear == null)
                            {
                                findDivingGear = new AIObjectiveFindDivingGear(character, true);
                                AddSubObjective(findDivingGear);
                            }
                            else if (!findDivingGear.CanBeCompleted)
                            {
                                abandon = true;
                            }
                        }
                    }
                }
            }
        }

        public override bool IsCompleted()
        {
            if (repeat) return false;

            bool completed = false;

            float allowedDistance = CloseEnough;

            if (Target is Item item)
            {
                allowedDistance = Math.Max(ConvertUnits.ToSimUnits(item.InteractDistance), CloseEnough);
                if (item.IsInsideTrigger(character.WorldPosition)) completed = true;
            }
            else if (Target is Character targetCharacter)
            {
                if (character.CanInteractWith(targetCharacter)) completed = true;
            }

            completed = completed || Vector2.DistanceSquared(Target != null ? Target.SimPosition : targetPos, character.SimPosition) < allowedDistance * allowedDistance;

            if (completed) character.AIController.SteeringManager.Reset();

            return completed;
        }

        public override bool IsDuplicate(AIObjective otherObjective)
        {
            AIObjectiveGoTo objective = otherObjective as AIObjectiveGoTo;
            if (objective == null) return false;

            if (objective.Target == Target) return true;

            return (objective.targetPos == targetPos);
        }
    }
}
