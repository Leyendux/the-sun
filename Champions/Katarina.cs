using System;
using System.Linq;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Katarina : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 625f) { Delay = 0.25f };
            W = new Spell(SpellSlot.W, 0f) { Delay = 0f };
            E = new Spell(SpellSlot.E, 725f) { Delay = 0f };
            R = new Spell(SpellSlot.R, 550f) { Delay = 0f };
        }
        protected override void SetupEvents()
        {
            GameEvent.OnGameTick += OnTick;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnBuffAdd += OnBuffAdd;
            AIBaseClient.OnBuffRemove += OnBuffRemove;
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

            AIHeroClient target;
            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                && i.DistanceToPlayer() <= 775 && !i.IsDead)
                .OrderBy(i => i.Health).FirstOrDefault();

            if (target != null && !target.IsDead && target.InRange(775))
            {
                if(target.Health <= E.GetDamage(target) + Player.GetAutoAttackDamage(target, true))
                {
                    AIMinionClient dagger = GameObjects.AllyMinions.Where(i => i.Name == "HiddenMinion" && i.Distance(target) <= 775).OrderBy(i => i.Distance(target)).FirstOrDefault();
                    if (dagger != null && !dagger.IsDead)
                    {
                        if (target.Distance(dagger) <= 340)
                        {
                            E.Cast(dagger.Position);
                        }
                        else
                        {
                            E.Cast(target.Position);
                        }
                    }
                    else
                    {
                        E.Cast(target.Position);
                    }
                }
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

                if (target != null && !target.IsDead && target.InRange(R.Range))
                {
                    if(target.Health <= (Player.GetAutoAttackDamage(target, true) + R.GetDamage(target)))
                    {
                        R.Cast();
                    }
                }
            }
            if (E.IsReady())
            {
                if (TargetSelector.SelectedTarget == null)
                {
                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                    && i.DistanceToPlayer() <= 775 && !i.IsDead)
                    .OrderBy(i => i.Health).FirstOrDefault();
                }
                else
                {
                    target = TargetSelector.SelectedTarget;
                }

                if (target != null && !target.IsDead && target.InRange(775))
                {
                    AIMinionClient dagger = GameObjects.AllyMinions.Where(i => i.Name == "HiddenMinion" && i.Distance(target) <= 775).OrderBy(i => i.Distance(target)).FirstOrDefault();
                    if (dagger != null && !dagger.IsDead)
                    {
                        if(target.Distance(dagger) <= 340)
                        {
                            E.Cast(dagger.Position);
                            if(W.IsReady())
                            {
                                W.Cast();
                            }
                        } else
                        {
                            E.Cast(target.Position);
                            if (W.IsReady())
                            {
                                W.Cast();
                            }
                        }
                    } else
                    {
                        E.Cast(target.Position);
                        if (W.IsReady())
                        {
                            W.Cast();
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
        }

        private void OnBuffAdd(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {
            if(sender.IsMe && args.Buff.Name == "katarinarsound")
            {
                Orbwalker.ResetAutoAttackTimer();
                Orbwalker.SetPauseTime(2500);
            }
        }

        private void OnBuffRemove(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name == "katarinarsound")
            {
                Orbwalker.ResetAutoAttackTimer();
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

            AIMinionClient[] minion = GameObjects.AllyMinions.Where(i => i.Name == "HiddenMinion").ToArray();

            foreach(AIMinionClient dagger in minion)
            {
                if(dagger != null && !dagger.IsDead)
                {
                    Render.Circle.DrawCircle(dagger.Position, 135f, Color.Blue);
                }
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
