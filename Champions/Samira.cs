using System;
using System.Linq;
using System.Drawing;
using the_sun.Core;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

namespace the_sun.Champions
{
    public class Samira : Champion
    {
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 950f) { Delay = 0.25f, Speed = 2600f, Width = 120f, Collision = true, IsSkillShot = true, Type = SpellType.Line };
            W = new Spell(SpellSlot.W, 325f) { Delay = 0.1f };
            E = new Spell(SpellSlot.E, 600f) { Delay = 0f, Speed = 1600f, Width = 300f, IsSkillShot = false, Collision = false };
            R = new Spell(SpellSlot.R, 600f) { Delay = 0f, IsSkillShot = false, Collision = false };
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

            QMenu = MainMenu.Add(new Menu("Q", "[Q] Flair"));
            {
                QMenu.Add(new MenuBool("AutoQ", "Auto Q Harass", true));
                QMenu.Add(new MenuBool("QFarm", "Use Q Farm", true));
                QMenu.Add(new MenuSlider("QMana", "Q Mana Farm & Harass", 30, 0, 100));
            }

            EMenu = MainMenu.Add(new Menu("E", "[E] Wild Rush"));
            {
                EMenu.Add(new MenuBool("Eturret", "E under turret", false));
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

            if(Player.HasBuff("SamiraW"))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            if(Player.HasBuff("SamiraR"))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if(QMenu["AutoQ"].GetValue<MenuBool>().Enabled && (Orbwalker.ActiveMode != OrbwalkerMode.Combo || Orbwalker.ActiveMode != OrbwalkerMode.Harass) && !Player.IsUnderEnemyTurret())
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
        private void OnLastHit()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            if (Q.IsReady() && QMenu["QFarm"].GetValue<MenuBool>().Enabled && Player.ManaPercent >= QMenu["QMana"].GetValue<MenuSlider>().Value)
            {
                AIMinionClient[] minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                        && i.DistanceToPlayer() <= Q.Range && !i.IsDead).ToArray();

                foreach (AIMinionClient m in minions)
                {
                    if (m.Health < Q.GetDamage(m) && (!Player.CanAttack || !m.InAutoAttackRange(Player)))
                    {
                        Q.Cast(m.Position);
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
                Q.Collision = true;
                AIMinionClient[] minions = GameObjects.EnemyMinions.Where(i => i.IsValidTarget()
                       && i.DistanceToPlayer() <= 340 && !i.IsDead).ToArray();

                if (minions.Length >= 3)
                {
                    if (minions.FirstOrDefault() != null || !minions.FirstOrDefault().IsDead)
                    {
                        Q.Collision = false;
                        Q.Cast(minions.FirstOrDefault().Position);
                    }
                }

                Q.Collision = true;
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
        }
        private void OnHarassUpdate()
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            AIHeroClient target;
            Q.Collision = true;
            if (Q.IsReady() && Player.ManaPercent >= QMenu["QMana"].GetValue<MenuSlider>().Value)
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
                    if (target.DistanceToPlayer() <= 340)
                    {
                        Q.Collision = false;
                    }
                    PredictionOutput output = Q.GetPrediction(target);

                    if (output.Hitchance >= HitChance.High)
                    {
                        Q.Cast(output.CastPosition);
                    }
                }
            }
        }
        private void OnComboUpdate()
        {
            if (Player.IsDodgingMissiles || Player.Spellbook.IsAutoAttack)
            {
                return;
            }
            if (R.IsReady())
            {
                if (Player.CountEnemyHeroesInRange(R.Range) > 0)
                {
                    R.Cast();
                }
            }

        }
        private void OnAfterAttack(object sender, AfterAttackEventArgs e)
        {
            if (Player.IsDodgingMissiles)
            {
                return;
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                AIHeroClient target;
                Q.Collision = true;
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
                        if (target.DistanceToPlayer() <= 340)
                        {
                            Q.Collision = false;
                        }
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

                    if (target != null && !target.IsDead)
                    {
                        if(EMenu["Eturret"].GetValue<MenuBool>().Enabled)
                        {
                            E.Cast(target);
                        } else { 
                            if(!target.IsUnderEnemyTurret())
                            {
                                E.Cast(target);
                            }
                        }
                        
                        return;
                    }
                }
                if (W.IsReady())
                {
                    if (Player.CountEnemyHeroesInRange(W.Range) > 0)
                    {
                        W.Cast();
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

            if(sender.IsEnemy && W.IsReady())
            {
                handleSpell(sender, args);
                if (args.Target.IsMe && sender.Spellbook.IsAutoAttack && sender.IsRanged && Player.HealthPercent <= 30)
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

        private void handleSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender is AIMinionClient)
            {
                var owner = sender.Owner as AIHeroClient;
                if (owner == null || !owner.IsValid || (owner.CharacterName != "Shaco" || owner.CharacterName != "Heimerdinger" || owner.CharacterName != "Zyra"))
                {
                    return;
                }
                if(args.Target.IsMe)
                {
                    W.Cast();
                }
            }
            var EnemyName = sender.CharacterName;
            var target = args.Target;
            var slot = args.Slot;
            var to = args.To;
            if(EnemyName == "Shaco" && target.IsMe)
            {
                if(slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Pantheon" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Malzahar" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Kassadin" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Gragas" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ezreal" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ezreal" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Blitzcrank" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Anivia")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Teemo" && target.IsMe)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Zilean" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Veigar")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Urgot")
            {
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "TwistedFate")
            {
                if (sender.Spellbook.IsAutoAttack && sender.HasBuff("GoldCardPreAttack") && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Tristana" && target.IsMe)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Sona" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Singed" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Olaf" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Nidalee" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Morgana" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "MissFortune")
            {
                if (slot == SpellSlot.Q && (target.IsMe || to.DistanceToPlayer() <= W.Range))
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "KogMaw" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Kennen" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Kayle" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Janna")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Heimerdinger" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Gangplank" && target.IsMe)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Galio" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Fiddlesticks")
            {
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "DrMundo")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Corki" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.E || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "ChoGath")
            {
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (sender.Spellbook.IsAutoAttack && sender.HasBuff("VorpalSpikes") && sender.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Swain" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Leblanc")
            {
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Irelia" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Cassiopeia")
            {
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Karma")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "JarvanIV" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Nocturne")
            {
                if (slot == SpellSlot.E && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "LeeSin" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Brand" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Rumble" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Vayne" && target.IsMe)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Leona" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Orianna" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Skarner")
            {
                if (slot == SpellSlot.R && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Xerath" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Graves" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q || slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Shyvana" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Fizz" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Viktor")
            {
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Sejuani")
            {
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (sender.Spellbook.IsAutoAttack && target.IsMe)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ziggs" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Nautilus" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Lulu")
            {
                if (slot == SpellSlot.W && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Varus" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Darius" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Draven" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Draven" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Jayce" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Zyra" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Diana" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Rengar" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Syndra")
            {
                if (slot == SpellSlot.R && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "KhaZix" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Elise" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Zed" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Nami")
            {
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && (target.IsMe || to.DistanceToPlayer() <= W.Range))
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Vi" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Thresh" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Quinn" && target.IsMe)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Zac" && target.IsMe)
            {
                if (slot == SpellSlot.Q || sender.Spellbook.IsAutoAttack)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Lissandra" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Aatrox" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Lucian")
            {
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && (target.IsMe || to.DistanceToPlayer() <= W.Range))
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Jinx" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.W || slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Yasuo" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "VelKoz" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Braum" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Gnar" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Kalista" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Bard" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ekko" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "TahmKench" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Illaoi" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Jhin")
            {
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && (target.IsMe || to.DistanceToPlayer() <= W.Range))
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "AurelionSol" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Taliyah" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Kled" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ivern" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Xayah" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Rakan" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ornn" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Zoe" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "KaiSa" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Pyke" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Neeko" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Sylas" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Yuumi" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Qiyana" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Senna")
            {
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && (target.IsMe || to.DistanceToPlayer() <= W.Range))
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Aphelios" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Lillia" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Yone" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Seraphine" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Rell" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Viego" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Akshan" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Vex" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ashe" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.R || slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Sivir" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Caitlyn")
            {
                if (slot == SpellSlot.R && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.Q && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.E && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Katarina")
            {
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.R && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Akali" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ahri" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E || slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Evelynn" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Talon" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.W)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Annie")
            {
                if (slot == SpellSlot.Q && target.IsMe)
                {
                    W.Cast();
                }
                if (slot == SpellSlot.W && to.DistanceToPlayer() <= W.Range)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Amumu" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Lux" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Ryze" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Samira" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.Q || slot == SpellSlot.R)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Vladimir" && to.DistanceToPlayer() <= W.Range)
            {
                if (slot == SpellSlot.E)
                {
                    W.Cast();
                }
            }
            if (EnemyName == "Malphite" && target.IsMe)
            {
                if (slot == SpellSlot.Q)
                {
                    W.Cast();
                }
            }
        }
    }
}
