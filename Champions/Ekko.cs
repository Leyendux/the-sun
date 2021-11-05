using System;
using System.Linq;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Ekko : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1075f) { Delay = 0.25f, Speed = 1200, Width = 60, IsSkillShot = true, Collision = false, Type = SpellType.Line };
            W = new Spell(SpellSlot.W, 1600f) { Delay = 0.25f, Width = 375, IsSkillShot = true, Collision = false, Type = SpellType.Circle };
            E = new Spell(SpellSlot.E, 550f) { Delay = 0f };
            R = new Spell(SpellSlot.R, 375f) { Delay = 0.5f };
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

        AIMinionClient ekkoShadow;
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

            if(R.IsReady())
            {
                ekkoShadow = GameObjects.AllyMinions.Where(i => i.Name == "Ekko").FirstOrDefault();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    OnComboUpdate();
                    break;
            }
        }
        private void OnComboUpdate()
        {
            if(Player.IsDodgingMissiles)
            {
                return;
            }
            AIHeroClient target;
            if(R.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.Distance(ekkoShadow) <= R.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if(target != null && !target.IsDead)
                {
                    if(target.Health <= (Player.GetAutoAttackDamage(target, true) + R.GetDamage(target)) || ekkoShadow.CountEnemyHeroesInRange(R.Range) >= 2)
                    {
                        R.Cast();
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

                if (target != null && !target.IsDead)
                {
                    PredictionOutput output = Q.GetPrediction(target);

                    if (output.Hitchance >= HitChance.High)
                    { 
                        Q.Cast(output.CastPosition);
                        return;
                    }
                }
            }
            if (E.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= E.Range && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && !target.IsDead && target.InRange(E.Range))
                {
                    E.Cast(target.Position);
                }
            }
            if (W.IsReady())
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
                    PredictionOutput output = W.GetPrediction(target);

                    if (output.Hitchance >= HitChance.VeryHigh)
                    {
                        W.Cast(output.CastPosition);
                        return;
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

            if (sender.IsEnemy && R.IsReady())
            {
                if ((args.Target.IsMe || args.To.DistanceToPlayer() <= R.Range) && Player.HealthPercent <= 30 && !(sender is AIMinionClient))
                {
                    R.Cast();
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
                    Render.Circle.DrawCircle(ekkoShadow.Position, R.Range, Color.Red);
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
