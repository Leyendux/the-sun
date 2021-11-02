﻿using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using the_sun.Champions;

namespace the_sun
{
    internal class Program
    {
        private static readonly string[] Champions = new[] { "Samira" };

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
            //GameEvent.OnGameTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
            foreach(BuffInstance b in GameObjects.Player.Buffs)
            {
                Console.WriteLine(b.Name);
            }
        }

        private static void OnGameLoad()
        {
            string ChampionName = GameObjects.Player.CharacterName;

            if (!Champions.Contains(ChampionName))
            {
                return;
            }

            switch (ChampionName)
            {
                case "Samira":
                    new Samira();
                    break;
                default:
                    break;
            }
        }

    }
}