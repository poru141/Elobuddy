using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace P1_Cassiopeia
{
    class Program
    {
        static void Main(string[] args)
        {
            //happens when done loading
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;

            //used for drawings that dont override game UI
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Damage_Indicator;

        }

        //makes Player.Instance Player
        private static AIHeroClient User = Player.Instance;

        //Cassiopeia Q
        private static Spell.Skillshot Q;
        //Cassiopeia W
        private static Spell.Skillshot W;
        //Cassiopeia E
        private static Spell.Targeted E;
        //Cassiopeia R
        private static Spell.Skillshot R;

        //Declare the menu
        private static Menu CassiopeiaMenu, ComboMenu, DrawingsMenu, LaneClearMenu, LastHitMenu;

        //a list that contains Player spells
        private static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();


        public static float QDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 75f, 120f, 165f, 210f, 255f }[Q.Level] + 0.7f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float WDamage(Obj_AI_Base target)
        {
            if (W.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 40f, 70f, 100f, 130f, 240f }[W.Level] + 0.30f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float EDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 10f, 40f, 70f, 100f, 130f }[E.Level] + 0.45f * Player.Instance.TotalMagicalDamage + 48f +4*User.Level);
            else
                return 0f;
        }
        public static float EWBDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, 48f + 4 * User.Level + 0.1f*User.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float RDamage(Obj_AI_Base target)
        {
            if (R.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 150f, 250f, 350f}[R.Level] + 0.25f * Player.Instance.TotalMagicalDamage + 0.6f * (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage));
            else
                return 0f;
        }

        public static float Damagefromspell(Obj_AI_Base target)
        {
            if (target == null)
            {
                return 0f;
            }
            else
            {
                return QDamage(target) + WDamage(target) + EDamage(target) + RDamage(target) + EDamage(target) + EDamage(target) + EDamage(target) + EDamage(target) + EDamage(target);
            }
        }


        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            //Makes sure you are Cassiopeia
            if (User.ChampionName != "Cassiopeia")
                return;
            Chat.Print("P1 Cassiopeia loaded! Have fun!");
            //Creates the menu
            CassiopeiaMenu = MainMenu.AddMenu("Cass", "P1 Cassiopeia");

            //Creates a SubMenu
            ComboMenu = CassiopeiaMenu.AddSubMenu("Combo");
            LaneClearMenu = CassiopeiaMenu.AddSubMenu("Lane Clear");
            LastHitMenu = CassiopeiaMenu.AddSubMenu("Last Hit");
            DrawingsMenu = CassiopeiaMenu.AddSubMenu("Drawings");



            //Checkbox should be - YourMenu.Add(String MenuID, new CheckBox(String DisplayName, bool DefaultValue);
            ComboMenu.Add("Q", new CheckBox("Use Q in combo"));
            ComboMenu.Add("W", new CheckBox("Use W in combo"));
            ComboMenu.Add("E", new CheckBox("Use E in combo"));
            ComboMenu.Add("R", new CheckBox("Use R in combo"));
            LaneClearMenu.Add("E", new CheckBox("Use E in lane clear"));
            LastHitMenu.Add("E", new CheckBox("Use E in Last hit"));



            Q = new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Circular, 400, null, 300);
           

            W = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Circular, 250, null, 125);
            E = new Spell.Targeted(SpellSlot.E, 750);
            R = new Spell.Skillshot(SpellSlot.R, 825, SkillShotType.Cone, 600, null, (int)((80 * Math.PI) / 180));

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

            //happens on every core tick
            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            //var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            //Chat.Print(QDamage(target));
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (!Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Orbwalker.DisableAttacking = false;
            }
            if(Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }


        }

        private static void LaneClear()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            if (minion == null) return;

           
            if (LaneClearMenu["E"].Cast<CheckBox>().CurrentValue && (EWBDamage(minion) > minion.Health) && E.IsReady() && E.IsInRange(minion))
            {
                E.Cast(minion);
            }
            else if (LaneClearMenu["E"].Cast<CheckBox>().CurrentValue && (EWBDamage(minion) < 2.2*minion.Health) && E.IsReady() && E.IsInRange(minion))
            {
                Chat.Print("casted");
                E.Cast(minion);
            }
        }
        private static void LastHit()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            if (minion == null) return;


            if (LastHitMenu["E"].Cast<CheckBox>().CurrentValue && (EWBDamage(minion) > minion.Health) && E.IsReady() && E.IsInRange(minion))
            {
                E.Cast(minion);
            }
        }
        private static void Combo()
        {
            if (ComboMenu["E"].Cast<CheckBox>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                //checks if target doesn't exist
                if (target == null)
                    return;
                if (target.IsValidTarget(E.Range))
                    E.Cast(target);

            }
            if (ComboMenu["Q"].Cast<CheckBox>().CurrentValue)
            {
                //returns targetSelector target
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                //checks if target doesn't exist
                if (target == null)
                    return;

                //returns the prediction for q on the target
                var Qpred = Q.GetPrediction(target);

                //gives true if hitchance is high
                if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChancePercent >= 80)
                {
                    //casts on the prediction
                    Orbwalker.DisableAttacking = true;
                    Q.Cast(Q.GetPrediction(target).CastPosition);
                }
            }
            if (ComboMenu["W"].Cast<CheckBox>().CurrentValue)
            {
                //returns targetSelector target
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);

                //checks if target doesn't exist
                if (target == null)
                    return;

                //returns the prediction for q on the target
                var Wpred = W.GetPrediction(target);

                //gives true if hitchance is high
                if (target.IsValidTarget(Q.Range) && W.IsReady() && Wpred.HitChance >= HitChance.Medium)
                {
                    //casts on the prediction
                    W.Cast(target);
                }
            }
            if (ComboMenu["R"].Cast<CheckBox>().CurrentValue)
            {
                //returns targetSelector target
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                //checks if target doesn't exist
                if (target == null)
                    return;

                //returns the prediction for q on the target
                var Rpred = R.GetPrediction(target);

                //gives true if hitchance is high
                if (target.IsValidTarget(R.Range) && R.IsReady() && Rpred.HitChance >= HitChance.Medium && target.IsFacing(Player.Instance))
                {
                    //casts on the prediction
                    R.Cast(target);
                }
            }
            if (User.Mana <= Q.ManaCost && User.Mana <= W.ManaCost && User.Mana <= E.ManaCost)
                Orbwalker.DisableAttacking = false;
            else
                Orbwalker.DisableAttacking = true;
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            //returns Each spell from the list that are enabled from the menu
            foreach (var Spell in SpellList.Where(Spell => DrawingsMenu[Spell.Slot.ToString()].Cast<CheckBox>().CurrentValue))
            {
                //Draws a circle with spell range around the player
                Circle.Draw(Spell.IsReady() ? Color.SteelBlue : Color.OrangeRed, Spell.Range, User);
            }



        }
        private static void Damage_Indicator(EventArgs args)
        {
            foreach (var unit in EntityManager.Heroes.Enemies.Where(u => u.IsValidTarget() && u.IsHPBarRendered)
                    )
            {

                if (EDamage(unit) + WDamage(unit) + QDamage(unit) >= unit.Health)
                {

                }

                var damage = Damagefromspell(unit);

                if (damage <= 0)
                {
                    continue;
                }
                var Special_X = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -12 : 0;
                var Special_Y = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -3 : 9;

                var DamagePercent = ((unit.TotalShieldHealth() - damage) > 0
                    ? (unit.TotalShieldHealth() - damage)
                    : 0) / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);
                var currentHealthPercent = unit.TotalShieldHealth() / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);
                var StartPoint = new Vector2((int)(unit.HPBarPosition.X + DamagePercent * 107), (int)unit.HPBarPosition.Y - 5 + 14);
                var EndPoint = new Vector2((int)(unit.HPBarPosition.X + currentHealthPercent * 107) + 1, (int)unit.HPBarPosition.Y - 5 + 14);

                Drawing.DrawLine(StartPoint, EndPoint, 9.82f, System.Drawing.Color.LightGreen);

            }
        }
    }
}
