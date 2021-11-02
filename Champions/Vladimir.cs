using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Damages.Spells;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Vladimir : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 600f) { Delay = 0.25f };
            W = new Spell(SpellSlot.W, 150f) { Delay = 0f };
            E = new Spell(SpellSlot.E, 600f) { Width = 120f, Speed = 4000f, Delay = 0f };
            R = new Spell(SpellSlot.R, 625f) { Width = 375f, Delay = 0f, IsSkillShot = true, Collision = false, Type = SpellType.Circle, };
        }
        protected override void SetupEvents()
        {
            GameEvent.OnGameTick += OnTick;
            AIBaseClient.OnDoCast += OnDoCast;
            Drawing.OnDraw += OnDraw;
        }
        protected override void SetupMenus()
        {
            MainMenu = new Menu(Player.CharacterName, "[the_sun] " + Player.CharacterName, true).Attach();

            QMenu = MainMenu.Add(new Menu("Q", "[Q] Transfusion"));
            {
                QMenu.Add(new MenuBool("AutoQ", "Auto Q Harass", true));
                QMenu.Add(new MenuBool("QFarm", "Use Q Farm", true));
            }

            EMenu = MainMenu.Add(new Menu("E", "[E] Tides of Blood"));
            {
                EMenu.Add(new MenuBool("EFarm", "Use E Farm", true));
            }

            RMenu = MainMenu.Add(new Menu("R", "[R] Hemoplague"));
            {
                RMenu.Add(new MenuSlider("Raoe", "R AoE", 2, 2, 5));
            }

            DrawMenu = MainMenu.Add(new Menu("Draw", "Draw Range"));
            {
                DrawMenu.Add(new MenuBool("Q", "Draw Q Range"));
                DrawMenu.Add(new MenuBool("W", "Draw W Range"));
                DrawMenu.Add(new MenuBool("E", "Draw E Range"));
                DrawMenu.Add(new MenuBool("R", "Draw R Range"));
                DrawMenu.Add(new MenuSeparator("xx", " "));
                DrawMenu.Add(new MenuBool("OnlyReady", "Draw Only Ready"));
            }
        }

        private void OnTick(EventArgs args)
        {
            if (Player == null || Player.IsDead || Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }

            if (QMenu["AutoQ"].GetValue<MenuBool>().Enabled && (Orbwalker.ActiveMode != OrbwalkerMode.Combo || Orbwalker.ActiveMode != OrbwalkerMode.Harass) && !Player.IsUnderEnemyTurret())
            {
                OnHarassUpdate();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    OnComboUpdate();
                    break;
                case OrbwalkerMode.Harass:
                    OnHarassUpdate();
                    break;
                case OrbwalkerMode.LaneClear:
                    OnLaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    OnLastHit();
                    break;
            }
        }
        private void OnComboUpdate()
        {
            AIHeroClient target;
            if (R.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= R.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && !target.IsDead)
                {
                    PredictionOutput output = R.GetPrediction(target);

                    if ((target.HealthPercent <= 60 && target.HealthPercent >= 20) || target == TargetSelector.SelectedTarget || output.AoeTargetsHitCount >= RMenu["Raoe"].GetValue<MenuSlider>().Value)
                    {
                        if(output.Hitchance >= HitChance.Low)
                        {
                            R.Cast(output.CastPosition);
                        }
                    }
                }
            }
            if (Q.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= Q.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && !target.IsDead && target.InRange(Q.Range))
                {
                    Q.Cast(target);
                }
            }
            if(E.IsReady())
            {
                if(Player.CountEnemyHeroesInRange(E.Range) > 0)
                {
                    E.Cast();
                }
            }
            if(W.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= W.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }
                if (target != null && !target.IsDead)
                {
                    if(target.Health < W.GetDamage(target))
                    {
                        W.Cast();
                    }
                }
            }
        }
        private void OnHarassUpdate()
        {
            AIHeroClient target;
            if (Q.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= Q.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && !target.IsDead && target.InRange(Q.Range))
                {
                    Q.Cast(target);
                }
            }
        }
        private void OnLaneClear()
        {
            if (E.IsReady() && EMenu["EFarm"].GetValue<MenuBool>().Enabled)
            {
                AIMinionClient[] minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.DistanceToPlayer() <= E.Range && !i.IsDead).ToArray();

                if (minions.Length >= 3)
                {
                    if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                    {
                        E.Cast();
                    }
                }
            }
            if(Q.IsReady() && QMenu["QFarm"].GetValue<MenuBool>().Enabled)
            {
                AIMinionClient minion = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= Q.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();

                if(minion != null || !minion.IsDead)
                {
                    if(minion.Health <= Q.GetDamage(minion))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }
        private void OnLastHit()
        {
            if (Q.IsReady() && QMenu["QFarm"].GetValue<MenuBool>().Enabled)
            {
                AIMinionClient minion = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= Q.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();

                if (minion != null || !minion.IsDead)
                {
                    if (minion.Health <= Q.GetDamage(minion))
                    {
                        Q.Cast(minion);
                    }
                }
            }
        }
        private void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsValid)
            {
                return;
            }

            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.W)
                {
                    Orbwalker.SetAttackPauseTime(2000);
                }

                if (args.Slot == SpellSlot.E)
                {
                    Orbwalker.SetAttackPauseTime(1500);
                }
            }
            
            if(sender.IsEnemy && W.IsReady())
            {
                if(sender.Spellbook.IsAutoAttack && args.Target.IsMe && Player.HealthPercent <= 30)
                {
                    W.Cast();
                } else if(args.Target.IsMe && !(sender is AIMinionClient))
                {
                    W.Cast();
                }
            }
        }
        private void OnDraw(EventArgs args)
        {
            if (Player == null || Player.IsDead || Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }

            if (DrawMenu["OnlyReady"].GetValue<MenuBool>().Enabled)
            {
                if (DrawMenu["Q"].GetValue<MenuBool>().Enabled && Q.IsReady())
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, Q.Range, Color.Aqua);
                }

                if (DrawMenu["W"].GetValue<MenuBool>().Enabled && W.IsReady())
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, W.Range, Color.Gold);
                }

                if (DrawMenu["E"].GetValue<MenuBool>().Enabled && E.IsReady())
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, E.Range, Color.Yellow);
                }

                if (DrawMenu["R"].GetValue<MenuBool>().Enabled && R.IsReady())
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, R.Range, Color.Red);
                }
            }
            else
            {
                if (DrawMenu["Q"].GetValue<MenuBool>().Enabled)
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, Q.Range, Color.Aqua);
                }

                if (DrawMenu["W"].GetValue<MenuBool>().Enabled)
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, W.Range, Color.Gold);
                }

                if (DrawMenu["E"].GetValue<MenuBool>().Enabled)
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, E.Range, Color.Yellow);
                }

                if (DrawMenu["R"].GetValue<MenuBool>().Enabled)
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, R.Range, Color.Red);
                }
            }
        }
    }
}
