using System;
using System.Linq;
using System.Drawing;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using the_sun.Core;

namespace the_sun.Champions
{
    public class Orianna : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 825f) { Delay = 0f, Speed = 1400f, Width = 175f, Collision = false, IsSkillShot = true, Type = SpellType.Circle };
            W = new Spell(SpellSlot.W, 225f) { Delay = 0f };
            E = new Spell(SpellSlot.E, 1120f) { Delay = 0f, Speed = 1850f, Width = 160f };
            R = new Spell(SpellSlot.R, 415f) { Delay = 0.5f };
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

            QMenu = MainMenu.Add(new Menu("Q", "[Q] Command: Attack"));
            {
                QMenu.Add(new MenuBool("AutoQ", "Auto Q Harass", true));
                QMenu.Add(new MenuBool("QFarm", "Use Q Farm", true));
                QMenu.Add(new MenuSlider("QMana", "Q Mana Farm & Harass", 30, 0, 100));
            }

            WMenu = MainMenu.Add(new Menu("W", "[W] Command: Dissonance"));
            {
                WMenu.Add(new MenuBool("AutoW", "Auto W Harass", true));
                WMenu.Add(new MenuBool("WFarm", "Use W Farm", true));
                WMenu.Add(new MenuSlider("WMana", "W Mana Farm & Harass", 30, 0, 100));
            }

            RMenu = MainMenu.Add(new Menu("R", "[R] Command: Shockwave"));
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

        AIMinionClient ball;

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

            ball = GameObjects.AllyMinions.Where(i => i.Name == "TheDoomBall").FirstOrDefault();

            if ((QMenu["AutoQ"].GetValue<MenuBool>().Enabled || WMenu["AutoW"].GetValue<MenuBool>().Enabled) && (Orbwalker.ActiveMode != OrbwalkerMode.Combo || Orbwalker.ActiveMode != OrbwalkerMode.Harass) && !Player.IsUnderEnemyTurret())
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
            if(Player.IsDodgingMissiles)
            {
                return;
            }
            AIHeroClient target;
            if(R.IsReady())
            {
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        if (TargetSelector.SelectedTarget == null)
                        {
                            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                            && i.Distance(ally.Position) <= R.Range && !i.IsDead)
                            .OrderBy(i => i.Health).FirstOrDefault();
                        }
                        else
                        {
                            target = TargetSelector.SelectedTarget;
                        }
                        if (ally.InRange(E.Range) && ally.CountEnemyHeroesInRange(R.Range) >= RMenu["Raoe"].GetValue<MenuSlider>().Value)
                        {
                            E.Cast(ally);
                        } else if((target != null && !target.IsDead) && target.HealthPercent <= 60 && ally.InRange(E.Range))
                        {
                            E.Cast(ally);
                        }
                    }
                }
                if(ball != null && !ball.IsDead)
                {
                    if (TargetSelector.SelectedTarget == null)
                    {
                        target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                        && i.Distance(ball) <= R.Range && !i.IsDead)
                        .OrderBy(i => i.Health).FirstOrDefault();
                    }
                    else
                    {
                        target = TargetSelector.SelectedTarget;
                    }
                    if (ball.CountEnemyHeroesInRange(R.Range) >= RMenu["Raoe"].GetValue<MenuSlider>().Value)
                    {
                        R.Cast();
                    }
                    else if ((target != null && !target.IsDead) && target.HealthPercent <= 60)
                    {
                        R.Cast();
                    }
                } else 
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        foreach (BuffInstance buffAlly in ally.Buffs)
                        {
                            if (buffAlly.Name.Contains("orianaghost"))
                            {
                                if (TargetSelector.SelectedTarget == null)
                                {
                                    target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                                    && i.Distance(ally) <= R.Range && !i.IsDead)
                                    .OrderBy(i => i.Health).FirstOrDefault();
                                }
                                else
                                {
                                    target = TargetSelector.SelectedTarget;
                                }
                                if (ally.CountEnemyHeroesInRange(R.Range) >= RMenu["Raoe"].GetValue<MenuSlider>().Value)
                                {
                                    R.Cast();
                                }
                                else if ((target != null && !target.IsDead) && target.HealthPercent <= 60)
                                {
                                    R.Cast();
                                }
                            }
                        }
                    }
                }
            }
            if(Q.IsReady())
            {
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        if (TargetSelector.SelectedTarget == null)
                        {
                            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                            && i.Distance(ally) <= Q.Range && !i.IsDead)
                            .OrderBy(i => i.Health).FirstOrDefault();
                        }
                        else
                        {
                            target = TargetSelector.SelectedTarget;
                        }
                        if (ally.InRange(E.Range) && ally.CountEnemyHeroesInRange(Q.Range) > 0)
                        {
                            E.Cast(ally);
                        }
                    }
                }
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
                    }
                }
            }
            if(W.IsReady())
            {
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        if (TargetSelector.SelectedTarget == null)
                        {
                            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                            && i.Distance(ally) <= W.Range && !i.IsDead)
                            .OrderBy(i => i.Health).FirstOrDefault();
                        }
                        else
                        {
                            target = TargetSelector.SelectedTarget;
                        }
                        if (ally.InRange(E.Range) && ally.CountEnemyHeroesInRange(W.Range) > 0)
                        {
                            E.Cast(ally);
                        }
                    }
                }
                if (ball != null && !ball.IsDead)
                {
                    if (ball.CountEnemyHeroesInRange(W.Range) > 0)
                    {
                        W.Cast();
                    }
                }
                else
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        foreach (BuffInstance buffAlly in ally.Buffs)
                        {
                            if (buffAlly.Name.Contains("orianaghost"))
                            {
                                if (ally.CountEnemyHeroesInRange(W.Range) > 0)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                }
            }
        }
        private void OnHarassUpdate()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            AIHeroClient target;
            if (Q.IsReady() && QMenu["AutoQ"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= QMenu["QMana"].GetValue<MenuSlider>().Value)
            {
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        if (TargetSelector.SelectedTarget == null)
                        {
                            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                            && i.Distance(ally) <= Q.Range && !i.IsDead)
                            .OrderBy(i => i.Health).FirstOrDefault();
                        }
                        else
                        {
                            target = TargetSelector.SelectedTarget;
                        }
                        if (ally.InRange(E.Range) && ally.CountEnemyHeroesInRange(Q.Range) > 0)
                        {
                            E.Cast(ally);
                        }
                    }
                }
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
                    }
                }
            }
            if (W.IsReady() && WMenu["AutoW"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= WMenu["WMana"].GetValue<MenuSlider>().Value)
            {
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        if (TargetSelector.SelectedTarget == null)
                        {
                            target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget()
                            && i.Distance(ally) <= W.Range && !i.IsDead)
                            .OrderBy(i => i.Health).FirstOrDefault();
                        }
                        else
                        {
                            target = TargetSelector.SelectedTarget;
                        }
                        if (ally.InRange(E.Range) && ally.CountEnemyHeroesInRange(W.Range) > 0)
                        {
                            E.Cast(ally);
                        }
                    }
                }
                if (ball != null && !ball.IsDead)
                {
                    if(ball.CountEnemyHeroesInRange(W.Range) > 0)
                    {
                        W.Cast();
                    }
                } else
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        foreach (BuffInstance buffAlly in ally.Buffs)
                        {
                            if (buffAlly.Name.Contains("orianaghost"))
                            {
                                if(ally.CountEnemyHeroesInRange(W.Range) > 0)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                }
            }
        }
        private void OnLaneClear()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            if (Q.IsReady() && QMenu["QFarm"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= QMenu["QMana"].GetValue<MenuSlider>().Value)
            {
                AIMinionClient[] minions;
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes.Where(i => i.InRange(E.Range) && !i.IsDead))
                    {
                       minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.Distance(ally) <= Q.Range && !i.IsDead).ToArray();

                        if (minions.Length >= 3)
                        {
                            if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                            {
                                E.Cast(ally);
                            }
                        }

                        foreach (AIMinionClient m in minions)
                        {
                            if (m.Health < Q.GetDamage(m))
                            {
                                E.Cast(ally);
                            }
                        }
                    }
                }
                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.DistanceToPlayer() <= Q.Range && !i.IsDead).ToArray();

                if (minions.Length >= 3)
                {
                    if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                    {
                        Q.Cast(minions.FirstOrDefault().Position);
                    }
                }

                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                        && i.DistanceToPlayer() <= Q.Range && !i.IsDead).ToArray();

                foreach (AIMinionClient m in minions)
                {
                    if (m.Health < Q.GetDamage(m))
                    {
                        Q.Cast(m.Position);
                    }
                }
            }

            if (W.IsReady() && WMenu["WFarm"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= WMenu["WMana"].GetValue<MenuSlider>().Value)
            {
                AIMinionClient[] minions;
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes.Where(i => i.InRange(E.Range) && !i.IsDead))
                    {
                        minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.Distance(ally) <= W.Range && !i.IsDead).ToArray();

                        if (minions.Length >= 3)
                        {
                            if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                            {
                                E.Cast(ally);
                            }
                        }

                        foreach (AIMinionClient m in minions)
                        {
                            if (m.Health < W.GetDamage(m))
                            {
                                E.Cast(ally);
                            }
                        }
                    }
                }
                if (ball != null && !ball.IsDead)
                {
                    minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.Distance(ball) <= W.Range && !i.IsDead).ToArray();

                    if (minions.Length >= 3)
                    {
                        if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                        {
                            W.Cast(minions.FirstOrDefault().Position);
                        }
                    }

                    minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                            && i.Distance(ball) <= W.Range && !i.IsDead).ToArray();

                    foreach (AIMinionClient m in minions)
                    {
                        if (m.Health < W.GetDamage(m))
                        {
                            W.Cast(m.Position);
                        }
                    }
                } else
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        foreach (BuffInstance buffAlly in ally.Buffs)
                        {
                            if (buffAlly.Name.Contains("orianaghost"))
                            {
                                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                                   && i.Distance(ally) <= W.Range && !i.IsDead).ToArray();

                                if (minions.Length >= 3)
                                {
                                    if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                                    {
                                        W.Cast(minions.FirstOrDefault().Position);
                                    }
                                }

                                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                                        && i.Distance(ally) <= W.Range && !i.IsDead).ToArray();

                                foreach (AIMinionClient m in minions)
                                {
                                    if (m.Health < W.GetDamage(m))
                                    {
                                        W.Cast(m.Position);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void OnLastHit()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            if (Q.IsReady() && QMenu["QFarm"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= QMenu["QMana"].GetValue<MenuSlider>().Value)
            {
                AIMinionClient[] minions;
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes.Where(i => i.InRange(E.Range) && !i.IsDead))
                    {
                        minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                        && i.Distance(ally) <= Q.Range && !i.IsDead).ToArray();

                        foreach (AIMinionClient m in minions)
                        {
                            if (m.Health < Q.GetDamage(m))
                            {
                                E.Cast(ally);
                            }
                        }
                    }
                }
                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                        && i.DistanceToPlayer() <= Q.Range && !i.IsDead).ToArray();

                foreach (AIMinionClient m in minions)
                {
                    if (m.Health < Q.GetDamage(m))
                    {
                        Q.Cast(m.Position);
                    }
                }
            }

            if (W.IsReady() && WMenu["WFarm"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= WMenu["WMana"].GetValue<MenuSlider>().Value)
            {
                AIMinionClient[] minions;
                if (E.IsReady())
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes.Where(i => i.InRange(E.Range) && !i.IsDead))
                    {
                        minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                        && i.Distance(ally) <= W.Range && !i.IsDead).ToArray();

                        foreach (AIMinionClient m in minions)
                        {
                            if (m.Health < W.GetDamage(m))
                            {
                                E.Cast(ally);
                            }
                        }
                    }
                }
                if (ball != null && !ball.IsDead)
                {
                    minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                            && i.Distance(ball) <= W.Range && !i.IsDead).ToArray();

                    foreach (AIMinionClient m in minions)
                    {
                        if (m.Health < W.GetDamage(m))
                        {
                            W.Cast(m.Position);
                        }
                    }
                }
                else
                {
                    foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                    {
                        foreach (BuffInstance buffAlly in ally.Buffs)
                        {
                            if (buffAlly.Name.Contains("orianaghost"))
                            {
                                minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                                        && i.Distance(ally) <= W.Range && !i.IsDead).ToArray();

                                foreach (AIMinionClient m in minions)
                                {
                                    if (m.Health < W.GetDamage(m))
                                    {
                                        W.Cast(m.Position);
                                    }
                                }
                            }
                        }
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

            if (sender.IsEnemy && E.IsReady() && args.Target is AIHeroClient)
            {
                var target = args.Target as AIHeroClient;
                if (sender.Spellbook.IsAutoAttack && args.Target.IsAlly && target.HealthPercent <= 60 && args.Target.InRange(E.Range))
                {
                    E.Cast(target);
                }
                else if ((args.Target.IsAlly || args.To.Distance(target) <= E.Range) && !(sender is AIMinionClient) && args.Target.InRange(E.Range))
                {
                    E.Cast(target);
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
                    if (ball != null && !ball.IsDead)
                    {
                        Render.Circle.DrawCircle(ball.Position, W.Range, Color.Gold);
                    }
                    else
                    {
                        foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                        {
                            foreach (BuffInstance buffAlly in ally.Buffs)
                            {
                                if (buffAlly.Name.Contains("orianaghost"))
                                {
                                    Render.Circle.DrawCircle(ally.Position, W.Range, Color.Gold);
                                }
                            }
                        }
                    }
                }

                if (DrawMenu["E"].GetValue<MenuBool>().Enabled && E.IsReady())
                {
                    Render.Circle.DrawCircle(GameObjects.Player.Position, E.Range, Color.Yellow);
                }

                if (DrawMenu["R"].GetValue<MenuBool>().Enabled && R.IsReady())
                {
                    if (ball != null && !ball.IsDead)
                    {
                        Render.Circle.DrawCircle(ball.Position, R.Range, Color.Red);
                    }
                    else
                    {
                        foreach (AIBaseClient ally in GameObjects.AllyHeroes)
                        {
                            foreach (BuffInstance buffAlly in ally.Buffs)
                            {
                                if (buffAlly.Name.Contains("orianaghost"))
                                {
                                    Render.Circle.DrawCircle(ally.Position, R.Range, Color.Red);
                                }
                            }
                        }
                    }
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
