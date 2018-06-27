﻿using Barotrauma.Items.Components;
using Barotrauma.Steam;
using FarseerPhysics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Barotrauma
{
    static class SteamAchievementManager
    {
        private static HashSet<string> unlockedAchievements = new HashSet<string>();

        class RoundData
        {
            public List<Reactor> Reactors = new List<Reactor>();

            public bool EnteredCrushDepth;
            public bool ReactorMeltdown;
        }

        private static RoundData roundData;

        public static void OnStartRound()
        {
            roundData = new RoundData();
            foreach (Item item in Item.ItemList)
            {
                Reactor reactor = item.GetComponent<Reactor>();
                if (reactor != null) roundData.Reactors.Add(reactor);
            }
        }

        public static void Update()
        {
            if (Character.Controlled == null || GameMain.GameSession == null) return;

            if (!Character.Controlled.IsDead)
            {
                if (GameMain.GameSession.EventManager.CurrentIntensity > 0.99f)
                {
                    UnlockAchievement("maxintensity");
                }

                if (Character.Controlled.Submarine != null)
                {
                    foreach (Reactor reactor in roundData.Reactors)
                    {
                        if (reactor.Item.Condition <= 0.0f) roundData.ReactorMeltdown = true;
                    }

                    Submarine sub = Character.Controlled.Submarine;
                    //convert submarine velocity to km/h
                    Vector2 submarineVel = Physics.DisplayToRealWorldRatio * ConvertUnits.ToDisplayUnits(sub.Velocity) * 3.6f;
                    //achievement for going > 100 km/h
                    if (Math.Abs(submarineVel.X) > 100.0f) UnlockAchievement("subhighvelocity");

                    //achievement for descending below crush depth and coming back
                    if (sub.Position.Y < SubmarineBody.DamageDepth)
                    {
                        roundData.EnteredCrushDepth = true;
                    }
                    else if (roundData.EnteredCrushDepth && sub.Position.Y > SubmarineBody.DamageDepth * 0.5f)
                    {
                        UnlockAchievement("survivecrushdepth");
                    }

                    float realWorldDepth = Math.Abs(sub.Position.Y - Level.Loaded.Size.Y) * Physics.DisplayToRealWorldRatio;
                    if (realWorldDepth > 5000.0f)
                    {
                        UnlockAchievement("subdeep");
                    }
                }
            }
        }

        public static void OnItemRepaired(Item item, Character fixer)
        {
            if (fixer != null && fixer == Character.Controlled)
            {
                UnlockAchievement("repairdevice");
                UnlockAchievement("repair" + item.Prefab.Identifier);
            }
        }

        public static void OnCharacterKilled(Character character, Character killer)
        {
            if (killer != null && killer == Character.Controlled)
            {
                UnlockAchievement("kill" + character.SpeciesName);
                if (character.CurrentHull == null)
                {
                    UnlockAchievement("kill" + character.SpeciesName + "indoors");
                }
            }

            if (GameMain.Server?.TraitorManager != null)
            {
                //TODO: give clients the appropriate achievement when they complete their traitor objective or kill a traitor
                foreach (Traitor traitor in GameMain.Server.TraitorManager.TraitorList)
                {
                    if (traitor.Character == Character.Controlled && killer == Character.Controlled && traitor.TargetCharacter == character)
                    {
                        //killed the target as a traitor
                        UnlockAchievement("traitorwin");
                    }
                    else if (traitor.Character == character && killer != null && killer == Character.Controlled)
                    {
                        //killed a traitor
                        UnlockAchievement("killtraitor");
                    }
                }
            }
        }

        public static void OnRoundEnded(GameSession gameSession)
        {
            if (gameSession.Mission != null && gameSession.Mission.Completed)
            {
                UnlockAchievement(gameSession.Mission.Prefab.AchievementIdentifier);
            }

            //made it to the destination despite a reactor meltdown
            if (gameSession.Submarine.AtEndPosition && roundData.ReactorMeltdown)
            {
                UnlockAchievement("survivereactormeltdown");
            }
        }
        
        public static void UnlockAchievement(string identifier)
        {
            //already unlocked, no need to do anything
            if (unlockedAchievements.Contains(identifier)) return;

            SteamManager.UnlockAchievement(identifier);

            unlockedAchievements.Add(identifier);
        }
    }
}