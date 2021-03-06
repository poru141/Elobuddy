﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;

using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using System.Timers;
using static P1_Katarina.Program;
namespace P1_Katarina
{
    class Program
    {

        static void Main(string[] args)
        {
            //happens when done loading
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;

        }



        //makes Player.Instance Player
        private static AIHeroClient User = Player.Instance;

        //Katarina Q
        private static Spell.Targeted Q;
        //Katarina W
        private static Spell.Active W;
        //Katarina E
        private static Spell.Skillshot E;
        //Katarina R
        private static Spell.Active R;
        private static List<Dagger> daggers = new List<Dagger>();


        private static int daggertime = 0;
        private static Vector3 previouspos;
        private static List<float> daggerstart = new List<float>();
        private static List<float> daggerend = new List<float>();
        public static List<Vector2> daggerpos = new List<Vector2>();
        public static Vector3 qdaggerpos;
        public static Vector3 wdaggerpos;
        public static int comboNum;





        public class Dagger
        {


            public float StartTime { get; set; }
            public float EndTime { get; set; }
            public Vector3 Position { get; set; }
            public int Width = 230;
        }

        //Declare the menu
        private static Menu KatarinaMenu, ComboMenu, LaneClearMenu, LastHitMenu, HarassAutoharass, DrawingsMenu, KillStealMenu, HumanizerMenu;


        //a list that contains Player spells
        private static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();
        public static bool harassNeedToEBack = false;
        private static AIHeroClient target;

