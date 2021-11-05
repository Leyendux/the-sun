using System;
using System.Linq;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Leblanc : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 700f) { Delay = 0.25f, Speed = 2000 };
            W = new Spell(SpellSlot.W, 600f) { Delay = 0f, Speed = 1450, Width = 220, IsSkillShot = true, Collision = false, Type = SpellType.Circle };
            E = new Spell(SpellSlot.E, 925f) { Delay = 0.25f, Speed = 1750, Width = 55, IsSkillShot = true, Collision = true, Type = SpellType.Line };
            R = new Spell(SpellSlot.R, 0f);
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

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    OnComboUpdate();
                    break;
            }
        }

        private void OnComboUpdate()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            AIHeroClient target;
            if (R.IsReady())
            {
                if(R.Name == "LeblancRE")
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

                    if (target != null && !target.IsDead)
                    {
                        PredictionOutput output = E.GetPrediction(target);

                        if (output.Hitchance >= HitChance.High)
                        {
                            R.Cast(output.CastPosition);
                            return;
                        }
                    }
                }
                if (R.Name == "LeblancRQ")
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
                        R.Cast(target);
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

                if (target != null && !target.IsDead)
                {
                    PredictionOutput output = E.GetPrediction(target);

                    if (output.Hitchance >= HitChance.High)
                    {
                        E.Cast(output.CastPosition);
                        return;
                    }
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

                    if (output.Hitchance >= HitChance.High)
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
