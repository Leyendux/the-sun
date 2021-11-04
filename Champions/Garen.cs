using System;
using System.Linq;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Garen : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 650f) { Delay = 0f };
            W = new Spell(SpellSlot.W, 0f) { Delay = 0f };
            E = new Spell(SpellSlot.E, 325f) { Delay = 0f };
            R = new Spell(SpellSlot.R, 400f) { Delay = 0.435f };
        }
        protected override void SetupEvents()
        {
            GameEvent.OnGameTick += OnTick;
            AIBaseClient.OnDoCast += OnDoCast;
            Orbwalker.OnAfterAttack += OnAfterAttack;
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
            if(R.IsReady())
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
                    if(target.Health < (Q.GetDamage(target) + E.GetDamage(target) + R.GetDamage(target) + Player.GetAutoAttackDamage(target)))
                    {
                        R.Cast(target);
                    }
                }
            }
            if(Q.IsReady())
            {
                if(Player.CountEnemyHeroesInRange(Q.Range) > 0)
                {
                    Q.Cast();
                }
            }
        }
        private void OnAfterAttack(Object sender, AfterAttackEventArgs args)
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            if(E.IsReady() && !Q.IsReady())
            {
                if(Player.CountEnemyHeroesInRange(E.Range) > 0)
                {
                    E.Cast();
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
                if(args.Slot == SpellSlot.E)
                {
                    Orbwalker.SetAttackPauseTime(3000);
                }
            }

            if (sender.IsEnemy && W.IsReady())
            {
                if (sender.Spellbook.IsAutoAttack && args.Target.IsMe && Player.HealthPercent <= 80)
                {
                    W.Cast();
                }
                else if ((args.Target.IsMe || args.To.DistanceToPlayer() <= E.Range) && !(sender is AIMinionClient))
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
