using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace MoreBroadcast
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "MoreBroadcast";
        public override string Author => "Konoara";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 12, 4);

        private CoroutineHandle _broadcastCoroutine;
        private readonly HashSet<string> KnownPlayers = new HashSet<string>();

        private string DataFile
        {
            get { return Path.Combine(Paths.Configs, "players.txt"); }
        }

        public override void OnEnabled()
        {
            LoadKnownPlayers();
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            _broadcastCoroutine = Timing.RunCoroutine(BroadcastLoop());
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Verified -= OnVerified;

            if (_broadcastCoroutine.IsRunning)
                Timing.KillCoroutines(_broadcastCoroutine);

            SaveKnownPlayers();
            base.OnDisabled();
        }

        private IEnumerator<float> BroadcastLoop()
        {
            while (true)
            {
                float wait = UnityEngine.Random.Range(Config.MinBroadcastDelay, Config.MaxBroadcastDelay);
                yield return Timing.WaitForSeconds(wait);

                Map.Broadcast(
                    Config.BroadcastDuration,
                    Config.BroadcastMessage,
                    Broadcast.BroadcastFlags.Normal,
                    true
                );
            }
        }

        private void OnVerified(VerifiedEventArgs ev)
        {
            Player player = ev.Player;
            string userId = player.UserId;

            if (!KnownPlayers.Contains(userId))
            {
                KnownPlayers.Add(userId);
                SaveKnownPlayers();

                int total = KnownPlayers.Count;

                string msg = Config.WelcomeMessage
                    .Replace("{player}", player.Nickname)
                    .Replace("{player_count}", total.ToString());

                Map.Broadcast(Config.WelcomeDuration, msg, Broadcast.BroadcastFlags.Normal, true);
            }
        }

        private void LoadKnownPlayers()
        {
            if (File.Exists(DataFile))
            {
                foreach (string line in File.ReadAllLines(DataFile))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        KnownPlayers.Add(line.Trim());
                }
            }
        }

        private void SaveKnownPlayers()
        {
            File.WriteAllLines(DataFile, KnownPlayers.ToArray());
        }
    }

    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        public bool Debug { get; set; } = false;

        public string BroadcastMessage { get; set; } =
            "<color=#00FFAA><b>Welcome to our server!</b></color>\n" +
            "<color=#FFFFFF>Join our community and have fun!</color>";

        public ushort BroadcastDuration { get; set; } = 10;

        public float MinBroadcastDelay { get; set; } = 300f;

        public float MaxBroadcastDelay { get; set; } = 1200f;

        public string WelcomeMessage { get; set; } =
            "<color=#00FFAA><b>Welcome {player}!</b></color>\n" +
            "<color=#FFFFFF>You are the <b>{player_count}ᵗʰ</b> unique player to join our server!</color>";

        public ushort WelcomeDuration { get; set; } = 12;
    }
}
