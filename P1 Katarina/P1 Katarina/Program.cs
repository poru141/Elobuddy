using System;
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
        private static Spell.Targeted E;
        //Katarina R
        private static Spell.Active R;

        //Declare the menu
        private static Menu KatarinaMenu, ComboMenu, LaneClearMenu, LastHitMenu, HarassAutoharass, WardJumpMenu, DrawingsMenu;


        //a list that contains Player spells
        private static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();

        private static bool HasRBuff()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Mixed);
            return Player.HasBuff("KatarinaR") || Player.Instance.Spellbook.IsChanneling ||
                   Player.HasBuff("katarinarsound"); //|| target.HasBuff("Grevious") && sender.IsMe
        }
        //ggggsdfgfdgfdsg

        private static long _lastCheck;
        public static long LastWard;
        private static Vector3 _jumpPos;
        public static Menu WardjumpMenu;
        private static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender is Obj_AI_Base && sender.Name.ToLower().Contains("ward") && sender.Distance(_Player) < 600 && _jumpPos.Distance(sender) < 200)
            {
                E.Cast((Obj_AI_Base)sender);
            }
        }


        public static void WardJump(Vector3 pos, bool max, bool cursorOnly)
        {
            if (WardjumpMenu["wardjumpKeybind"].Cast<KeyBind>().CurrentValue)
                Orbwalker.OrbwalkTo(Game.CursorPos.Extend(Game.CursorPos, 200).To3D());
            if (!_jumpPos.IsValid() || _lastCheck <= Environment.TickCount)
            {
                _jumpPos = pos;
                _lastCheck = Environment.TickCount + WardjumpMenu["checkTime"].Cast<Slider>().CurrentValue;
            }

            var jumpPoint = _jumpPos;
            if (max && jumpPoint.Distance(_Player.Position) > 600)
            {
                jumpPoint = _Player.Position.Extend(_jumpPos, 600).To3D();
            }
            else if (cursorOnly && jumpPoint.Distance(_Player.Position) > 600)
            {
                return;
            }

            _jumpPos = jumpPoint;
            var ward =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(a => a.IsAlly && a.Distance(_jumpPos) < 100);
            if (ward != null)
            {
                if (E.IsReady())
                {
                    Player.CastSpell(SpellSlot.E, ward);
                }
            }
            else
            {
                var wardSpot = GetWardSlot();
                if (wardSpot == null)
                {
                    return;
                }
                if (E.IsReady() && LastWard + 400 < Environment.TickCount)
                {
                    GetWardSlot().Cast(_jumpPos);
                    LastWard = Environment.TickCount;
                }
            }
        }

        public static InventorySlot GetWardSlot()
        {
            var wardIds = new[] { ItemId.Warding_Totem_Trinket, ItemId.Sightstone, ItemId.Ruby_Sightstone, ItemId.Vision_Ward, ItemId.Greater_Stealth_Totem_Trinket };
            return _Player.InventoryItems.FirstOrDefault(a => wardIds.Contains(a.Id) && a.IsWard && a.CanUseItem());
        }


        public static float QDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 75f, 115f, 155f, 195f, 235f }[Q.Level] + 0.65f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float QTDamage(Obj_AI_Base target)
        {
            if (Q.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 60f, 85f, 110f, 135f, 160f }[Q.Level] + 0.45f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float WDamage(Obj_AI_Base target)
        {
            if (W.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 40f, 75f, 110f, 145f, 180f }[W.Level] + 0.25f * Player.Instance.TotalMagicalDamage + 0.6f * (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage));
            else
                return 0f;
        }

        public static float EDamage(Obj_AI_Base target)
        {
            if (E.IsReady())
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, new[] { 0f, 40f, 70f, 100f, 130f, 160f }[Q.Level] + 0.25f * Player.Instance.TotalMagicalDamage);
            else
                return 0f;
        }

        public static float RDamage(Obj_AI_Base target)
        {
            if (!R.IsOnCooldown)
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, (new[] { 0f, 35f, 55f, 750f }[R.Level] + 0.25f * Player.Instance.TotalMagicalDamage + 0.6f * (Player.Instance.TotalAttackDamage - Player.Instance.BaseAttackDamage)) * 10);
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
                return QDamage(target) + WDamage(target) + EDamage(target) + RDamage(target);
            }
        }

        private static void OnNotify(GameNotifyEventArgs args)
        {
            if (args.EventId == GameEventId.OnChampionKill)
            {
                //Chat.Print("Kill");
            }

            if (args.EventId == GameEventId.OnDamageTaken)
            {
                //Chat.Print("Damage");
            }

        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            
            //Makes sure you are Katarina fdsgfdgdsgsd
            if (User.ChampionName != "Katarina")
                return;
            
            Chat.Print("P1 Katarina loaded! Have fun!");
            //Creates the menu
            KatarinaMenu = MainMenu.AddMenu("Katarina", "P1 Katarina");

            //Creates a SubMenu
            ComboMenu = KatarinaMenu.AddSubMenu("Combo");
            LaneClearMenu = KatarinaMenu.AddSubMenu("Lane Clear");
            LastHitMenu = KatarinaMenu.AddSubMenu("LastHit");
            HarassAutoharass = KatarinaMenu.AddSubMenu("Harass/AutoHarass");
            WardjumpMenu = KatarinaMenu.AddSubMenu("WardJump");
            DrawingsMenu = KatarinaMenu.AddSubMenu("Drawings");



            WardjumpMenu.AddGroupLabel("Wardjump Settings");
            var a = WardjumpMenu.Add("alwaysMax", new CheckBox("Always Jump To Max Range"));
            var b = WardjumpMenu.Add("onlyToCursor", new CheckBox("Always Jump To Cursor", false));
            a.OnValueChange += delegate { if (a.CurrentValue) b.CurrentValue = false; };
            b.OnValueChange += delegate { if (b.CurrentValue) a.CurrentValue = false; };
            WardjumpMenu.AddSeparator();
            WardjumpMenu.AddLabel("Time Modifications");
            WardjumpMenu.Add("checkTime", new Slider("Position Reset Time (ms)", 0, 1, 2000));
            WardjumpMenu.AddSeparator();
            WardjumpMenu.AddLabel("Keybind Settings");
            var wj = WardjumpMenu.Add("wardjumpKeybind",
            new KeyBind("WardJump", false, KeyBind.BindTypes.HoldActive, 'T'));
            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnTick += delegate
            {
                if (wj.CurrentValue)
                {
                    WardJump(Game.CursorPos, a.CurrentValue, b.CurrentValue);
                    return;
                }
            };

            //Checkbox should be - YourMenu.Add(String MenuID, new CheckBox(String DisplayName, bool DefaultValue);
            ComboMenu.Add("Q", new CheckBox("Use Q in combo"));
            ComboMenu.Add("W", new CheckBox("Use W in combo"));
            ComboMenu.Add("E", new CheckBox("Use E in combo"));
            ComboMenu.Add("R", new CheckBox("Use R in combo"));
            LaneClearMenu.Add("Q", new CheckBox("Use Q in lane clear"));
            LaneClearMenu.Add("W", new CheckBox("Use W in lane clear"));
            LastHitMenu.Add("Q", new CheckBox("Use Q in last hit"));
            LastHitMenu.Add("W", new CheckBox("Use W in last hit"));
            HarassAutoharass.Add("HQ", new CheckBox("Use Q in harass"));
            HarassAutoharass.Add("HW", new CheckBox("Use W in harass"));
            HarassAutoharass.Add("AHQ", new CheckBox("Use Q in auto harass"));
            HarassAutoharass.Add("AHW", new CheckBox("Use W in auto harass"));
            


            //Giving Q values
            Q = new Spell.Targeted(SpellSlot.Q, 675, DamageType.Magical);

            //Giving W values
            W = new Spell.Active(SpellSlot.W, 400, DamageType.Magical);

            //Giving E values
            E = new Spell.Targeted(SpellSlot.E, 700, DamageType.Magical);

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

            Game.OnNotify += OnNotify;
            //used for drawings that dont override game UI
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Damage_Indicator;


            //happens on every core tick
            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {

            
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

            if (HarassAutoharass["AHQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target.IsValidTarget())
                    Q.Cast(target);
            }
            if (HarassAutoharass["AHW"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target.IsValidTarget())
                    W.Cast();
            }

           // kills = User.ChampionsKilled;
            //assists = User.Assists;
        }
        private static void Harass()
        {
            if (HarassAutoharass["HQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (target.IsValidTarget())
                    Q.Cast(target);
            }
            if (HarassAutoharass["HW"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target.IsValidTarget())
                    W.Cast();
            }
        }
        private static void LaneClear()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(a => a.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            if (minion == null) return;

            if (LaneClearMenu["Q"].Cast<CheckBox>().CurrentValue && (QTDamage(minion) > minion.Health) && Q.IsReady())
            {
                Program.Q.Cast(minion);
            }
            if (LaneClearMenu["W"].Cast<CheckBox>().CurrentValue && (WDamage(minion) > minion.Health) && W.IsReady() && W.IsInRange(minion))
            {
                Program.W.Cast();
            }
        }
        private static void LastHit()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.Distance(Player.Instance) < Q.Range).OrderBy(a => a.Health);
            var minion = minions.FirstOrDefault();
            if (!minion.IsValidTarget())
                return;
            if (LastHitMenu["Q"].Cast<CheckBox>().CurrentValue && (QTDamage(minion) >= minion.Health) && Q.IsReady())
            {
                Program.Q.Cast(minion);
            }
            if (LastHitMenu["W"].Cast<CheckBox>().CurrentValue && (WDamage(minion) >= minion.Health) && W.IsReady() && W.IsInRange(minion))
            {
                Program.W.Cast();
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (EDamage(target) + WDamage(target) >= target.Health)
            {
                //Chat.Print("e w");


                E.Cast(target);
                Program.W.Cast();
            }



            else if (QDamage(target) + WDamage(target) + EDamage(target) >= target.Health)
            {
                //Chat.Print("e q w");


                E.Cast(target);
                Q.Cast(target);
                Program.W.Cast();
            }
            else if (HasRBuff())
            {
                Orbwalker.DisableAttacking = true;
                Orbwalker.DisableMovement = true;
            }
            else if (!HasRBuff())
            {
                //returns true if Q checkbox is checked
                //Chat.Print("nothing");
                if (ComboMenu["Q"].Cast<CheckBox>().CurrentValue)
                {
                    //returns targetSelector target

                    //checks if target doesn't exist
                    if (target == null)
                        return;


                    if (Q.CanCast(target))
                    {
                        Q.Cast(target);
                        if (target == null || E.IsOnCooldown)
                            return;
                        else
                        {
                            if (ComboMenu["E"].Cast<CheckBox>().CurrentValue)
                                E.Cast(target);
                        }

                    }
                    else if (Q.IsOnCooldown)
                    {
                        if (ComboMenu["E"].Cast<CheckBox>().CurrentValue)
                        {
                            if (E.CanCast(target))
                                E.Cast(target);
                        }

                    }
                }

                if (ComboMenu["W"].Cast<CheckBox>().CurrentValue)
                {
                    if (target == null)
                        return;
                    else
                    {
                        if (W.CanCast(target))
                            Program.W.Cast();
                    }
                }
                if (ComboMenu["R"].Cast<CheckBox>().CurrentValue)
                {
                    //returns targetSelector target
                    //checks if target doesn't exist
                    if (target == null)
                        return;

                    //returns the prediction for q on the target

                    //gives true if hitchance is high
                    if (target.IsValidTarget(R.Range) && R.IsReady() && E.IsOnCooldown)
                    {
                        //casts on the prediction
                        Orbwalker.DisableMovement = true;
                        Orbwalker.DisableAttacking = true;
                        Core.DelayAction(() => Program.R.Cast(), 250);


                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {

            //returns Each spell from the list that are enabled from the menu
            foreach (var Spell in SpellList.Where(Spell => DrawingsMenu[Spell.Slot.ToString()].Cast<CheckBox>().CurrentValue))
            {
                //Draws a circle with spell range around the player
                Circle.Draw(Spell.IsReady() ? Color.SteelBlue : Color.OrangeRed, Spell.Range, User);
            }

            if (WardjumpMenu["drawWJ"].Cast<CheckBox>().CurrentValue && WardjumpMenu["wardjumpKeybind"].Cast<KeyBind>().CurrentValue)
            {
                Circle.Draw(Color.Teal, 100, _jumpPos);
                Circle.Draw(Color.White, 600, _Player.Position);
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

                Drawing.DrawLine(StartPoint, EndPoint, 9.82f, System.Drawing.Color.DeepPink);

            }
        }
    }
}