        private static bool HasRBuff()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Mixed);
            return Player.Instance.Spellbook.IsChanneling;
        }
        public static float SpinDamage(Obj_AI_Base target)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, ((User.Level / 1.75f) + 3f) * User.Level + 71.5f + 1.25f * (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage) + User.TotalMagicalDamage * new[] { .55f, .70f, .80f, 1.00f }[R.Level]);

        }
        public static float QDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 75f, 105f, 135f, 165f, 195f }[Q.Level] + 0.3f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }
        public static float WDamage(Obj_AI_Base target)
        {
            return 0f;
        }
        public static float EDamage(Obj_AI_Base target)
        {
            if (E.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 25f, 40f, 55f, 70f, 85f }[E.Level] + 0.25f * Player.Instance.TotalMagicalDamage + 0.5f * User.TotalAttackDamage);
            else
                return 0f;
        }
        public static float RDamage(Obj_AI_Base target)
        {
            if (!R.IsOnCooldown)
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, (new[] { 0f, 375f, 562.5f, 750f }[R.Level] + 2.85f * Player.Instance.TotalMagicalDamage + 3.3f * (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage)));
            else
                return 0f;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {

            //Makes sure you are Katarina fdsgfdgdsgsd
            if (User.ChampionName != "Katarina")
                return;

            //print("P1 Katarina loaded! Have fun!");
            //Creates the menu
            KatarinaMenu = MainMenu.AddMenu("Katarina", "P1 Katarina");

            //Creates a SubMenu
            ComboMenu = KatarinaMenu.AddSubMenu("Combo");
            LaneClearMenu = KatarinaMenu.AddSubMenu("Lane Clear");
            LastHitMenu = KatarinaMenu.AddSubMenu("LastHit");
            HarassAutoharass = KatarinaMenu.AddSubMenu("Harass/AutoHarass");
            KillStealMenu = KatarinaMenu.AddSubMenu("Killsteal");

            HumanizerMenu = KatarinaMenu.AddSubMenu("Humanizer");
            DrawingsMenu = KatarinaMenu.AddSubMenu("Drawings");

            //Checkbox should be - YourMenu.Add(String MenuID, new CheckBox(String DisplayName, bool DefaultValue);
            ComboMenu.AddLabel("I don't know what to have here, if you have any suggestions please tell me");
            ComboMenu.Add("EAA", new CheckBox("Only use e if target is outside auto attack range"));
            LaneClearMenu.Add("Q", new CheckBox("Use Q in lane clear"));
            LastHitMenu.Add("Q", new CheckBox("Use Q in last hit"));
            HarassAutoharass.Add("HQ", new CheckBox("Use Q in harass"));
            HarassAutoharass.Add("CC", new CheckBox("Use E reset combo in harass"));
            HarassAutoharass.Add("AHQ", new CheckBox("Use Q in auto harass"));
            KillStealMenu.Add("Q", new CheckBox("Use Q to killsteal"));
            KillStealMenu.Add("R", new CheckBox("Use R to killsteal", false));
            HumanizerMenu.Add("Q", new Slider("Q delay", 0, 0, 1000));
            HumanizerMenu.Add("W", new Slider("W delay", 0, 0, 1000));
            HumanizerMenu.Add("E", new Slider("E delay", 0, 0, 1000));
            HumanizerMenu.Add("R", new Slider("R delay", 0, 0, 1000));



            //Giving Q values
            Q = new Spell.Targeted(SpellSlot.Q, 600, DamageType.Magical);

            //Giving W values
            W = new Spell.Active(SpellSlot.W, 150, DamageType.Magical);

            //Giving E values
            E = new Spell.Skillshot(SpellSlot.E, 700, EloBuddy.SDK.Enumerations.SkillShotType.Circular, 7, null, 150, DamageType.Magical);

            //Giving R values
            R = new Spell.Active(SpellSlot.R, 550, DamageType.Magical);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Creating menu using foreach from a list
            foreach (var Spell in SpellList)
            {
                //Creates checkboxes using Spell Slot
                DrawingsMenu.Add(Spell.Slot.ToString(), new CheckBox("Draw " + Spell.Slot));
            }


            //used for drawings that dont override game UI
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Damage_Indicator;
            //Drawing.OnEndScene += Draw_Q;


            //happens on every core tick
            Game.OnTick += Game_OnTick;
            Game.OnTick += Game_OnTick1;
        }

        private static void Game_OnTick1(EventArgs args)
        {
            if (HarassAutoharass["AHQ"].Cast<CheckBox>().CurrentValue)
            {
                target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                castQ(target);
            }
            target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (QDamage(target) >= target.Health && KillStealMenu["Q"].Cast<CheckBox>().CurrentValue)
                castQ(target);
            target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            if (WDamage(target) >= target.Health && KillStealMenu["W"].Cast<CheckBox>().CurrentValue)
                CastW();
            target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (EDamage(target) >= target.Health && KillStealMenu["E"].Cast<CheckBox>().CurrentValue)
                CastE(target.Position);
            target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (EDamage(target) + WDamage(target) >= target.Health && KillStealMenu["EW"].Cast<CheckBox>().CurrentValue)
            {
                CastE(target.Position);
                CastW();
            }
        }

        public static void castQ(Obj_AI_Base target)
        {

            Q.Cast(target);

            // daggers.Add(new Dagger() { StartTime = Game.Time + 2, EndTime = Game.Time + 7, Position = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position });

            qdaggerpos = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position;


        }
        private static void CastW()
        {

            W.Cast();



            //daggers.Add(new Dagger() { StartTime = Game.Time + 1.25f, EndTime = Game.Time + 6.25f, Position = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position });

            wdaggerpos = User.Position;
        }
        private static void CastE(Vector3 target)
        {
            if (daggers.Count == 0 && !HasRBuff())
                E.Cast(target);
            foreach (Dagger dagger in daggers)
            {

                {

                }
                if (target.Distance(dagger.Position) <= 550)
                    User.Spellbook.CastSpell(E.Slot, dagger.Position.Extend(target, 150).To3D(), false, false);

                else if (ComboMenu["EAA"].Cast<CheckBox>().CurrentValue && target.Distance(User) >= User.GetAutoAttackRange())
                    E.Cast(target);
                else if (!ComboMenu["EAA"].Cast<CheckBox>().CurrentValue)
                    E.Cast(target);
                else
                    return;
            }





        }
        private static void Game_OnTick(EventArgs args)
        {



            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            // //print(target.Direction);
            // else if(!target.IsFacing(User))
            // {
            //  //print("no");
            // }

            if (HasRBuff())
            {
                Orbwalker.DisableMovement = true;
                Orbwalker.DisableAttacking = true;
            }
            else
            {
                Orbwalker.DisableMovement = false;
                Orbwalker.DisableAttacking = false;
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {

                LastHit();

            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (!Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Orbwalker.DisableAttacking = false;
            }






            for (var index = daggers.Count - 1; index >= 0; index--)
            {

                ////print("dagger: " + daggers[index].EndTime);

                if (User.Distance(daggers[index].Position) <= daggers[index].Width && Game.Time >= daggers[index].StartTime || daggers[index] == null || Game.Time >= daggers[index].EndTime)
                {
                    daggers.RemoveAt(index);

                }
            }

            // kills = User.ChampionsKilled;
            //assists = User.Assists;

            // if(target.IsFacing(User))



            var DaggerFirst = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position;


            if (ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position != previouspos)
            {
                //print("Added dagger");
                daggers.Add(new Dagger() { StartTime = Game.Time + 1.25f, EndTime = Game.Time + 5.1f, Position = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position });
                previouspos = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid).Position;
            }
        }
        private static void Harass()
        {

            if (HarassAutoharass["HQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target.IsValidTarget())
                    castQ(target);
            }
            if (HarassAutoharass["CC"].Cast<CheckBox>().CurrentValue)
            {
                if (harassNeedToEBack && E.IsReady())
                {
                    User.Spellbook.CastSpell(E.Slot, wdaggerpos, false, false);
                    harassNeedToEBack = false;
                }


                target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target.IsValidTarget() && !harassNeedToEBack)
                {
                    Core.DelayAction(() => castQ(target), HumanizerMenu["Q"].Cast<Slider>().CurrentValue);
                    Core.DelayAction(() => CastW(), HumanizerMenu["W"].Cast<Slider>().CurrentValue + 50);
                    Core.DelayAction(() => CastE(target.Position), HumanizerMenu["E"].Cast<Slider>().CurrentValue + 250);
                    if (E.IsOnCooldown)
                        harassNeedToEBack = true;


                }

            }
        }
        private static void LaneClear()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            if (minion == null) return;

            if (LaneClearMenu["Q"].Cast<CheckBox>().CurrentValue && (QDamage(minion) > minion.Health) && Q.IsReady())
            {
                Program.castQ(minion);
            }

        }
        private static void LastHit()
        {

            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            //print(minion);
            if (!minion.IsValidTarget())
                return;
            if (LastHitMenu["Q"].Cast<CheckBox>().CurrentValue && (QDamage(minion) >= minion.Health) && Q.IsReady())
            {
                castQ(minion);
            }

        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (E.IsReady() && Q.IsReady() && W.IsReady() && comboNum == 0)
            {
                if (!HasRBuff() || (HasRBuff() && target.Health < QDamage(target) + WDamage(target) + EDamage(target) + (2f * SpinDamage(target))))
                    comboNum = 1;
            }

            else if (E.IsReady() && Q.IsReady() && comboNum == 0)
            {
                if (!HasRBuff() || (HasRBuff() && target.Health < QDamage(target) + EDamage(target) + SpinDamage(target)))
                    comboNum = 2;
            }

            else if (W.IsReady() && E.IsReady() && comboNum == 0)
            {
                if (!HasRBuff() || (HasRBuff() && target.Health < EDamage(target) + SpinDamage(target)))
                    comboNum = 3;
            }

            else if (E.IsReady() && comboNum == 0)
            {
                if (!HasRBuff() || (HasRBuff() && target.Health < EDamage(target)))
                    comboNum = 4;
            }

            else if (Q.IsReady() && comboNum == 0)
            {
                if (!HasRBuff() || (HasRBuff() && target.Health < QDamage(target)))
                    comboNum = 5;
            }

            else if (W.IsReady() && comboNum == 0 && User.Distance(target) <= 300)
            {
                if (!HasRBuff())
                    comboNum = 6;
            }

            else if (R.IsReady() && comboNum == 0 && User.Distance(target)<=400)
                comboNum = 7;

            //combo 1, Q W and E
            if (comboNum == 1)
            {
                castQ(target);
                Core.DelayAction(() => CastE(User.Position.Extend(target.Position, User.Distance(target) + 140).To3D()), 0);
                Core.DelayAction(() => CastW(), 50);

                if (Q.IsOnCooldown && W.IsOnCooldown && E.IsOnCooldown)
                    comboNum = 0;
            }

            //combo 2, Q and E
            if (comboNum == 2)
            {
                castQ(target);
                Core.DelayAction(() => CastE(User.Position.Extend(target.Position, User.Distance(target) + 140).To3D()), 0);

                if (Q.IsOnCooldown && E.IsOnCooldown)
                    comboNum = 0;
            }

            //combo 3, W and E
            if (comboNum == 3)
            {
                Core.DelayAction(() => CastE(User.Position.Extend(target.Position, User.Distance(target) + 140).To3D()), 0);
                Core.DelayAction(() => CastW(), 50);


                if (W.IsOnCooldown && E.IsOnCooldown)
                    comboNum = 0;
            }

            //combo 4, E
            if (comboNum == 4)
            {
                CastE(target.Position);
                comboNum = 0;
            }

            //combo 5, Q
            if (comboNum == 5)
            {
                castQ(target);
                comboNum = 0;
            }

            //combo 6, W
            if (comboNum == 6)
            {
                CastW();
                comboNum = 0;
            }

            //combo 7, R
            if (comboNum == 7)
            {
                R.Cast();
                comboNum = 0;
            }

        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Dagger dagger in daggers)
            {
                if (dagger.StartTime <= Game.Time)
                {
                    Circle.Draw(Color.SandyBrown, 140, dagger.Position);
                }
                else
                    Circle.Draw(Color.Red, 140, dagger.Position);


            }
            var DaggerFirst = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid);
            var DaggerLast = ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid);
            //returns Each spell from the list that are enabled from the menu

            //Circle.Draw(Color.Green, 150, ObjectManager.Get<Obj_AI_Minion>().LastOrDefault(a => a.Name == "HiddenMinion" && a.IsValid));

            foreach (var Spell in SpellList.Where(Spell => DrawingsMenu[Spell.Slot.ToString()].Cast<CheckBox>().CurrentValue))
            {

                //Draws a circle with spell range around the player
                //Circle.Draw(Color.Green, 150, DaggerFirst.Position);
                //Circle.Draw(Color.Green, 150, DaggerLast.Position);
                Circle.Draw(Spell.IsReady() ? Color.SteelBlue : Color.OrangeRed, Spell.Range, User);
            }

        }
        private static void Damage_Indicator(EventArgs args)
        {

            foreach (var unit in EntityManager.Heroes.Enemies.Where(x => x.VisibleOnScreen && x.IsValidTarget() && x.IsHPBarRendered))
            {
                var damage = 0f;
                if (Q.IsReady() && W.IsReady() && E.IsReady())
                    damage = QDamage(unit) + WDamage(unit) + EDamage(unit) + (2f * SpinDamage(unit));
                else if (Q.IsReady() && W.IsReady())
                    damage = QDamage(unit) + WDamage(unit) + SpinDamage(unit);
                else if (Q.IsReady() && E.IsReady())
                    damage = QDamage(unit) + EDamage(unit) + SpinDamage(unit);
                else if (W.IsReady() && E.IsReady())
                    damage = EDamage(unit) + WDamage(unit) + SpinDamage(unit);
                else if (Q.IsReady())
                    damage = QDamage(unit);
                else if (W.IsReady())
                    damage = WDamage(unit);
                else if (E.IsReady())
                    damage = EDamage(unit);
                if (!R.IsOnCooldown)
                    damage += RDamage(unit);
                //Chat.Print(damage);
                var Special_X = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -12 : 0;
                var Special_Y = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -3 : 9;

                var DamagePercent = ((unit.TotalShieldHealth() - damage) > 0
                    ? (unit.TotalShieldHealth() - damage)
                    : 0) / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);
                var currentHealthPercent = unit.TotalShieldHealth() / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);
                var StartPoint = new Vector2((int)(unit.HPBarPosition.X + DamagePercent * 107), (int)unit.HPBarPosition.Y - 5 + 14);
                var EndPoint = new Vector2((int)(unit.HPBarPosition.X + currentHealthPercent * 107) + 1, (int)unit.HPBarPosition.Y - 5 + 14);

                Drawing.DrawLine(StartPoint, EndPoint, 9.82f, System.Drawing.Color.SandyBrown);

            }
        }
    }
}
